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
        // World-space hand, or null when the hand isn't tracked.
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
            return HandJointMap.Convert(side, joints, toWorld, Confidence);
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

        public event Action<PalmfolkRunner> Opened;
        public event Action<IReadOnlyList<GameEvent>> Ticked;
        public event Action<PalmfolkRunner> Presented;

        private void Awake()
        {
            if (Hands == null) Hands = new XRHandsReader { TrackingSpace = trackingSpace };
        }

        private void Start()
        {
            if (openOnStart && Game == null)
                Open(Content(), new DirectorySaveStorage(System.IO.Path.Combine(Application.persistentDataPath, saveFolder)));
        }

        // Opens the newest save in `storage` (or a fresh valley). VR mode resumes at the saved pose; Table mode plants
        // first (finding the spatial anchor is D9), and the saved bridges appear on the new valley.
        public void Open(ValleySpec valley, ISaveStorage storage, GameSettings settings = null)
        {
            if (valley == null) throw new ArgumentNullException(nameof(valley));
            repository = storage == null ? null : new SaveRepository(storage);
            SaveData save = null;
            LoadStatus = repository == null ? SaveLoadStatus.Missing : repository.Load(out save);
            if (save == null) save = new SaveData();
            var pose = save.Mode == ValleyMode.VR ? save.Pose : null;
            Open(valley, save, pose, settings);
        }

        public void Open(ValleySpec valley, SaveData save, ValleyPose pose, GameSettings settings = null)
        {
            OpenedFrom = save ?? new SaveData(); OpenedPose = pose; BaseSettings = (settings ?? new GameSettings()).Copy();
            Player = (OpenedFrom.Settings ?? new PlayerSettings()).Copy();
            Game = PalmfolkGame.Open(valley, OpenedFrom, pose, BaseSettings);
            StepTarget = Game; Clock = new FixedTickRunner(); Source.ClearCommands();
            Place();
            Opened?.Invoke(this);
        }

        public bool Request(InputCommand command) => Source.Request(command);

        public void Apply(PlayerSettings player)
        {
            if (Game == null) throw new InvalidOperationException("Open a game first.");
            Game.Apply(player); Player = player.Copy(); Place(); SaveNow();
        }

        public SaveData Capture() => Game?.Capture(Player, OpenedFrom);

        public void SaveNow()
        {
            if (repository == null || Game == null) return;
            repository.Save(Capture()); Saves++;
        }

        private void Update()
        {
            if (Game == null) return;
            Frame(Time.unscaledDeltaTime, Time.realtimeSinceStartupAsDouble);
        }

        // One rendered frame: sample, tick, place, present. Public so tests can drive it without the player loop.
        public int Frame(double renderedSeconds, double realtimeSeconds)
        {
            if (Game == null) throw new InvalidOperationException("Open a game first.");
            var hands = Hands;
            Source.Sample(hands?.Read(HandSide.Left), hands?.Read(HandSide.Right), (long)(realtimeSeconds * 1e6), focused, hands != null && hands.ControllersInHand);
            int ticks = Clock.Advance(Math.Min(Math.Max(0, renderedSeconds), MaximumFrameSeconds), Source, StepTarget ?? Game);
            bool save = false;
            foreach (var e in Clock.Events)
                save |= e.Kind == GameEventKind.Planted || e.Kind == GameEventKind.BridgeBaked || e.Kind == GameEventKind.PuzzleCompleted || e.Kind == GameEventKind.LedgeWidened;
            Place();
            if (Clock.Events.Count > 0) Ticked?.Invoke(Clock.Events);
            Presented?.Invoke(this);
            if (save) SaveNow();
            return ticks;
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
