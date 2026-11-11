#if UNITY_5_3_OR_NEWER
using Palmfolk.Core;
using UnityEngine;

namespace Palmfolk.Views
{
    public sealed class NeedSignView : MonoBehaviour
    {
        private Transform sign;
        private Material panel, carving;
        private readonly LineRenderer[] strokes = new LineRenderer[5];
        private NeedSign drawn;
        public NeedSign Showing { get; private set; }
        public void Show(GazeTarget target, Transform viewer, bool reducedMotion, long tick)
        {
            if (target == null) { Showing = NeedSign.None; if (sign != null) sign.gameObject.SetActive(false); return; }
            Ensure(); sign.gameObject.SetActive(true); Showing = target.Need;
            float scale = (float)(target.Radius / NeedSigns.FigureRadius);
            float lift = reducedMotion ? 0 : Mathf.Sin(tick / 25f) * 0.002f;
            sign.position = PalmfolkView.V(target.Position) + Vector3.up * (0.027f + lift) * scale; sign.localScale = Vector3.one * scale;
            if (viewer != null && (sign.position - viewer.position).sqrMagnitude > 1e-6f) sign.rotation = Quaternion.LookRotation(sign.position - viewer.position);
            if (drawn == target.Need) return; drawn = target.Need;
            foreach (var stroke in strokes) stroke.gameObject.SetActive(false);
            switch (drawn)
            {
                case NeedSign.Rope: Line(0, new Vector2(-0.013f, 0.003f), new Vector2(0, -0.003f), new Vector2(0.013f, 0.003f)); break;
                case NeedSign.Plank:
                    for (int i = 0; i < 5; i++) Line(i, new Vector2((i - 2) * 0.005f, -0.007f), new Vector2((i - 2) * 0.005f, 0.007f)); break;
                case NeedSign.Branches:
                    Line(0, new Vector2(0, -0.008f), Vector3.zero, new Vector2(-0.011f, 0.008f)); Line(1, Vector3.zero, new Vector2(0.011f, 0.008f)); break;
                case NeedSign.Arch:
                    Line(0, new Vector2(-0.014f, -0.006f), new Vector2(-0.009f, 0.004f), new Vector2(0, 0.008f), new Vector2(0.009f, 0.004f), new Vector2(0.014f, -0.006f)); break;
            }
        }
        private void Line(int index, params Vector3[] points)
        { var line = strokes[index]; line.gameObject.SetActive(true); line.positionCount = points.Length; for (int i = 0; i < points.Length; i++) line.SetPosition(i, points[i] + Vector3.back * 0.002f); }
        private void Ensure()
        {
            if (sign != null) return;
            sign = new GameObject("Raised need sign").transform; sign.SetParent(transform, false);
            panel = new Material(Resources.Load<Material>("Palmfolk/HighContrast")) { color = new Color(0.05f, 0.06f, 0.04f) };
            carving = new Material(Resources.Load<Material>("Palmfolk/Lines")) { color = Color.white };
            var board = PalmfolkView.Primitive(PrimitiveType.Cube, "Carved sign", panel, sign); board.localScale = new Vector3(0.035f, 0.025f, 0.002f);
            for (int i = 0; i < strokes.Length; i++) { var line = new GameObject("Carved need stroke").AddComponent<LineRenderer>(); line.transform.SetParent(sign, false); line.useWorldSpace = false; line.widthMultiplier = 0.0018f; line.sharedMaterial = carving; strokes[i] = line; }
        }
        private void OnDestroy() { if (panel != null) DestroyImmediate(panel); if (carving != null) DestroyImmediate(carving); }
    }
}
#endif
