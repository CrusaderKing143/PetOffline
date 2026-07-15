using System.Collections;
using NUnit.Framework;
using PetOffline.Core;
using PetOffline.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace PetOffline.Tests.PlayMode
{
    public sealed class BootstrapSceneTests
    {
        [UnityTest]
        public IEnumerator BootstrapLoadsWithPersistentServicesAndNoWorld()
        {
            SceneManager.LoadScene(SceneNames.Bootstrap);
            yield return null;

            Assert.That(Object.FindAnyObjectByType<GameSession>(), Is.Not.Null);
            Assert.That(GameObject.Find("UIRoot"), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<UIRootController>().WaitingForLevel, Is.True);
            Assert.That(SceneManager.GetSceneByName(SceneNames.Day1).isLoaded, Is.False);
            Assert.That(SceneManager.GetSceneByName(SceneNames.Day2).isLoaded, Is.False);
        }

        [UnityTest]
        public IEnumerator UIRootTestBindsMockWithoutWorldScene()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(SceneNames.UIRootTestPath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("UIRoot test scene is intentionally disabled in release Build Settings.");
#endif
            yield return null;
            yield return null;

            var uiRoot = Object.FindAnyObjectByType<UIRootController>();
            Assert.That(uiRoot, Is.Not.Null);
            Assert.That(uiRoot.WaitingForLevel, Is.False);
            Assert.That(uiRoot.Model, Is.TypeOf<MockLevelViewModelHost>());
        }

        [UnityTest]
        public IEnumerator ConcurrentWorldRequestsKeepOnlyOneWorldLoaded()
        {
            SceneManager.LoadScene(SceneNames.Bootstrap);
            yield return null;

            var flow = Object.FindAnyObjectByType<SceneFlowService>();
            flow.LoadWorld(LevelId.Day1);
            flow.LoadWorld(LevelId.Day2);

            for (var i = 0; i < 120 && !SceneManager.GetSceneByName(SceneNames.Day1).isLoaded; i++)
                yield return null;

            Assert.That(SceneManager.GetSceneByName(SceneNames.Day1).isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByName(SceneNames.Day2).isLoaded, Is.False);

            flow.ReturnToTitle();
            for (var i = 0; i < 120 && SceneManager.GetSceneByName(SceneNames.Day1).isLoaded; i++)
                yield return null;
            Assert.That(SceneManager.GetSceneByName(SceneNames.Day1).isLoaded, Is.False);
        }

        [UnityTest]
        public IEnumerator ReturningToTitleStopsPersistentDialogueWithoutCompletingIt()
        {
            SceneManager.LoadScene(SceneNames.Bootstrap);
            yield return null;

            var session = Object.FindAnyObjectByType<GameSession>();
            var sequence = ScriptableObject.CreateInstance<DialogueSequenceSO>();
            sequence.Configure("Test.Long", new[] { new DialogueLine("AI", "still playing", 10f) });
            var completed = false;
            session.Dialogue.Play(sequence, () => completed = true);
            Assert.That(session.Dialogue.IsPlaying, Is.True);

            session.ReturnToTitle();
            yield return null;
            yield return null;

            Assert.That(session.Dialogue.IsPlaying, Is.False);
            Assert.That(completed, Is.False);
            Object.DestroyImmediate(sequence);
        }

        [UnityTest]
        public IEnumerator DialogueDirectorQueuesSequencesWithoutDroppingCompletionCallbacks()
        {
            var owner = new GameObject("DialogueQueueTest");
            var director = owner.AddComponent<DialogueDirector>();
            var first = ScriptableObject.CreateInstance<DialogueSequenceSO>();
            var second = ScriptableObject.CreateInstance<DialogueSequenceSO>();
            first.Configure("Test.First", new[] { new DialogueLine("AI", "first", 10f) });
            second.Configure("Test.Second", new[] { new DialogueLine("AI", "second", 10f) });
            var order = string.Empty;

            director.Play(first, () => order += "A");
            director.Play(second, () => order += "B");
            director.FinishSequence();
            Assert.That(order, Is.EqualTo("A"));
            Assert.That(director.IsPlaying, Is.True);
            director.FinishSequence();
            Assert.That(order, Is.EqualTo("AB"));
            Assert.That(director.IsPlaying, Is.False);

            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(owner);
            yield return null;
        }
    }
}
