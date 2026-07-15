using System;
using System.IO;
using System.Linq;
using PetOffline.Core;
using PetOffline.UI;
using TMPro;
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
    public static class UIAutomation
    {
        const string InputPath = "Assets/PetOffline/Settings/Input/PetOffline.inputactions";
        const string FontPath = "Assets/PetOffline/Art/UI/Fonts/NotoSansCJKsc-Dynamic.asset";
        const string TitleBackgroundPath = "Assets/PetOffline/Art/UI/TitleBackground.png";
        const string UiConfirmCuePath = "Assets/PetOffline/Data/Audio/Audio_UIConfirm.asset";
        const string UiReportCuePath = "Assets/PetOffline/Data/Audio/Audio_UIReport.asset";

        static readonly Color Ink = new(0.15f, 0.17f, 0.2f);
        static readonly Color Paper = new(0.96f, 0.9f, 0.77f, 0.98f);
        static readonly Color Teal = new(0.18f, 0.5f, 0.48f);
        static readonly Color Coral = new(0.82f, 0.36f, 0.27f);
        static readonly Color Dim = new(0.04f, 0.05f, 0.07f, 0.82f);
        static TMP_FontAsset productionFont;
        static Sprite titleBackground;

        [MenuItem("Tools/Pet Offline/Setup Production UI")]
        public static void SetupProductionUi()
        {
            EnsureNoDirtyScenes();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                RebuildScene(SceneNames.BootstrapPath, true);
                RebuildScene(SceneNames.UIRootTestPath, false);
                AssetDatabase.SaveAssets();
                Debug.Log("[PetOffline] Production UI setup complete.");
            }
            finally
            {
                if (setup.Any(value => value.isLoaded))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        static void RebuildScene(string path, bool runtimeScene)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Missing scene {path}. Run project setup first.", path);

            var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (input == null)
                throw new InvalidOperationException($"Missing {InputPath}. Run project setup first.");
            productionFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            titleBackground = AssetDatabase.LoadAssetAtPath<Sprite>(TitleBackgroundPath);
            if (titleBackground == null)
                throw new InvalidOperationException($"Missing title background {TitleBackgroundPath}.");

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().FirstOrDefault(value => value.name == "UIRoot");
            if (root == null)
                throw new InvalidOperationException($"{scene.name} has no UIRoot.");

            var controller = root.GetComponent<UIRootController>() ?? root.AddComponent<UIRootController>();
            for (var i = root.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

            var bindings = BuildHierarchy(root.transform, out var module);
            var session = runtimeScene ? FindComponent<GameSession>(scene) : null;
            var audio = runtimeScene ? FindComponent<AudioService>(scene) : null;
            controller.Configure(session, input, module, bindings, audio);
            controller.ConfigureAudioCues(
                AssetDatabase.LoadAssetAtPath<AudioCueDefinitionSO>(UiConfirmCuePath),
                AssetDatabase.LoadAssetAtPath<AudioCueDefinitionSO>(UiReportCuePath));
            EditorUtility.SetDirty(controller);

            if (!runtimeScene)
            {
                var mock = FindComponent<MockLevelViewModelHost>(scene);
                if (mock == null)
                    mock = new GameObject("MockLevelViewModelHost").AddComponent<MockLevelViewModelHost>();
                mock.Configure(controller);
                EditorUtility.SetDirty(mock);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Failed to save {path}.");
        }

        static UIRootController.ViewBindings BuildHierarchy(
            Transform root,
            out InputSystemUIInputModule inputModule)
        {
            var hudCanvas = CreateCanvas(root, "Canvas_HUD", 0);
            var overlayCanvas = CreateCanvas(root, "Canvas_Overlay", 100);
            inputModule = CreateEventSystem(root);

            var views = new UIRootController.ViewBindings { font = productionFont };
            BuildHud(hudCanvas.transform, views);
            BuildTitle(overlayCanvas.transform, views);
            BuildReport(overlayCanvas.transform, views);
            BuildChoice(overlayCanvas.transform, views);
            BuildPause(overlayCanvas.transform, views);
            BuildEnding(overlayCanvas.transform, views);
            BuildDialogue(overlayCanvas.transform, views);

            views.titlePanel.SetActive(true);
            views.hudPanel.SetActive(false);
            views.dialoguePanel.SetActive(false);
            views.reportPanel.SetActive(false);
            views.choicePanel.SetActive(false);
            views.pausePanel.SetActive(false);
            views.endingPanel.SetActive(false);
            return views;
        }

        static void BuildHud(Transform parent, UIRootController.ViewBindings views)
        {
            views.hudPanel = RectObject(parent, "HUDPanel").gameObject;
            Stretch((RectTransform)views.hudPanel.transform);

            var objectiveCard = Card(views.hudPanel.transform, "ObjectiveCard", new Vector2(0f, 1f),
                new Vector2(35f, -35f), new Vector2(870f, 165f), new Vector2(0f, 1f));
            Text(objectiveCard.transform, "Caption", "当前目标", 24f, Teal, new Vector2(22f, -18f),
                new Vector2(820f, 32f), new Vector2(0f, 1f), TextAlignmentOptions.Left, FontStyles.Bold);
            views.objectiveText = Text(objectiveCard.transform, "Objective", string.Empty, 32f, Ink,
                new Vector2(22f, -58f), new Vector2(820f, 48f), new Vector2(0f, 1f),
                TextAlignmentOptions.Left, FontStyles.Bold);
            views.progressSlider = CreateSlider(objectiveCard.transform, "Progress", new Vector2(22f, 22f),
                new Vector2(710f, 20f), new Vector2(0f, 0f), false);
            views.progressText = Text(objectiveCard.transform, "ProgressText", "0%", 22f, Ink,
                new Vector2(-20f, 18f), new Vector2(90f, 28f), new Vector2(1f, 0f),
                TextAlignmentOptions.Right, FontStyles.Bold);

            var cameraCard = Card(views.hudPanel.transform, "CameraCard", new Vector2(1f, 1f),
                new Vector2(-35f, -35f), new Vector2(430f, 74f), new Vector2(1f, 1f));
            views.cameraText = Text(cameraCard.transform, "CameraState", "监控：OFFLINE", 27f,
                new Color(0.35f, 0.8f, 1f), Vector2.zero, new Vector2(390f, 48f), new Vector2(0.5f, 0.5f),
                TextAlignmentOptions.Center, FontStyles.Bold);

            var controlsCard = Card(views.hudPanel.transform, "ControlsCard", new Vector2(0.5f, 0f),
                new Vector2(0f, 28f), new Vector2(1540f, 58f), new Vector2(0.5f, 0f));
            views.controlsText = Text(controlsCard.transform, "Controls", string.Empty, 22f, Ink,
                Vector2.zero, new Vector2(1490f, 40f), new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center);
        }

        static void BuildTitle(Transform parent, UIRootController.ViewBindings views)
        {
            views.titlePanel = FullPanel(parent, "TitlePanel", new Color(0.08f, 0.12f, 0.13f, 1f));
            var background = RectObject(views.titlePanel.transform, "Background", true);
            Stretch(background);
            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = titleBackground;
            backgroundImage.color = Color.white;
            backgroundImage.raycastTarget = false;
            var card = Card(views.titlePanel.transform, "TitleCard", new Vector2(0.5f, 0.5f),
                new Vector2(0f, -270f), new Vector2(680f, 430f), new Vector2(0.5f, 0.5f));
            card.GetComponent<Image>().color = new Color(Paper.r, Paper.g, Paper.b, 0.94f);
            Text(card.transform, "Title", "《老板，我狗开会了》", 38f, Ink, new Vector2(0f, 155f),
                new Vector2(620f, 58f), new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center, FontStyles.Bold);
            views.newGameButton = CreateButton(card.transform, "NewGameButton", "新游戏", new Vector2(0f, 65f), Teal).Button;
            views.continueButton = CreateButton(card.transform, "ContinueButton", "继续 Day 2", new Vector2(0f, -20f),
                new Color(0.26f, 0.42f, 0.58f)).Button;
            views.quitButton = CreateButton(card.transform, "QuitButton", "退出游戏", new Vector2(0f, -105f), Coral).Button;
            Text(card.transform, "ControlsHint", "键盘与鼠标 · 离线单机", 20f, new Color(0.35f, 0.38f, 0.4f),
                new Vector2(0f, -170f), new Vector2(600f, 35f), new Vector2(0.5f, 0.5f),
                TextAlignmentOptions.Center);
        }

        static void BuildDialogue(Transform parent, UIRootController.ViewBindings views)
        {
            views.dialoguePanel = Card(parent, "DialoguePanel", new Vector2(0.5f, 0f),
                new Vector2(0f, 110f), new Vector2(1320f, 180f), new Vector2(0.5f, 0f));
            views.dialogueSpeakerText = Text(views.dialoguePanel.transform, "Speaker", string.Empty, 25f, Coral,
                new Vector2(28f, -18f), new Vector2(1240f, 32f), new Vector2(0f, 1f),
                TextAlignmentOptions.Left, FontStyles.Bold);
            views.dialogueBodyText = Text(views.dialoguePanel.transform, "Body", string.Empty, 31f, Ink,
                new Vector2(28f, -58f), new Vector2(1240f, 92f), new Vector2(0f, 1f),
                TextAlignmentOptions.TopLeft);
        }

        static void BuildReport(Transform parent, UIRootController.ViewBindings views)
        {
            views.reportPanel = FullPanel(parent, "ReportPanel", Dim);
            var card = Card(views.reportPanel.transform, "ReportCard", new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1080f, 820f), new Vector2(0.5f, 0.5f));
            views.reportTitleText = Text(card.transform, "Title", "日报", 46f, Ink, new Vector2(0f, 330f),
                new Vector2(950f, 70f), new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center, FontStyles.Bold);
            views.reportBodyText = Text(card.transform, "Body", string.Empty, 28f, Ink, new Vector2(0f, 20f),
                new Vector2(900f, 520f), new Vector2(0.5f, 0.5f), TextAlignmentOptions.TopLeft);
            var button = CreateButton(card.transform, "ContinueButton", "继续", new Vector2(0f, -330f), Teal);
            views.reportContinueButton = button.Button;
            views.reportContinueText = button.Label;
        }

        static void BuildChoice(Transform parent, UIRootController.ViewBindings views)
        {
            views.choicePanel = FullPanel(parent, "ChoicePanel", Dim);
            var card = Card(views.choicePanel.transform, "ChoiceCard", new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 560f), new Vector2(0.5f, 0.5f));
            Text(card.transform, "Title", "是否恢复远程确认？", 43f, Ink, new Vector2(0f, 170f),
                new Vector2(800f, 70f), new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center, FontStyles.Bold);
            Text(card.transform, "Hint", "连接可以回来，安静也可以被保留。", 25f,
                new Color(0.3f, 0.34f, 0.36f), new Vector2(0f, 105f), new Vector2(760f, 45f),
                new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center);
            views.restoreButton = CreateButton(card.transform, "RestoreButton", "恢复连接", new Vector2(0f, 10f),
                new Color(0.26f, 0.42f, 0.58f)).Button;
            views.keepQuietButton = CreateButton(card.transform, "KeepQuietButton", "保持安静（推荐）",
                new Vector2(0f, -90f), Teal).Button;
        }

        static void BuildPause(Transform parent, UIRootController.ViewBindings views)
        {
            views.pausePanel = FullPanel(parent, "PausePanel", Dim);
            var card = Card(views.pausePanel.transform, "PauseCard", new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(720f, 580f), new Vector2(0.5f, 0.5f));
            Text(card.transform, "Title", "暂停", 48f, Ink, new Vector2(0f, 190f), new Vector2(600f, 70f),
                new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center, FontStyles.Bold);
            Text(card.transform, "VolumeLabel", "主音量", 26f, Ink, new Vector2(-230f, 75f),
                new Vector2(180f, 40f), new Vector2(0.5f, 0.5f), TextAlignmentOptions.Left, FontStyles.Bold);
            views.volumeSlider = CreateSlider(card.transform, "VolumeSlider", new Vector2(60f, 75f),
                new Vector2(430f, 28f), new Vector2(0.5f, 0.5f), true);
            views.resumeButton = CreateButton(card.transform, "ResumeButton", "继续游戏", new Vector2(0f, -45f), Teal).Button;
            views.pauseReturnTitleButton = CreateButton(card.transform, "ReturnTitleButton", "返回标题",
                new Vector2(0f, -145f), Coral).Button;
        }

        static void BuildEnding(Transform parent, UIRootController.ViewBindings views)
        {
            views.endingPanel = FullPanel(parent, "EndingPanel", new Color(0.05f, 0.08f, 0.09f, 0.94f));
            var card = Card(views.endingPanel.transform, "EndingCard", new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(940f, 590f), new Vector2(0.5f, 0.5f));
            Text(card.transform, "Title", "END", 32f, Teal, new Vector2(0f, 190f), new Vector2(800f, 45f),
                new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center, FontStyles.Bold);
            views.endingText = Text(card.transform, "EndingText", string.Empty, 38f, Ink,
                new Vector2(0f, 75f), new Vector2(820f, 150f), new Vector2(0.5f, 0.5f),
                TextAlignmentOptions.Center, FontStyles.Bold);
            views.endingReturnTitleButton = CreateButton(card.transform, "ReturnTitleButton", "返回标题",
                new Vector2(-180f, -135f), Teal, new Vector2(300f, 70f)).Button;
            views.endingRestartButton = CreateButton(card.transform, "RestartButton", "重新开始",
                new Vector2(180f, -135f), Coral, new Vector2(300f, 70f)).Button;
        }

        static GameObject CreateCanvas(Transform parent, string name, int sortingOrder)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            gameObject.transform.SetParent(parent, false);
            SetUiLayer(gameObject);
            var canvas = gameObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = gameObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return gameObject;
        }

        static InputSystemUIInputModule CreateEventSystem(Transform parent)
        {
            var gameObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<InputSystemUIInputModule>();
        }

        static GameObject FullPanel(Transform parent, string name, Color color)
        {
            var panel = RectObject(parent, name, true);
            Stretch(panel);
            panel.GetComponent<Image>().color = color;
            return panel.gameObject;
        }

        static GameObject Card(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Vector2 pivot)
        {
            var card = RectObject(parent, name, true);
            Place(card, anchor, position, size, pivot);
            card.GetComponent<Image>().color = Paper;
            return card.gameObject;
        }

        static TMP_Text Text(
            Transform parent,
            string name,
            string content,
            float fontSize,
            Color color,
            Vector2 position,
            Vector2 size,
            Vector2 anchor,
            TextAlignmentOptions alignment,
            FontStyles style = FontStyles.Normal)
        {
            var rect = RectObject(parent, name);
            Place(rect, anchor, position, size, anchor);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (productionFont != null)
                text.font = productionFont;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        static (Button Button, TMP_Text Label) CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 position,
            Color color,
            Vector2? size = null)
        {
            var rect = RectObject(parent, name, true);
            Place(rect, new Vector2(0.5f, 0.5f), position, size ?? new Vector2(430f, 72f),
                new Vector2(0.5f, 0.5f));
            var image = rect.GetComponent<Image>();
            image.color = color;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = Text(rect, "Label", label, 28f, Color.white, Vector2.zero,
                Vector2.zero, new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch((RectTransform)text.transform, new Vector2(18f, 8f));
            return (button, text);
        }

        static Slider CreateSlider(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Vector2 anchor,
            bool interactable)
        {
            var rect = RectObject(parent, name, true);
            Place(rect, anchor, position, size, anchor);
            rect.GetComponent<Image>().color = new Color(0.2f, 0.22f, 0.24f, 0.35f);
            var slider = rect.gameObject.AddComponent<Slider>();

            var fill = RectObject(rect, "Fill", true);
            Stretch(fill, new Vector2(3f, 3f));
            fill.GetComponent<Image>().color = Teal;
            var handle = RectObject(rect, "Handle", true);
            Place(handle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, size.y + 12f),
                new Vector2(0.5f, 0.5f));
            handle.GetComponent<Image>().color = Color.white;

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = interactable;
            return slider;
        }

        static RectTransform RectObject(Transform parent, string name, bool image = false)
        {
            var gameObject = image
                ? new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                : new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            SetUiLayer(gameObject);
            return (RectTransform)gameObject.transform;
        }

        static void Stretch(RectTransform rect, Vector2 inset = default)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = inset;
            rect.offsetMax = -inset;
        }

        static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static void SetUiLayer(GameObject gameObject)
        {
            var layer = LayerMask.NameToLayer("UI");
            if (layer >= 0)
                gameObject.layer = layer;
        }

        static T FindComponent<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var value = root.GetComponentInChildren<T>(true);
                if (value != null)
                    return value;
            }
            return null;
        }

        static void EnsureNoDirtyScenes()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scenes before rebuilding production UI.");
        }
    }
}
