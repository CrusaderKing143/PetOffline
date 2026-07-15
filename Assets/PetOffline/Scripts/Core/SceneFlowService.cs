using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PetOffline.Core
{
    public sealed class SceneFlowService : MonoBehaviour
    {
        GameSession session;
        string loadedWorldScene;
        Coroutine transition;

        public void Configure(GameSession owner) => session = owner;

        public void LoadWorld(LevelId level)
        {
            var sceneName = SceneNames.GetWorldScene(level);
            if (string.IsNullOrEmpty(sceneName) || transition != null)
                return;

            transition = StartCoroutine(LoadWorldRoutine(sceneName));
        }

        public void ReturnToTitle()
        {
            if (transition == null)
                transition = StartCoroutine(ReturnToTitleRoutine());
        }

        IEnumerator LoadWorldRoutine(string sceneName)
        {
            yield return UnloadWorldScene();

            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError($"Unable to load world scene {sceneName}.");
                transition = null;
                yield break;
            }

            yield return operation;
            loadedWorldScene = sceneName;
            var scene = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(scene);
            session?.BindLevel(FindViewModel(scene));
            transition = null;
        }

        IEnumerator ReturnToTitleRoutine()
        {
            yield return UnloadWorldScene();
            transition = null;
        }

        IEnumerator UnloadWorldScene()
        {
            session?.BindLevel(null);
            if (!string.IsNullOrEmpty(loadedWorldScene) && SceneManager.GetSceneByName(loadedWorldScene).isLoaded)
                yield return SceneManager.UnloadSceneAsync(loadedWorldScene);

            loadedWorldScene = null;
            var bootstrap = SceneManager.GetSceneByName(SceneNames.Bootstrap);
            if (bootstrap.isLoaded)
                SceneManager.SetActiveScene(bootstrap);
        }

        static ILevelViewModel FindViewModel(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour is ILevelViewModel model)
                    return model;

            Debug.LogError($"Scene {scene.name} has no ILevelViewModel.");
            return null;
        }
    }
}
