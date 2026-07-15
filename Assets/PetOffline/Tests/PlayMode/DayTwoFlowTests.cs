using System;
using System.Collections;
using NUnit.Framework;
using PetOffline.Core;
using PetOffline.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PetOffline.Tests.PlayMode
{
    public sealed class DayTwoFlowTests
    {
        static readonly Vector2 BananaPlacement = new(0.85f, -1.45f);

        LevelTwoFlowController flow;
        PlayerController2D player;
        CarryController carry;
        CarryableObject banana;
        RobotPatrol robot;
        CameraVisionSensor2D feederCamera;
        CameraVisionSensor2D backupCamera;
        PlayerTrigger2D sunZone;
        PlayerTrigger2D feederArea;
        PlayerTrigger2D sideDoor;
        Transform playerSpawn;
        Transform backupRetrySpawn;
        Transform sniffPoint;
        Transform sleepPoint;
        Collider2D feederCollider;
        GameObject feederDevice;
        GameObject feederOfflineDisplay;
        GameObject backupActiveDisplay;
        GameSession session;
        SaveService save;
        DialogueDirector dialogue;
        bool sawBananaContact;
        bool sawFeederContact;
        bool sawRobotSlip;

        [UnitySetUp]
        public IEnumerator LoadDayTwo()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.Day2);
            yield return null;

            BindSceneReferences();
            CreateTestSession();
            flow.Bind(session);
            FinishDialogue();
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.SunFirst));
        }

        [UnityTearDown]
        public IEnumerator ResetTestState()
        {
            Time.timeScale = 1f;
            if (save != null)
                save.ClearProgress();
            else
                UnityEngine.Object.FindAnyObjectByType<SaveService>()?.ClearProgress();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstTenSecondsStartsConfirmationAndPausesSunProgress()
        {
            yield return StartFirstConfirmation();

            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.CameraCheck));
            Assert.That(flow.ConfirmationActive, Is.True);
            Assert.That(flow.ConfirmationCount, Is.EqualTo(1));
            Assert.That(flow.SunTime, Is.EqualTo(10f).Within(0.001f));
            Assert.That(flow.SunProgress01, Is.EqualTo(0.5f).Within(0.001f));

            flow.Tick(3f);
            Assert.That(flow.SunTime, Is.EqualTo(10f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator FeederReturnClearsProgressAndTheTenSecondLoopCanRepeat()
        {
            yield return ReachDestroyCamera();

            Assert.That(flow.SunTime, Is.Zero);
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.DestroyCamera));
            Assert.That(flow.FoodCameraActive, Is.True);

            yield return EnterTrigger(sunZone);
            player.SetLying(true);
            flow.Tick(10.01f);

            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.CameraCheck));
            Assert.That(flow.ConfirmationActive, Is.True);
            Assert.That(flow.ConfirmationCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator IgnoredConfirmationKeepsProgressPausedAndTemporarilyAlertsCamera()
        {
            yield return StartFirstConfirmation();
            var pausedAt = flow.SunTime;

            flow.Tick(8.99f);
            Assert.That(flow.IgnoredConfirmationCount, Is.Zero);
            Assert.That(flow.AlertActive, Is.False);

            flow.Tick(0.02f);
            Assert.That(flow.IgnoredConfirmationCount, Is.EqualTo(1));
            Assert.That(flow.ConfirmationActive, Is.True);
            Assert.That(flow.AlertActive, Is.True);
            Assert.That(feederCamera.IsAlert, Is.True);
            Assert.That(flow.SunTime, Is.EqualTo(pausedAt).Within(0.001f));

            flow.Tick(7.21f);
            Assert.That(flow.AlertActive, Is.False);
            Assert.That(feederCamera.IsAlert, Is.False);
            Assert.That(flow.SunTime, Is.EqualTo(pausedAt).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator BananaRobotAndFeederContactsDisableOnlyTheFeederCamera()
        {
            yield return ReachDestroyCamera();
            yield return DisableFeederCameraWithPhysics();

            Assert.That(sawBananaContact, Is.True);
            Assert.That(sawRobotSlip, Is.True);
            Assert.That(sawFeederContact, Is.True);
            Assert.That(flow.FoodCameraActive, Is.False);
            Assert.That(feederCamera.SensorActive, Is.False);
            Assert.That(feederCamera.GetComponent<LineRenderer>().enabled, Is.False);
            Assert.That(flow.FeederDeviceActive, Is.True);
            Assert.That(feederDevice.activeSelf, Is.True);
            Assert.That(feederOfflineDisplay.activeSelf, Is.True);
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.Backup));
        }

        [UnityTest]
        public IEnumerator SideDoorPhysicsTriggerActivatesTheBackupCamera()
        {
            yield return ReachDestroyCamera();
            yield return DisableFeederCameraWithPhysics();
            yield return ActivateBackupCamera();

            Assert.That(sideDoor.Inside, Is.True);
            Assert.That(flow.BackupCameraActive, Is.True);
            Assert.That(flow.BackupCameraEverTriggered, Is.True);
            Assert.That(backupCamera.SensorActive, Is.True);
            Assert.That(backupCamera.GetComponent<LineRenderer>().enabled, Is.True);
            Assert.That(backupActiveDisplay.activeSelf, Is.True);
            Assert.That(flow.SunTime, Is.Zero);
        }

        [UnityTest]
        public IEnumerator WrongRouteStillConfirmsAndThenRecoversToFinalSun()
        {
            yield return ReachDestroyCamera();
            yield return DisableFeederCameraWithPhysics();
            yield return ActivateBackupCamera();
            yield return EnterTrigger(sunZone);
            player.SetLying(true);
            flow.Tick(10.01f);

            Assert.That(flow.ConfirmationActive, Is.True);
            Assert.That(flow.AttemptReacquired, Is.True);
            Assert.That(flow.BackupCameraActive, Is.True);
            Assert.That(flow.ConfirmationCount, Is.EqualTo(2));

            FinishDialogue();
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.FinalSun));
            Assert.That(flow.BackupLessonCompleted, Is.True);
            Assert.That(flow.BackupCameraActive, Is.False);
            Assert.That(flow.ConfirmationActive, Is.False);
            Assert.That(flow.AttemptReacquired, Is.False);
            Assert.That(flow.BackupResetCount, Is.EqualTo(1));
            Assert.That(Vector2.Distance(player.Position, backupRetrySpawn.position), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator ReenteringSideDoorDuringBackupConfirmationKeepsItsCompletionCallback()
        {
            yield return ReachDestroyCamera();
            yield return DisableFeederCameraWithPhysics();
            yield return ActivateBackupCamera();
            yield return EnterTrigger(sunZone);
            player.SetLying(true);
            flow.Tick(10.01f);

            Assert.That(flow.ConfirmationActive, Is.True);
            var confirmationCount = flow.ConfirmationCount;
            yield return EnterTrigger(sideDoor);

            Assert.That(flow.ConfirmationActive, Is.True);
            Assert.That(flow.ConfirmationCount, Is.EqualTo(confirmationCount));
            FinishDialogue();
            Assert.That(flow.ConfirmationActive, Is.False);
            Assert.That(flow.BackupCameraActive, Is.False);
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.FinalSun));
            Assert.That(flow.BackupResetCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BackupLessonCannotBeSkippedAfterTheFeederCameraIsOffline()
        {
            yield return ReachDestroyCamera();
            yield return DisableFeederCameraWithPhysics();
            yield return EnterTrigger(sunZone);
            player.SetLying(true);
            flow.Tick(20.01f);

            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.Backup));
            Assert.That(flow.BackupLessonCompleted, Is.False);
            Assert.That(flow.SunTime, Is.Zero);
            Assert.That(flow.Context.Report, Is.Null);
        }

        [UnityTest]
        public IEnumerator CorrectLivingRoomRouteCompletesAtTwentySecondsWithoutReacquisition()
        {
            yield return CompleteBackupLesson();
            yield return EnterSunThroughLivingRoomRoute();
            player.SetLying(true);

            flow.Tick(19.99f);
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.FinalSun));
            Assert.That(flow.ConfirmationActive, Is.False);
            Assert.That(flow.AttemptReacquired, Is.False);

            flow.Tick(0.02f);
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.Report));
            Assert.That(flow.SunTime, Is.EqualTo(20f).Within(0.001f));
            Assert.That(flow.Context.Report, Is.Not.Null);
            Assert.That(flow.FoodCameraActive, Is.False);
            Assert.That(flow.BackupCameraActive, Is.False);
        }

        [UnityTest]
        public IEnumerator RestoreConnectionRunsANewTenSecondsAndShowsOwnerCallBeforeSaving()
        {
            yield return ReachChoice();
            var ownerCalled = false;
            Action<string, string> onLine = (speaker, line) =>
                ownerCalled |= speaker == "Owner" && line.Contains("拿铁");
            dialogue.LineShown += onLine;

            session.SubmitChoice(FinalChoice.RestoreConnection);
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.End));
            Assert.That(flow.RestoreLoopActive, Is.True);
            Assert.That(flow.FoodCameraActive, Is.True);
            Assert.That(feederCamera.SensorActive, Is.True);
            Assert.That(feederOfflineDisplay.activeSelf, Is.False);

            yield return EnterTrigger(sunZone);
            player.SetLying(true);
            flow.Tick(9.99f);
            Assert.That(flow.RestoreConfirmationTriggered, Is.False);
            Assert.That(flow.EndingComplete, Is.False);

            flow.Tick(0.02f);
            Assert.That(flow.SunTime, Is.EqualTo(10f).Within(0.001f));
            Assert.That(flow.ConfirmationActive, Is.True);
            Assert.That(flow.RestoreConfirmationTriggered, Is.True);
            Assert.That(flow.EndingComplete, Is.False);

            var deadline = Time.realtimeSinceStartup + 4.5f;
            while (!ownerCalled && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(ownerCalled, Is.True, "D2.Restore never replayed the owner's call.");

            FinishDialogue();
            dialogue.LineShown -= onLine;
            Assert.That(flow.EndingComplete, Is.True);
            Assert.That(save.DayTwoCompleted, Is.True);
            Assert.That(save.LastChoice, Is.EqualTo(FinalChoice.RestoreConnection));
        }

        [UnityTest]
        public IEnumerator KeepQuietTurnsOffBothCamerasSleepsShowsSubtitleAndSaves()
        {
            yield return ReachChoice();

            session.SubmitChoice(FinalChoice.KeepQuiet);

            for (var i = 0; i < 20 && !flow.EndingComplete; i++)
            {
                flow.Tick(0.5f);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.End));
            Assert.That(flow.SelectedChoice, Is.EqualTo(FinalChoice.KeepQuiet));
            Assert.That(flow.FoodCameraActive, Is.False);
            Assert.That(flow.BackupCameraActive, Is.False);
            Assert.That(feederCamera.SensorActive, Is.False);
            Assert.That(backupCamera.SensorActive, Is.False);
            Assert.That(feederDevice.activeSelf, Is.True);
            Assert.That(feederOfflineDisplay.activeSelf, Is.True);
            Assert.That(flow.KeepQuietSniffed, Is.True);
            Assert.That(player.IsLying, Is.True);
            Assert.That(Vector2.Distance(player.Position, sleepPoint.position), Is.LessThan(0.01f));
            Assert.That(flow.FinalSubtitle, Is.EqualTo(LevelTwoFlowController.KeepQuietSubtitle));
            Assert.That(flow.EndingComplete, Is.True);
            Assert.That(save.DayTwoCompleted, Is.True);
            Assert.That(save.LastChoice, Is.EqualTo(FinalChoice.KeepQuiet));
        }

        [UnityTest]
        public IEnumerator DisabledUIRootDoesNotBlockTheCompleteDayTwoWorldChain()
        {
            save.ClearProgress();
            SceneManager.LoadScene(SceneNames.Bootstrap);
            yield return null;

            session = UnityEngine.Object.FindAnyObjectByType<GameSession>();
            var uiRoot = GameObject.Find("UIRoot");
            Assert.That(session, Is.Not.Null);
            Assert.That(uiRoot, Is.Not.Null);
            session.Save.ClearProgress();
            session.Save.MarkDayOneCompleted();
            uiRoot.SetActive(false);
            session.ContinueSavedGame();

            for (var i = 0; i < 240; i++)
            {
                flow = UnityEngine.Object.FindAnyObjectByType<LevelTwoFlowController>();
                if (flow != null && flow.IsBound)
                    break;
                yield return null;
            }

            Assert.That(flow, Is.Not.Null);
            Assert.That(flow.IsBound, Is.True);
            BindSceneReferences();
            save = session.Save;
            dialogue = session.Dialogue;
            FinishDialogue();
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.SunFirst));

            yield return ReachReport();
            session.ContinueReport();
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.Choice));
            player.ResetTo(sleepPoint.position);
            Physics2D.SyncTransforms();
            session.SubmitChoice(FinalChoice.KeepQuiet);

            for (var i = 0; i < 20 && !flow.EndingComplete; i++)
            {
                flow.Tick(0.5f);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(flow.EndingComplete, Is.True);
            Assert.That(session.Save.DayTwoCompleted, Is.True);
            Assert.That(session.Save.LastChoice, Is.EqualTo(FinalChoice.KeepQuiet));
            Assert.That(uiRoot.activeSelf, Is.False);
        }

        IEnumerator StartFirstConfirmation()
        {
            yield return EnterTrigger(sunZone);
            player.SetLying(true);
            flow.Tick(9.99f);
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.SunFirst));
            flow.Tick(0.02f);
        }

        IEnumerator ReachDestroyCamera()
        {
            yield return StartFirstConfirmation();
            yield return EnterTrigger(feederArea, Vector2.left * 0.85f);
            FinishDialogue();
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.DestroyCamera));
            Assert.That(flow.SunTime, Is.Zero);
        }

        IEnumerator DisableFeederCameraWithPhysics()
        {
            sawBananaContact = false;
            sawFeederContact = false;
            sawRobotSlip = false;
            robot.Contacted += TrackRobotContact;

            Assert.That(carry.TryPickup(banana), Is.True);
            carry.ForceDrop(BananaPlacement);
            Physics2D.SyncTransforms();

            for (var i = 0; i < 300 && flow.FoodCameraActive; i++)
            {
                yield return new WaitForFixedUpdate();
                sawRobotSlip |= robot.IsSlipping;
            }

            robot.Contacted -= TrackRobotContact;
            Assert.That(flow.FoodCameraActive, Is.False, "Robot never reached the feeder through physics contacts.");
            Assert.That(sawBananaContact, Is.True, "Robot never contacted the placed BananaPeel.");
            Assert.That(sawFeederContact, Is.True, "Slipping robot never contacted FutureFeeder.");
            FinishDialogue();
        }

        IEnumerator ActivateBackupCamera()
        {
            yield return EnterTrigger(sideDoor);
            FinishDialogue();
            Assert.That(flow.BackupCameraActive, Is.True);
        }

        IEnumerator CompleteBackupLesson()
        {
            yield return ReachDestroyCamera();
            yield return DisableFeederCameraWithPhysics();
            yield return ActivateBackupCamera();
            yield return EnterTrigger(sunZone);
            player.SetLying(true);
            flow.Tick(10.01f);
            Assert.That(flow.ConfirmationActive, Is.True);
            Assert.That(flow.AttemptReacquired, Is.True);
            FinishDialogue();
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.FinalSun));
            Assert.That(flow.BackupLessonCompleted, Is.True);
        }

        IEnumerator ReachReport()
        {
            yield return CompleteBackupLesson();
            yield return EnterSunThroughLivingRoomRoute();
            player.SetLying(true);
            flow.Tick(20.01f);
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.Report));
            FinishDialogue();
        }

        IEnumerator ReachChoice()
        {
            yield return ReachReport();
            session.ContinueReport();
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.Choice));
        }

        IEnumerator EnterTrigger(PlayerTrigger2D trigger, Vector2 offset = default)
        {
            player.SetLying(false);
            player.ResetTo(playerSpawn.position);
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            player.ResetTo((Vector2)trigger.transform.position + offset);
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            if (!trigger.Inside)
                yield return new WaitForFixedUpdate();
            Assert.That(trigger.Inside, Is.True, $"Physics2D did not enter {trigger.name}.");
        }

        IEnumerator EnterSunThroughLivingRoomRoute()
        {
            var sideDoorEntries = 0;
            void CountSideDoorEntry() => sideDoorEntries++;
            sideDoor.Entered += CountSideDoorEntry;
            player.SetLying(false);

            foreach (var point in new[]
                     {
                         (Vector2)backupRetrySpawn.position, new Vector2(2.8f, -3.3f),
                         new Vector2(4f, -3f), new Vector2(5.2f, -2.8f), (Vector2)sunZone.transform.position
                     })
            {
                player.ResetTo(point);
                Physics2D.SyncTransforms();
                yield return new WaitForFixedUpdate();
                Assert.That(sideDoor.Inside, Is.False, "Living-room route crossed SideDoorTrigger.");
            }

            sideDoor.Entered -= CountSideDoorEntry;
            Assert.That(sideDoorEntries, Is.Zero);
            Assert.That(flow.BackupCameraActive, Is.False);
            Assert.That(sunZone.Inside, Is.True);
        }

        void TrackRobotContact(Collider2D other)
        {
            if (other == null)
                return;
            sawBananaContact |= other.GetComponentInParent<CarryableObject>() == banana;
            sawFeederContact |= other == feederCollider || other.transform.IsChildOf(feederCollider.transform) ||
                                feederCollider.transform.IsChildOf(other.transform);
        }

        void FinishDialogue()
        {
            for (var i = 0; i < 16 && dialogue != null && dialogue.IsPlaying; i++)
                dialogue.FinishSequence();
        }

        void CreateTestSession()
        {
            var services = new GameObject("DayTwoTestServices");
            save = services.AddComponent<SaveService>();
            dialogue = services.AddComponent<DialogueDirector>();
            session = services.AddComponent<GameSession>();
            session.Configure(null, save, null, dialogue);
            save.ClearProgress();
        }

        void BindSceneReferences()
        {
            var worldRoot = GameObject.Find("WorldRoot").transform;
            flow = UnityEngine.Object.FindAnyObjectByType<LevelTwoFlowController>();
            player = GameObject.Find("WorldRoot/Actors/Latte").GetComponent<PlayerController2D>();
            carry = player.GetComponent<CarryController>();
            banana = GameObject.Find("WorldRoot/Interactables/BananaPeel").GetComponent<CarryableObject>();
            robot = GameObject.Find("WorldRoot/Devices/RobotVacuum").GetComponent<RobotPatrol>();
            feederCamera = GameObject.Find("WorldRoot/Sensors/FeederCameraVision")
                .GetComponent<CameraVisionSensor2D>();
            backupCamera = GameObject.Find("WorldRoot/Sensors/BackupCameraVision")
                .GetComponent<CameraVisionSensor2D>();
            sunZone = GameObject.Find("WorldRoot/Triggers/SunZone").GetComponent<PlayerTrigger2D>();
            feederArea = GameObject.Find("WorldRoot/Triggers/FeederConfirmationArea").GetComponent<PlayerTrigger2D>();
            sideDoor = GameObject.Find("WorldRoot/Triggers/SideDoorTrigger").GetComponent<PlayerTrigger2D>();
            playerSpawn = GameObject.Find("WorldRoot/Triggers/PlayerSpawn").transform;
            backupRetrySpawn = GameObject.Find("WorldRoot/Triggers/BackupRetrySpawn").transform;
            sniffPoint = GameObject.Find("WorldRoot/Triggers/EndingSniffPoint").transform;
            sleepPoint = GameObject.Find("WorldRoot/Triggers/EndingSleepPoint").transform;
            feederDevice = GameObject.Find("WorldRoot/Devices/FutureFeeder");
            feederCollider = feederDevice.GetComponent<Collider2D>();
            feederOfflineDisplay = worldRoot.Find("Devices/FeederStatusText").gameObject;
            backupActiveDisplay = worldRoot.Find("Devices/BackupStatusText").gameObject;
        }
    }
}
