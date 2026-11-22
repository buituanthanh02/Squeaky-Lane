using NUnit.Framework;
using SqueakyLane.Core;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class CoreAssemblyTests
    {
        [Test]
        public void CoreAssembly_DoesNotReferenceUnityEngine()
        {
            // Lấy DLL qua kiểu Core để kiểm assembly thật, không nhầm assembly chứa test.
            var coreAssembly = typeof(CoreAssemblyMarker).Assembly;
            Assert.That(coreAssembly.GetName().Name, Is.EqualTo("SqueakyLane.Core"));

            // Chặn cả UnityEngine lẫn các module UnityEngine.* để luật luôn là C# thuần.
            foreach (var reference in coreAssembly.GetReferencedAssemblies())
            {
                Assert.That(reference.Name, Does.Not.StartWith("UnityEngine"),
                    $"Core không được tham chiếu engine: {reference.FullName}");
            }
        }
    }
}
