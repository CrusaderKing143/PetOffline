using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PetOffline.Core;
using PetOffline.Gameplay;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PetOffline.Editor
{
    public static class ProjectValidator
    {
        static readonly string[] RequiredScenes =
        {
            SceneNames.BootstrapPath,
            SceneNames.Day1Path,
            SceneNames.Day2Path,
            SceneNames.UIRootTestPath
        };

        static readonly string[] RequiredWorldContainers =
        {
            "Environment", "Collision", "Actors", "Interactables", "Devices", "Sensors", "Triggers", "Paths",
            "WorldVFX", "WorldAudio", "LevelFlow", "VirtualCamera"
        };

        static readonly string[] RequiredPhysicsLayers =
        {
            "Player", "WorldStatic", "Carryable", "Robot", "Sensor", "VisionOccluder", "WorldTrigger", "WorldUI"
        };

        static readonly string[] RequiredSortingLayers =
        {
            "Ground", "GroundDecal", "FurnitureBack", "Actor", "Carried", "FurnitureFront", "WorldFX", "WorldUI"
        };

        static readonly string[] RequiredAudioCues =
        {
            "Audio_Bark.asset", "Audio_Robot.asset", "Audio_CameraAlert.asset", "Audio_FeederOffline.asset",
            "Audio_UIConfirm.asset", "Audio_UIReport.asset", "Audio_Ambience.asset"
        };

        static readonly HashSet<string> ForbiddenBelowCanvas = new()
        {
            "PlayerController2D", "CarryController", "CameraVisionSensor2D", "RobotPatrol", "LevelFlowController"
        };

        [MenuItem("Tools/Pet Offline/Validate Project")]
        public static void ValidateProject()
        {
            var errors = CollectErrors();
            WriteReport(errors);
            if (errors.Count == 0)
            {
                Debug.Log("[PetOffline] Project validation passed.");
                return;
            }

            foreach (var error in errors)
                Debug.LogError("[PetOffline] " + error);
        }

        public static void ValidateBatch()
        {
            try
            {
                var errors = CollectErrors();
                WriteReport(errors);
                if (errors.Count > 0)
                    throw new BuildFailedException(string.Join(Environment.NewLine, errors));
                Debug.Log("[PetOffline] Project validation passed.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static List<string> CollectErrors()
        {
            var errors = new List<string>();
            if (Application.unityVersion != "6000.3.14f1")
                errors.Add($"Expected Unity 6000.3.14f1, got {Application.unityVersion}.");

            ValidateAssemblyBoundaries(errors);
            ValidateBuildSettings(errors);
            ValidateLayers(errors);
            ValidateDayOneAssets(errors);
            ValidateDayTwoAssets(errors);
            ValidateAudioAssets(errors);
            ValidateScenes(errors);
            return errors;
        }

        static void ValidateDayOneAssets(List<string> errors)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelConfigSO>(
                "Assets/PetOffline/Data/Levels/Level_Day1.asset");
            if (level == null)
            {
                errors.Add("Missing Day 1 LevelConfigSO.");
                return;
            }

            if (level.OpeningDialogue == null)
                errors.Add("Day 1 LevelConfigSO is missing its opening dialogue.");
            if (level.DayOneReport == null)
                errors.Add("Day 1 LevelConfigSO is missing its report.");
            if (AssetDatabase.LoadAssetAtPath<CameraScanConfigSO>(
                    "Assets/PetOffline/Data/Cameras/Camera_Day1_B.asset") == null)
                errors.Add("Missing Day 1 Camera B scan config.");
            if (AssetDatabase.LoadAssetAtPath<CarryableConfigSO>(
                    "Assets/PetOffline/Data/Carryables/Carryable_Slipper.asset") == null)
                errors.Add("Missing Day 1 slipper carry config.");
            if (AssetDatabase.LoadAssetAtPath<CarryableConfigSO>(
                    "Assets/PetOffline/Data/Carryables/Carryable_Pillow.asset") == null)
                errors.Add("Missing Day 1 pillow carry config.");
        }

        static void ValidateDayTwoAssets(List<string> errors)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelConfigSO>(
                "Assets/PetOffline/Data/Levels/Level_Day2.asset");
            if (level == null)
                errors.Add("Missing Day 2 LevelConfigSO.");
            var report = AssetDatabase.LoadAssetAtPath<ReportDefinitionSO>(
                "Assets/PetOffline/Data/Reports/Report_Day2.asset");
            if (report == null)
                errors.Add("Missing Day 2 report.");
            if (level != null)
            {
                if (level.DayTwoReport == null || level.DayTwoReport != report)
                    errors.Add("Day 2 LevelConfigSO is missing its fixed report reference.");
                foreach (var id in new[]
                         {
                             "D2.Opening", "D2.FirstConfirm", "D2.ConfirmReturn", "D2.FeederOffline",
                             "D2.BackupActive", "D2.BackupConfirm", "D2.Complete", "D2.Restore", "D2.KeepQuiet"
                         })
                    if (level.DayTwoDialogue(id) == null)
                        errors.Add($"Day 2 LevelConfigSO is missing dialogue {id}.");
            }
            if (AssetDatabase.LoadAssetAtPath<CameraScanConfigSO>(
                    "Assets/PetOffline/Data/Cameras/Camera_Day2_Feeder.asset") == null)
                errors.Add("Missing Day 2 feeder-camera scan config.");
            if (AssetDatabase.LoadAssetAtPath<CameraScanConfigSO>(
                    "Assets/PetOffline/Data/Cameras/Camera_Day2_Backup.asset") == null)
                errors.Add("Missing Day 2 backup-camera scan config.");
            if (AssetDatabase.LoadAssetAtPath<CarryableConfigSO>(
                    "Assets/PetOffline/Data/Carryables/Carryable_BananaPeel.asset") == null)
                errors.Add("Missing Day 2 banana-peel carry config.");

            foreach (var name in new[]
                     {
                         "Opening", "FirstConfirm", "ConfirmReturn", "FeederOffline", "BackupActive",
                         "BackupConfirm", "Complete", "Restore", "KeepQuiet"
                     })
                if (AssetDatabase.LoadAssetAtPath<DialogueSequenceSO>(
                        $"Assets/PetOffline/Data/Dialogue/Dialogue_D2_{name}.asset") == null)
                    errors.Add($"Missing Day 2 dialogue sequence: D2.{name}.");
        }

        static void ValidateAudioAssets(List<string> errors)
        {
            foreach (var fileName in RequiredAudioCues)
            {
                var cue = AssetDatabase.LoadAssetAtPath<AudioCueDefinitionSO>(
                    $"Assets/PetOffline/Data/Audio/{fileName}");
                if (cue == null)
                    errors.Add($"Missing AudioCueDefinitionSO: {fileName}");
                else if (cue.Clip == null)
                    errors.Add($"Audio cue has no clip: {fileName}");
            }
        }

        static void ValidateAssemblyBoundaries(List<string> errors)
        {
            var ui = ReadAsset("Assets/PetOffline/Scripts/UI/PetOffline.UI.asmdef");
            var gameplay = ReadAsset("Assets/PetOffline/Scripts/Gameplay/PetOffline.Gameplay.asmdef");
            if (ui.Contains("PetOffline.Gameplay", StringComparison.Ordinal))
                errors.Add("PetOffline.UI must not reference PetOffline.Gameplay.");
            if (gameplay.Contains("PetOffline.UI", StringComparison.Ordinal))
                errors.Add("PetOffline.Gameplay must not reference PetOffline.UI.");
        }

        static string ReadAsset(string path)
        {
            var absolute = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            return File.Exists(absolute) ? File.ReadAllText(absolute) : string.Empty;
        }

        static void ValidateBuildSettings(List<string> errors)
        {
            var scenes = EditorBuildSettings.scenes;
            for (var i = 0; i < RequiredScenes.Length; i++)
            {
                var scene = scenes.FirstOrDefault(value => value.path == RequiredScenes[i]);
                if (scene == null)
                {
                    errors.Add($"Scene missing from Build Settings: {RequiredScenes[i]}");
                    continue;
                }

                var index = Array.IndexOf(scenes, scene);
                if (i < 3 && (!scene.enabled || index != i))
                    errors.Add($"Required runtime scene has wrong order or is disabled: {RequiredScenes[i]}");
                if (i == 3 && (scene.enabled || index != i))
                    errors.Add($"UIRoot test scene must be build index 3 and disabled: {RequiredScenes[i]}");
            }
        }

        static void ValidateLayers(List<string> errors)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets.Length == 0)
            {
                errors.Add("TagManager.asset not found.");
                return;
            }

            var manager = new SerializedObject(assets[0]);
            var layers = manager.FindProperty("layers");
            foreach (var required in RequiredPhysicsLayers)
                if (!Enumerable.Range(0, layers.arraySize).Any(i => layers.GetArrayElementAtIndex(i).stringValue == required))
                    errors.Add($"Missing physics layer: {required}");

            var sortingLayers = manager.FindProperty("m_SortingLayers");
            var ids = new HashSet<int>();
            foreach (var required in RequiredSortingLayers)
            {
                SerializedProperty found = null;
                for (var i = 0; i < sortingLayers.arraySize; i++)
                {
                    var element = sortingLayers.GetArrayElementAtIndex(i);
                    if (element.FindPropertyRelative("name").stringValue == required)
                    {
                        found = element;
                        break;
                    }
                }

                if (found == null)
                {
                    errors.Add($"Missing sorting layer: {required}");
                    continue;
                }

                var id = found.FindPropertyRelative("uniqueID").intValue;
                if (id == 0 || !ids.Add(id))
                    errors.Add($"Sorting layer {required} must have a unique non-zero ID.");
            }
        }

        static void ValidateScenes(List<string> errors)
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var path in RequiredScenes)
                {
                    if (!File.Exists(path))
                    {
                        errors.Add($"Required scene asset is missing: {path}");
                        continue;
                    }

                    var scene = SceneManager.GetSceneByPath(path);
                    var openedHere = !scene.isLoaded;
                    if (openedHere)
                        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    ValidateScene(scene, errors);
                    if (openedHere)
                        EditorSceneManager.CloseScene(scene, true);
                }
            }
            finally
            {
                if (setup.Any(value => value.isLoaded))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        static void ValidateScene(Scene scene, List<string> errors)
        {
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    var missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                    if (missing > 0)
                        errors.Add($"{scene.name}/{GetPath(transform)} contains {missing} missing script(s).");
                }

                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null)
                        continue;
                    ValidateBehaviour(scene, behaviour, errors);
                }
            }

            if (scene.path == SceneNames.BootstrapPath)
                ValidateBootstrap(scene, errors);
            else if (scene.path == SceneNames.Day1Path || scene.path == SceneNames.Day2Path)
                ValidateWorldScene(scene, errors);
        }

        static void ValidateBehaviour(Scene scene, MonoBehaviour behaviour, List<string> errors)
        {
            var type = behaviour.GetType();
            var isGameplay = type.Assembly.GetName().Name == "PetOffline.Gameplay";
            var forbiddenBelowCanvas = isGameplay || ForbiddenBelowCanvas.Contains(type.Name) ||
                                       typeof(LevelFlowController).IsAssignableFrom(type);
            if (forbiddenBelowCanvas && behaviour.GetComponentInParent<Canvas>(true) != null)
                errors.Add($"{scene.name}/{GetPath(behaviour.transform)} places {type.Name} below a Canvas.");

            if (isGameplay)
            {
                if (behaviour.transform is RectTransform)
                    errors.Add($"{scene.name}/{GetPath(behaviour.transform)} is gameplay using RectTransform.");
                if (behaviour.GetComponent<Graphic>() != null)
                    errors.Add($"{scene.name}/{GetPath(behaviour.transform)} is gameplay using a UGUI Graphic.");
            }

            foreach (var field in GetInstanceFields(type))
            {
                if (field.GetCustomAttribute<RequiredReferenceAttribute>() == null)
                    continue;

                if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                {
                    if (field.GetValue(behaviour) is not UnityEngine.Object reference || reference == null)
                        errors.Add($"{scene.name}/{GetPath(behaviour.transform)} missing required reference {field.Name}.");
                    continue;
                }

                var elementType = field.FieldType.IsArray ? field.FieldType.GetElementType() : null;
                if (elementType == null || !typeof(UnityEngine.Object).IsAssignableFrom(elementType))
                    continue;
                if (field.GetValue(behaviour) is not Array values || values.Length == 0 ||
                    values.Cast<object>().Any(value => value is not UnityEngine.Object reference || reference == null))
                    errors.Add($"{scene.name}/{GetPath(behaviour.transform)} missing required reference {field.Name}.");
            }
        }

        static IEnumerable<FieldInfo> GetInstanceFields(Type type)
        {
            while (type != null && type != typeof(MonoBehaviour))
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public |
                                                     BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    yield return field;
                type = type.BaseType;
            }
        }

        static void ValidateBootstrap(Scene scene, List<string> errors)
        {
            foreach (var type in new[]
                     {
                         typeof(GameSession), typeof(SceneFlowService), typeof(InputRouter), typeof(AudioService),
                         typeof(SaveService), typeof(DialogueDirector)
                     })
                if (!ContainsComponent(scene, type))
                    errors.Add($"Bootstrap missing {type.Name}.");

            if (!scene.GetRootGameObjects().Any(root => root.name == "UIRoot"))
                errors.Add("Bootstrap missing UIRoot.");
        }

        static void ValidateWorldScene(Scene scene, List<string> errors)
        {
            var worldRoot = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "WorldRoot");
            if (worldRoot == null)
            {
                errors.Add($"{scene.name} missing WorldRoot.");
                return;
            }

            foreach (var name in RequiredWorldContainers)
                if (worldRoot.transform.Find(name) == null)
                    errors.Add($"{scene.name}/WorldRoot missing {name}.");

            foreach (var root in scene.GetRootGameObjects())
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                if (canvas.renderMode != RenderMode.WorldSpace)
                    errors.Add($"{scene.name}/{GetPath(canvas.transform)} contains screen-space HUD in a world scene.");

            foreach (var transform in worldRoot.GetComponentsInChildren<Transform>(true))
            {
                if (transform is not RectTransform)
                    continue;
                var canvas = transform.GetComponentInParent<Canvas>(true);
                if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
                    errors.Add($"{scene.name}/{GetPath(transform)} is a world object using RectTransform.");
            }

            var ambience = worldRoot.transform.Find("WorldAudio")?.GetComponent<AudioSource>();
            if (ambience == null || ambience.clip == null || !ambience.loop)
                errors.Add($"{scene.name}/WorldRoot/WorldAudio is missing its looping ambience clip.");

            var latteVisual = worldRoot.transform.Find("Actors/Latte")?.GetComponent<LatteVisual2D>();
            if (latteVisual == null || latteVisual.BarkCue == null || latteVisual.BarkCue.Clip == null)
                errors.Add($"{scene.name}/WorldRoot/Actors/Latte is missing its bark AudioCueDefinitionSO.");

            if (scene.path == SceneNames.Day1Path)
                ValidateDayOne(worldRoot, errors);
            else if (scene.path == SceneNames.Day2Path)
                ValidateDayTwo(worldRoot, errors);
        }

        static void ValidateDayOne(GameObject worldRoot, List<string> errors)
        {
            foreach (var path in new[]
                     {
                         "Actors/Latte", "Interactables/OwnerSlipper", "Interactables/BossPillow",
                         "Interactables/BananaSlipZone", "Devices/CameraA", "Devices/CameraB",
                         "Devices/RobotVacuum", "Sensors/CameraBVision", "Triggers/CameraAGoalArea",
                         "Triggers/DogBedGoalArea", "Paths/RobotPath_Day1"
                     })
                if (worldRoot.transform.Find(path) == null)
                    errors.Add($"{SceneNames.Day1}/WorldRoot missing {path}.");

            var cameraA = worldRoot.transform.Find("Devices/CameraA");
            if (cameraA != null && cameraA.GetComponentInChildren<CameraVisionSensor2D>(true) != null)
                errors.Add("Day 1 Camera A must be a goal camera and must not have a hostile sensor.");

            var cameraBVision = worldRoot.transform.Find("Sensors/CameraBVision");
            var cameraBSensor = cameraBVision != null
                ? cameraBVision.GetComponent<CameraVisionSensor2D>()
                : null;
            if (cameraBSensor == null)
                errors.Add("Day 1 Camera B is missing CameraVisionSensor2D.");
            else if (cameraBVision.GetComponent<LineRenderer>() == null)
                errors.Add("Day 1 Camera B is missing its world-space vision cone.");

            var flows = worldRoot.GetComponentsInChildren<LevelFlowController>(true);
            if (flows.Length != 1 || flows[0] is not LevelOneFlowController)
                errors.Add("Day 1 must contain exactly one LevelOneFlowController.");

            var shoeGoal = worldRoot.transform.Find("Triggers/CameraAGoalArea")?.GetComponent<CarryGoalZone2D>();
            if (shoeGoal == null || shoeGoal.Target == null || shoeGoal.Target.name != "OwnerSlipper")
                errors.Add("Day 1 Camera A GoalArea must target OwnerSlipper.");

            var bedGoal = worldRoot.transform.Find("Triggers/DogBedGoalArea")?.GetComponent<CarryGoalZone2D>();
            if (bedGoal == null || bedGoal.Target == null || bedGoal.Target.name != "BossPillow")
                errors.Add("Day 1 Dog Bed GoalArea must target BossPillow.");
        }

        static void ValidateDayTwo(GameObject worldRoot, List<string> errors)
        {
            foreach (var path in new[]
                     {
                         "Environment/LivingRoom", "Environment/Kitchen", "Environment/Balcony",
                         "Actors/Latte", "Interactables/BananaPeel", "Interactables/OwnerSlipper",
                         "Devices/FutureFeeder",
                         "Devices/FeederCamera", "Devices/RobotVacuum", "Devices/BackupCamera",
                         "Devices/FeederStatusText", "Devices/BackupStatusText", "Sensors/FeederCameraVision",
                         "Sensors/BackupCameraVision",
                         "Triggers/PlayerSpawn", "Triggers/BackupRetrySpawn", "Triggers/EndingSniffPoint", "Triggers/EndingSleepPoint",
                         "Triggers/SunZone", "Triggers/FeederConfirmationArea", "Triggers/SideDoorTrigger",
                         "Paths/RobotPath_Day2", "VirtualCamera/CM_Day2"
                     })
                if (worldRoot.transform.Find(path) == null)
                    errors.Add($"{SceneNames.Day2}/WorldRoot missing {path}.");

            var flows = worldRoot.GetComponentsInChildren<LevelFlowController>(true);
            if (flows.Length != 1 || flows[0] is not LevelTwoFlowController)
                errors.Add("Day 2 must contain exactly one LevelTwoFlowController.");

            var latte = worldRoot.transform.Find("Actors/Latte")?.GetComponent<PlayerController2D>();
            if (latte == null)
                errors.Add("Day 2 Latte is missing PlayerController2D.");

            var banana = worldRoot.transform.Find("Interactables/BananaPeel")?.GetComponent<CarryableObject>();
            var bananaConfig = AssetDatabase.LoadAssetAtPath<CarryableConfigSO>(
                "Assets/PetOffline/Data/Carryables/Carryable_BananaPeel.asset");
            if (banana == null || banana.Config != bananaConfig)
                errors.Add("Day 2 BananaPeel must use Carryable_BananaPeel.");

            if (worldRoot.transform.Find("Devices/RobotVacuum")?.GetComponent<RobotPatrol>() == null)
                errors.Add("Day 2 RobotVacuum is missing RobotPatrol.");
            if (worldRoot.transform.Find("Devices/FutureFeeder")?.GetComponent<Collider2D>() == null)
                errors.Add("Day 2 FutureFeeder is missing the robot-contact Collider2D.");

            foreach (var path in new[] { "Sensors/FeederCameraVision", "Sensors/BackupCameraVision" })
            {
                var sensorTransform = worldRoot.transform.Find(path);
                if (sensorTransform?.GetComponent<CameraVisionSensor2D>() == null)
                    errors.Add($"Day 2 {path} is missing CameraVisionSensor2D.");
                if (sensorTransform?.GetComponent<LineRenderer>() == null)
                    errors.Add($"Day 2 {path} is missing its world-space vision cone.");
            }

            foreach (var path in new[] { "Triggers/SunZone", "Triggers/FeederConfirmationArea", "Triggers/SideDoorTrigger" })
            {
                var trigger = worldRoot.transform.Find(path);
                var collider = trigger?.GetComponent<Collider2D>();
                if (trigger?.GetComponent<PlayerTrigger2D>() == null || collider == null || !collider.isTrigger)
                    errors.Add($"Day 2 {path} must be a world PlayerTrigger2D with a trigger Collider2D.");
            }

        }

        static bool ContainsComponent(Scene scene, Type type)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var component in root.GetComponentsInChildren(type, true))
                if (component != null)
                    return true;
            return false;
        }

        static string GetPath(Transform value)
        {
            var path = value.name;
            while (value.parent != null)
            {
                value = value.parent;
                path = value.name + "/" + path;
            }
            return path;
        }

        static void WriteReport(IReadOnlyCollection<string> errors)
        {
            var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Artifacts/TestResults"));
            Directory.CreateDirectory(directory);
            var lines = new List<string>
            {
                $"Pet Offline validation: {(errors.Count == 0 ? "PASS" : "FAIL")}",
                $"Unity: {Application.unityVersion}",
                $"UTC: {DateTime.UtcNow:O}"
            };
            lines.AddRange(errors);
            File.WriteAllLines(Path.Combine(directory, "ValidationReport.txt"), lines);
        }
    }
}
