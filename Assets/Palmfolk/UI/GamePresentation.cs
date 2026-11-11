#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using Palmfolk.Audio;
using Palmfolk.Core;
using Palmfolk.Platform;
using Palmfolk.Views;
using UnityEngine;

namespace Palmfolk.UI
{
    // One live composition. Keep this root unscaled and outside Runner.ValleyRoot: views apply Pose themselves.
    public sealed class GamePresentation : MonoBehaviour
    {
        [SerializeField] private PalmfolkRunner runner;
        [SerializeField] private Transform head;
        private ValleyView valley;
        private LivingValleyView living;
        private StartupView startup;
        private XRayView xray;
        private NeedSignView need;
        private AnchorSearchView search;
        private AudioPresenter audioView;
        private NightMusic music;
        private ContactAudioBinding contacts;
        private bool placedInvitation;
        public NightView Night { get; private set; }
        public WaystoneView Waystone { get; private set; }
        public PalmfolkRunner Runner => runner;
        public void Configure(PalmfolkRunner source, Transform viewer)
        { Detach(); runner = source; head = viewer; Ensure(); Attach(); if (runner?.Game != null) Opened(runner); }
        private void OnEnable() { if (runner != null) { Ensure(); Attach(); if (runner.Game != null) Opened(runner); } }
        private void OnDisable() { Detach(); }
        private void Attach()
        {
            if (runner == null) return; Detach();
            runner.Opened += Opened; runner.Ticked += Consume; runner.Presented += Present;
            GetComponentInChildren<WaystoneBinding>().Configure(runner, Waystone, head);
            GetComponentInChildren<WaystonePokes>().Configure(runner);
        }
        private void Detach()
        {
            if (runner != null) { runner.Opened -= Opened; runner.Ticked -= Consume; runner.Presented -= Present; }
            var binding = GetComponentInChildren<WaystoneBinding>(); if (binding != null) binding.Configure(null, null);
            var pokes = GetComponentInChildren<WaystonePokes>(); if (pokes != null) pokes.Configure(null);
        }
        private T Child<T>(string label) where T : Component
        { var child = new GameObject(label); child.transform.SetParent(transform, false); return child.AddComponent<T>(); }
        private void Ensure()
        {
            if (valley != null) return;
            valley = Child<ValleyView>("Valley view"); living = Child<LivingValleyView>("Visitor views");
            startup = Child<StartupView>("Startup view"); search = Child<AnchorSearchView>("Anchor search view");
            Night = Child<NightView>("Night view"); xray = Child<XRayView>("X-ray view"); need = Child<NeedSignView>("Need signs");
            audioView = Child<AudioPresenter>("Game cues and captions"); music = Child<NightMusic>("Valley music"); contacts = gameObject.AddComponent<ContactAudioBinding>();
            Waystone = Child<WaystoneView>("Waystone"); Waystone.gameObject.AddComponent<WaystoneBinding>();
            Child<WaystonePokes>("Ten finger poke input");
            contacts.Configure(audioView, transform);
        }
        private void Opened(PalmfolkRunner source)
        {
            if (source.Game?.Valley == null) return;
            Night.Configure(source.Game.Valley.Valley.Island(source.Game.Valley.Valley.Sanctuary).Top);
            Present(source);
        }
        private void Consume(IReadOnlyList<GameEvent> events)
        {
            if (runner.Game?.Valley == null) return;
            valley.Consume(events); startup.Consume(events);
            audioView.Consume(events, new ValleyPresentation(runner.Game, 1));
        }
        public void Present(PalmfolkRunner source)
        {
            Ensure(); PlaceInvitation();
            search.Show(source.WaitingForAnchor, source.Player.ReducedMotion, source.Game?.Tick ?? (long)(Time.unscaledTime * 60));
            bool playing = source.Game?.Valley != null;
            valley.gameObject.SetActive(playing); living.gameObject.SetActive(playing); Night.gameObject.SetActive(playing); xray.gameObject.SetActive(playing); music.gameObject.SetActive(playing); need.gameObject.SetActive(playing);
            if (!playing) { Night.TableMode = false; startup.gameObject.SetActive(false); Waystone.gameObject.SetActive(false); GetComponentInChildren<WaystonePokes>().Refresh(source); return; }
            startup.gameObject.SetActive(true); Waystone.gameObject.SetActive(true);
            var settings = source.Player; bool table = source.Flow?.Running == ValleyMode.Table;
            valley.Configure(settings.Apply(source.BaseSettings)); valley.ReducedMotion = living.ReducedMotion = startup.ReducedMotion = Night.ReducedMotion = settings.ReducedMotion;
            valley.HighContrast = living.HighContrast = settings.HighContrast; valley.TableMode = Night.TableMode = table;
            audioView.Captions = music.Captions = settings.Captions; xray.SetVisible(settings.XRay);
            var frame = new ValleyPresentation(source.Game, source.InterpolationAlpha);
            valley.Present(frame, (float)source.InterpolationAlpha); living.Present(frame); startup.Present(frame); Night.Present(frame); xray.Present(frame); music.Present(frame);
            need.Show(source.Looks.Shown, head, settings.ReducedMotion, frame.Tick);
            if (frame.Pose != null) Waystone.transform.position = PalmfolkView.V(frame.Pose.ToWorld(new Vec3(0, 0.05, -0.22)));
            if (head != null) Waystone.LayoutForViewer(head.position);
            contacts.RefreshBindings();
        }
        private void PlaceInvitation()
        {
            if (placedInvitation || head == null) return; placedInvitation = true;
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized; if (forward.sqrMagnitude < 0.1f) forward = Vector3.forward;
            var position = head.position + forward * 0.4f + Vector3.down * 0.15f;
            var rotation = Quaternion.LookRotation(forward);
            startup.transform.SetPositionAndRotation(position, rotation); search.transform.SetPositionAndRotation(position, rotation); Waystone.transform.SetPositionAndRotation(position + Vector3.down * 0.12f, rotation);
        }
        private void Update() { if (runner != null && runner.WaitingForAnchor) Present(runner); }
    }
}
#endif
