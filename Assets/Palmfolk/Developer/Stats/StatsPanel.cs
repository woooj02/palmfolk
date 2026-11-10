#if PALMFOLK_DEVELOPMENT && UNITY_5_3_OR_NEWER
using UnityEngine;

namespace Palmfolk.Developer
{
    // Display-only. Caller supplies measured per-puzzle counts and honest platform labels.
    // No recorder, file access, SDK input or fabricated default measurement.
    public sealed class StatsPanel : MonoBehaviour
    {
        private TextMesh text;
        private void Ensure()
        {
            if (text != null) return;
            text = new GameObject("Developer stats").AddComponent<TextMesh>(); text.transform.SetParent(transform, false);
            text.characterSize = 0.003f; text.fontSize = 32; text.anchor = TextAnchor.UpperLeft;
        }
        public void Show(double? fps, int? dropouts, int? disruptive, string puzzle, string gazeSource, string mode, string version)
        {
            Ensure();
            text.text = "DEVELOPER / " + (version ?? "version unbound") + "\n" + (mode ?? "mode unbound") + " / gaze: " + (gazeSource ?? "unbound") +
                "\nFPS: " + (fps.HasValue ? fps.Value.ToString("0.0") : "unmeasured") + "\n" + (puzzle ?? "puzzle unbound") +
                " dropouts: " + (dropouts.HasValue ? dropouts.Value.ToString() : "unmeasured") +
                " / disruptive: " + (disruptive.HasValue ? disruptive.Value.ToString() : "unmeasured");
        }
    }
}
#endif
