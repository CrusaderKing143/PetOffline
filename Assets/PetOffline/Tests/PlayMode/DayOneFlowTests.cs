using System.Collections;
using NUnit.Framework;
using PetOffline.Core;
using PetOffline.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PetOffline.Tests.PlayMode
{
    public sealed class DayOneFlowTests
    {
        LevelOneFlowController flow;
        PlayerController2D player;
        CarryController carry;
        CarryableObject slipper;
        CarryableObject pillow;
        CarryGoalZone2D shoeGoal;
        CarryGoalZone2D bedGoal;
        CameraVisionSensor2D cameraB;
        RobotPatrol robot;
        Keyboard addedKeyboard;
        Keyboard queuedKeyboard;
        InputActionAsset restrictedActions;
        InputSettings.BackgroundBehavior previousBackgroundBehavior;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior;
#endif

        [UnitySetUp]
        public IEnumerator LoadDayOne()
        {
            Time.timeScale = 1f;
            previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            SceneManager.LoadScene(SceneNames.Day1);
            yield return null;
            BindSceneReferences();
            flow.Bind(null);
            Assert.That(flow.OpeningReady, Is.True);
            player.Bark();
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.TaskShoes));
        }

        [UnityTearDown]
        public IEnumerator ResetTestState()
        {
            Time.timeScale = 1f;
            InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior;
#endif
            if (queuedKeyboard != null && queuedKeyboard.added)
                InputSystem.QueueStateEvent(queuedKeyboard, new KeyboardState());
            yield return null;
            if (restrictedActions != null)
                restrictedActions.devices = null;
            if (addedKeyboard != null && addedKeyboard.added)
                InputSystem.RemoveDevice(addedKeyboard);
            addedKeyboard = null;
            queuedKeyboard = null;
            restrictedActions = null;
            Object.FindAnyObjectByType<SaveService>()?.ClearProgress();
        }

        [Test]
        public void ShoeGoalRequiresTwoSeconds()
        {
            DropInGoal(slipper, shoeGoal);
            shoeGoal.Tick(1.99f);
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.TaskShoes));
            Assert.That(shoeGoal.Progress01, Is.LessThan(1f));

            shoeGoal.Tick(0.02f);
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.TaskPillow));
            Assert.That(flow.ShoesCompleted, Is.True);
            Assert.That(slipper.IsAvailable, Is.False);
            Assert.That(pillow.IsAvailable, Is.True);
        }

        [Test]
        public void CameraDetectionResetsOnlyTheCurrentShoeTask()
        {
            Assert.That(carry.TryPickup(slipper), Is.True);
            PutPlayerInCameraCone();
            cameraB.Tick(0.35f);

            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.TaskShoes));
            Assert.That(flow.ShoesResetCount, Is.EqualTo(1));
            Assert.That(flow.PillowResetCount, Is.Zero);
            Assert.That(carry.Held, Is.Null);
            Assert.That(Vector2.Distance(slipper.transform.position, new Vector2(-5.35f, -3.8f)),
                Is.LessThan(0.01f));
        }

        [Test]
        public void PillowDetectionPreservesCompletedShoeTask()
        {
            CompleteShoes();
            Assert.That(carry.TryPickup(pillow), Is.True);
            PutPlayerInCameraCone();
            cameraB.Tick(0.35f);

            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.TaskPillow));
            Assert.That(flow.ShoesCompleted, Is.True);
            Assert.That(flow.PillowCompleted, Is.False);
            Assert.That(flow.ShoesResetCount, Is.Zero);
            Assert.That(flow.PillowResetCount, Is.EqualTo(1));
            Assert.That(slipper.IsAvailable, Is.False);
            Assert.That(Vector2.Distance(pillow.transform.position, new Vector2(2.8f, 1.6f)),
                Is.LessThan(0.01f));
        }

        [Test]
        public void CameraDetectionDoesNotCancelAnActiveBossCall()
        {
            flow.Tick(14.01f);
            Assert.That(flow.BossCallActive, Is.True);
            var remaining = flow.BossCallRemaining;

            Assert.That(carry.TryPickup(slipper), Is.True);
            PutPlayerInCameraCone();
            cameraB.Tick(0.35f);

            Assert.That(flow.ShoesResetCount, Is.EqualTo(1));
            Assert.That(flow.BossCallActive, Is.True);
            Assert.That(flow.BossCallRemaining, Is.EqualTo(remaining).Within(0.001f));
        }

        [Test]
        public void HeavyPillowDropsOnBarkAndRobotPushesItBeforeCompletion()
        {
            CompleteShoes();
            Assert.That(carry.TryPickup(pillow), Is.True);
            player.Bark();
            Assert.That(carry.Held, Is.Null);
            Assert.That(pillow.IsCarried, Is.False);

            pillow.ResetTo(robot.Body.position + Vector2.right * 0.1f);
            var beforePush = pillow.Body.position;
            Assert.That(robot.TryPush(pillow.Collider), Is.True);
            Assert.That(Vector2.Distance(beforePush, pillow.Body.position), Is.EqualTo(0.65f).Within(0.01f));

            pillow.ResetTo(bedGoal.transform.position);
            bedGoal.NotifyEnter(pillow);
            Assert.That(flow.PillowCompleted, Is.True);
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.FinalBark));
        }

        [Test]
        public void BossCallSuccessCreatesSafetyAndTimeoutCreatesAlertWithoutFailure()
        {
            flow.Tick(14.01f);
            Assert.That(flow.BossCallActive, Is.True);
            player.Bark();
            Assert.That(flow.SuccessfulBossCalls, Is.EqualTo(1));
            Assert.That(flow.SafeWindowActive, Is.True);
            Assert.That(cameraB.DetectionSuppressed, Is.True);
            var frozenAngle = cameraB.CurrentAngle;
            cameraB.Tick(1f);
            Assert.That(cameraB.CurrentAngle, Is.EqualTo(frozenAngle).Within(0.001f));

            flow.Tick(3.01f);
            Assert.That(flow.SafeWindowActive, Is.False);
            cameraB.Tick(0.1f);
            Assert.That(cameraB.CurrentAngle, Is.Not.EqualTo(frozenAngle));
            flow.Tick(23.1f);
            Assert.That(flow.BossCallActive, Is.True);
            flow.Tick(3.61f);

            Assert.That(flow.MissedBossCalls, Is.EqualTo(1));
            Assert.That(flow.AlertActive, Is.True);
            Assert.That(cameraB.IsAlert, Is.True);
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.TaskShoes));
            flow.Tick(7.21f);
            Assert.That(flow.AlertActive, Is.False);
            Assert.That(cameraB.IsAlert, Is.False);
        }

        [UnityTest]
        public IEnumerator DisabledUIRootDoesNotBlockWorldOrDayTwoTransition()
        {
            SceneManager.LoadScene(SceneNames.Bootstrap);
            yield return null;

            var session = Object.FindAnyObjectByType<GameSession>();
            var uiRoot = GameObject.Find("UIRoot");
            Assert.That(session, Is.Not.Null);
            Assert.That(uiRoot, Is.Not.Null);
            session.Save.ClearProgress();
            uiRoot.SetActive(false);
            session.StartNewGame();

            for (var i = 0; i < 180; i++)
            {
                flow = Object.FindAnyObjectByType<LevelOneFlowController>();
                if (flow != null && flow.IsBound)
                    break;
                yield return null;
            }

            Assert.That(flow, Is.Not.Null);
            Assert.That(flow.IsBound, Is.True);
            BindSceneReferences();
            session.Dialogue.FinishSequence();
            Assert.That(flow.OpeningReady, Is.True);
            player.Bark();
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.TaskShoes));
            Assert.That(session.Input.Gameplay.enabled, Is.True);

            var playerBefore = player.Position;
            var keyboard = addedKeyboard = InputSystem.AddDevice<Keyboard>();
            queuedKeyboard = keyboard;
            restrictedActions = session.Input.Actions;
            session.Input.Gameplay.Disable();
            restrictedActions.devices = new InputDevice[] { keyboard };
            session.Input.Gameplay.Enable();
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();
            yield return null;
            Assert.That(player.InputEnabled, Is.True);
            Assert.That(player.MovementEnabled, Is.True);
            Assert.That(session.Input.Gameplay.FindAction("Move").ReadValue<Vector2>().x, Is.GreaterThan(0.5f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Assert.That(player.Position, Is.Not.EqualTo(playerBefore));

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Assert.That(carry.Held, Is.EqualTo(slipper));

            var slideFacing = player.Facing;
            var pushCount = 0;
            player.PushPressed += () => pushCount++;
            player.Slide(Vector2.up, 1.6f, 1f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift, Key.E, Key.Q));
            yield return null;
            Assert.That(player.Facing, Is.EqualTo(slideFacing));
            Assert.That(player.IsLying, Is.False);
            Assert.That(carry.Held, Is.EqualTo(slipper));
            Assert.That(pushCount, Is.Zero);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            player.CancelSlide();

            var cameraAngle = cameraB.CurrentAngle;
            var robotBefore = robot.Body.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(cameraB.CurrentAngle, Is.Not.EqualTo(cameraAngle));
            Assert.That(robot.Body.position, Is.Not.EqualTo(robotBefore));

            carry.ForceDrop(shoeGoal.transform.position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(shoeGoal.IsInside, Is.True);
            shoeGoal.Tick(2.01f);
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.TaskPillow));
            Assert.That(carry.TryPickup(pillow), Is.True);
            carry.ForceDrop(bedGoal.transform.position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.FinalBark));
            player.Bark();
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.Report));
            Assert.That(flow.Context.Report, Is.Not.Null);

            var speakerPoint = GameObject.Find("WorldRoot/Triggers/EndingSpeakerPoint").transform;
            var bedPoint = GameObject.Find("WorldRoot/Triggers/EndingBedPoint").transform;
            player.ResetTo(speakerPoint.position);
            Time.timeScale = 50f;
            session.ContinueReport();
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.Ending));
            player.ResetTo(bedPoint.position);

            for (var i = 0; i < 240 && !SceneManager.GetSceneByName(SceneNames.Day2).isLoaded; i++)
                yield return null;

            Assert.That(session.Save.DayOneCompleted, Is.True);
            Assert.That(SceneManager.GetSceneByName(SceneNames.Day1).isLoaded, Is.False);
            Assert.That(SceneManager.GetSceneByName(SceneNames.Day2).isLoaded, Is.True);
            var dayTwo = Object.FindAnyObjectByType<LevelTwoFlowController>();
            Assert.That(dayTwo, Is.Not.Null);
            Assert.That(dayTwo.IsBound, Is.True);
            Assert.That(uiRoot.activeSelf, Is.False);
        }

        void BindSceneReferences()
        {
            flow = Object.FindAnyObjectByType<LevelOneFlowController>();
            player = GameObject.Find("WorldRoot/Actors/Latte").GetComponent<PlayerController2D>();
            carry = player.GetComponent<CarryController>();
            slipper = GameObject.Find("WorldRoot/Interactables/OwnerSlipper").GetComponent<CarryableObject>();
            pillow = GameObject.Find("WorldRoot/Interactables/BossPillow").GetComponent<CarryableObject>();
            shoeGoal = GameObject.Find("WorldRoot/Triggers/CameraAGoalArea").GetComponent<CarryGoalZone2D>();
            bedGoal = GameObject.Find("WorldRoot/Triggers/DogBedGoalArea").GetComponent<CarryGoalZone2D>();
            cameraB = GameObject.Find("WorldRoot/Sensors/CameraBVision").GetComponent<CameraVisionSensor2D>();
            robot = GameObject.Find("WorldRoot/Devices/RobotVacuum").GetComponent<RobotPatrol>();
        }

        void CompleteShoes()
        {
            DropInGoal(slipper, shoeGoal);
            shoeGoal.Tick(2.01f);
            Assert.That(flow.Phase, Is.EqualTo(LevelPhase.TaskPillow));
        }

        void DropInGoal(CarryableObject item, CarryGoalZone2D goal)
        {
            Assert.That(carry.TryPickup(item), Is.True);
            carry.ForceDrop(goal.transform.position);
            goal.NotifyEnter(item);
        }

        void PutPlayerInCameraCone()
        {
            cameraB.ResetScan();
            var pivot = GameObject.Find("WorldRoot/Devices/CameraB/ScanPivot").transform;
            player.ResetTo((Vector2)pivot.position + (Vector2)pivot.right * 2f);
        }
    }
}
