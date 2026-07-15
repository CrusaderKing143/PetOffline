using System;
using System.IO;
using System.Linq;
using PetOffline.Core;
using PetOffline.Gameplay;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PetOffline.Editor
{
    public static class WorldVisualAutomation
    {
        const string FontPath = "Assets/PetOffline/Art/UI/Fonts/NotoSansCJKsc-Dynamic.asset";
        const string AudioRoot = "Assets/PetOffline/Data/Audio/";

        [MenuItem("Tools/Pet Offline/Setup World Visuals")]
        public static void SetupWorldVisuals()
        {
            EnsureNoDirtyScenes();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                PolishAndSave(SceneNames.Day1Path, LevelId.Day1);
                PolishAndSave(SceneNames.Day2Path, LevelId.Day2);
                Debug.Log("[PetOffline] World visual polish complete.");
            }
            finally
            {
                if (setup.Any(value => value.isLoaded))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        public static void Polish(Scene scene, LevelId level)
        {
            var world = scene.GetRootGameObjects().SingleOrDefault(value => value.name == "WorldRoot");
            if (world == null)
                throw new InvalidOperationException($"{scene.name} has no WorldRoot.");

            var vfx = world.transform.Find("WorldVFX");
            if (vfx == null)
                throw new InvalidOperationException($"{scene.name} has no WorldVFX.");
            RemoveChild(vfx, "VisualPolish");
            var polish = Child(vfx, "VisualPolish");

            AddBackdrop(polish.transform, level);
            AddIsometricGrid(polish.transform);
            AddRoomAccents(polish.transform, level);
            DecorateLatte(world.transform.Find("Actors/Latte"));
            DecorateSharedObjects(world.transform, level);

            var worldAudio = world.transform.Find("WorldAudio");
            if (worldAudio != null)
            {
                var source = AddAudioSource(worldAudio.gameObject);
                ConfigureAudio(source, Cue("Audio_Ambience.asset"), true);
            }
        }

        static void PolishAndSave(string path, LevelId level)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(path);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Polish(scene, level);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Failed to save {path}.");
        }

        static void AddBackdrop(Transform parent, LevelId level)
        {
            var color = level == LevelId.Day1
                ? new Color(0.16f, 0.12f, 0.11f)
                : new Color(0.12f, 0.16f, 0.17f);
            Sprite(parent, "Backdrop", Vector2.zero, new Vector2(22f, 13f), color, "Ground", -100);
        }

        static void AddIsometricGrid(Transform parent)
        {
            var color = new Color(0.18f, 0.2f, 0.2f, 0.085f);
            for (var i = -8; i <= 8; i++)
            {
                Sprite(parent, $"GridA_{i + 8:00}", new Vector2(0f, i * 0.72f),
                    new Vector2(22f, 0.018f), color, "GroundDecal", -20, 26.565f);
                Sprite(parent, $"GridB_{i + 8:00}", new Vector2(0f, i * 0.72f),
                    new Vector2(22f, 0.018f), color, "GroundDecal", -20, -26.565f);
            }
        }

        static void AddRoomAccents(Transform parent, LevelId level)
        {
            if (level == LevelId.Day1)
            {
                Sprite(parent, "MeetingRug", new Vector2(-1.2f, 1.15f), new Vector2(4.6f, 2.7f),
                    new Color(0.55f, 0.27f, 0.2f, 0.55f), "GroundDecal", -5, -3f);
                Sprite(parent, "WarmWindowGlow", new Vector2(-5.5f, 3.7f), new Vector2(2.4f, 1.7f),
                    new Color(1f, 0.65f, 0.22f, 0.22f), "WorldFX", -2, -8f);
                WorldText(parent, "RoomLabel", new Vector2(-5.3f, 4.55f), "MEETING DAY",
                    new Color(0.95f, 0.78f, 0.42f));
            }
            else
            {
                Sprite(parent, "LivingRoomRug", new Vector2(-3.2f, 1.55f), new Vector2(4.7f, 2.75f),
                    new Color(0.25f, 0.46f, 0.43f, 0.45f), "GroundDecal", -5, 2f);
                Sprite(parent, "SunRayA", new Vector2(6f, -2.4f), new Vector2(5.2f, 0.28f),
                    new Color(1f, 0.78f, 0.2f, 0.15f), "WorldFX", -1, 32f);
                Sprite(parent, "SunRayB", new Vector2(6f, -2.4f), new Vector2(5.2f, 0.2f),
                    new Color(1f, 0.84f, 0.32f, 0.12f), "WorldFX", -1, -32f);
                WorldText(parent, "BalconyLabel", new Vector2(6f, 4.5f), "SUN ZONE",
                    new Color(0.95f, 0.72f, 0.22f));
            }
        }

        static void DecorateLatte(Transform latte)
        {
            if (latte == null)
                return;
            RemoveChild(latte, "__Visual");
            RemoveChild(latte, "__Shadow");
            var renderer = latte.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.enabled = false;

            Sprite(latte, "__Shadow", new Vector2(0f, -0.28f), new Vector2(0.82f, 0.3f),
                new Color(0f, 0f, 0f, 0.26f), "Actor", -5);
            var visual = Child(latte, "__Visual");
            visual.layer = latte.gameObject.layer;
            var orange = new Color(0.86f, 0.48f, 0.2f);
            var cream = new Color(1f, 0.91f, 0.72f);
            Sprite(visual.transform, "Tail", new Vector2(0f, 0.4f), new Vector2(0.24f, 0.2f), orange, "Actor", 0, 25f);
            Sprite(visual.transform, "Body", new Vector2(0f, 0.1f), new Vector2(0.78f, 0.5f), orange, "Actor", 1);
            Sprite(visual.transform, "Chest", new Vector2(0f, -0.04f), new Vector2(0.36f, 0.34f), cream, "Actor", 2);
            Sprite(visual.transform, "Head", new Vector2(0f, -0.22f), new Vector2(0.58f, 0.48f), orange, "Actor", 3);
            Sprite(visual.transform, "EarL", new Vector2(-0.2f, -0.03f), new Vector2(0.2f, 0.28f), orange, "Actor", 2, -18f);
            Sprite(visual.transform, "EarR", new Vector2(0.2f, -0.03f), new Vector2(0.2f, 0.28f), orange, "Actor", 2, 18f);
            Sprite(visual.transform, "Muzzle", new Vector2(0f, -0.31f), new Vector2(0.34f, 0.2f), cream, "Actor", 4);
            Sprite(visual.transform, "EyeL", new Vector2(-0.105f, -0.25f), new Vector2(0.055f, 0.075f), Color.black, "Actor", 5);
            Sprite(visual.transform, "EyeR", new Vector2(0.105f, -0.25f), new Vector2(0.055f, 0.075f), Color.black, "Actor", 5);
            Sprite(visual.transform, "Nose", new Vector2(0f, -0.37f), new Vector2(0.09f, 0.065f), new Color(0.12f, 0.09f, 0.08f), "Actor", 6);
            Sprite(visual.transform, "Collar", new Vector2(0f, -0.06f), new Vector2(0.5f, 0.055f), new Color(0.18f, 0.42f, 0.58f), "Actor", 5);

            var player = latte.GetComponent<PlayerController2D>();
            var mouth = latte.Find("MouthAnchor");
            var source = latte.GetComponent<AudioSource>();
            if (source == null)
                source = latte.gameObject.AddComponent<AudioSource>();
            if (latte.GetComponent<Animator>() == null)
                latte.gameObject.AddComponent<Animator>();
            var animation = latte.GetComponent<LatteVisual2D>();
            if (animation == null)
                animation = latte.gameObject.AddComponent<LatteVisual2D>();
            animation.Configure(player, visual.transform, mouth, source, Cue("Audio_Bark.asset"));
            EditorUtility.SetDirty(animation);
        }

        static void DecorateSharedObjects(Transform world, LevelId level)
        {
            DecorateRobot(world.Find("Devices/RobotVacuum"));
            DecorateCamera(world.Find(level == LevelId.Day1 ? "Devices/CameraA" : "Devices/FeederCamera"), new Color(0.14f, 0.55f, 0.82f));
            DecorateCamera(world.Find(level == LevelId.Day1 ? "Devices/CameraB" : "Devices/BackupCamera"), new Color(0.85f, 0.3f, 0.2f));
            DecorateCarryable(world.Find("Interactables/OwnerSlipper"), new Color(0.42f, 0.16f, 0.12f));
            DecorateCarryable(world.Find("Interactables/BossPillow"), new Color(0.92f, 0.72f, 0.24f));
            DecorateCarryable(world.Find("Interactables/BananaPeel"), new Color(0.28f, 0.19f, 0.04f));
            DecorateFurniture(world.Find("Environment/BackFurniture/Couch") ?? world.Find("Environment/LivingRoom/Couch"));
            DecorateFurniture(world.Find("Environment/BackFurniture/Desk") ?? world.Find("Environment/Kitchen/Counter"));

            if (level == LevelId.Day1)
            {
                WorldText(world.Find("WorldVFX/VisualPolish"), "CameraALabel", new Vector2(4.65f, 3.55f), "CAM-A · GOAL", new Color(0.25f, 0.75f, 1f));
                WorldText(world.Find("WorldVFX/VisualPolish"), "CameraBLabel", new Vector2(0.4f, 4.78f), "CAM-B · SECURITY", new Color(1f, 0.38f, 0.25f));
            }
            else
            {
                var feeder = world.Find("Devices/FutureFeeder");
                if (feeder != null)
                {
                    RemoveChild(feeder, "__Visual");
                    var details = Child(feeder, "__Visual");
                    Sprite(details.transform, "Panel", new Vector2(0f, 0.08f), new Vector2(0.62f, 0.45f), new Color(0.08f, 0.22f, 0.25f), "FurnitureFront", 2);
                    Sprite(details.transform, "Bowl", new Vector2(0f, -0.34f), new Vector2(0.72f, 0.18f), new Color(0.82f, 0.55f, 0.24f), "FurnitureFront", 3);
                    ConfigureAudio(AddAudioSource(feeder.gameObject), Cue("Audio_FeederOffline.asset"), false);
                }
            }
        }

        static void DecorateRobot(Transform target)
        {
            if (target == null)
                return;
            RemoveChild(target, "__Visual");
            var root = Child(target, "__Visual");
            Sprite(root.transform, "Inner", Vector2.zero, new Vector2(0.72f, 0.58f), new Color(0.08f, 0.18f, 0.2f), "Actor", 2);
            Sprite(root.transform, "Top", new Vector2(0f, 0.03f), new Vector2(0.42f, 0.28f), new Color(0.28f, 0.72f, 0.64f), "Actor", 3);
            Sprite(root.transform, "Led", new Vector2(0.18f, -0.12f), new Vector2(0.08f, 0.08f), new Color(1f, 0.72f, 0.18f), "Actor", 4);
            ConfigureAudio(AddAudioSource(target.gameObject), Cue("Audio_Robot.asset"), true);
        }

        static void DecorateCamera(Transform target, Color accent)
        {
            if (target == null)
                return;
            RemoveChild(target, "__Visual");
            var root = Child(target, "__Visual");
            Sprite(root.transform, "Lens", new Vector2(0f, -0.02f), new Vector2(0.42f, 0.52f), new Color(0.05f, 0.08f, 0.1f), "FurnitureFront", 2);
            Sprite(root.transform, "Glass", new Vector2(0f, -0.02f), new Vector2(0.22f, 0.28f), accent, "FurnitureFront", 3);
            Sprite(root.transform, "Led", new Vector2(0.32f, 0.2f), new Vector2(0.08f, 0.1f), accent, "FurnitureFront", 4);
            ConfigureAudio(AddAudioSource(target.gameObject), Cue("Audio_CameraAlert.asset"), false);
        }

        static void DecorateCarryable(Transform target, Color accent)
        {
            if (target == null)
                return;
            RemoveChild(target, "__Visual");
            var root = Child(target, "__Visual");
            Sprite(root.transform, "Detail", Vector2.zero, new Vector2(0.55f, 0.42f), accent, "Carried", 2, -8f);
        }

        static void DecorateFurniture(Transform target)
        {
            if (target == null)
                return;
            RemoveChild(target, "__Visual");
            var root = Child(target, "__Visual");
            Sprite(root.transform, "InsetL", new Vector2(-0.24f, 0f), new Vector2(0.38f, 0.66f), new Color(0.18f, 0.34f, 0.32f, 0.72f), "FurnitureBack", 2);
            Sprite(root.transform, "InsetR", new Vector2(0.24f, 0f), new Vector2(0.38f, 0.66f), new Color(0.18f, 0.34f, 0.32f, 0.72f), "FurnitureBack", 2);
        }

        static AudioSource AddAudioSource(GameObject target)
        {
            var source = target.GetComponent<AudioSource>();
            if (source == null)
                source = target.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0.85f;
            source.minDistance = 1f;
            source.maxDistance = 14f;
            return source;
        }

        static void ConfigureAudio(AudioSource source, AudioCueDefinitionSO cue, bool playOnAwake)
        {
            if (source == null || cue == null || cue.Clip == null)
                return;
            source.clip = cue.Clip;
            source.volume = cue.Volume;
            source.pitch = cue.Pitch;
            source.spatialBlend = cue.SpatialBlend;
            source.loop = cue.Loop;
            source.maxDistance = cue.MaxDistance;
            source.ignoreListenerPause = cue.PlayWhenPaused;
            source.playOnAwake = playOnAwake;
        }

        static AudioCueDefinitionSO Cue(string fileName) =>
            AssetDatabase.LoadAssetAtPath<AudioCueDefinitionSO>(AudioRoot + fileName);

        static GameObject Sprite(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 scale,
            Color color,
            string sortingLayer,
            int sortingOrder,
            float rotation = 0f)
        {
            var gameObject = Child(parent, name);
            gameObject.transform.localPosition = position;
            gameObject.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            gameObject.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = Vector2.one;
            renderer.color = color;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = sortingOrder;
            return gameObject;
        }

        static void WorldText(Transform parent, string name, Vector2 position, string value, Color color)
        {
            if (parent == null)
                return;

            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = position;
            gameObject.layer = parent.gameObject.layer;
            var canvas = gameObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "WorldUI";
            canvas.sortingOrder = 20;
            var canvasRect = (RectTransform)gameObject.transform;
            canvasRect.sizeDelta = new Vector2(480f, 100f);
            canvasRect.localScale = Vector3.one * 0.008f;

            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            label.transform.SetParent(gameObject.transform, false);
            label.layer = gameObject.layer;
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;
            var text = label.GetComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            text.text = value;
            text.fontSize = 34f;
            text.color = color;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
        }

        static GameObject Child(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.layer = parent.gameObject.layer;
            return child;
        }

        static void RemoveChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        static void EnsureNoDirtyScenes()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scenes before rebuilding world visuals.");
        }
    }
}
