using NUnit.Framework;
using PetOffline.Core;
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

        [Test]
        public void DayTwoConfigsAndFixedReportMatchPlayableBaseline()
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelConfigSO>(
                "Assets/PetOffline/Data/Levels/Level_Day2.asset");
            var feederCamera = AssetDatabase.LoadAssetAtPath<CameraScanConfigSO>(
                "Assets/PetOffline/Data/Cameras/Camera_Day2_Feeder.asset");
            var backupCamera = AssetDatabase.LoadAssetAtPath<CameraScanConfigSO>(
                "Assets/PetOffline/Data/Cameras/Camera_Day2_Backup.asset");
            var banana = AssetDatabase.LoadAssetAtPath<CarryableConfigSO>(
                "Assets/PetOffline/Data/Carryables/Carryable_BananaPeel.asset");
            var report = AssetDatabase.LoadAssetAtPath<ReportDefinitionSO>(
                "Assets/PetOffline/Data/Reports/Report_Day2.asset");

            Assert.That(level, Is.Not.Null);
            Assert.That(feederCamera, Is.Not.Null);
            Assert.That(backupCamera, Is.Not.Null);
            Assert.That(banana, Is.Not.Null);
            Assert.That(report, Is.Not.Null);
            Assert.That(report.Fields.Count, Is.EqualTo(5));
            Assert.That(level.DayTwoObjective, Is.EqualTo("让拿铁晒满20秒太阳"));
            Assert.That(level.SunTargetSeconds, Is.EqualTo(20f).Within(0.001f));
            Assert.That(level.ConfirmationSeconds, Is.EqualTo(10f).Within(0.001f));
            Assert.That(level.ConfirmationIgnoreDelay, Is.EqualTo(9f).Within(0.001f));
            Assert.That(level.ConfirmationAlertSeconds, Is.EqualTo(7.2f).Within(0.001f));
            Assert.That(level.RobotSlipSpeed, Is.EqualTo(4.6f).Within(0.001f));
            Assert.That(level.RobotSlipDuration, Is.EqualTo(1.35f).Within(0.001f));

            AssertCamera(feederCamera, -35f, 35f, 28f, 7.2f, 58f);
            AssertCamera(backupCamera, -25f, 25f, 32f, 5.5f, 55f);

            Assert.That(banana.Heavy, Is.False);
            Assert.That(banana.DropsOnBark, Is.False);
            Assert.That(banana.CarrySpeedMultiplier, Is.EqualTo(0.85f).Within(0.001f));
            Assert.That(banana.RobotPushDistance, Is.Zero);

            Assert.That(level.DayTwoReport, Is.SameAs(report));
            Assert.That(report.Fields[0].Label, Is.EqualTo("晒太阳"));
            Assert.That(report.Fields[0].Value, Is.EqualTo("已完成（20 秒）"));
            Assert.That(report.Fields[1].Label, Is.EqualTo("远程确认"));
            Assert.That(report.Fields[1].Value, Is.EqualTo("失败"));
            Assert.That(report.Fields[2].Label, Is.EqualTo("投食器摄像头"));
            Assert.That(report.Fields[2].Value, Is.EqualTo("CAMERA OFFLINE"));
            Assert.That(report.Fields[3].Label, Is.EqualTo("当前画面"));
            Assert.That(report.Fields[3].Value, Is.EqualTo("墙"));
            Assert.That(report.Fields[4].Label, Is.EqualTo("情绪判断"));
            Assert.That(report.Fields[4].Value, Is.EqualTo("无法确认拿铁是否仍然想念主人"));
            Assert.That(report.Warning, Is.EqualTo("关闭后，您将无法实时确认拿铁是否仍在想您。"));
            Assert.That(report.ChoicePrompt, Is.EqualTo("是否恢复远程确认？"));
            Assert.That(report.RecommendedChoice, Is.EqualTo(FinalChoice.KeepQuiet));

            foreach (var id in new[]
                     {
                         "D2.Opening", "D2.FirstConfirm", "D2.ConfirmReturn", "D2.FeederOffline",
                         "D2.BackupActive", "D2.BackupConfirm", "D2.Complete", "D2.Restore", "D2.KeepQuiet"
                     })
                Assert.That(level.DayTwoDialogue(id), Is.Not.Null, $"Missing fixed dialogue {id}.");
        }

        static void AssertCamera(
            CameraScanConfigSO camera,
            float minimum,
            float maximum,
            float speed,
            float range,
            float fieldOfView)
        {
            Assert.That(camera.MinimumAngle, Is.EqualTo(minimum).Within(0.001f));
            Assert.That(camera.MaximumAngle, Is.EqualTo(maximum).Within(0.001f));
            Assert.That(camera.ScanDegreesPerSecond, Is.EqualTo(speed).Within(0.001f));
            Assert.That(camera.Range, Is.EqualTo(range).Within(0.001f));
            Assert.That(camera.FieldOfView, Is.EqualTo(fieldOfView).Within(0.001f));
            Assert.That(camera.AlertSpeedMultiplier, Is.EqualTo(2f).Within(0.001f));
            Assert.That(camera.AlertRangeMultiplier, Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(camera.AlertFieldOfViewMultiplier, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(camera.SampleInterval, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(camera.DetectionHoldSeconds, Is.EqualTo(0.2f).Within(0.001f));
        }
    }
}
