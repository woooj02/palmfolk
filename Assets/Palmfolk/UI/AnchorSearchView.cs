#if UNITY_5_3_OR_NEWER
using Palmfolk.Views;
using UnityEngine;

namespace Palmfolk.UI
{
    public sealed class AnchorSearchView : MonoBehaviour
    {
        private Transform content;
        private Material material;
        private TravelerRig pip;
        public bool Finding => content != null && content.gameObject.activeSelf;
        public void Show(bool finding, bool reducedMotion, long tick)
        {
            if (!finding) { if (content != null) content.gameObject.SetActive(false); return; }
            if (content == null)
            {
                content = new GameObject("Looking for your valley").transform; content.SetParent(transform, false);
                material = new Material(Resources.Load<Material>("Palmfolk/StandardOpaque")) { color = new Color(0.65f, 0.51f, 0.31f) };
                var figure = new GameObject("Pip finding the valley"); figure.transform.SetParent(content, false); pip = figure.AddComponent<TravelerRig>(); pip.Initialize(material);
                var text = new GameObject("Anchor search text").AddComponent<TextMesh>(); text.transform.SetParent(content, false); text.transform.localPosition = Vector3.down * 0.035f;
                text.text = "Looking for your valley…"; text.fontSize = 32; text.characterSize = 0.002f; text.anchor = TextAnchor.MiddleCenter;
            }
            content.gameObject.SetActive(true); pip.ReducedMotion = reducedMotion; pip.Show(BodyPose.Wave, tick);
        }
        private void OnDestroy() { if (material != null) DestroyImmediate(material); }
    }
}
#endif
