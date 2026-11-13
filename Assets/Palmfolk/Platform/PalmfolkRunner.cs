using System;
using System.Collections.Generic;
using Palmfolk.Core;
using Palmfolk.Core.Content;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

namespace Palmfolk.Platform
{
    // Where hands come from: XR Hands on the headset, a script in tests.
    public interface IHandReader
    {
        // World-space hand, or null when the hand isn't tracked. It may be refilled by the next Read (D11): the runner
        // reads it within the frame, and the developer recorder keeps copies.
        HandSample Read(HandSide side);
        bool ControllersInHand { get; }
    }

    // Reads XR Hands (OpenXR XR_EXT_hand_tracking) and converts with HandJointMap. `TrackingSpace` is the transform
    // the XR origin's poses are relative to (OVRCameraRig's TrackingSpace); null means world space.
    public sealed class XRHandsReader : IHandReader
    {
        private static readonly List<XRHandSubsystem> subsystems = new List<XRHandSubsystem>();
        private static readonly List<InputDevice> devices = new List<InputDevice>();
        private readonly RawJoint[] joints = new RawJoint[HandJointMap.JointCount];
        // D11: each hand is converted into the same buffer every frame (Read's sample changes on the next Read).
        private readonly HandSampleBuffer left = new HandSampleBuffer(HandSide.Left), right = new HandSampleBuffer(HandSide.Right);
        private XRHandSubsystem subsystem;
        public Transform TrackingSpace { get; set; }
        public ConfidencePolicy Confidence { get; } = new ConfidencePolicy();
        public bool HasSubsystem => Find() != null;

        private XRHandSubsystem Find()
        {
            if (subsystem != null && subsystem.running) return subsystem;
            subsystem = null; SubsystemManager.GetSubsystems(subsystems);
            foreach (var s in subsystems) if (s.running) { subsystem = s; break; }
            return subsystem;
        }

        public HandSample Read(HandSide side)
        {
            var s = Find(); if (s == null) return null;
            var hand = side == HandSide.Left ? s.leftHand : s.rightHand;
            if (!HandJointMap.Read(hand, joints)) return null;
            var toWorld = TrackingSpace != null ? TrackingSpace.localToWorldMatrix : Matrix4x4.identity;
            return HandJointMap.Convert(joints, toWorld, Confidence, side == HandSide.Left ? left : right);
        }

