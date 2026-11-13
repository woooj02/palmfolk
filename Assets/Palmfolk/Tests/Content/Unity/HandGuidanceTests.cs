#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Core.Content;
using Palmfolk.Views;
using UnityEngine;

namespace Palmfolk.Tests.Content
{
    public class HandGuidanceTests
    {
        [TestCase(HandSide.Left)] [TestCase(HandSide.Right)]
        public void RecoveryHandUsesObservedJointsAndStaysStillUntilRecoveryEnds(HandSide side)
        {
            var game = new ValleySimulation(ChapterOne.Greybox()); var player = new TestPlayer { Side = side };
            Assert.That(player.RunUntil(game, () => game.PuzzleIndex == 1 && game.Session?.Build?.Progress > 0.3), Is.True);
            game.Step(new HandFrame(game.Tick + 1, (game.Tick + 1) * 16667, null, null));
            var root = new GameObject("Frozen hand test");
            try
            {
                var view = root.AddComponent<HandGuidanceView>(); var frame = new ValleyPresentation(game, 1); view.Present(frame);
                Assert.That(view.GhostJoints, Is.EqualTo(26)); Assert.That(view.GhostRoot.gameObject.activeSelf, Is.True); Assert.That(view.ShowingHint, Is.Null);
                var positions = view.GhostRoot.GetComponentsInChildren<Renderer>().Select(r => r.transform.position).ToArray();
                Assert.That(positions.Length, Is.GreaterThan(40));
                game.Step(new HandFrame(game.Tick + 1, (game.Tick + 1) * 16667, null, null)); view.Present(new ValleyPresentation(game, 1));
                Assert.That(view.GhostRoot.GetComponentsInChildren<Renderer>().Select(r => r.transform.position), Is.EqualTo(positions));
                Assert.That(player.RunUntil(game, () => game.Session?.Recovery == null), Is.True); view.Present(new ValleyPresentation(game, 1));
                Assert.That(view.GhostRoot.gameObject.activeSelf, Is.False); Assert.That(view.GhostJoints, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test] public void HintWaitsForActualHintDueAndReducedMotionStopsItsAnimation()
        {
            var game = new ValleySimulation(ChapterOne.Greybox()); var root = new GameObject("Hint timing test");
            try
            {
                var view = root.AddComponent<HandGuidanceView>(); bool due = false;
                for (int i = 0; i < 700 && !due; i++)
                {
                    game.Step(new HandFrame(game.Tick + 1, (game.Tick + 1) * 16667, null, null));
                    due = game.Events.Any(e => e.Kind == GameEventKind.HintDue); view.Consume(game.Events); view.Present(new ValleyPresentation(game, 1));
                    if (!due) Assert.That(view.ShowingHint, Is.Null);
                }
                Assert.That(due, Is.True); Assert.That(view.ShowingHint, Is.EqualTo(HintShape.CarryFinger));
                var handParts = view.HintRoot.Find("Illustrated hand shape"); var palm = handParts.GetChild(0).localPosition;
                var tree = new ValleyPresentation(game, 1).Hint.Tree;
                Assert.That(Vector3.Distance(palm, PalmfolkView.V(tree.Curves[0].End)), Is.LessThan(Vector3.Distance(palm, PalmfolkView.V(tree.Curves[0].Start))), "Board: fingertips at Pip, palm toward the next island");
                var first = view.HintRoot.localPosition;
                for (int i = 0; i < 10; i++) game.Step(new HandFrame(game.Tick + 1, (game.Tick + 1) * 16667, null, null));
                view.Present(new ValleyPresentation(game, 1)); Assert.That(view.HintRoot.localPosition, Is.Not.EqualTo(first));
                view.ReducedMotion = true; view.Present(new ValleyPresentation(game, 1)); Assert.That(view.HintRoot.localPosition, Is.EqualTo(Vector3.zero));
                view.Consume(new[] { new GameEvent(game.Tick, GameEventKind.Boarded) }); view.Present(new ValleyPresentation(game, 1)); Assert.That(view.ShowingHint, Is.Null);
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test] public void EveryPublishedHintShapeDrawsItsPassingTreeWithoutChangingTheGame()
        {
            var game = new ValleySimulation(ChapterOne.Greybox()); var player = new TestPlayer(); var frames = new Dictionary<HintShape, ValleyPresentation>();
            for (int i = 0; i < 30000 && !game.IsComplete; i++)
            { player.Step(game); var frame = new ValleyPresentation(game, 1); if (frame.Hint != null && !frames.ContainsKey(frame.Hint.Shape)) frames.Add(frame.Hint.Shape, frame); }
            Assert.That(frames.Keys, Is.EquivalentTo(new[] { HintShape.CarryFinger, HintShape.OneFinger, HintShape.FlatHand, HintShape.Arch, HintShape.Spread }));
            var root = new GameObject("Hint shapes test"); long tick = game.Tick;
            try
            {
                var view = root.AddComponent<HandGuidanceView>(); view.ReducedMotion = true;
                foreach (var pair in frames)
                {
                    view.Present(pair.Value); view.Consume(new[] { new GameEvent(pair.Value.Tick, GameEventKind.HintDue) }); view.Present(pair.Value);
                    Assert.That(view.ShowingHint, Is.EqualTo(pair.Key));
                    var dots = view.HintRoot.Find("Passing route dots").GetComponentsInChildren<Renderer>();
                    Assert.That(dots.Length, Is.EqualTo(pair.Value.Hint.Tree.Curves.Sum(c => c.Points.Count)));
                    Assert.That(view.HintRoot.GetComponentsInChildren<Renderer>().Length, Is.GreaterThan(50));
                }
                Assert.That(game.Tick, Is.EqualTo(tick));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
#endif
