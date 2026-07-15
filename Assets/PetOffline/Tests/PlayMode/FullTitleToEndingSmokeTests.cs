using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PetOffline.Core;
using PetOffline.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PetOffline.Tests.PlayMode
{
    public sealed class FullTitleToEndingSmokeTests
    {
        const string ScreenshotEnvironmentVariable = "PETOFFLINE_CAPTURE_SCREENSHOTS";
        const string ScreenshotDirectoryEnvironmentVariable = "PETOFFLINE_SCREENSHOT_DIR";
        const string KeepSaveEnvironmentVariable = "PETOFFLINE_KEEP_SAVE";
        const string RequireExistingSaveEnvironmentVariable = "PETOFFLINE_REQUIRE_EXISTING_SAVE";

        GameSession session;

        [UnityTest]
        public IEnumerator ProductionTitleButtonReachesTheKeepQuietEndingAcrossBothDays()
        {
            Time.timeScale = 1f;
            PrepareScreenshotCapture();
            SceneManager.LoadScene(SceneNames.Bootstrap);
            yield return null;
            session = Object.FindAnyObjectByType<GameSession>();
            Assert.That(session, Is.Not.Null);
            session.Save.ClearProgress();
            yield return CaptureScreenshot("Title.png");

            FindActiveButton("NewGameButton").onClick.Invoke();
            var dayOne = default(LevelOneFlowController);
            for (var i = 0; i < 300 && (dayOne == null || !dayOne.IsBound); i++)
            {
                dayOne = Object.FindAnyObjectByType<LevelOneFlowController>();
                yield return null;
            }
            Assert.That(dayOne, Is.Not.Null);
            yield return CaptureScreenshot("Day1_Opening.png");
            DrainDialogue(session.Dialogue);

            var player = GameObject.Find("WorldRoot/Actors/Latte").GetComponent<PlayerController2D>();
            var carry = player.GetComponent<CarryController>();
            var slipper = GameObject.Find("WorldRoot/Interactables/OwnerSlipper").GetComponent<CarryableObject>();
            var pillow = GameObject.Find("WorldRoot/Interactables/BossPillow").GetComponent<CarryableObject>();
            var shoeGoal = GameObject.Find("WorldRoot/Triggers/CameraAGoalArea").GetComponent<CarryGoalZone2D>();
            var bedGoal = GameObject.Find("WorldRoot/Triggers/DogBedGoalArea").GetComponent<CarryGoalZone2D>();
            player.Bark();
            Assert.That(carry.TryPickup(slipper), Is.True);
            carry.ForceDrop(shoeGoal.transform.position);
            shoeGoal.NotifyEnter(slipper);
            shoeGoal.Tick(2.01f);
            Assert.That(carry.TryPickup(pillow), Is.True);
            carry.ForceDrop(bedGoal.transform.position);
            bedGoal.NotifyEnter(pillow);
            player.Bark();
            Assert.That(dayOne.Phase, Is.EqualTo(LevelPhase.Report));
            yield return CaptureScreenshot("Day1_Report.png", 4.6f);

            Time.timeScale = 30f;
            FindActiveButton("ContinueButton").onClick.Invoke();
            Assert.That(dayOne.Phase, Is.EqualTo(LevelPhase.Ending));
            var dayTwo = default(LevelTwoFlowController);
            for (var i = 0; i < 1200 && (dayTwo == null || !dayTwo.IsBound); i++)
            {
                dayTwo = Object.FindAnyObjectByType<LevelTwoFlowController>();
                yield return new WaitForFixedUpdate();
            }
            Assert.That(dayTwo, Is.Not.Null);
            Assert.That(session.Save.DayOneCompleted, Is.True);
            Time.timeScale = 1f;
            yield return ReachDayTwoChoice(dayTwo, true);
            FindActiveButton("KeepQuietButton").onClick.Invoke();
            Assert.That(dayTwo.Phase, Is.EqualTo(LevelPhase.End));

            for (var i = 0; i < 200 && !dayTwo.EndingComplete; i++)
            {
                dayTwo.Tick(0.25f);
                yield return new WaitForFixedUpdate();
            }
            Assert.That(dayTwo.EndingComplete, Is.True);
            DrainDialogue(session.Dialogue);
            yield return null;
            yield return null;

            Assert.That(session.Save.DayTwoCompleted, Is.True);
            Assert.That(session.Save.LastChoice, Is.EqualTo(FinalChoice.KeepQuiet));
            Assert.That(dayTwo.FinalSubtitle, Is.EqualTo(LevelTwoFlowController.KeepQuietSubtitle));
            Assert.That(FindObject("EndingPanel").activeInHierarchy, Is.True);
            yield return CaptureScreenshot("Ending_KeepQuiet.png");

            FindActiveButton("ReturnTitleButton").onClick.Invoke();
            for (var i = 0; i < 300 && session.CurrentLevel != null; i++)
                yield return null;
            Assert.That(session.CurrentLevel, Is.Null);
            Assert.That(FindObject("TitlePanel").activeInHierarchy, Is.True);
        }

        [UnityTest]
        public IEnumerator ProductionSavedContinueReachesRestoreEndingAndVisibleRestartLoadsDayOne()
        {
            Time.timeScale = 1f;
            PrepareScreenshotCapture();
            if (System.Environment.GetEnvironmentVariable(RequireExistingSaveEnvironmentVariable) != "1")
            {
                var saveSeed = new GameObject("SavedGameSetup").AddComponent<SaveService>();
                saveSeed.ClearProgress();
                saveSeed.MarkDayOneCompleted();
                Object.DestroyImmediate(saveSeed.gameObject);
            }

            SceneManager.LoadScene(SceneNames.Bootstrap);
            yield return null;
            session = Object.FindAnyObjectByType<GameSession>();
            Assert.That(session, Is.Not.Null);
            Assert.That(session.Save.DayOneCompleted, Is.True);

            var continueButton = FindActiveButton("ContinueButton");
            Assert.That(continueButton.interactable, Is.True);
            continueButton.onClick.Invoke();

            var dayTwo = default(LevelTwoFlowController);
            for (var i = 0; i < 300 && (dayTwo == null || !dayTwo.IsBound); i++)
            {
                dayTwo = Object.FindAnyObjectByType<LevelTwoFlowController>();
                yield return null;
            }
            Assert.That(dayTwo, Is.Not.Null);
            Assert.That(dayTwo.IsBound, Is.True);

            yield return ReachDayTwoChoice(dayTwo, false);
            var restoreButton = FindActiveButton("RestoreButton");
            Assert.That(restoreButton.interactable, Is.True);
            restoreButton.onClick.Invoke();
            Assert.That(dayTwo.Phase, Is.EqualTo(LevelPhase.End));
            Assert.That(dayTwo.RestoreLoopActive, Is.True);

            var player = GameObject.Find("WorldRoot/Actors/Latte").GetComponent<PlayerController2D>();
            var sunZone = GameObject.Find("WorldRoot/Triggers/SunZone").GetComponent<PlayerTrigger2D>();
            sunZone.SetInside(true);
            player.SetLying(true);
            dayTwo.Tick(10.01f);
            Assert.That(dayTwo.RestoreConfirmationTriggered, Is.True);
            Assert.That(dayTwo.EndingComplete, Is.False);

            DrainDialogue(session.Dialogue);
            yield return null;
            yield return null;
            Assert.That(dayTwo.EndingComplete, Is.True);
            Assert.That(session.Save.DayTwoCompleted, Is.True);
            Assert.That(session.Save.LastChoice, Is.EqualTo(FinalChoice.RestoreConnection));
            Assert.That(FindObject("EndingPanel").activeInHierarchy, Is.True);
            yield return CaptureScreenshot("Ending_Restore.png");

            var restartButton = FindActiveButton("RestartButton");
            Assert.That(restartButton.interactable, Is.True);
            restartButton.onClick.Invoke();
            var dayOne = default(LevelOneFlowController);
            for (var i = 0; i < 300 && (dayOne == null || !dayOne.IsBound); i++)
            {
                dayOne = Object.FindAnyObjectByType<LevelOneFlowController>();
                yield return null;
            }
            Assert.That(dayOne, Is.Not.Null);
            Assert.That(dayOne.IsBound, Is.True);
        }

        IEnumerator ReachDayTwoChoice(LevelTwoFlowController dayTwo, bool captureJourney)
        {
            DrainDialogue(session.Dialogue);
            var player = GameObject.Find("WorldRoot/Actors/Latte").GetComponent<PlayerController2D>();
            var carry = player.GetComponent<CarryController>();
            var banana = GameObject.Find("WorldRoot/Interactables/BananaPeel").GetComponent<CarryableObject>();
            var sunZone = GameObject.Find("WorldRoot/Triggers/SunZone").GetComponent<PlayerTrigger2D>();
            var feederArea = GameObject.Find("WorldRoot/Triggers/FeederConfirmationArea").GetComponent<PlayerTrigger2D>();
            var sideDoor = GameObject.Find("WorldRoot/Triggers/SideDoorTrigger").GetComponent<PlayerTrigger2D>();

            sunZone.SetInside(true);
            player.SetLying(true);
            dayTwo.Tick(10.01f);
            feederArea.SetInside(true);
            DrainDialogue(session.Dialogue);
            Assert.That(dayTwo.Phase, Is.EqualTo(LevelPhase.DestroyCamera));

            Assert.That(carry.TryPickup(banana), Is.True);
            carry.ForceDrop(new Vector2(0.85f, -1.45f));
            Physics2D.SyncTransforms();
            for (var i = 0; i < 400 && dayTwo.FoodCameraActive; i++)
                yield return new WaitForFixedUpdate();
            Assert.That(dayTwo.FoodCameraActive, Is.False);
            if (captureJourney)
                yield return CaptureScreenshot("Day2_CameraOffline.png");
            DrainDialogue(session.Dialogue);

            sideDoor.SetInside(true);
            if (captureJourney)
                yield return CaptureScreenshot("Day2_BackupActive.png");
            DrainDialogue(session.Dialogue);
            sunZone.SetInside(true);
            player.SetLying(true);
            dayTwo.Tick(10.01f);
            DrainDialogue(session.Dialogue);
            Assert.That(dayTwo.Phase, Is.EqualTo(LevelPhase.FinalSun));

            player.ResetTo(sunZone.transform.position);
            Physics2D.SyncTransforms();
            sunZone.SetInside(true);
            player.SetLying(true);
            dayTwo.Tick(20.01f);
            Assert.That(dayTwo.Phase, Is.EqualTo(LevelPhase.Report));
            DrainDialogue(session.Dialogue);
            if (captureJourney)
                yield return CaptureScreenshot("Day2_Report.png");
            FindActiveButton("ContinueButton").onClick.Invoke();
            Assert.That(dayTwo.Phase, Is.EqualTo(LevelPhase.Choice));
            if (captureJourney)
                yield return CaptureScreenshot("Day2_Choice.png");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (System.Environment.GetEnvironmentVariable(KeepSaveEnvironmentVariable) != "1")
                session?.Save?.ClearProgress();
            yield return null;
        }

        static Button FindActiveButton(string name) =>
            Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(button => button.name == name && button.gameObject.activeInHierarchy);

        static GameObject FindObject(string name) =>
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(transform => transform.name == name).gameObject;

        static bool ScreenshotsRequested =>
            System.Environment.GetEnvironmentVariable(ScreenshotEnvironmentVariable) == "1";

        static string ScreenshotDirectory
        {
            get
            {
                var configured = System.Environment.GetEnvironmentVariable(
                    ScreenshotDirectoryEnvironmentVariable);
                return !string.IsNullOrWhiteSpace(configured)
                    ? Path.GetFullPath(configured)
                    : Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Artifacts", "Screenshots"));
            }
        }

        void PrepareScreenshotCapture()
        {
            if (!ScreenshotsRequested)
                return;
            Directory.CreateDirectory(Path.Combine(ScreenshotDirectory, "Native"));
            Screen.SetResolution(1920, 1080, false);
        }

        IEnumerator CaptureScreenshot(string fileName, float settleSeconds = 0f)
        {
            if (!ScreenshotsRequested)
                yield break;

            if (settleSeconds > 0f)
                yield return new WaitForSecondsRealtime(settleSeconds);
            yield return null;
            yield return new WaitForEndOfFrame();

            var path = Path.Combine(ScreenshotDirectory, "Native", fileName);
            if (File.Exists(path))
                File.Delete(path);
            ScreenCapture.CaptureScreenshot(path);
            var deadline = Time.realtimeSinceStartup + 10f;
            while ((!File.Exists(path) || new FileInfo(path).Length == 0) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(File.Exists(path) && new FileInfo(path).Length > 0, Is.True,
                $"Screenshot was not written within 10 seconds: {path}");
        }

        static void DrainDialogue(DialogueDirector dialogue)
        {
            for (var i = 0; i < 24 && dialogue != null && dialogue.IsPlaying; i++)
                dialogue.FinishSequence();
        }
    }
}
