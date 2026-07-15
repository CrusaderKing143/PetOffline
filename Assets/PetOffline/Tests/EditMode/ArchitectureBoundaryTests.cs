using NUnit.Framework;
using PetOffline.Editor;

namespace PetOffline.Tests.EditMode
{
    public sealed class ArchitectureBoundaryTests
    {
        [Test]
        public void ProjectValidatorPasses()
        {
            var errors = ProjectValidator.CollectErrors();
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }
    }
}
