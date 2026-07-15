using System;
using System.IO;
using System.Linq;
using PetOffline.Core;
using PetOffline.Gameplay;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PetOffline.Editor
{
    public static class DayOneAutomation
    {
        const string LevelPath = "Assets/PetOffline/Data/Levels/Level_Day1.asset";
        const string OpeningPath = "Assets/PetOffline/Data/Dialogue/Dialogue_D1_Opening.asset";
        const string ReportPath = "Assets/PetOffline/Data/Reports/Report_Day1.asset";
        const string CameraPath = "Assets/PetOffline/Data/Cameras/Camera_Day1_B.asset";
        const string SlipperPath = "Assets/PetOffline/Data/Carryables/Carryable_Slipper.asset";
        const string PillowPath = "Assets/PetOffline/Data/Carryables/Carryable_Pillow.asset";
        const string ConeMaterialPath = "Assets/PetOffline/Art/VFX/GreyboxVision.mat";

        static readonly Vector2 PlayerSpawn = new(-5.9f, -4f);
        static readonly Vector2 PillowRetrySpawn = new(2.8f, 1.6f);

        [MenuItem("Tools/Pet Offline/Setup Day 1 Greybox")]
        public static void SetupDayOne()
        {
            EnsureNoDirtyScenes();
            EnsureFolders();
            var opening = EnsureAsset<DialogueSequenceSO>(OpeningPath, asset => asset.Configure(
                "D1.Opening",
                new[]
                {
                    new DialogueLine("Boss", "拿铁，把主人的拖鞋带到摄像头前。", 1.6f),
                    new DialogueLine("AI", "请搬运。警告：会议期间禁止搬运。", 1.8f),
                    new DialogueLine("Owner", "这两条规则是不是冲突了？按 Space 汪一声。", 1.8f)
                }, true, true, "D1.Opening.Complete"));

            var report = EnsureAsset<ReportDefinitionSO>(ReportPath, asset => asset.Configure(
                "D1.Report",
                "拿铁会议表现报告",
                new[]
                {
                    new ReportField("远程指令响应", "成功"),
                    new ReportField("主人气味资产展示", "1 次"),
                    new ReportField("企业文化物料接触", "1 次"),
                    new ReportField("行为安全警报", "已折叠"),
                    new ReportField("情绪价值输出", "优秀"),
                    new ReportField("主人贡献度", "一般"),
                    new ReportField("建议", "明日继续接入会议。")
                }, "展开后影响阅读体验。", "继续"));

            var level = EnsureAsset<LevelConfigSO>(LevelPath, asset =>
            {
                asset.Configure(opening, report, 2f, 14f, 26f, 3.6f, 3f, 7.2f);
                asset.ConfigureWorldMotion(1.6f, 0.9f, 3f, 1.5f, 1f);
            });
            var camera = EnsureAsset<CameraScanConfigSO>(CameraPath,
                asset => asset.Configure(-43f, 43f, 0f, 28f, 7.1f, 54f, 1.75f, 8.45f / 7.1f, 1.5f,
                    0.1f, 0.24f));
            var slipper = EnsureAsset<CarryableConfigSO>(SlipperPath,
                asset => asset.Configure(false, 0.85f, 0f));
            var pillow = EnsureAsset<CarryableConfigSO>(PillowPath,
                asset => asset.Configure(true, 0.6f, 0.65f));
            var coneMaterial = EnsureConeMaterial();

            AssetDatabase.SaveAssets();
            BuildScene(level, camera, slipper, pillow, coneMaterial);
            AssetDatabase.SaveAssets();
            Debug.Log("[PetOffline] Day 1 greybox setup complete.");
        }

        public static void SetupDayOneBatch()
        {
            try
            {
                SetupDayOne();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        static void BuildScene(
            LevelConfigSO levelConfig,
            CameraScanConfigSO cameraConfig,
            CarryableConfigSO slipperConfig,
            CarryableConfigSO pillowConfig,
            Material coneMaterial)
        {
            if (!File.Exists(SceneNames.Day1Path))
                throw new FileNotFoundException("Run Tools/Pet Offline/Setup Project first.", SceneNames.Day1Path);

            var scene = EditorSceneManager.OpenScene(SceneNames.Day1Path, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects().Where(value => value.name == "WorldRoot").ToArray())
                UnityEngine.Object.DestroyImmediate(root);

            var world = new GameObject("WorldRoot");
            var environment = Child(world.transform, "Environment");
            var collision = Child(world.transform, "Collision");
            var actors = Child(world.transform, "Actors");
            var interactables = Child(world.transform, "Interactables");
            var devices = Child(world.transform, "Devices");
            var sensors = Child(world.transform, "Sensors");
            var triggers = Child(world.transform, "Triggers");
            var paths = Child(world.transform, "Paths");
            Child(world.transform, "WorldVFX");
            Child(world.transform, "WorldAudio");
            var levelFlow = Child(world.transform, "LevelFlow");
            var virtualCamera = Child(world.transform, "VirtualCamera");

            BuildRoom(environment.transform, collision.transform);

            var playerObject = SpriteObject(actors.transform, "Latte", PlayerSpawn, new Vector2(0.8f, 0.55f),
                new Color(0.82f, 0.55f, 0.28f), "Actor", "Player");
            playerObject.transform.localScale = Vector3.one;
            var playerRenderer = playerObject.GetComponent<SpriteRenderer>();
            playerRenderer.drawMode = SpriteDrawMode.Sliced;
            playerRenderer.size = new Vector2(0.8f, 0.55f);
            var playerBody = playerObject.AddComponent<Rigidbody2D>();
            playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            playerObject.AddComponent<CapsuleCollider2D>().size = new Vector2(0.8f, 0.55f);
            var player = playerObject.AddComponent<PlayerController2D>();
            player.Configure(playerBody, playerRenderer, levelConfig.PlayerMoveSpeed);
            var mouth = Child(playerObject.transform, "MouthAnchor");
            mouth.transform.localPosition = new Vector3(0f, -0.48f, 0f);
            var carry = playerObject.AddComponent<CarryController>();
            carry.Configure(player, mouth.transform, LayerMask.GetMask("Carryable"), 1.1f, 0.55f,
                LayerMask.GetMask("WorldStatic"));

            var slipper = CreateCarryable(interactables.transform, "OwnerSlipper", new Vector2(-5.35f, -3.8f),
                new Vector2(0.72f, 0.28f), new Color(0.88f, 0.34f, 0.27f), slipperConfig, true);
            var pillow = CreateCarryable(interactables.transform, "BossPillow", new Vector2(3.95f, 1.95f),
                new Vector2(0.95f, 0.75f), new Color(0.36f, 0.62f, 0.72f), pillowConfig, false);

            var bananaObject = SpriteObject(interactables.transform, "BananaSlipZone", new Vector2(0f, -0.75f),
                new Vector2(1.25f, 0.5f), new Color(1f, 0.82f, 0.12f, 0.8f), "GroundDecal", "WorldTrigger");
            var bananaTrigger = bananaObject.AddComponent<BoxCollider2D>();
            bananaTrigger.size = Vector2.one;
            bananaTrigger.isTrigger = true;
            var banana = bananaObject.AddComponent<BananaSlipZone>();
            banana.Configure(bananaTrigger, bananaObject.GetComponent<SpriteRenderer>(), levelConfig, Vector2.right);

            SpriteObject(devices.transform, "CameraA", new Vector2(4.65f, 3f), new Vector2(0.65f, 0.45f),
                new Color(0.2f, 0.7f, 0.95f), "FurnitureFront", "WorldStatic");
            var cameraBObject = SpriteObject(devices.transform, "CameraB", new Vector2(0.4f, 4.35f),
                new Vector2(0.65f, 0.45f), new Color(0.25f, 0.62f, 0.9f), "FurnitureFront", "WorldStatic");
            var scanPivot = Child(cameraBObject.transform, "ScanPivot");
            scanPivot.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);

            var cameraVisionObject = Child(sensors.transform, "CameraBVision");
            cameraVisionObject.transform.position = cameraBObject.transform.position;
            SetLayer(cameraVisionObject, "Sensor");
            var visionRenderer = cameraVisionObject.AddComponent<SpriteRenderer>();
            visionRenderer.sprite = GreyboxSprite();
            visionRenderer.enabled = false;
            var cone = cameraVisionObject.AddComponent<LineRenderer>();
            cone.sharedMaterial = coneMaterial;
            cone.widthMultiplier = 0.035f;
            cone.sortingLayerName = "WorldFX";
            cone.sortingOrder = 2;
            var cameraSensor = cameraVisionObject.AddComponent<CameraVisionSensor2D>();
            cameraSensor.Configure(cameraConfig, player, scanPivot.transform, cone, LayerMask.GetMask("VisionOccluder"));

            SpriteObject(devices.transform, "Speaker", new Vector2(2.7f, 1.9f), new Vector2(0.55f, 0.7f),
                new Color(0.28f, 0.3f, 0.36f), "FurnitureBack", "WorldStatic");

            var pathRoot = Child(paths.transform, "RobotPath_Day1");
            var waypointPositions = new[]
            {
                new Vector2(-0.6f, -3.5f), new Vector2(3.2f, -3.5f), new Vector2(5f, -1.6f),
                new Vector2(3.4f, 0.2f), new Vector2(-0.8f, -0.5f)
            };
            var waypoints = waypointPositions.Select((position, index) =>
            {
                var waypoint = Child(pathRoot.transform, $"Waypoint_{index:00}");
                waypoint.transform.position = position;
                return waypoint.transform;
            }).ToArray();
            var robotObject = SpriteObject(devices.transform, "RobotVacuum", waypointPositions[0],
                new Vector2(0.9f, 0.55f), new Color(0.34f, 0.85f, 0.67f), "Actor", "Robot");
            var robotBody = robotObject.AddComponent<Rigidbody2D>();
            var robotCollider = robotObject.AddComponent<BoxCollider2D>();
            robotCollider.size = Vector2.one;
            var robot = robotObject.AddComponent<RobotPatrol>();
            robot.Configure(robotBody, robotCollider, robotObject.GetComponent<SpriteRenderer>(), waypoints,
                levelConfig.RobotPatrolSpeed);

            var playerSpawn = Marker(triggers.transform, "PlayerSpawn", PlayerSpawn);
            var pillowRetry = Marker(triggers.transform, "PillowRetrySpawn", PillowRetrySpawn);
            var speakerPoint = Marker(triggers.transform, "EndingSpeakerPoint", new Vector2(2.7f, 1.9f));
            var bedPoint = Marker(triggers.transform, "EndingBedPoint", new Vector2(-5.7f, -3.9f));

            var shoeGoalObject = SpriteObject(triggers.transform, "CameraAGoalArea", new Vector2(3.9f, 2.15f),
                new Vector2(1.8f, 1.8f), new Color(0.18f, 0.72f, 1f, 0.22f), "GroundDecal", "WorldTrigger");
            var shoeGoalTrigger = shoeGoalObject.AddComponent<CircleCollider2D>();
            shoeGoalTrigger.radius = 0.5f;
            shoeGoalTrigger.isTrigger = true;
            var shoeGoal = shoeGoalObject.AddComponent<CarryGoalZone2D>();
            shoeGoal.Configure(shoeGoalTrigger, slipper, levelConfig.ShoeGoalHoldSeconds);

            var bedGoalObject = SpriteObject(triggers.transform, "DogBedGoalArea", new Vector2(-5.7f, -3.9f),
                new Vector2(2.5f, 2.5f), new Color(0.35f, 0.85f, 0.45f, 0.2f), "GroundDecal", "WorldTrigger");
            var bedGoalTrigger = bedGoalObject.AddComponent<CircleCollider2D>();
            bedGoalTrigger.radius = 0.5f;
            bedGoalTrigger.isTrigger = true;
            var bedGoal = bedGoalObject.AddComponent<CarryGoalZone2D>();
            bedGoal.Configure(bedGoalTrigger, pillow, 0f);

            var context = levelFlow.AddComponent<LevelSceneContext>();
            context.Configure(LevelId.Day1, levelConfig.OpeningObjective);
            var flow = levelFlow.AddComponent<LevelOneFlowController>();
            flow.Configure(context, levelConfig, player, carry, slipper, pillow, shoeGoal, bedGoal, cameraSensor, robot,
                playerSpawn, pillowRetry, speakerPoint, bedPoint);

            var cameraObject = Child(virtualCamera.transform, "CM_Day1");
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var cinemachineCamera = cameraObject.AddComponent<CinemachineCamera>();
            cinemachineCamera.Priority = 10;
            cinemachineCamera.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            cinemachineCamera.Lens.OrthographicSize = 5.6f;

            WorldVisualAutomation.Polish(scene, LevelId.Day1);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Failed to save {SceneNames.Day1Path}.");
        }

        static void BuildRoom(Transform environment, Transform collision)
        {
            SpriteObject(environment, "Ground", Vector2.zero, new Vector2(14f, 10f),
                new Color(0.82f, 0.75f, 0.61f), "Ground", "WorldStatic");
            var back = Child(environment, "BackFurniture");
            SpriteObject(back.transform, "Desk", new Vector2(3.9f, 2.55f), new Vector2(2.6f, 1.1f),
                new Color(0.25f, 0.46f, 0.43f), "FurnitureBack", "WorldStatic");
            SpriteObject(back.transform, "Couch", new Vector2(-1.2f, 1.4f), new Vector2(3f, 1.2f),
                new Color(0.27f, 0.52f, 0.48f), "FurnitureBack", "WorldStatic");
            var front = Child(environment, "FrontFurniture");
            SpriteObject(front.transform, "DogBed", new Vector2(-5.7f, -3.9f), new Vector2(2.1f, 1.35f),
                new Color(0.68f, 0.38f, 0.28f), "FurnitureFront", "WorldStatic");

            var walls = Child(collision, "Walls");
            ColliderBox(walls.transform, "Wall_North", new Vector2(0f, 5.1f), new Vector2(14.4f, 0.3f),
                "WorldStatic", false);
            ColliderBox(walls.transform, "Wall_South", new Vector2(0f, -5.1f), new Vector2(14.4f, 0.3f),
                "WorldStatic", false);
            ColliderBox(walls.transform, "Wall_West", new Vector2(-7.1f, 0f), new Vector2(0.3f, 10f),
                "WorldStatic", false);
            ColliderBox(walls.transform, "Wall_East", new Vector2(7.1f, 0f), new Vector2(0.3f, 10f),
                "WorldStatic", false);

            var furniture = Child(collision, "FurnitureColliders");
            ColliderBox(furniture.transform, "CouchCollider", new Vector2(-1.2f, 1.4f), new Vector2(3f, 1.2f),
                "WorldStatic", false);
            ColliderBox(furniture.transform, "DeskCollider", new Vector2(3.9f, 3.05f), new Vector2(2.6f, 0.45f),
                "WorldStatic", false);

            var occluders = Child(collision, "VisionOccluders");
            ColliderBox(occluders.transform, "CouchVisionOccluder", new Vector2(-1.2f, 1.4f),
                new Vector2(3f, 1.2f), "VisionOccluder", true);
            ColliderBox(occluders.transform, "DeskVisionOccluder", new Vector2(3.9f, 3.05f),
                new Vector2(2.6f, 0.45f), "VisionOccluder", true);
        }

        static CarryableObject CreateCarryable(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color,
            CarryableConfigSO config,
            bool available)
        {
            var gameObject = SpriteObject(parent, name, position, size, color, "Carried", "Carryable");
            var body = gameObject.AddComponent<Rigidbody2D>();
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            var carryable = gameObject.AddComponent<CarryableObject>();
            carryable.Configure(body, collider, gameObject.GetComponent<SpriteRenderer>(), config, available);
            return carryable;
        }

        static GameObject SpriteObject(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color,
            string sortingLayer,
            string physicsLayer)
        {
            var gameObject = Child(parent, name);
            gameObject.transform.position = position;
            gameObject.transform.localScale = new Vector3(size.x, size.y, 1f);
            SetLayer(gameObject, physicsLayer);
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GreyboxSprite();
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = Vector2.one;
            renderer.color = color;
            renderer.sortingLayerName = sortingLayer;
            return gameObject;
        }

        static GameObject ColliderBox(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            string physicsLayer,
            bool isTrigger)
        {
            var gameObject = Child(parent, name);
            gameObject.transform.position = position;
            gameObject.transform.localScale = new Vector3(size.x, size.y, 1f);
            SetLayer(gameObject, physicsLayer);
            var collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.isTrigger = isTrigger;
            return gameObject;
        }

        static Transform Marker(Transform parent, string name, Vector2 position)
        {
            var marker = Child(parent, name).transform;
            marker.position = position;
            return marker;
        }

        static GameObject Child(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        static Sprite GreyboxSprite()
        {
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (sprite == null)
                throw new InvalidOperationException("Unity built-in greybox sprite was not found.");
            return sprite;
        }

        static void SetLayer(GameObject gameObject, string layerName)
        {
            var layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
                throw new InvalidOperationException($"Missing physics layer {layerName}. Run project setup first.");
            gameObject.layer = layer;
        }

        static T EnsureAsset<T>(string path, Action<T> configure) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            configure(asset);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static Material EnsureConeMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(ConeMaterialPath);
            if (material != null)
                return material;

            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException("No unlit shader is available for the vision cone.");
            material = new Material(shader) { name = "GreyboxVision" };
            AssetDatabase.CreateAsset(material, ConeMaterialPath);
            return material;
        }

        static void EnsureFolders()
        {
            foreach (var path in new[]
                     {
                         "Assets/PetOffline/Data/Levels", "Assets/PetOffline/Data/Dialogue",
                         "Assets/PetOffline/Data/Reports", "Assets/PetOffline/Data/Cameras",
                         "Assets/PetOffline/Data/Carryables", "Assets/PetOffline/Art/VFX"
                     })
                Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }

        static void EnsureNoDirtyScenes()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scenes before rebuilding the Day 1 greybox.");
        }
    }
}
