#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Audio;
using Palmfolk.Core;
using Palmfolk.Views;
using UnityEngine;

namespace Palmfolk.Tests.Content
{
    public class AudioTests
    {
        [Test] public void OriginalNightLoopIsBoundedAndCaptioned()
        {
            var samples = NightMusic.Samples(); Assert.That(samples.Length, Is.EqualTo(88200));
            Assert.That(samples.All(s => !float.IsNaN(s) && !float.IsInfinity(s) && Math.Abs(s) < 0.1), Is.True);
            Assert.That(NightMusic.Caption(false), Is.Not.Empty); Assert.That(NightMusic.Caption(true), Is.Not.Empty);
        }
        [Test] public void ContactBindingOnlySubscribesToItsAssignedContentRoot()
        {
            var root = new GameObject("Audio binding test");
            try
            {
                var scope = new GameObject("Scoped rigs"); scope.transform.SetParent(root.transform);
                scope.AddComponent<TravelerRig>(); root.AddComponent<TravelerRig>();
                var audio = root.AddComponent<AudioPresenter>(); var binding = root.AddComponent<ContactAudioBinding>(); binding.Configure(audio, scope.transform);
                binding.RefreshBindings(); Assert.That(binding.BoundCount, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test] public void EveryOriginalCueHasCaptionAndBoundedSamples()
        {
            foreach (Cue cue in Enum.GetValues(typeof(Cue)))
            {
                Assert.That(CueCatalog.Caption(cue), Is.Not.Empty);
                var samples = CueCatalog.Samples(cue);
                Assert.That(samples.Length, Is.GreaterThan(100));
                Assert.That(samples.All(s => !float.IsNaN(s) && !float.IsInfinity(s) && Math.Abs(s) <= 1), Is.True);
                Assert.That(samples[0], Is.Zero);
                Assert.That(Math.Abs(samples[samples.Length - 1]), Is.LessThan(1e-6));
            }
        }
        [Test] public void BuildCompletionAndSectionCommitAreDistinctCues()
        {
            Assert.That(CueCatalog.TryMap(GameEventKind.BridgeBaked, out var done), Is.True);
            Assert.That(CueCatalog.TryMap(GameEventKind.SectionCommitted, out var tap), Is.True);
            Assert.That(done, Is.EqualTo(Cue.BuildDone)); Assert.That(tap, Is.EqualTo(Cue.Hammer));
            Assert.That(CueCatalog.TryMap(GameEventKind.Boarded, out _), Is.False, "Boarding is not a connection-lock signal.");
        }
    }
}
#endif
