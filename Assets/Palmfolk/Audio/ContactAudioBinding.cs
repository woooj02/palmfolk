#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using Palmfolk.Views;
using UnityEngine;

namespace Palmfolk.Audio
{
    public sealed class ContactAudioBinding : MonoBehaviour
    {
        private AudioPresenter audio;
        private Transform contentRoot;
        private readonly HashSet<TravelerRig> bound = new HashSet<TravelerRig>();
        private readonly List<TravelerRig> found = new List<TravelerRig>();
        public int BoundCount => bound.Count;
        public void Configure(AudioPresenter presenter, Transform root)
        { Unbind(); audio = presenter; contentRoot = root; RefreshBindings(); }
        public void RefreshBindings()
        {
            if (audio == null || contentRoot == null) return;
            found.Clear(); contentRoot.GetComponentsInChildren(true, found);
            foreach (var rig in found) if (bound.Add(rig)) rig.FootContact += audio.FootContact;
        }
        private void LateUpdate() { RefreshBindings(); }
        private void Unbind()
        { if (audio != null) foreach (var rig in bound) if (rig != null) rig.FootContact -= audio.FootContact; bound.Clear(); }
        private void OnDisable() { Unbind(); }
    }
}
#endif
