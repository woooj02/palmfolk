#if UNITY_EDITOR
using NUnit.Framework;
using Palmfolk.Core;
using Palmfolk.Views;
using UnityEngine;
using System.Collections.Generic;

namespace Palmfolk.Tests.Content
{
    public class ContactTests
    {
        [Test] public void RigConvertsLandingToWorldSpace()
        {
            var root = new GameObject("Contact rig"); var material = new Material(Shader.Find("Standard"));
            try
            {
                var rig = root.AddComponent<TravelerRig>(); rig.Initialize(material);
                var pose = new ValleyPose(new Vec3(0.4, 0.7, 0.2), new Vec3(0, 0, 1), 2);
                var saved = new Dictionary<string, CurveTree> { { "test", CurveTree.Single(Line()) } };
                Vec3? landed = null; rig.FootContact += point => landed = point;
                rig.PresentTraveler(new Vec3(0.05, 0, 0), 0, false, pose, saved);
                rig.PresentTraveler(new Vec3(0.06, 0, 0), 1, false, pose, saved);
                rig.PresentTraveler(new Vec3(0.06, 0, 0), 13, false, pose, saved);
                Assert.That(landed.HasValue, Is.True);
                Assert.That(landed.Value.X, Is.EqualTo(0.52).Within(1e-9));
                Assert.That(landed.Value.Y, Is.EqualTo(0.7).Within(1e-9));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(material); }
        }
        private static Route Line(double height = 0) => new Route(new[] { new Vec3(0, height, 0), new Vec3(0.1, height, 0), new Vec3(0.2, height, 0) }, new[] { 0.04, 0.04 });
        [Test] public void PlantedFeetMoveWithTheHandWithoutCreatingSteps()
        {
            var feet = new FootPlanting(); int sounds = 0; feet.Landed += _ => sounds++;
            feet.Update(Line(), new Vec3(0.05, 0, 0), new Vec3(1, 0, 0), 0, true);
            Vec3 left = feet.Left;
            feet.Update(Line(0.1), new Vec3(0.05, 0.1, 0), new Vec3(1, 0, 0), 1, true);
            Assert.That(feet.Left.X, Is.EqualTo(left.X).Within(1e-12)); Assert.That(feet.Left.Y, Is.EqualTo(left.Y + 0.1).Within(1e-12));
            Assert.That(sounds, Is.Zero);
        }
        [Test] public void StepSoundIsEmittedAtLandingAfterTheSwing()
        {
            var feet = new FootPlanting(); int sounds = 0; feet.Landed += _ => sounds++;
            feet.Update(Line(), new Vec3(0.05, 0, 0), new Vec3(1, 0, 0), 0, true);
            feet.Update(Line(), new Vec3(0.06, 0, 0), new Vec3(1, 0, 0), 1, true);
            Assert.That(sounds, Is.Zero);
            feet.Update(Line(), new Vec3(0.06, 0, 0), new Vec3(1, 0, 0), 7, true);
            Assert.That(feet.Left.Y, Is.GreaterThan(0)); Assert.That(sounds, Is.Zero);
            feet.Update(Line(), new Vec3(0.06, 0, 0), new Vec3(1, 0, 0), 13, true);
            Assert.That(feet.Left.Y, Is.EqualTo(0).Within(1e-12)); Assert.That(sounds, Is.EqualTo(1));
        }
    }
}
#endif
