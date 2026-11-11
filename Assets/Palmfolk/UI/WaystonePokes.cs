#if UNITY_5_3_OR_NEWER
using Oculus.Interaction;
using Palmfolk.Core;
using Palmfolk.Platform;
using Palmfolk.Views;
using UnityEngine;

namespace Palmfolk.UI
{
    // Ten real ISDK poke interactors, driven by the same world-space samples the live clock reads.
    public sealed class WaystonePokes : MonoBehaviour
    {
        [SerializeField] private PalmfolkRunner runner;
        private readonly PokeInteractor[] fingers = new PokeInteractor[10];
        private bool focused = true;
        private static readonly JointId[] tips = { JointId.ThumbTip, JointId.IndexTip, JointId.MiddleTip, JointId.RingTip, JointId.LittleTip };
        public PokeInteractor Finger(int index) => fingers[index];
        public void Configure(PalmfolkRunner source) { Detach(); runner = source; Attach(); }
        private void OnEnable() => Attach();
        private void OnApplicationFocus(bool value) => focused = value;
        private void OnDisable() { Detach(); foreach (var finger in fingers) if (finger != null) finger.enabled = false; }
        private void Attach() { if (runner == null) return; runner.Presented -= Refresh; runner.Presented += Refresh; }
        private void Detach() { if (runner != null) runner.Presented -= Refresh; }
        public void Refresh(PalmfolkRunner source)
        {
            for (int i = 0; i < 10; i++)
            {
                if (fingers[i] == null)
                {
                    var go = new GameObject((i < 5 ? "Left " : "Right ") + tips[i % 5] + " poke"); go.SetActive(false); go.transform.SetParent(transform, false);
                    fingers[i] = go.AddComponent<PokeInteractor>(); fingers[i].InjectAllPokeInteractor(go.transform); fingers[i].enabled = false; go.SetActive(true);
                }
                var hand = i < 5 ? source.Source.Left : source.Source.Right;
                bool valid = focused && source.Game != null && !(source.Hands?.ControllersInHand ?? false) && hand != null && hand[tips[i % 5]].Usable;
                if (valid)
                {
                    var joint = hand[tips[i % 5]]; fingers[i].transform.position = PalmfolkView.V(joint.Position);
                    fingers[i].InjectRadius(joint.HasRadius ? (float)joint.Radius : 0.005f);
                }
                fingers[i].enabled = valid;
            }
        }
    }
}
#endif
