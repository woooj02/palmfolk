#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Audio;
using Palmfolk.Core;

namespace Palmfolk.Tests.Content
{
    public class AudioTests
    {
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
