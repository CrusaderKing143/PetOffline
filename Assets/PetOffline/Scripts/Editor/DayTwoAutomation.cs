using System;
using System.IO;
using System.Linq;
using PetOffline.Core;
using PetOffline.Gameplay;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PetOffline.Editor
{
    public static class DayTwoAutomation
    {
        const string LevelPath = "Assets/PetOffline/Data/Levels/Level_Day2.asset";
        const string ReportPath = "Assets/PetOffline/Data/Reports/Report_Day2.asset";
        const string FeederCameraPath = "Assets/PetOffline/Data/Cameras/Camera_Day2_Feeder.asset";
        const string BackupCameraPath = "Assets/PetOffline/Data/Cameras/Camera_Day2_Backup.asset";
        const string BananaPath = "Assets/PetOffline/Data/Carryables/Carryable_BananaPeel.asset";
        const string DialogueRoot = "Assets/PetOffline/Data/Dialogue/Dialogue_D2_";
        const string ConeMaterialPath = "Assets/PetOffline/Art/VFX/GreyboxVision.mat";
        const string FontPath = "Assets/PetOffline/Art/UI/Fonts/NotoSansCJKsc-Dynamic.asset";
        const string MainObjective = "让拿铁晒满20秒太阳";

        static readonly Vector2 PlayerSpawn = new(-5.8f, -3.5f);
        static readonly Vector2 BackupRetrySpawn = new(-1.2f, -3.3f);
        static readonly Vector2 EndingSniffPoint = new(5.25f, -2.85f);
        static readonly Vector2 EndingSleepPoint = new(6f, -2.6f);
        static readonly Vector2 FeederPosition = new(2.65f, 2.55f);

        [MenuItem("Tools/Pet Offline/Setup Day 2 Greybox")]
        public static void SetupDayTwo()
        {
            EnsureNoDirtyScenes();
            EnsureFolders();

            var dialogues = new[]
            {
                EnsureDialogue("Opening", "D2.Opening", new[]
                {
                    new DialogueLine("Owner", "拿铁今天怎么老往阳台看？", 1.6f),
                    new DialogueLine("AI", "那边有太阳。", 1.2f),
                    new DialogueLine("Boss", "那就让它晒会儿。", 1.4f),
                    new DialogueLine("Owner", "这也要管？", 1.2f)
                }, true),
                EnsureDialogue("FirstConfirm", "D2.FirstConfirm", new[]
                {
                    new DialogueLine("AI", "宠物已离开投食器范围10秒。开始点名。", 1.8f),
                    new DialogueLine("Owner", "拿铁？过来一下，爸爸看看。", 1.8f)
                }),
                EnsureDialogue("ConfirmReturn", "D2.ConfirmReturn", new[]
                {
                    new DialogueLine("AI", "看到了，确认完成。", 1.4f),
                    new DialogueLine("Owner", "……那它刚才不是白晒了？", 1.8f)
                }),
                EnsureDialogue("FeederOffline", "D2.FeederOffline", new[]
                {
                    new DialogueLine("AI", "投食器摄像头角度异常。当前画面：墙。", 2f),
                    new DialogueLine("Owner", "它还能用吗？", 1.2f),
                    new DialogueLine("AI", "可以。正在认真观察墙。", 1.7f)
                }),
                EnsureDialogue("BackupActive", "D2.BackupActive", new[]
                {
                    new DialogueLine("AI", "侧门看到它了。备用摄像头已启用。", 1.8f),
                    new DialogueLine("Owner", "这边也有摄像头？", 1.5f)
                }),
                EnsureDialogue("BackupConfirm", "D2.BackupConfirm", new[]
                {
                    new DialogueLine("AI", "又有十秒没看到它了。侧门摄像头接管确认。", 2f),
                    new DialogueLine("Owner", "……不是已经撞坏一个了吗？", 1.8f),
                    new DialogueLine("AI", "备用摄像头会守住侧门。请从客厅下方绕回阳台。", 2.2f)
                }),
                EnsureDialogue("Complete", "D2.Complete", new[]
                {
                    new DialogueLine("AI", "未检测到拿铁。检测到墙。", 1.7f),
                    new DialogueLine("AI", "宠物状态：看起来挺舒服。", 1.6f),
                    new DialogueLine("Owner", "那不是睡着了吗？", 1.4f)
                }),
                EnsureDialogue("Restore", "D2.Restore", new[]
                {
                    new DialogueLine("AI", "远程确认已恢复。全天候陪伴模式开启。", 1.9f),
                    new DialogueLine("Owner", "拿铁？爸爸又能看见你了。", 1.8f)
                }),
                EnsureDialogue("KeepQuiet", "D2.KeepQuiet", new[]
                {
                    new DialogueLine("AI", "关掉以后，你就不能随时确认拿铁是不是还在想你。", 2.2f),
                    new DialogueLine("Owner", "……嗯。让它安静一会儿吧。", 1.8f),
                    new DialogueLine("Subtitle", "它不是不想你。\n它只是终于不用证明它在想你。", 3.5f)
                }, true)
            };

            var report = EnsureAsset<ReportDefinitionSO>(ReportPath, asset => asset.Configure(
                "D2.Report",
                "拿铁第二日陪伴报告",
                new[]
                {
                    new ReportField("晒太阳", "已完成（20 秒）"),
                    new ReportField("远程确认", "失败"),
                    new ReportField("投食器摄像头", "CAMERA OFFLINE"),
                    new ReportField("当前画面", "墙"),
                    new ReportField("情绪判断", "无法确认拿铁是否仍然想念主人")
                },
                "关闭后，您将无法实时确认拿铁是否仍在想您。",
                "进入最终选择",
                "是否恢复远程确认？",
                FinalChoice.KeepQuiet));

            var level = EnsureAsset<LevelConfigSO>(LevelPath, asset =>
            {
                asset.ConfigureDayTwo(report, dialogues, 20f, 10f, 9f, 7.2f, 4.6f, 1.35f, MainObjective);
                asset.ConfigureWorldMotion(moveSpeed: 3.25f, patrolSpeed: 1.22f, endingSpeed: 3f);
            });
            var feederCamera = EnsureAsset<CameraScanConfigSO>(FeederCameraPath,
                asset => asset.Configure(-35f, 35f, 0f, 28f, 7.2f, 58f, 2f, 1.25f, 1.5f, 0.1f, 0.2f));
            var backupCamera = EnsureAsset<CameraScanConfigSO>(BackupCameraPath,
                asset => asset.Configure(-25f, 25f, 0f, 32f, 5.5f, 55f, 2f, 1.25f, 1.5f, 0.1f, 0.2f));
            var banana = EnsureAsset<CarryableConfigSO>(BananaPath,
                asset => asset.Configure(false, 0.85f, 0f));
            var coneMaterial = EnsureConeMaterial();

            AssetDatabase.SaveAssets();
            BuildScene(level, feederCamera, backupCamera, banana, coneMaterial);
            AssetDatabase.SaveAssets();
            Debug.Log("[PetOffline] Day 2 greybox setup complete.");
        }

        public static void SetupDayTwoBatch()
        {
            try
            {
                SetupDayTwo();
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
            CameraScanConfigSO feederCameraConfig,
            CameraScanConfigSO backupCameraConfig,
            CarryableConfigSO bananaConfig,
            Material coneMaterial)
        {
            if (!File.Exists(SceneNames.Day2Path))
                throw new FileNotFoundException("Run Tools/Pet Offline/Setup Project first.", SceneNames.Day2Path);

            var scene = EditorSceneManager.OpenScene(SceneNames.Day2Path, OpenSceneMode.Single);
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
            var worldVfx = Child(world.transform, "WorldVFX");
            Child(world.transform, "WorldAudio");
            var levelFlow = Child(world.transform, "LevelFlow");
            var virtualCamera = Child(world.transform, "VirtualCamera");

            BuildRoom(environment.transform, collision.transform, worldVfx.transform);

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

            var banana = CreateCarryable(interactables.transform, "BananaPeel", new Vector2(-3.1f, -2.2f),
                new Vector2(0.72f, 0.3f), new Color(1f, 0.82f, 0.12f), bananaConfig);
            SpriteObject(interactables.transform, "OwnerSlipper", new Vector2(5.25f, -3.25f),
                new Vector2(0.7f, 0.28f), new Color(0.88f, 0.34f, 0.27f), "Carried", "WorldStatic");
            SpriteObject(interactables.transform, "BananaPlacementHint", new Vector2(0.85f, -1.45f),
                new Vector2(1.1f, 0.65f), new Color(1f, 0.82f, 0.12f, 0.22f), "GroundDecal", "WorldTrigger");

            var feederObject = SpriteObject(devices.transform, "FutureFeeder", FeederPosition,
                new Vector2(1.05f, 1.35f), new Color(0.28f, 0.72f, 0.76f), "FurnitureFront", "WorldStatic");
            var feederCollider = feederObject.AddComponent<BoxCollider2D>();
            feederCollider.size = new Vector2(0.9f, 0.9f);
            var feederCameraObject = SpriteObject(devices.transform, "FeederCamera", new Vector2(3.1f, 3.25f),
                new Vector2(0.55f, 0.4f), new Color(0.2f, 0.65f, 0.95f), "FurnitureFront", "WorldStatic");
            var feederPivot = Child(feederCameraObject.transform, "ScanPivot");
            feederPivot.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);

            var backupCameraObject = SpriteObject(devices.transform, "BackupCamera", new Vector2(4.55f, 1.75f),
                new Vector2(0.55f, 0.4f), new Color(0.9f, 0.45f, 0.2f), "FurnitureFront", "WorldStatic");
            var backupPivot = Child(backupCameraObject.transform, "ScanPivot");

            var feederStatus = WorldText(devices.transform, "FeederStatusText", new Vector2(2.65f, 4.05f),
                "CAMERA OFFLINE\n当前画面：墙", new Color(1f, 0.35f, 0.2f), false);
            var backupStatus = WorldText(devices.transform, "BackupStatusText", new Vector2(5.85f, 3.55f),
                "BACKUP CAMERA ACTIVE", new Color(1f, 0.45f, 0.18f), false);

            var feederSensor = CreateCameraSensor(sensors.transform, "FeederCameraVision", feederCameraObject.transform.position,
                feederCameraConfig, player, feederPivot.transform, coneMaterial);
            var backupSensor = CreateCameraSensor(sensors.transform, "BackupCameraVision", backupCameraObject.transform.position,
                backupCameraConfig, player, backupPivot.transform, coneMaterial);
            backupSensor.SetSensorActive(false);

            var pathRoot = Child(paths.transform, "RobotPath_Day2");
            var waypointPositions = new[]
            {
                new Vector2(-0.8f, -1.45f), new Vector2(0.85f, -1.45f), new Vector2(2.6f, -0.5f),
                new Vector2(2.65f, 1.45f), new Vector2(0.4f, 0.75f)
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
            robotCollider.isTrigger = true;
            var robot = robotObject.AddComponent<RobotPatrol>();
            robot.Configure(robotBody, robotCollider, robotObject.GetComponent<SpriteRenderer>(), waypoints,
                levelConfig.RobotPatrolSpeed);

            var playerSpawn = Marker(triggers.transform, "PlayerSpawn", PlayerSpawn);
            var backupRetry = Marker(triggers.transform, "BackupRetrySpawn", BackupRetrySpawn);
            var sniffPoint = Marker(triggers.transform, "EndingSniffPoint", EndingSniffPoint);
            var sleepPoint = Marker(triggers.transform, "EndingSleepPoint", EndingSleepPoint);

            var sunZone = CreatePlayerTrigger(triggers.transform, "SunZone", new Vector2(6f, -2.4f),
                new Vector2(3f, 3.2f), new Color(1f, 0.82f, 0.2f, 0.28f), player);
            var feederArea = CreatePlayerTrigger(triggers.transform, "FeederConfirmationArea", FeederPosition,
                new Vector2(2.2f, 2.2f), new Color(0.15f, 0.75f, 1f, 0.2f), player);
            var sideDoor = CreatePlayerTrigger(triggers.transform, "SideDoorTrigger", new Vector2(4.15f, 1.7f),
                new Vector2(0.8f, 2f), new Color(1f, 0.35f, 0.15f, 0.2f), player);
            WorldText(triggers.transform, "SideDoorRouteHint", new Vector2(3.8f, 0.45f),
                "侧门 → 阳台", new Color(0.95f, 0.7f, 0.2f), true);

            var context = levelFlow.AddComponent<LevelSceneContext>();
            context.Configure(LevelId.Day2, levelConfig.DayTwoObjective);
            var flow = levelFlow.AddComponent<LevelTwoFlowController>();
            flow.Configure(context, levelConfig, player, carry, banana, robot, feederSensor, backupSensor, sunZone,
                feederArea, sideDoor, playerSpawn, backupRetry, feederCollider, sniffPoint, sleepPoint, feederObject, feederStatus,
                backupStatus);

            var cameraObject = Child(virtualCamera.transform, "CM_Day2");
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var cinemachineCamera = cameraObject.AddComponent<CinemachineCamera>();
            cinemachineCamera.Priority = 10;
            cinemachineCamera.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            cinemachineCamera.Lens.OrthographicSize = 5.8f;

            WorldVisualAutomation.Polish(scene, LevelId.Day2);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Failed to save {SceneNames.Day2Path}.");
        }

        static void BuildRoom(Transform environment, Transform collision, Transform worldVfx)
        {
            var livingRoom = Child(environment, "LivingRoom");
            SpriteObject(livingRoom.transform, "Floor", new Vector2(-3.25f, 0f), new Vector2(9.5f, 10f),
                new Color(0.78f, 0.69f, 0.55f), "Ground", "WorldStatic");
            SpriteObject(livingRoom.transform, "Couch", new Vector2(-3.3f, 1.65f), new Vector2(3.2f, 1.2f),
                new Color(0.3f, 0.5f, 0.46f), "FurnitureBack", "WorldStatic");
            SpriteObject(livingRoom.transform, "DogBed", new Vector2(-5.9f, -3.7f), new Vector2(2f, 1.25f),
                new Color(0.68f, 0.38f, 0.28f), "FurnitureFront", "WorldStatic");

            var kitchen = Child(environment, "Kitchen");
            SpriteObject(kitchen.transform, "Floor", new Vector2(2.1f, 0.9f), new Vector2(3.2f, 8.2f),
                new Color(0.7f, 0.72f, 0.67f), "Ground", "WorldStatic");
            SpriteObject(kitchen.transform, "Counter", new Vector2(1.8f, 4.05f), new Vector2(3f, 0.8f),
                new Color(0.38f, 0.42f, 0.4f), "FurnitureBack", "WorldStatic");

            var balcony = Child(environment, "Balcony");
            SpriteObject(balcony.transform, "Floor", new Vector2(6.05f, 0f), new Vector2(3.9f, 10f),
                new Color(0.78f, 0.78f, 0.68f), "Ground", "WorldStatic");
            SpriteObject(balcony.transform, "Railing", new Vector2(7.65f, 0f), new Vector2(0.3f, 9.4f),
                new Color(0.35f, 0.5f, 0.55f), "FurnitureFront", "WorldStatic");
            SpriteObject(worldVfx, "SunGlow", new Vector2(6f, -2.4f), new Vector2(3.4f, 3.6f),
                new Color(1f, 0.8f, 0.22f, 0.18f), "WorldFX", "WorldTrigger");

            var walls = Child(collision, "Walls");
            ColliderBox(walls.transform, "Wall_North", new Vector2(0f, 5.1f), new Vector2(16.4f, 0.3f),
                "WorldStatic", false);
            ColliderBox(walls.transform, "Wall_South", new Vector2(0f, -5.1f), new Vector2(16.4f, 0.3f),
                "WorldStatic", false);
            ColliderBox(walls.transform, "Wall_West", new Vector2(-8.1f, 0f), new Vector2(0.3f, 10f),
                "WorldStatic", false);
            ColliderBox(walls.transform, "Wall_East", new Vector2(8.1f, 0f), new Vector2(0.3f, 10f),
                "WorldStatic", false);

            var routeDividers = Child(collision, "RouteDividers");
            ColliderBox(routeDividers.transform, "BalconyDivider_North", new Vector2(4.15f, 3.9f),
                new Vector2(0.25f, 2.2f), "WorldStatic", false);
            ColliderBox(routeDividers.transform, "BalconyDivider_Middle", new Vector2(4.15f, -0.8f),
                new Vector2(0.25f, 2.8f), "WorldStatic", false);

            var furniture = Child(collision, "FurnitureColliders");
            ColliderBox(furniture.transform, "CouchCollider", new Vector2(-3.3f, 1.65f),
                new Vector2(3.2f, 1.2f), "WorldStatic", false);
            ColliderBox(furniture.transform, "CounterCollider", new Vector2(1.8f, 4.05f),
                new Vector2(3f, 0.8f), "WorldStatic", false);

            var occluders = Child(collision, "VisionOccluders");
            ColliderBox(occluders.transform, "CouchVisionOccluder", new Vector2(-3.3f, 1.65f),
                new Vector2(3.2f, 1.2f), "VisionOccluder", true);
            ColliderBox(occluders.transform, "CounterVisionOccluder", new Vector2(1.8f, 4.05f),
                new Vector2(3f, 0.8f), "VisionOccluder", true);
            ColliderBox(occluders.transform, "BalconyVisionOccluder", new Vector2(4.15f, -0.8f),
                new Vector2(0.25f, 2.8f), "VisionOccluder", true);
        }

        static CarryableObject CreateCarryable(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color,
            CarryableConfigSO config)
        {
            var gameObject = SpriteObject(parent, name, position, size, color, "Carried", "Carryable");
            var body = gameObject.AddComponent<Rigidbody2D>();
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            var carryable = gameObject.AddComponent<CarryableObject>();
            carryable.Configure(body, collider, gameObject.GetComponent<SpriteRenderer>(), config, true);
            return carryable;
        }

        static CameraVisionSensor2D CreateCameraSensor(
            Transform parent,
            string name,
            Vector2 position,
            CameraScanConfigSO config,
            PlayerController2D player,
            Transform pivot,
            Material coneMaterial)
        {
            var gameObject = Child(parent, name);
            gameObject.transform.position = position;
            SetLayer(gameObject, "Sensor");
            var hiddenRenderer = gameObject.AddComponent<SpriteRenderer>();
            hiddenRenderer.sprite = GreyboxSprite();
            hiddenRenderer.enabled = false;
            var cone = gameObject.AddComponent<LineRenderer>();
            cone.sharedMaterial = coneMaterial;
            cone.widthMultiplier = 0.035f;
            cone.sortingLayerName = "WorldFX";
            cone.sortingOrder = 2;
            var sensor = gameObject.AddComponent<CameraVisionSensor2D>();
            sensor.Configure(config, player, pivot, cone, LayerMask.GetMask("VisionOccluder"));
            return sensor;
        }

        static PlayerTrigger2D CreatePlayerTrigger(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color,
            PlayerController2D player)
        {
            var gameObject = SpriteObject(parent, name, position, size, color, "GroundDecal", "WorldTrigger");
            var collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.isTrigger = true;
            var trigger = gameObject.AddComponent<PlayerTrigger2D>();
            trigger.Configure(collider, player);
            return trigger;
        }

        static GameObject WorldText(
            Transform parent,
            string name,
            Vector2 position,
            string content,
            Color color,
            bool active)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.position = position;
            SetLayer(canvasObject, "WorldUI");
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "WorldUI";
            canvas.sortingOrder = 5;
            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(480f, 100f);
            canvasRect.localScale = Vector3.one * 0.008f;

            var textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(canvasObject.transform, false);
            SetLayer(textObject, "WorldUI");
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            text.text = content;
            text.color = color;
            text.fontSize = 34f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            canvasObject.SetActive(active);
            return canvasObject;
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

        static DialogueSequenceSO EnsureDialogue(
            string fileSuffix,
            string sequenceId,
            DialogueLine[] lines,
            bool pauseGameplay = false)
            => EnsureAsset<DialogueSequenceSO>(DialogueRoot + fileSuffix + ".asset",
                asset => asset.Configure(sequenceId, lines, pauseGameplay, true, sequenceId + ".Complete"));

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
                    throw new InvalidOperationException("Save open scenes before rebuilding the Day 2 greybox.");
        }
    }
}
