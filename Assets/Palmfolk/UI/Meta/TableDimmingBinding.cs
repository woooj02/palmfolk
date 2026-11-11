#if UNITY_5_3_OR_NEWER
using Palmfolk.Views;
using UnityEngine;

namespace Palmfolk.UI.Meta
{
    public sealed class TableDimmingBinding : MonoBehaviour
    {
        [SerializeField, Range(0, 1)] private float ownerVerifiedMaximum;
        private NightView night;
        private OVRPassthroughLayer layer;
        public float Applied { get; private set; }
        public void Configure(NightView source, OVRPassthroughLayer passthrough = null)
        { if (night != null) night.TableDimmingRequested -= Apply; night = source; layer = passthrough; if (night != null) { night.VerifiedTableDimmingLimit = ownerVerifiedMaximum; night.TableDimmingRequested += Apply; } }
        public void Apply(float requested)
        {
            Applied = night != null && night.TableMode ? Mathf.Clamp(requested, 0, ownerVerifiedMaximum) : 0;
            if (layer == null) layer = FindAnyObjectByType<OVRPassthroughLayer>();
            if (layer != null) layer.SetBrightnessContrastSaturation(-Applied);
        }
        private void Start() { var presentation = GetComponent<GamePresentation>(); if (presentation != null) Configure(presentation.Night); }
        private void Update() { if (night != null && !night.TableMode && Applied != 0) Apply(0); }
        private void OnDisable() { if (night != null) night.TableDimmingRequested -= Apply; if (layer != null) layer.SetBrightnessContrastSaturation(); Applied = 0; }
    }
}
#endif
