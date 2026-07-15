using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PetOffline.Core;
using PetOffline.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PetOffline.Tests.PlayMode
{
    public sealed class ProductionUIControllerTests
    {
        GameObject root;
        GameObject services;
        ReportDefinitionSO report;
        DialogueSequenceSO firstDialogue;
        DialogueSequenceSO secondDialogue;

        [UnityTest]
        public IEnumerator PhaseChangesSelectTheExpectedProductionPanel()
        {
            root = new GameObject("ProductionUITestRoot");
            var controller = root.AddComponent<UIRootController>();
            var views = new UIRootController.ViewBindings
            {
                titlePanel = Child("Title"),
                hudPanel = Child("HUD"),
                dialoguePanel = Child("Dialogue"),
                reportPanel = Child("Report"),
                choicePanel = Child("Choice"),
                pausePanel = Child("Pause"),
                endingPanel = Child("Ending")
            };
            controller.Configure(null, null, null, views);
            yield return null;

            Assert.That(controller.ProductionViewsReady, Is.True);
            Assert.That(views.titlePanel.activeSelf, Is.True);
            Assert.That(views.hudPanel.activeSelf, Is.False);

            var model = new FakeModel();
            controller.Bind(model);
            model.Set(LevelPhase.TaskShoes, null);
            Assert.That(views.hudPanel.activeSelf, Is.True);
            Assert.That(views.titlePanel.activeSelf, Is.False);

            report = ScriptableObject.CreateInstance<ReportDefinitionSO>();
            report.Configure("test", "日报", new[] { new ReportField("结果", "通过") });
            model.Set(LevelPhase.Report, report);
            Assert.That(views.reportPanel.activeSelf, Is.True);
            Assert.That(views.hudPanel.activeSelf, Is.False);

            model.Set(LevelPhase.Choice, report);
            Assert.That(views.choicePanel.activeSelf, Is.True);
            Assert.That(views.reportPanel.activeSelf, Is.False);

            controller.Bind(null);
            Assert.That(views.titlePanel.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator DisablingPausedUiRestoresTheWorldClock()
        {
            root = new GameObject("PausedUITestRoot");
            var controller = root.AddComponent<UIRootController>();
            controller.Bind(new FakeModel());
            typeof(UIRootController).GetField("paused", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(controller, true);
            Time.timeScale = 0f;

            root.SetActive(false);
            yield return null;

            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(controller.IsPaused, Is.False);
        }

        [UnityTest]
        public IEnumerator EndingPanelWaitsUntilQueuedEndingDialogueIsFullyDrained()
        {
            root = new GameObject("EndingQueueUITestRoot");
            services = new GameObject("EndingQueueServices");
            var dialogue = services.AddComponent<DialogueDirector>();
            var session = services.AddComponent<GameSession>();
            session.Configure(null, null, null, dialogue);
            var controller = root.AddComponent<UIRootController>();
            var views = new UIRootController.ViewBindings
            {
                titlePanel = Child("Title"),
                hudPanel = Child("HUD"),
                reportPanel = Child("Report"),
                choicePanel = Child("Choice"),
                pausePanel = Child("Pause"),
                endingPanel = Child("Ending"),
                dialoguePanel = Child("Dialogue")
            };
            controller.Configure(session, null, null, views);
            var model = new FakeModel();
            controller.Bind(model);
            model.Set(LevelPhase.End, null);
            firstDialogue = ScriptableObject.CreateInstance<DialogueSequenceSO>();
            secondDialogue = ScriptableObject.CreateInstance<DialogueSequenceSO>();
            firstDialogue.Configure("Test.PreEnding", new[] { new DialogueLine("AI", "queued first", 10f) });
            secondDialogue.Configure("Test.Ending", new[] { new DialogueLine("AI", "queued ending", 10f) });

            dialogue.Play(firstDialogue);
            dialogue.Play(secondDialogue);
            dialogue.FinishSequence();
            yield return null;
            Assert.That(views.endingPanel.activeSelf, Is.False);

            dialogue.FinishSequence();
            yield return null;
            yield return null;
            Assert.That(views.endingPanel.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator ProductionChoiceButtonsSendBothConfiguredEndingCommands()
        {
            root = new GameObject("ChoiceButtonUITestRoot");
            services = new GameObject("ChoiceButtonServices");
            var session = services.AddComponent<GameSession>();
            var controller = root.AddComponent<UIRootController>();
            var views = new UIRootController.ViewBindings
            {
                titlePanel = Child("Title"),
                hudPanel = Child("HUD"),
                reportPanel = Child("Report"),
                choicePanel = Child("Choice"),
                pausePanel = Child("Pause"),
                endingPanel = Child("Ending"),
                restoreButton = ButtonChild("RestoreButton"),
                keepQuietButton = ButtonChild("KeepQuietButton")
            };
            controller.Configure(session, null, null, views);
            var model = new FakeModel();
            controller.Bind(model);
            model.Set(LevelPhase.Choice, null);
            FinalChoice? received = null;
            session.ChoiceRequested += choice => received = choice;
            yield return null;

            views.restoreButton.onClick.Invoke();
            Assert.That(received, Is.EqualTo(FinalChoice.RestoreConnection));
            views.keepQuietButton.onClick.Invoke();
            Assert.That(received, Is.EqualTo(FinalChoice.KeepQuiet));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (report != null)
                UnityEngine.Object.DestroyImmediate(report);
            if (firstDialogue != null)
                UnityEngine.Object.DestroyImmediate(firstDialogue);
            if (secondDialogue != null)
                UnityEngine.Object.DestroyImmediate(secondDialogue);
            if (services != null)
                UnityEngine.Object.DestroyImmediate(services);
            if (root != null)
                UnityEngine.Object.DestroyImmediate(root);
            yield return null;
        }

        GameObject Child(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            return child;
        }

        Button ButtonChild(string name) => Child(name).AddComponent<Button>();

        sealed class FakeModel : ILevelViewModel
        {
            public LevelId Level => LevelId.Day1;
            public LevelPhase Phase { get; private set; } = LevelPhase.Opening;
            public string Objective => "测试目标";
            public float Progress01 => 0.5f;
            public CameraUiState CameraState => new("B", true, false);
            public ReportDefinitionSO Report { get; private set; }
            public event Action Changed;

            public void Set(LevelPhase phase, ReportDefinitionSO value)
            {
                Phase = phase;
                Report = value;
                Changed?.Invoke();
            }
        }
    }
}
