using System;
using System.IO;
using System.Linq;
using PetOffline.Core;
using PetOffline.Gameplay;
using PetOffline.UI;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PetOffline.Editor
{
    public static class ProjectAutomation
    {
        const string Root = "Assets/PetOffline";
        const string InputPath = Root + "/Settings/Input/PetOffline.inputactions";

        static readonly string[] Folders =
        {
            "Art/Characters", "Art/Environment", "Art/Props", "Art/UI", "Art/VFX",
            "Audio/Music", "Audio/SFX", "Audio/Voice",
            "Data/Levels", "Data/Dialogue", "Data/Reports", "Data/Cameras", "Data/Carryables", "Data/Audio",
            "Prefabs/World", "Prefabs/UI", "Scenes", "Settings/Input", "Settings/URP", "Settings/Audio"
        };

        static readonly string[] PhysicsLayers =
        {
            "Player", "WorldStatic", "Carryable", "Robot", "Sensor", "VisionOccluder", "WorldTrigger", "WorldUI"
        };

        static readonly (string Name, int Id)[] SortingLayers =
        {
            ("Ground", 11001), ("GroundDecal", 11002), ("FurnitureBack", 11003), ("Actor", 11004),
            ("Carried", 11005), ("FurnitureFront", 11006), ("WorldFX", 11007), ("WorldUI", 11008)
        };

        [MenuItem("Tools/Pet Offline/Setup Project")]
        public static void SetupProject()
        {
            EnsureFolders();
            EnsureLayers();
            var input = EnsureInputActions();
            EnsureScenes(input);
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PetOffline] Project setup complete.");
        }

        public static void SetupBatch()
        {
            try
            {
                SetupProject();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory(Root);
            foreach (var folder in Folders)
                Directory.CreateDirectory($"{Root}/{folder}");
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "../Artifacts/TestResults")));
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "../Artifacts/Screenshots")));
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/Windows/Development")));
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/Windows/Release")));
            AssetDatabase.Refresh();
        }

        static InputActionAsset EnsureInputActions()
        {
            var existing = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (existing != null)
                return existing;

            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            actions.name = "PetOffline";

            var gameplay = actions.AddActionMap("Gameplay");
            var move = gameplay.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            AddMoveComposite(move, "<Keyboard>/w", "<Keyboard>/s", "<Keyboard>/a", "<Keyboard>/d");
            AddMoveComposite(move, "<Keyboard>/upArrow", "<Keyboard>/downArrow", "<Keyboard>/leftArrow", "<Keyboard>/rightArrow");
            gameplay.AddAction("Interact", InputActionType.Button).AddBinding("<Keyboard>/e");
            gameplay.AddAction("Bark", InputActionType.Button).AddBinding("<Keyboard>/space");
            gameplay.AddAction("Push", InputActionType.Button).AddBinding("<Keyboard>/q");
            gameplay.AddAction("Lie", InputActionType.Button).AddBinding("<Keyboard>/leftShift");
            gameplay.AddAction("Pause", InputActionType.Button).AddBinding("<Keyboard>/escape");

            var ui = actions.AddActionMap("UI");
            var navigate = ui.AddAction("Navigate", InputActionType.PassThrough, expectedControlLayout: "Vector2");
            AddMoveComposite(navigate, "<Keyboard>/w", "<Keyboard>/s", "<Keyboard>/a", "<Keyboard>/d");
            AddMoveComposite(navigate, "<Keyboard>/upArrow", "<Keyboard>/downArrow", "<Keyboard>/leftArrow", "<Keyboard>/rightArrow");
            ui.AddAction("Submit", InputActionType.Button).AddBinding("<Keyboard>/enter");
            ui.FindAction("Submit").AddBinding("<Keyboard>/space");
            ui.AddAction("Cancel", InputActionType.Button).AddBinding("<Keyboard>/escape");
            ui.AddAction("Point", InputActionType.PassThrough, "<Mouse>/position", expectedControlLayout: "Vector2");
            ui.AddAction("Click", InputActionType.PassThrough, "<Mouse>/leftButton", expectedControlLayout: "Button");

            File.WriteAllText(InputPath, actions.ToJson());
            UnityEngine.Object.DestroyImmediate(actions);
            AssetDatabase.ImportAsset(InputPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
        }

        static void AddMoveComposite(InputAction action, string up, string down, string left, string right)
        {
            action.AddCompositeBinding("2DVector")
                .With("Up", up)
                .With("Down", down)
                .With("Left", left)
                .With("Right", right);
        }

        static void EnsureScenes(InputActionAsset input)
        {
            EnsureScene(SceneNames.BootstrapPath, () => CreateBootstrap(input));
            EnsureScene(SceneNames.Day1Path, () => CreateWorld(LevelId.Day1, "把主人拖鞋叼到摄像头A前", "CM_Day1"));
            EnsureScene(SceneNames.Day2Path, () => CreateWorld(LevelId.Day2, "让拿铁晒满20秒太阳", "CM_Day2"));
            EnsureScene(SceneNames.UIRootTestPath, () => CreateUiRootTest(input));
            EnsureDayTwoRuntime();

            var required = new[]
            {
                new EditorBuildSettingsScene(SceneNames.BootstrapPath, true),
                new EditorBuildSettingsScene(SceneNames.Day1Path, true),
                new EditorBuildSettingsScene(SceneNames.Day2Path, true),
                new EditorBuildSettingsScene(SceneNames.UIRootTestPath, false)
            };
            var requiredPaths = required.Select(scene => scene.path).ToHashSet();
            EditorBuildSettings.scenes = required
                .Concat(EditorBuildSettings.scenes.Where(scene => !requiredPaths.Contains(scene.path)))
                .ToArray();
            EditorBuildSettings.AddConfigObject("com.unity.input.settings.actions", input, true);

            var bootstrap = EditorSceneManager.OpenScene(SceneNames.BootstrapPath, OpenSceneMode.Single);
            RefreshBootstrapReferences(input);
            EditorSceneManager.MarkSceneDirty(bootstrap);
            EditorSceneManager.SaveScene(bootstrap);
        }

        static void RefreshBootstrapReferences(InputActionAsset input)
        {
            var gameSession = UnityEngine.Object.FindObjectsByType<GameSession>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).FirstOrDefault();
            var sceneFlow = UnityEngine.Object.FindObjectsByType<SceneFlowService>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).FirstOrDefault();
            var inputRouter = UnityEngine.Object.FindObjectsByType<InputRouter>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).FirstOrDefault();
            var save = UnityEngine.Object.FindObjectsByType<SaveService>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).FirstOrDefault();
            var dialogue = UnityEngine.Object.FindObjectsByType<DialogueDirector>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).FirstOrDefault();
            if (gameSession == null || sceneFlow == null || inputRouter == null || save == null || dialogue == null)
                throw new InvalidOperationException("Bootstrap services are incomplete.");

            inputRouter.Configure(input);
            gameSession.Configure(sceneFlow, save, inputRouter, dialogue);
        }

        static void EnsureScene(string path, Action create)
        {
            if (File.Exists(path))
                return;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            create();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path);
        }

        static void EnsureDayTwoRuntime()
        {
            var scene = EditorSceneManager.OpenScene(SceneNames.Day2Path, OpenSceneMode.Single);
            var context = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<LevelSceneContext>(true))
                .SingleOrDefault();
            if (context == null)
                throw new InvalidOperationException("Day 2 must contain exactly one LevelSceneContext.");

            var changed = false;
            var dayTwo = context.GetComponent<LevelTwoFlowController>();
            foreach (var flow in context.GetComponents<LevelFlowController>())
            {
                if (flow == dayTwo)
                    continue;
                UnityEngine.Object.DestroyImmediate(flow);
                changed = true;
            }

            if (dayTwo == null)
            {
                dayTwo = context.gameObject.AddComponent<LevelTwoFlowController>();
                changed = true;
            }

            if (dayTwo.Context != context)
            {
                dayTwo.Configure(context);
                changed = true;
            }

            if (!changed)
                return;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void CreateBootstrap(InputActionAsset input)
        {
            var app = new GameObject("App");
            var gameSession = CreateComponentChild<GameSession>(app.transform, "GameSession");
            var sceneFlow = CreateComponentChild<SceneFlowService>(app.transform, "SceneFlowService");
            var inputRouter = CreateComponentChild<InputRouter>(app.transform, "InputRouter");
            var audio = CreateComponentChild<AudioService>(app.transform, "AudioService");
            var save = CreateComponentChild<SaveService>(app.transform, "SaveService");
            var dialogue = CreateComponentChild<DialogueDirector>(app.transform, "DialogueDirector");
            inputRouter.Configure(input);
            audio.SetMasterVolume(save.MasterVolume);
            gameSession.Configure(sceneFlow, save, inputRouter, dialogue);

            var cameras = new GameObject("Cameras");
            var mainCamera = new GameObject("Main Camera");
            mainCamera.transform.SetParent(cameras.transform);
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            mainCamera.tag = "MainCamera";
            var camera = mainCamera.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
            mainCamera.AddComponent<AudioListener>();
            mainCamera.AddComponent<CinemachineBrain>();

            CreateUiRoot(gameSession, input);
        }

        static void CreateWorld(LevelId level, string objective, string cameraName)
        {
            var root = new GameObject("WorldRoot");
            foreach (var name in new[]
                     {
                         "Environment", "Collision", "Actors", "Interactables", "Devices", "Sensors", "Triggers",
                         "Paths", "WorldVFX", "WorldAudio"
                     })
                CreateChild(root.transform, name);

            var flowRoot = CreateChild(root.transform, "LevelFlow");
            var context = flowRoot.AddComponent<LevelSceneContext>();
            context.Configure(level, objective);
            LevelFlowController flow = level == LevelId.Day2
                ? flowRoot.AddComponent<LevelTwoFlowController>()
                : flowRoot.AddComponent<LevelFlowController>();
            flow.Configure(context);

            var virtualCameraRoot = CreateChild(root.transform, "VirtualCamera");
            var cameraObject = CreateChild(virtualCameraRoot.transform, cameraName);
            var virtualCamera = cameraObject.AddComponent<CinemachineCamera>();
            virtualCamera.Priority = 10;
            virtualCamera.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            virtualCamera.Lens.OrthographicSize = 7f;
        }

        static void CreateUiRootTest(InputActionAsset input)
        {
            var previewCamera = new GameObject("PreviewCamera");
            previewCamera.transform.position = new Vector3(0f, 0f, -10f);
            previewCamera.AddComponent<Camera>().orthographic = true;
            previewCamera.AddComponent<AudioListener>();

            var uiRoot = CreateUiRoot(null, input);
            var mock = new GameObject("MockLevelViewModelHost").AddComponent<MockLevelViewModelHost>();
            mock.Configure(uiRoot);
        }

        static UIRootController CreateUiRoot(GameSession session, InputActionAsset input)
        {
            var root = new GameObject("UIRoot");
            var controller = root.AddComponent<UIRootController>();

            CreateCanvas(root.transform, "Canvas_HUD", 0);
            CreateCanvas(root.transform, "Canvas_Overlay", 100);

            var eventSystemObject = CreateChild(root.transform, "EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            var inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            controller.Configure(session, input, inputModule);
            return controller;
        }

        static void CreateCanvas(Transform parent, string name, int sortingOrder)
        {
            var canvasObject = CreateChild(parent, name);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        static T CreateComponentChild<T>(Transform parent, string name) where T : Component
            => CreateChild(parent, name).AddComponent<T>();

        static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.productName = "Pet Offline";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = false;
        }

        static void EnsureLayers()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets.Length == 0)
                throw new InvalidOperationException("TagManager.asset not found.");

            var manager = new SerializedObject(assets[0]);
            var layers = manager.FindProperty("layers");
            foreach (var layerName in PhysicsLayers)
            {
                if (Contains(layers, layerName))
                    continue;

                for (var i = 8; i < layers.arraySize; i++)
                {
                    var layer = layers.GetArrayElementAtIndex(i);
                    if (!string.IsNullOrEmpty(layer.stringValue))
                        continue;
                    layer.stringValue = layerName;
                    break;
                }
            }

            var sortingLayers = manager.FindProperty("m_SortingLayers");
            foreach (var sortingLayer in SortingLayers)
            {
                var index = FindSortingLayer(sortingLayers, sortingLayer.Name);
                if (index < 0)
                {
                    sortingLayers.InsertArrayElementAtIndex(sortingLayers.arraySize);
                    index = sortingLayers.arraySize - 1;
                }

                var element = sortingLayers.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("name").stringValue = sortingLayer.Name;
                element.FindPropertyRelative("uniqueID").intValue = sortingLayer.Id;
                element.FindPropertyRelative("locked").boolValue = false;
            }

            manager.ApplyModifiedPropertiesWithoutUndo();
        }

        static bool Contains(SerializedProperty array, string value)
        {
            for (var i = 0; i < array.arraySize; i++)
                if (array.GetArrayElementAtIndex(i).stringValue == value)
                    return true;
            return false;
        }

        static int FindSortingLayer(SerializedProperty array, string value)
        {
            for (var i = 0; i < array.arraySize; i++)
                if (array.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == value)
                    return i;
            return -1;
        }
    }
}
