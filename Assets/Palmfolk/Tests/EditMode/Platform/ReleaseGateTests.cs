#if UNITY_5_3_OR_NEWER
using System;
using System.Linq;
using NUnit.Framework;

namespace Palmfolk.Tests
{
    // D11 (T10) in the Unity editor: Unity's own compile of the project. Without PALMFOLK_DEVELOPMENT the recording
    // and developer assemblies don't exist at all; with it they do. No shipped assembly depends on one either way.
    // (tools/check_source.py checks the same from the sources; the APK manifest has its own check.)
    public class ReleaseGateTests
    {
        private static readonly string[] Developer = { "Palmfolk.Developer", "Palmfolk.Platform.Developer", "Palmfolk.Stats" };
        private static readonly string[] Shipped = { "Palmfolk.Core", "Palmfolk.Platform", "Palmfolk.Platform.Meta", "Palmfolk.Views", "Palmfolk.UI", "Palmfolk.Audio" };

#if PALMFOLK_DEVELOPMENT
        private static readonly bool Development = true;
#else
        private static readonly bool Development = false;
#endif

        [Test] public void DeveloperAssembliesExistOnlyWithTheDevelopmentDefine()
        {
            var loaded = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name).ToList();
            foreach (var name in Developer) Assert.That(loaded.Contains(name), Is.EqualTo(Development), name);
            Assert.That(loaded, Does.Contain("Palmfolk.Core"));
        }

        [Test] public void ShippedAssembliesNeverReferenceDeveloperOnes()
        {
            var shipped = AppDomain.CurrentDomain.GetAssemblies().Where(a => Shipped.Contains(a.GetName().Name)).ToList();
            Assert.That(shipped.Select(a => a.GetName().Name), Is.EquivalentTo(Shipped));
            foreach (var assembly in shipped)
            {
                var references = assembly.GetReferencedAssemblies().Select(r => r.Name).ToList();
                Assert.That(references.Intersect(Developer), Is.Empty, assembly.GetName().Name);
                // Recording and stats types live only in the developer assemblies.
                Assert.That(assembly.GetTypes().Where(t => t.Name.Contains("Recorder") || t.Name.Contains("Recording")).Select(t => t.FullName), Is.Empty, assembly.GetName().Name);
            }
        }
    }
}
#endif
