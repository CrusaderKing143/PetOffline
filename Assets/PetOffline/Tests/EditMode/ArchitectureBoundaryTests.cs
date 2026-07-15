using NUnit.Framework;
using PetOffline.Editor;
using PetOffline.Gameplay;
using UnityEditor;

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

        [Test]
        public void DayOneCameraAndCarryConfigsMatchPlayableBaseline()
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelConfigSO>(
                "Assets/PetOffline/Data/Levels/Level_Day1.asset");
            var camera = AssetDatabase.LoadAssetAtPath<CameraScanConfigSO>(
                "Assets/PetOffline/Data/Cameras/Camera_Day1_B.asset");
            var slipper = AssetDatabase.LoadAssetAtPath<CarryableConfigSO>(
                "Assets/PetOffline/Data/Carryables/Carryable_Slipper.asset");
            var pillow = AssetDatabase.LoadAssetAtPath<CarryableConfigSO>(
                "Assets/PetOffline/Data/Carryables/Carryable_Pillow.asset");

            Assert.That(level, Is.Not.Null);
            Assert.That(level.ShoeGoalHoldSeconds, Is.EqualTo(2f));
            Assert.That(level.DayOneReport, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.Range, Is.EqualTo(7.1f).Within(0.001f));
            Assert.That(camera.FieldOfView, Is.EqualTo(54f).Within(0.001f));
            Assert.That(camera.DetectionHoldSeconds, Is.EqualTo(0.24f).Within(0.001f));
            Assert.That(slipper.CarrySpeedMultiplier, Is.EqualTo(0.85f).Within(0.001f));
            Assert.That(pillow.CarrySpeedMultiplier, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(pillow.DropsOnBark, Is.True);
            Assert.That(pillow.RobotPushDistance, Is.EqualTo(0.65f).Within(0.001f));
        }
    }
}
