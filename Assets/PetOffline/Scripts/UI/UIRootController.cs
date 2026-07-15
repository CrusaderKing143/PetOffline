using System;
using System.Collections;
using System.Text;
using PetOffline.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PetOffline.UI
{
    public sealed class UIRootController : MonoBehaviour
    {
        [Serializable]
        public sealed class ViewBindings
        {
            public GameObject titlePanel;
            public GameObject hudPanel;
            public GameObject dialoguePanel;
            public GameObject reportPanel;
            public GameObject choicePanel;
            public GameObject pausePanel;
            public GameObject endingPanel;

            public TMP_Text objectiveText;
            public TMP_Text progressText;
            public TMP_Text cameraText;
            public TMP_Text controlsText;
            public TMP_Text dialogueSpeakerText;
            public TMP_Text dialogueBodyText;
            public TMP_Text reportTitleText;
            public TMP_Text reportBodyText;
            public TMP_Text reportContinueText;
            public TMP_Text endingText;
            public TMP_FontAsset font;

            public Slider progressSlider;
            public Slider volumeSlider;

            public Button newGameButton;
            public Button continueButton;
            public Button quitButton;
            public Button reportContinueButton;
            public Button restoreButton;
            public Button keepQuietButton;
            public Button resumeButton;
            public Button pauseReturnTitleButton;
            public Button endingReturnTitleButton;
            public Button endingRestartButton;
        }

        const string Controls = "WASD / 方向键 移动   E 叼起/放下   Space 汪叫   Q 推动   Shift 躺下   Esc 暂停";
        const string QuietEnding = "它不是不想你。\n它只是终于不用证明它在想你。";

        [SerializeField] GameSession session;
        [SerializeField] AudioService audioService;
        [SerializeField] AudioCueDefinitionSO uiConfirmCue;
        [SerializeField] AudioCueDefinitionSO uiReportCue;
        [SerializeField, RequiredReference] InputActionAsset actions;
        [SerializeField, RequiredReference] InputSystemUIInputModule inputModule;
        [SerializeField] ViewBindings views = new();

        ILevelViewModel model;
        InputAction pauseAction;
        InputAction cancelAction;
        InputActionReference pointReference;
        InputActionReference clickReference;
        InputActionReference moveReference;
        InputActionReference submitReference;
        InputActionReference cancelReference;
        Coroutine dialogueHideRoutine;
        Coroutine endingReadyRoutine;
        FinalChoice? selectedChoice;
        string endingInstruction = string.Empty;
        bool sessionSubscribed;
        bool buttonsWired;
        bool paused;
        bool endingReady;
        bool settingVolume;
        bool cameraDrawn;
        string drawnObjective;
        string drawnCameraId;
        float drawnProgress = -1f;
        bool drawnCameraActive;
        bool drawnCameraAlert;
        bool reportWasVisible;

        static TMP_FontAsset runtimeChineseFont;

        public ILevelViewModel Model => model;
        public ICommandSink Commands => session;
        public bool WaitingForLevel => model == null;
        public bool IsPaused => paused;
        public bool ProductionViewsReady => views != null && views.titlePanel != null && views.hudPanel != null &&
                                            views.reportPanel != null && views.choicePanel != null &&
                                            views.pausePanel != null && views.endingPanel != null;

        public void Configure(GameSession owner, InputActionAsset inputActions, InputSystemUIInputModule module)
        {
            if (Application.isPlaying)
                UnsubscribeSession();
            session = owner;
            actions = inputActions;
            inputModule = module;
            if (!Application.isPlaying)
                return;

            ConfigureRuntime();
        }

        public void Configure(
            GameSession owner,
            InputActionAsset inputActions,
            InputSystemUIInputModule module,
            ViewBindings bindings,
            AudioService audio = null)
        {
            if (Application.isPlaying)
                UnwireButtons();
            views = bindings ?? new ViewBindings();
            audioService = audio;
            Configure(owner, inputActions, module);
            if (Application.isPlaying)
            {
                WireButtons();
                ApplyRuntimeFont();
                Refresh();
            }
        }

        public void ConfigureAudioCues(AudioCueDefinitionSO confirmCue, AudioCueDefinitionSO reportCue)
        {
            uiConfirmCue = confirmCue;
            uiReportCue = reportCue;
        }

        void Awake()
        {
            ConfigureRuntime();
            Bind(session != null ? session.CurrentLevel : null);
        }

        void Start() => Refresh();

        void OnEnable()
        {
            if (Application.isPlaying)
                Refresh();
        }

        void OnDisable()
        {
            if (!paused)
                return;
            paused = false;
            Time.timeScale = 1f;
            if (CanPause())
                session?.SetGameplayMode(true);
        }

        void ConfigureRuntime()
        {
            ConfigureInputModule();
            WireButtons();
            SubscribeSession();
            ApplyRuntimeFont();
            ApplySavedVolume();
            Refresh();
        }

        void OnDestroy()
        {
            if (paused)
                Time.timeScale = 1f;
            UnsubscribeSession();
            UnwireButtons();
            if (model != null)
                model.Changed -= Refresh;
            model = null;
            DestroyInputReferences();
        }

        void Update()
        {
            if (paused)
            {
                if (cancelAction != null && cancelAction.WasPressedThisFrame())
                    ResumeGame();
                return;
            }

            if (CanPause() && pauseAction != null && pauseAction.WasPressedThisFrame())
                PauseGame();
        }

        public void Bind(ILevelViewModel value)
        {
            if (model != null)
                model.Changed -= Refresh;
            model = value;
            if (model != null)
                model.Changed += Refresh;
            else
            {
                selectedChoice = null;
                endingInstruction = string.Empty;
                endingReady = false;
                HideDialogue();
            }
            Refresh();
        }

        void Refresh()
        {
            if (views == null)
                return;

            if (model == null)
            {
                reportWasVisible = false;
                SetActive(views.titlePanel, true);
                SetActive(views.hudPanel, false);
                SetActive(views.reportPanel, false);
                SetActive(views.choicePanel, false);
                SetActive(views.endingPanel, false);
                SetActive(views.pausePanel, paused);
                if (views.continueButton != null)
                    views.continueButton.interactable = session != null && session.Save != null &&
                                                        session.Save.DayOneCompleted;
                Select(views.continueButton != null && views.continueButton.interactable
                    ? views.continueButton
                    : views.newGameButton);
                return;
            }

            var phase = model.Phase;
            if (phase != LevelPhase.End)
            {
                selectedChoice = null;
                endingInstruction = string.Empty;
                endingReady = false;
            }

            var reportVisible = phase == LevelPhase.Report;
            if (Application.isPlaying && reportVisible && !reportWasVisible)
                audioService?.Play(uiReportCue);
            reportWasVisible = reportVisible;
            var choiceVisible = phase == LevelPhase.Choice;
            var endingVisible = phase == LevelPhase.End && endingReady;
            var hudVisible = !reportVisible && !choiceVisible && !endingVisible &&
                             phase is not LevelPhase.Ending and not LevelPhase.Complete;

            SetActive(views.titlePanel, false);
            SetActive(views.hudPanel, hudVisible);
            SetActive(views.reportPanel, reportVisible);
            SetActive(views.choicePanel, choiceVisible);
            SetActive(views.endingPanel, endingVisible);
            SetActive(views.pausePanel, paused);

            var objective = phase == LevelPhase.End && !string.IsNullOrEmpty(endingInstruction)
                ? endingInstruction
                : model.Objective;
            if (views.objectiveText != null && !string.Equals(drawnObjective, objective, StringComparison.Ordinal))
            {
                drawnObjective = objective;
                views.objectiveText.text = objective;
            }
            if (!Mathf.Approximately(drawnProgress, model.Progress01))
            {
                drawnProgress = model.Progress01;
                views.progressSlider?.SetValueWithoutNotify(drawnProgress);
                views.progressText?.SetText("{0:0}%", drawnProgress * 100f);
            }

            var camera = model.CameraState;
            if (views.cameraText != null && (!cameraDrawn || drawnCameraActive != camera.Active ||
                                             drawnCameraAlert != camera.Alert ||
                                             !string.Equals(drawnCameraId, camera.CameraId,
                                                 StringComparison.Ordinal)))
            {
                cameraDrawn = true;
                drawnCameraActive = camera.Active;
                drawnCameraAlert = camera.Alert;
                drawnCameraId = camera.CameraId;
                views.cameraText.text = !camera.Active
                    ? model.Level == LevelId.Day2
                        ? "监控：CAMERA OFFLINE / 当前画面：墙"
                        : "监控：CAMERA OFFLINE"
                    : $"监控：{camera.CameraId} {(camera.Alert ? "警戒" : "在线")}";
                views.cameraText.color = camera.Alert
                    ? new Color(1f, 0.35f, 0.25f)
                    : camera.Active ? new Color(0.35f, 0.8f, 1f) : new Color(0.65f, 0.9f, 0.65f);
            }
            if (views.controlsText != null && views.controlsText.text != Controls)
                views.controlsText.text = Controls;

            if (reportVisible)
            {
                RefreshReport();
                Select(views.reportContinueButton);
            }
            else if (choiceVisible)
                Select(views.keepQuietButton);
            else if (endingVisible)
                Select(views.endingReturnTitleButton);
            if (endingVisible && views.endingText != null)
                views.endingText.text = selectedChoice == FinalChoice.KeepQuiet
                    ? QuietEnding
                    : selectedChoice == FinalChoice.RestoreConnection
                        ? "远程连接已恢复。\n确认循环重新开始。"
                        : "本次旅程结束。";
        }

        void RefreshReport()
        {
            var report = model?.Report;
            if (views.reportTitleText != null)
                views.reportTitleText.text = report != null ? report.Title : "日报";
            if (views.reportBodyText != null)
            {
                var body = new StringBuilder();
                if (report != null)
                {
                    for (var i = 0; i < report.Fields.Count; i++)
                    {
                        var field = report.Fields[i];
                        body.Append(field.Label).Append("：").AppendLine(field.Value);
                    }
                    if (!string.IsNullOrWhiteSpace(report.Warning))
                        body.AppendLine().Append(report.Warning);
                }
                views.reportBodyText.text = body.ToString();
            }
            if (views.reportContinueText != null)
                views.reportContinueText.text = report != null ? report.ContinueLabel : "继续";
        }

        void OnDialogueLine(string speaker, string line)
        {
            if (!isActiveAndEnabled)
                return;
            SetActive(views?.dialoguePanel, true);
            if (views?.dialogueSpeakerText != null)
                views.dialogueSpeakerText.text = SpeakerName(speaker);
            if (views?.dialogueBodyText != null)
                views.dialogueBodyText.text = line ?? string.Empty;

            if (dialogueHideRoutine != null)
                StopCoroutine(dialogueHideRoutine);
            dialogueHideRoutine = StartCoroutine(HideDialogueAfterDelay());
        }

        IEnumerator HideDialogueAfterDelay()
        {
            yield return new WaitForSecondsRealtime(4.5f);
            dialogueHideRoutine = null;
            SetActive(views?.dialoguePanel, false);
        }

        void OnDialogueFinished()
        {
            if (!isActiveAndEnabled)
                return;
            HideDialogue();
            if (model == null || model.Phase != LevelPhase.End)
                return;
            if (endingReadyRoutine != null)
                StopCoroutine(endingReadyRoutine);
            endingReadyRoutine = StartCoroutine(ReadyEndingWhenDialogueQueueDrains());
        }

        IEnumerator ReadyEndingWhenDialogueQueueDrains()
        {
            yield return null;
            endingReadyRoutine = null;
            if (model == null || model.Phase != LevelPhase.End ||
                (session?.Dialogue != null && session.Dialogue.IsPlaying))
                yield break;
            endingReady = true;
            session?.SetGameplayMode(false);
            Refresh();
        }

        void HideDialogue()
        {
            if (dialogueHideRoutine != null)
                StopCoroutine(dialogueHideRoutine);
            dialogueHideRoutine = null;
            SetActive(views?.dialoguePanel, false);
        }

        void PauseGame()
        {
            if (!CanPause())
                return;
            paused = true;
            Time.timeScale = 0f;
            session?.SetGameplayMode(false);
            SetActive(views?.pausePanel, true);
            Select(views?.resumeButton);
        }

        void ResumeGame()
        {
            if (!paused)
                return;
            paused = false;
            Time.timeScale = 1f;
            SetActive(views?.pausePanel, false);
            if (model != null && model.Phase is not LevelPhase.Report and not LevelPhase.Choice)
                session?.SetGameplayMode(true);
        }

        bool CanPause() => model != null && model.Phase is not LevelPhase.Report and not LevelPhase.Choice and
                           not LevelPhase.End and not LevelPhase.Complete;

        void OnNewGame()
        {
            PlayConfirm();
            session?.StartNewGame();
        }

        void OnContinueGame()
        {
            PlayConfirm();
            session?.ContinueSavedGame();
        }
        static void OnQuit() => Application.Quit();
        void OnReportContinue()
        {
            PlayConfirm();
            session?.ContinueReport();
        }

        void OnRestoreConnection()
        {
            PlayConfirm();
            selectedChoice = FinalChoice.RestoreConnection;
            endingInstruction = "远程确认已恢复：在太阳区躺下 10 秒";
            endingReady = false;
            session?.SubmitChoice(FinalChoice.RestoreConnection);
            Refresh();
        }

        void OnKeepQuiet()
        {
            PlayConfirm();
            selectedChoice = FinalChoice.KeepQuiet;
            endingInstruction = "让拿铁安静地睡一会儿";
            endingReady = false;
            session?.SubmitChoice(FinalChoice.KeepQuiet);
            Refresh();
        }

        void OnReturnToTitle()
        {
            PlayConfirm();
            paused = false;
            Time.timeScale = 1f;
            session?.ReturnToTitle();
        }

        void OnRestart()
        {
            PlayConfirm();
            paused = false;
            Time.timeScale = 1f;
            session?.Restart();
        }

        void PlayConfirm() => audioService?.Play(uiConfirmCue);

        void OnVolumeChanged(float value)
        {
            if (settingVolume)
                return;
            value = Mathf.Clamp01(value);
            if (audioService != null)
                audioService.SetMasterVolume(value);
            else
                AudioListener.volume = value;
            session?.Save?.SetMasterVolume(value);
        }

        void ApplySavedVolume()
        {
            var value = session?.Save != null ? session.Save.MasterVolume : AudioListener.volume;
            if (audioService != null)
                audioService.SetMasterVolume(value);
            else
                AudioListener.volume = value;
            if (views?.volumeSlider == null)
                return;
            settingVolume = true;
            views.volumeSlider.SetValueWithoutNotify(value);
            settingVolume = false;
        }

        void SubscribeSession()
        {
            if (sessionSubscribed || session == null)
                return;
            session.LevelChanged += Bind;
            if (session.Dialogue != null)
            {
                session.Dialogue.LineShown += OnDialogueLine;
                session.Dialogue.SequenceFinished += OnDialogueFinished;
            }
            sessionSubscribed = true;
        }

        void UnsubscribeSession()
        {
            if (!sessionSubscribed || session == null)
                return;
            session.LevelChanged -= Bind;
            if (session.Dialogue != null)
            {
                session.Dialogue.LineShown -= OnDialogueLine;
                session.Dialogue.SequenceFinished -= OnDialogueFinished;
            }
            sessionSubscribed = false;
        }

        void WireButtons()
        {
            if (buttonsWired || views == null)
                return;
            Add(views.newGameButton, OnNewGame);
            Add(views.continueButton, OnContinueGame);
            Add(views.quitButton, OnQuit);
            Add(views.reportContinueButton, OnReportContinue);
            Add(views.restoreButton, OnRestoreConnection);
            Add(views.keepQuietButton, OnKeepQuiet);
            Add(views.resumeButton, ResumeGame);
            Add(views.pauseReturnTitleButton, OnReturnToTitle);
            Add(views.endingReturnTitleButton, OnReturnToTitle);
            Add(views.endingRestartButton, OnRestart);
            views.volumeSlider?.onValueChanged.AddListener(OnVolumeChanged);
            buttonsWired = true;
        }

        void UnwireButtons()
        {
            if (!buttonsWired || views == null)
                return;
            Remove(views.newGameButton, OnNewGame);
            Remove(views.continueButton, OnContinueGame);
            Remove(views.quitButton, OnQuit);
            Remove(views.reportContinueButton, OnReportContinue);
            Remove(views.restoreButton, OnRestoreConnection);
            Remove(views.keepQuietButton, OnKeepQuiet);
            Remove(views.resumeButton, ResumeGame);
            Remove(views.pauseReturnTitleButton, OnReturnToTitle);
            Remove(views.endingReturnTitleButton, OnReturnToTitle);
            Remove(views.endingRestartButton, OnRestart);
            views.volumeSlider?.onValueChanged.RemoveListener(OnVolumeChanged);
            buttonsWired = false;
        }

        static void Add(Button button, UnityEngine.Events.UnityAction action) => button?.onClick.AddListener(action);
        static void Remove(Button button, UnityEngine.Events.UnityAction action) => button?.onClick.RemoveListener(action);

        static void Select(Button button)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || button == null || !button.gameObject.activeInHierarchy)
                return;
            var current = eventSystem.currentSelectedGameObject;
            if (current == null || !current.activeInHierarchy)
                eventSystem.SetSelectedGameObject(button.gameObject);
        }

        void ConfigureInputModule()
        {
            if (actions == null)
                return;
            pauseAction = actions.FindAction("Gameplay/Pause");
            cancelAction = actions.FindAction("UI/Cancel");
            if (inputModule == null)
                return;

            DestroyInputReferences();
            inputModule.actionsAsset = actions;
            inputModule.point = pointReference = InputActionReference.Create(actions.FindAction("UI/Point", true));
            inputModule.leftClick = clickReference = InputActionReference.Create(actions.FindAction("UI/Click", true));
            inputModule.move = moveReference = InputActionReference.Create(actions.FindAction("UI/Navigate", true));
            inputModule.submit = submitReference = InputActionReference.Create(actions.FindAction("UI/Submit", true));
            inputModule.cancel = cancelReference = InputActionReference.Create(actions.FindAction("UI/Cancel", true));
        }

        void DestroyInputReferences()
        {
            DestroyReference(ref pointReference);
            DestroyReference(ref clickReference);
            DestroyReference(ref moveReference);
            DestroyReference(ref submitReference);
            DestroyReference(ref cancelReference);
        }

        static void DestroyReference(ref InputActionReference reference)
        {
            if (reference != null)
                Destroy(reference);
            reference = null;
        }

        void ApplyRuntimeFont()
        {
            if (views?.font != null)
                runtimeChineseFont = views.font;
            if (runtimeChineseFont == null)
            {
                var font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" }, 36);
                if (font != null)
                {
                    runtimeChineseFont = TMP_FontAsset.CreateFontAsset(font);
                    runtimeChineseFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    runtimeChineseFont.hideFlags = HideFlags.DontUnloadUnusedAsset;
                }
            }

            if (runtimeChineseFont == null)
                return;
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                text.font = runtimeChineseFont;
        }

        static string SpeakerName(string speaker) => speaker switch
        {
            "Owner" => "主人",
            "Boss" => "老板",
            "AI" => "AI 助理",
            "Subtitle" => string.Empty,
            _ => speaker ?? string.Empty
        };

        static void SetActive(GameObject target, bool value)
        {
            if (target != null && target.activeSelf != value)
                target.SetActive(value);
        }
    }
}
