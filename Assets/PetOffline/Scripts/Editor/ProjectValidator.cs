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
            var errors = CollectErrors();
            WriteReport(errors);
            if (errors.Count > 0)
                throw new BuildFailedException(string.Join(Environment.NewLine, errors));
            Debug.Log("[PetOffline] Project validation passed.");
        }

        public static List<string> CollectErrors()
        {
            var errors = new List<string>();
            if (Application.unityVersion != "6000.3.14f1")
                errors.Add($"Expected Unity 6000.3.14f1, got {Application.unityVersion}.");

            ValidateAssemblyBoundaries(errors);
            ValidateBuildSettings(errors);
            ValidateLayers(errors);
            ValidateScenes(errors);
            return errors;
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
                var missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
                if (missing > 0)
                    errors.Add($"{scene.name}/{root.name} contains {missing} missing script(s).");

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
            var forbiddenBelowCanvas = ForbiddenBelowCanvas.Contains(type.Name) ||
                                       typeof(LevelFlowController).IsAssignableFrom(type);
            if (forbiddenBelowCanvas && behaviour.GetComponentInParent<Canvas>(true) != null)
                errors.Add($"{scene.name}/{GetPath(behaviour.transform)} places {type.Name} below a Canvas.");

            if (type.Assembly.GetName().Name == "PetOffline.Gameplay" && behaviour.transform is RectTransform)
                errors.Add($"{scene.name}/{GetPath(behaviour.transform)} is gameplay using RectTransform.");

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.GetCustomAttribute<RequiredReferenceAttribute>() == null)
                    continue;
                if (!typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                    continue;
                if (field.GetValue(behaviour) is not UnityEngine.Object reference || reference == null)
                    errors.Add($"{scene.name}/{GetPath(behaviour.transform)} missing required reference {field.Name}.");
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