        public bool ControllersInHand
        {
            get
            {
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand, devices);
                foreach (var d in devices) if (d.isValid && d.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked) return true;
                return false;
            }
        }
    }

    // The live game (D6): samples hands once per rendered frame, runs Core at 60 Hz with FixedTickRunner, places the
    // valley root at the planted pose, and saves. Views bind to `Ticked` and `Presented`; the waystone calls
    // `Request` and `Apply`. Nothing here decides gameplay.
    public sealed class PalmfolkRunner : MonoBehaviour
    {
        // A rendered frame longer than this (a hitch, the app suspended) is cut to it: the game never fast-forwards
        // to catch up with time it didn't see.
        public const double MaximumFrameSeconds = 0.1;

        [SerializeField] private Transform trackingSpace;
        [SerializeField] private Transform valleyRoot;
        [SerializeField] private bool openOnStart = true;
        [SerializeField] private string saveFolder = "saves";

        private SaveRepository repository;
        private bool focused = true;
        // D9: a launch waiting for its anchor opens the game once the flow decides. `generation` drops callbacks
        // from an earlier Open.
        private ValleySpec waitingValley; private GameSettings waitingSettings; private bool opening;
        private int generation;

        public PalmfolkGame Game { get; private set; }
        public LiveFrameSource Source { get; } = new LiveFrameSource();
        public FixedTickRunner Clock { get; private set; } = new FixedTickRunner();
        public IHandReader Hands { get; set; }
        // What the clock steps: the game, or a developer recorder wrapping it. Reset to the game on Open.
        public ITickSimulation StepTarget { get; set; }
        public SaveData OpenedFrom { get; private set; }
        public ValleyPose OpenedPose { get; private set; }
        public GameSettings BaseSettings { get; private set; }
        public PlayerSettings Player { get; private set; } = new PlayerSettings();
        public SaveLoadStatus LoadStatus { get; private set; } = SaveLoadStatus.Missing;
        public double InterpolationAlpha => Clock.InterpolationAlpha;
        public Transform ValleyRoot { get => valleyRoot; set => valleyRoot = value; }
        public Transform TrackingSpace { get => trackingSpace; set { trackingSpace = value; if (Hands is XRHandsReader x) x.TrackingSpace = value; } }
        public Func<ValleySpec> Content { get; set; } = ChapterOne.Greybox;
        public int Saves { get; private set; }
        // D9: set before Start (MetaTableMode does it in Awake). Without passthrough, Table mode runs as VR (MR5);
        // without anchors, Table mode plants on every launch.
        public ISpatialAnchors Anchors { get; set; }
        public IPassthrough Passthrough { get; set; }
        // Which mode runs and the anchor search. While `Flow.Search` is Finding, `Game` is null (up to 5 s); when
        // it is Lost the game opens in planting and Pip waves to replant.
        public ModeFlow Flow { get; private set; }
        public bool WaitingForAnchor => Game == null && waitingValley != null;
        // D10 (GZ1 to GZ3). Gaze never reaches the game: it raises need signs and chooses waystone buttons only, so
        // with gaze off (GazeEnabled false, or no reader) every puzzle plays the same. The waystone gives its
        // buttons through GazeButtons and gets look-and-pinch choices through GazeSelected.
        public IGazeReader Gaze { get; set; }
        public bool GazeEnabled { get; set; } = true;
        public LookWatcher Looks { get; } = new LookWatcher();
        public LookAndPinch Pointer { get; } = new LookAndPinch();
        public Func<IReadOnlyList<GazeButton>> GazeButtons { get; set; }
        public GazeSample LastGaze { get; private set; }
        public GazeSource GazeSource => LastGaze?.Source ?? GazeSource.None;

        public event Action<PalmfolkRunner> Opened;
        // The running mode changed (fallback, or the waystone's choice).
        public event Action<PalmfolkRunner> ModeChanged;
        // The waystone's player settings were applied to the game (Player holds them).
        public event Action<PalmfolkRunner> SettingsApplied;
        // GZ1: the sign held up (null: put down). GZ2: a waystone button chosen by look and pinch.
        public event Action<GazeTarget> NeedSignChanged;
        public event Action<string> GazeSelected;
        public event Action<IReadOnlyList<GameEvent>> Ticked;
        public event Action<PalmfolkRunner> Presented;

        private void Awake()
        {
            if (Hands == null) Hands = new XRHandsReader { TrackingSpace = trackingSpace };
        }

        private void Start()
        {
            if (openOnStart && Game == null)
                Open(Content(), new DirectorySaveStorage(System.IO.Path.Combine(Application.persistentDataPath, saveFolder)), LiveSettings());
        }

        // Quest 2 headset finding (Nov 13): a 2 cm connection and a 4 cm break were too tight to find by feel, and
        // carrying Pip read as busywork. Live play opens on building, connects within 4 cm, breaks past 8 cm, and
        // sends gusts at crossing travelers for the hand to wall off.
        public static GameSettings LiveSettings() => new GameSettings
        {
            SkipBoard = true, Gusts = true, ConnectDistance = 0.04, BreakDistance = 0.08, ConnectTicks = 6, StillnessSpeed = 0.045
        };

        // Opens the newest save in `storage` (or a fresh valley). D9: VR mode (or Table mode without passthrough)
        // resumes at the saved pose; Table mode looks for the saved anchor for up to 5 s and opens there, or plants.
        // The saved bridges appear on the valley either way.
        public void Open(ValleySpec valley, ISaveStorage storage, GameSettings settings = null)
        {
            if (valley == null) throw new ArgumentNullException(nameof(valley));
            repository = storage == null ? null : new SaveRepository(storage);
            SaveData save = null;
            LoadStatus = repository == null ? SaveLoadStatus.Missing : repository.Load(out save);
            if (save == null) save = new SaveData();
            int g = ++generation; Game = null; OpenedFrom = save;
            Flow = new ModeFlow(save, Passthrough != null && Passthrough.Available);
            Passthrough?.SetVisible(Flow.Passthrough);
            waitingValley = valley; waitingSettings = settings;
            if (!Flow.Decided)
            {
                if (Anchors == null) Flow.AnchorMissing();
                else Anchors.Find(save.AnchorId, (found, position, forward) =>
                {
                    if (g != generation || !WaitingForAnchor) return;
                    if (found) Flow.AnchorFound(position, forward); else Flow.AnchorMissing();
                    if (Flow.Decided) OpenDecided();
                });
            }
            if (Flow.Decided && WaitingForAnchor) OpenDecided();
        }

        private void OpenDecided()
        {
            var valley = waitingValley; waitingValley = null;
            opening = true;
            try { Open(valley, OpenedFrom, Flow.StartPose, waitingSettings); } finally { opening = false; }
        }

        // Opens a save at a known pose (null: plant). Called directly, the save's mode runs as saved, without
        // looking for an anchor.
        public void Open(ValleySpec valley, SaveData save, ValleyPose pose, GameSettings settings = null)
        {
            if (!opening)
            {
                generation++; waitingValley = null;
                Flow = new ModeFlow(save == null ? null : new SaveData { Mode = save.Mode, AnchorId = null, Pose = pose }, Passthrough != null && Passthrough.Available);
                if (save?.AnchorId != null) Flow.Anchored(save.AnchorId);
                Passthrough?.SetVisible(Flow.Passthrough);
            }
            OpenedFrom = save ?? new SaveData(); OpenedPose = pose; BaseSettings = (settings ?? new GameSettings()).Copy();
            Player = (OpenedFrom.Settings ?? new PlayerSettings()).Copy();
            Game = PalmfolkGame.Open(valley, OpenedFrom, pose, BaseSettings);
            StepTarget = Game; Clock = new FixedTickRunner(); Source.ClearCommands();
            Place();
            Opened?.Invoke(this);
        }

        // The waystone's mode choice (MR5). Switching into Table mode anchors the valley where it stands.
        public void ChooseMode(ValleyMode mode)
        {
            if (Flow == null) throw new InvalidOperationException("Open a game first.");
            bool changed = Flow.Choose(mode, Passthrough != null && Passthrough.Available);
            Passthrough?.SetVisible(Flow.Passthrough);
            if (changed && Flow.Running == ValleyMode.Table) AnchorHere();
            if (changed) ModeChanged?.Invoke(this);
            SaveNow();
        }

        // D9: a new anchor where the valley now stands; the old one is erased. Saved once the anchor is.
        private void AnchorHere()
        {
            if (Game?.Pose == null || Flow == null || Flow.Running != ValleyMode.Table) return;
            var old = Flow.Replace();
            if (old != null) Anchors?.Erase(old);
            if (Anchors == null) return;
            int g = generation; var pose = Game.Pose;
            Anchors.Save(pose.Origin, pose.Forward, id =>
            {
                if (id == null) return;
                bool current = g == generation && Game?.Pose != null && Game.Pose.Origin.Equals(pose.Origin) && Game.Pose.Forward.Equals(pose.Forward) &&
                    Flow.Running == ValleyMode.Table && Flow.AnchorId == null;
                if (!current) { Anchors.Erase(id); return; }
                Flow.Anchored(id); SaveNow();
            });
        }

        public bool Request(InputCommand command) => Source.Request(command);

        public void Apply(PlayerSettings player)
        {
            if (Game == null) throw new InvalidOperationException("Open a game first.");
            Game.Apply(player); Player = player.Copy(); Place(); SettingsApplied?.Invoke(this); SaveNow();
        }

        public SaveData Capture()
        {
            var save = Game?.Capture(Player, OpenedFrom);
            return save == null || Flow == null ? save : Flow.Into(save);
        }

        public void SaveNow()
        {
            if (repository == null || Game == null) return;
            repository.Save(Capture()); Saves++;
        }

        private void Update()
        {
            if (Game == null && !WaitingForAnchor) return;
            Frame(Time.unscaledDeltaTime, Time.realtimeSinceStartupAsDouble);
        }

        // One rendered frame: sample, tick, place, present. Public so tests can drive it without the player loop.
        public int Frame(double renderedSeconds, double realtimeSeconds)
        {
            if (Game == null && WaitingForAnchor)
            {
                Flow.Wait(Math.Min(Math.Max(0, renderedSeconds), MaximumFrameSeconds * 10));
                if (Flow.Decided) OpenDecided();
                if (Game == null) return 0;
            }
            if (Game == null) throw new InvalidOperationException("Open a game first.");
            // Passthrough stopped (failed, or withdrawn): VR carries on with the valley where it is.
            if (Flow != null && Flow.Running == ValleyMode.Table && Passthrough != null && !Passthrough.Available)
            { Flow.PassthroughLost(); Passthrough.SetVisible(false); ModeChanged?.Invoke(this); }
            var hands = Hands;
            Source.Sample(hands?.Read(HandSide.Left), hands?.Read(HandSide.Right), (long)(realtimeSeconds * 1e6), focused, hands != null && hands.ControllersInHand);
            int ticks = Clock.Advance(Math.Min(Math.Max(0, renderedSeconds), MaximumFrameSeconds), Source, StepTarget ?? Game);
            bool save = false; var raised = Clock.Events;
            for (int i = 0; i < raised.Count; i++)
            {
                var kind = raised[i].Kind;
                save |= kind == GameEventKind.Planted || kind == GameEventKind.BridgeBaked || kind == GameEventKind.PuzzleCompleted || kind == GameEventKind.LedgeWidened ||
                    kind == GameEventKind.VisitorCalled || kind == GameEventKind.VisitorSettled;
            }
            for (int i = 0; i < raised.Count; i++) if (raised[i].Kind == GameEventKind.Planted) AnchorHere();
            Place();
            Look(Math.Min(Math.Max(0, renderedSeconds), MaximumFrameSeconds));
            if (Clock.Events.Count > 0) Ticked?.Invoke(Clock.Events);
            Presented?.Invoke(this);
            if (save) SaveNow();
            return ticks;
        }

        private readonly List<GazeTarget> targets = new List<GazeTarget>();
        private void Look(double seconds)
        {
            var gaze = GazeEnabled ? Gaze?.Read() : null; LastGaze = gaze;
            var valley = Game.Valley;
            if (GazeEnabled && valley != null && Game.Stage == ValleyStage.Playing && !Game.IsPaused) NeedSigns.Targets(valley, Game.Pose, targets);
            else targets.Clear();
            if (Looks.Step(seconds, gaze, targets)) NeedSignChanged?.Invoke(Looks.Shown);
            var chosen = Pointer.Step(gaze, Source.Left, Source.Right, GazeEnabled ? GazeButtons?.Invoke() : null);
            if (chosen != null) GazeSelected?.Invoke(chosen);
        }

        // The valley root follows the planted pose (valley origin, facing and scale). Before the first planting it
        // stays where it is; views check `Game.Pose`.
        private void Place()
        {
            if (valleyRoot != null && Game?.Pose != null) Apply(valleyRoot, Game.Pose);
        }

        public static void Apply(Transform root, ValleyPose pose)
        {
            root.SetPositionAndRotation(new Vector3((float)pose.Origin.X, (float)pose.Origin.Y, (float)pose.Origin.Z),
                Quaternion.Euler(0, (float)(pose.YawRadians * 180 / Math.PI), 0));
            root.localScale = Vector3.one * (float)pose.Scale;
        }

        private void OnApplicationFocus(bool hasFocus) => focused = hasFocus;
        private void OnApplicationPause(bool pausing) { focused = !pausing; if (pausing) SaveNow(); }
        private void OnApplicationQuit() => SaveNow();
    }
}
