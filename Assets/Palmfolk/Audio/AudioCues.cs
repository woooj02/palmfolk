#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using Palmfolk.Core;
using UnityEngine;

namespace Palmfolk.Audio
{
    public enum Cue { Footstep, Hammer, RopeCreak, Connection, BuildDone, Refusal, VisitorArrival, Lantern }
    public static class CueCatalog
    {
        private static readonly string[] captions = { "Footsteps", "Hammer taps", "Rope creaks", "Connection locked", "Bridge finished", "Route refused", "A visitor arrives", "Lantern chime" };
        public static string Caption(Cue cue) => captions[(int)cue];
        public static bool TryMap(GameEventKind kind, out Cue cue)
        {
            switch (kind)
            {
                case GameEventKind.SectionCommitted: cue = Cue.Hammer; return true;
                case GameEventKind.LedgeWidening: cue = Cue.Hammer; return true;
                case GameEventKind.BridgeBaked: cue = Cue.BuildDone; return true;
                case GameEventKind.Refused: cue = Cue.Refusal; return true;
                case GameEventKind.BramFinished: cue = Cue.Lantern; return true;
                case GameEventKind.VisitorArrived: cue = Cue.VisitorArrival; return true;
                case GameEventKind.VisitorSettled: cue = Cue.Lantern; return true;
                default: cue = default; return false;
            }
        }
        // Original synthesized placeholders: no imported samples or external assets.
        public static float[] Samples(Cue cue, int rate = 22050)
        {
            if (!Enum.IsDefined(typeof(Cue), cue) || rate < 8000 || rate > 96000) throw new ArgumentOutOfRangeException();
            double duration = cue == Cue.BuildDone ? 0.42 : cue == Cue.Footstep ? 0.065 : 0.22;
            double[] frequencies = { 170, 480, 90, 880, 660, 220, 1046.5, 784 };
            var samples = new float[(int)(duration * rate)];
            uint noise = 37;
            for (int i = 0; i < samples.Length; i++)
            {
                double t = i / (double)rate, u = i / (double)(samples.Length - 1);
                double envelope = Math.Sin(Math.PI * u) * Math.Exp(-3 * u);
                double frequency = frequencies[(int)cue];
                if (cue == Cue.Refusal) frequency *= 1 - u * 0.4;
                if (cue == Cue.VisitorArrival) frequency *= 1 + u * 0.2;
                double wave = Math.Sin(2 * Math.PI * frequency * t);
                if (cue == Cue.BuildDone) wave = (wave + Math.Sin(2 * Math.PI * frequency * 1.5 * t)) / 2;
                if (cue == Cue.Hammer || cue == Cue.Footstep || cue == Cue.RopeCreak)
                { noise = unchecked(noise * 1664525u + 1013904223u); wave = wave * 0.7 + (noise / (double)uint.MaxValue * 2 - 1) * 0.3; }
                samples[i] = (float)(wave * envelope * 0.35);
            }
            return samples;
        }
    }

    public sealed class AudioPresenter : MonoBehaviour
    {
        private sealed class Voice { public AudioSource Source; }
        private sealed class CaptionState { public TextMesh Text; public float Until; }
        private readonly List<Voice> voices = new List<Voice>();
        private readonly Dictionary<Cue, AudioClip> clips = new Dictionary<Cue, AudioClip>();
        private readonly Dictionary<Cue, CaptionState> captions = new Dictionary<Cue, CaptionState>();
        public bool Captions { get; set; } = true;
        public float Volume { get; set; } = 0.5f;
        public void Consume(IReadOnlyList<GameEvent> events, WorldPresentation frame)
            => Consume(events, frame?.Pose, frame == null ? Vec3.Zero : frame.Pip);
        public void Consume(IReadOnlyList<GameEvent> events, ValleyPresentation frame)
        {
            if (frame?.Pose == null) return;
            foreach (var e in events)
                if (CueCatalog.TryMap(e.Kind, out var cue))
                {
                    var position = frame.Pip;
                    if (e.Kind == GameEventKind.VisitorArrived)
                        position = frame.VisitorFigure?.Position ?? frame.Valley.Island(frame.Visitor?.From ?? frame.Valley.Sanctuary).Top;
                    if (e.Kind == GameEventKind.VisitorSettled || e.Kind == GameEventKind.BramFinished) position = frame.Valley.Island(frame.Valley.Sanctuary).Top;
                    Play(cue, frame.Pose.ToWorld(position));
                }
        }
        private void Consume(IReadOnlyList<GameEvent> events, ValleyPose pose, Vec3 pip)
        {
            if (pose == null) return;
            foreach (var e in events)
                if (CueCatalog.TryMap(e.Kind, out var cue)) Play(cue, pose.ToWorld(pip));
        }
        // Call on actual foot contact; never synthesize step timing from an arbitrary wall clock.
        public void FootContact(Vec3 worldPosition) { Play(Cue.Footstep, worldPosition); }
        public void Play(Cue cue, Vec3 worldPosition)
        {
            if (!clips.TryGetValue(cue, out var clip))
            {
                var samples = CueCatalog.Samples(cue);
                clip = AudioClip.Create("Original " + cue, samples.Length, 1, 22050, false); clip.SetData(samples, 0); clips.Add(cue, clip);
            }
            Voice voice = voices.Find(v => !v.Source.isPlaying);
            if (voice == null)
            {
                var emitter = new GameObject("Cue and caption"); emitter.transform.SetParent(transform, false);
                var source = emitter.AddComponent<AudioSource>(); source.spatialBlend = 1; source.playOnAwake = false;
                source.minDistance = 0.05f; source.maxDistance = 2;
                voice = new Voice { Source = source }; voices.Add(voice);
            }
            voice.Source.transform.position = new Vector3((float)worldPosition.X, (float)worldPosition.Y, (float)worldPosition.Z);
            voice.Source.volume = Mathf.Clamp01(Volume); voice.Source.clip = clip; voice.Source.Play();
            if (!captions.TryGetValue(cue, out var caption))
            {
                var text = new GameObject("In-world " + cue + " caption").AddComponent<TextMesh>(); text.transform.SetParent(transform, false);
                text.characterSize = 0.002f; text.fontSize = 32; text.anchor = TextAnchor.MiddleCenter;
                caption = new CaptionState { Text = text }; captions.Add(cue, caption);
            }
            // Repeated footsteps/taps extend one caption, rather than spawning a cloud of text.
            if (caption.Until <= Time.unscaledTime) caption.Text.transform.position = voice.Source.transform.position + Vector3.up * 0.035f;
            caption.Text.text = CueCatalog.Caption(cue); caption.Text.gameObject.SetActive(Captions); caption.Until = Time.unscaledTime + 1.3f;
        }
        private void Update()
        {
            foreach (var caption in captions.Values) caption.Text.gameObject.SetActive(Captions && caption.Until > Time.unscaledTime);
        }
        private void OnDestroy() { foreach (var clip in clips.Values) DestroyImmediate(clip); }
    }
}
#endif
