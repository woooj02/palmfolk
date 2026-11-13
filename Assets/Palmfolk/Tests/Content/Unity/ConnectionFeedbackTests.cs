#if UNITY_EDITOR
using NUnit.Framework;
using Palmfolk.Audio;
using Palmfolk.Core;
using Palmfolk.Core.Content;
using Palmfolk.Views;
using UnityEngine;

namespace Palmfolk.Tests.Content
{
    public class ConnectionFeedbackTests
    {
        [Test] public void ActualLockEventStartsOneShortDustBurstAndHasACaptionedChime()
        {
            var game = new ValleySimulation(ChapterOne.Greybox()); var player = new TestPlayer();
            Assert.That(player.RunUntil(game, () => player.Count(GameEventKind.ConnectionLocked) > 0), Is.True);
            var frame = new ValleyPresentation(game, 1); Assert.That(frame.ConnectionLocked, Is.True);
            var root = new GameObject("Lock feedback test");
            try
            {
                var view = root.AddComponent<ValleyView>(); view.Present(frame); Assert.That(view.ShowingConnectionDust, Is.False, "geometry/flag alone is not a new lock event");
                view.Consume(game.Events); view.Present(frame); Assert.That(view.ShowingConnectionDust, Is.True);
                Assert.That(view.ValleyRoot.Find("Connection lock dust").GetComponentsInChildren<Renderer>().Length, Is.GreaterThanOrEqualTo(12));
                player.Step(game, 31); view.Present(new ValleyPresentation(game, 1)); Assert.That(view.ShowingConnectionDust, Is.False);
                Assert.That(CueCatalog.TryMap(GameEventKind.ConnectionLocked, out var cue), Is.True); Assert.That(cue, Is.EqualTo(Cue.Connection)); Assert.That(CueCatalog.Caption(cue), Is.EqualTo("Connection locked"));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
#endif
