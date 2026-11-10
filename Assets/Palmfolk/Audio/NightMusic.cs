#if UNITY_5_3_OR_NEWER
using System;
using Palmfolk.Core;
using Palmfolk.Views;
using UnityEngine;

namespace Palmfolk.Audio
{
    // Original placeholder loop; source preparation, not an audio-quality or headset pass.
    public sealed class NightMusic : MonoBehaviour, IValleyView
    {
        private AudioSource source;
        private AudioClip loop;
        private TextMesh caption;
        private Transform musicRoot;
        private float target, until;
        private int previous = -1;
        private bool wasFinale, paused;
        public bool Captions { get; set; } = true;
        public static string Caption(bool finale) => finale ? "Music settles" : "Music swells";
        public static float[] Samples(int rate = 22050)
        {
            if (rate < 8000 || rate > 96000) throw new ArgumentOutOfRangeException();
            var samples = new float[rate * 4]; double[] notes = { 220, 261.6256, 293.6648, 329.6276, 293.6648, 261.6256, 246.9417, 220 };
            for (int i = 0; i < samples.Length; i++)
            {
                double t = i / (double)rate; int step = Math.Min(7, (int)(t * 2)); double noteEnvelope = Math.Sin(Math.PI * (t * 2 - step));
                double edge = Math.Min(1, Math.Min(t / 0.04, (4 - t) / 0.04));
                samples[i] = (float)((Math.Sin(2 * Math.PI * 110 * t) * 0.04 + Math.Sin(2 * Math.PI * notes[step] * t) * noteEnvelope * 0.035) * edge);
            }
            return samples;
        }
        public void Present(ValleyPresentation frame)
        {
            if (frame.Pose == null) return;
            Ensure(); musicRoot.position = PalmfolkView.V(frame.Pose.Origin); musicRoot.localScale = Vector3.one * (float)frame.Pose.Scale;
            bool finale = frame.Complete || frame.Stage == DirectorStage.BramCrossing;
            int solved = frame.Complete ? 6 : frame.PuzzleIndex;
            target = finale ? 0.08f : 0.04f + solved / 6f * 0.10f;
            if (solved != previous || finale != wasFinale)
            {
                if (previous >= 0) { caption.text = Caption(finale); until = Time.unscaledTime + 1.8f; }
                previous = solved; wasFinale = finale;
            }
            if (frame.Paused && !paused) { source.Pause(); paused = true; }
            else if (!frame.Paused && paused) { source.UnPause(); paused = false; }
        }
        private void Ensure()
        {
            if (source != null) return;
            var samples = Samples(); loop = AudioClip.Create("Original valley music", samples.Length, 1, 22050, false); loop.SetData(samples, 0);
            musicRoot = new GameObject("Night music and caption").transform; musicRoot.SetParent(transform, false);
            source = musicRoot.gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.loop = true; source.spatialBlend = 0; source.volume = 0; source.clip = loop; source.Play();
            caption = new GameObject("In-world music caption").AddComponent<TextMesh>(); caption.transform.SetParent(musicRoot, false);
            caption.transform.localPosition = new Vector3(0, 0.06f, -0.12f); caption.characterSize = 0.002f; caption.fontSize = 32; caption.anchor = TextAnchor.MiddleCenter;
        }
        private void Update()
        {
            if (source == null) return;
            source.volume = Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * 0.08f);
            caption.gameObject.SetActive(Captions && Time.unscaledTime < until);
        }
        private void OnDestroy() { if (loop != null) DestroyImmediate(loop); }
    }
}
#endif
