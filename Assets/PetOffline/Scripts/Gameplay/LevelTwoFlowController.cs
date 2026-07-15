using System;
using PetOffline.Core;
using UnityEngine;

namespace PetOffline.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class LevelTwoFlowController : LevelFlowController
    {
        const string MainObjective = "让拿铁晒满20秒太阳";
        const string OpeningDialogueId = "D2.Opening";
        const string FirstConfirmDialogueId = "D2.FirstConfirm";
        const string ConfirmReturnDialogueId = "D2.ConfirmReturn";
        const string FeederOfflineDialogueId = "D2.FeederOffline";
        const string BackupActiveDialogueId = "D2.BackupActive";
        const string BackupConfirmDialogueId = "D2.BackupConfirm";
        const string CompleteDialogueId = "D2.Complete";
        const string RestoreDialogueId = "D2.Restore";
        const string KeepQuietDialogueId = "D2.KeepQuiet";
        const string FeederCameraId = "Feeder";
        const string BackupCameraId = "Backup";

        public const string KeepQuietSubtitle = "它不是不想你。\n它只是终于不用证明它在想你。";

        [SerializeField, RequiredReference] LevelConfigSO config;
        [SerializeField, RequiredReference] PlayerController2D player;
        [SerializeField, RequiredReference] CarryController carry;
        [SerializeField, RequiredReference] CarryableObject bananaPeel;
        [SerializeField, RequiredReference] RobotPatrol robot;
        [SerializeField, RequiredReference] CameraVisionSensor2D feederCamera;
        [SerializeField, RequiredReference] CameraVisionSensor2D backupCamera;
        [SerializeField, RequiredReference] PlayerTrigger2D sunZone;
        [SerializeField, RequiredReference] PlayerTrigger2D feederArea;
        [SerializeField, RequiredReference] PlayerTrigger2D sideDoor;
        [SerializeField, RequiredReference] Transform playerSpawn;
        [SerializeField, RequiredReference] Transform backupRetrySpawn;
        [SerializeField, RequiredReference] Collider2D feederCollider;
        [SerializeField, RequiredReference] Transform endingSniffPoint;
        [SerializeField, RequiredReference] Transform endingSleepPoint;
        [SerializeField, RequiredReference] GameObject feederDevice;
        [SerializeField, RequiredReference] GameObject feederOfflineDisplay;
        [SerializeField, RequiredReference] GameObject backupActiveDisplay;

        GameSession session;
        LevelPhase phase;
        float sunTime;
        float ignoreCountdown;
        float alertRemaining;
        bool ignoredRaised;
        bool subscribed;
        bool bananaMechanismStarted;
        bool keepQuietMovement;
        bool keepQuietSniffed;
        float keepQuietPauseRemaining;

        public bool IsBound { get; private set; }
        public LevelPhase Phase => phase;
        public float SunTime => sunTime;
        public float SunProgress01 => Mathf.Clamp01(sunTime / SunTargetSeconds);
        public float SunTargetSeconds => config != null ? config.SunTargetSeconds : 20f;
        public float ConfirmationSeconds => config != null ? config.ConfirmationSeconds : 10f;
        public bool FoodCameraActive { get; private set; }
        public bool BackupCameraActive { get; private set; }
        public bool BackupCameraEverTriggered { get; private set; }
        public bool BackupLessonCompleted { get; private set; }
        public bool ConfirmationActive { get; private set; }
        public bool AttemptReacquired { get; private set; }
        public bool AlertActive => alertRemaining > 0f ||
                                   (feederCamera != null && feederCamera.IsAlert) ||
                                   (backupCamera != null && backupCamera.IsAlert);
        public int ConfirmationCount { get; private set; }
        public int IgnoredConfirmationCount { get; private set; }
        public int BackupResetCount { get; private set; }
        public int ReacquisitionCount { get; private set; }
        public bool RestoreLoopActive { get; private set; }
        public bool RestoreConfirmationTriggered { get; private set; }
        public bool EndingComplete { get; private set; }
        public FinalChoice? SelectedChoice { get; private set; }
        public string FinalSubtitle { get; private set; } = string.Empty;
        public bool KeepQuietSniffed => keepQuietSniffed;
        public bool FeederDeviceActive => feederDevice == null || feederDevice.activeSelf;

        public event Action<LevelPhase> PhaseChanged;
        public event Action<float> SunTimeChanged;
        public event Action ConfirmationStarted;
        public event Action ConfirmationIgnored;
        public event Action FeederCameraDisabled;
        public event Action BackupCameraActivated;
        public event Action BackupAttemptReset;
        public event Action<FinalChoice> EndingCompleted;

        public void Configure(
            LevelSceneContext context,
            LevelConfigSO levelConfig,
            PlayerController2D latte,
            CarryController carryController,
            CarryableObject banana,
            RobotPatrol cleaningRobot,
            CameraVisionSensor2D foodCamera,
            CameraVisionSensor2D backup,
            PlayerTrigger2D sunlight,
            PlayerTrigger2D confirmationArea,
            PlayerTrigger2D sideDoorTrigger,
            Transform latteSpawn,
            Transform retrySpawn,
            Collider2D feederCollision,
            Transform sniffPoint,
            Transform sleepPoint,
            GameObject feeder,
            GameObject offlineDisplay,
            GameObject backupDisplay)
        {
            Unsubscribe();
            base.Configure(context);
            config = levelConfig;
            player = latte;
            carry = carryController;
            bananaPeel = banana;
            robot = cleaningRobot;
            feederCamera = foodCamera;
            backupCamera = backup;
            sunZone = sunlight;
            feederArea = confirmationArea;
            sideDoor = sideDoorTrigger;
            playerSpawn = latteSpawn;
            backupRetrySpawn = retrySpawn;
            feederCollider = feederCollision;
            endingSniffPoint = sniffPoint;
            endingSleepPoint = sleepPoint;
            feederDevice = feeder;
            feederOfflineDisplay = offlineDisplay;
            backupActiveDisplay = backupDisplay;
            if (IsBound && isActiveAndEnabled)
                Subscribe();
        }

        public override void Bind(GameSession owner)
        {
            Unsubscribe();
            session = owner;
            IsBound = true;
            Subscribe();
            InitializeLevel();
        }

        public void Tick(float deltaTime)
        {
            if (!IsBound || deltaTime <= 0f)
                return;

            TickAlert(deltaTime);
            if (keepQuietMovement)
            {
                TickKeepQuietEnding(deltaTime);
                return;
            }

            if (ConfirmationActive)
            {
                TickConfirmation(deltaTime);
                return;
            }

            if (RestoreLoopActive)
            {
                TickSun(deltaTime, ConfirmationSeconds, BeginRestoreConfirmation);
                return;
            }

            switch (phase)
            {
                case LevelPhase.SunFirst:
                    TickSun(deltaTime, ConfirmationSeconds, BeginFirstConfirmation);
                    break;
                case LevelPhase.DestroyCamera when FoodCameraActive:
                    TickSun(deltaTime, ConfirmationSeconds, BeginFirstConfirmation);
                    break;
                case LevelPhase.Backup when BackupCameraActive:
                case LevelPhase.FinalSun when BackupCameraActive:
                    TickSun(deltaTime, ConfirmationSeconds, BeginBackupConfirmation);
                    break;
                case LevelPhase.FinalSun when CanCompleteFinalAttempt():
                    TickSun(deltaTime, SunTargetSeconds, EnterReport);
                    break;
            }
        }

        public void ContinueReport()
        {
            if (phase != LevelPhase.Report)
                return;
            SetPhase(LevelPhase.Choice);
        }

        public void SubmitChoice(FinalChoice choice)
        {
            if (phase != LevelPhase.Choice)
                return;

            SelectedChoice = choice;
            SetPhase(LevelPhase.End);
            if (choice == FinalChoice.RestoreConnection)
                BeginRestoreEnding();
            else
                BeginKeepQuietEnding();
        }

        public void CompleteBackupConfirmation()
        {
            if (!BackupCameraActive && !ConfirmationActive)
                return;

            ClearConfirmation();
            BackupCameraActive = false;
            backupCamera?.SetSensorActive(false);
            backupCamera?.SetAlert(false);
            SetActive(backupActiveDisplay, false);
            BackupLessonCompleted = true;
            BackupResetCount++;
            AttemptReacquired = false;
            SetSunTime(0f);
            player?.SetLying(false);
            if (player != null && backupRetrySpawn != null)
                player.ResetTo(backupRetrySpawn.position);
            sunZone?.ResetState();
            feederArea?.ResetState();
            sideDoor?.ResetState();
            SetPhase(LevelPhase.FinalSun);
            BackupAttemptReset?.Invoke();
        }

        void InitializeLevel()
        {
            FoodCameraActive = true;
            BackupCameraActive = false;
            BackupCameraEverTriggered = false;
            BackupLessonCompleted = false;
            ConfirmationActive = false;
            AttemptReacquired = false;
            ConfirmationCount = 0;
            IgnoredConfirmationCount = 0;
            BackupResetCount = 0;
            ReacquisitionCount = 0;
            RestoreLoopActive = false;
            RestoreConfirmationTriggered = false;
            EndingComplete = false;
            SelectedChoice = null;
            FinalSubtitle = string.Empty;
            bananaMechanismStarted = false;
            keepQuietMovement = false;
            keepQuietSniffed = false;
            keepQuietPauseRemaining = 0f;
            ignoreCountdown = 0f;
            alertRemaining = 0f;
            ignoredRaised = false;
            SetSunTime(0f);

            if (carry != null && carry.Held != null)
                carry.ForceDrop(player != null ? player.Position : Vector2.zero);
            bananaPeel?.SetAvailable(true);
            bananaPeel?.ResetToStart();
            robot?.ResetPatrol();
            robot?.SetPatrolEnabled(true);
            sunZone?.ResetState();
            feederArea?.ResetState();
            sideDoor?.ResetState();

            feederCamera?.ResetScan();
            feederCamera?.SetSensorActive(true);
            backupCamera?.ResetScan();
            backupCamera?.SetSensorActive(false);
            SetActive(feederDevice, true);
            SetActive(feederOfflineDisplay, false);
            SetActive(backupActiveDisplay, false);

            if (player != null)
            {
                player.BindInput(session?.Input);
                if (playerSpawn != null)
                    player.ResetTo(playerSpawn.position);
                player.SetInputEnabled(true);
                player.SetMovementEnabled(false);
            }
            carry?.SetInteractionEnabled(true);
            session?.SetGameplayMode(true);

            Context?.Configure(LevelId.Day2, Objective);
            Context?.SetReport(null);
            SetPhase(LevelPhase.Start);
            PlayDialogue(OpeningDialogueId, EnterSunFirst);
        }

        void EnterSunFirst()
        {
            if (phase != LevelPhase.Start)
                return;
            player?.SetMovementEnabled(true);
            SetPhase(LevelPhase.SunFirst);
        }

        void TickSun(float deltaTime, float threshold, Action completed)
        {
            if (sunZone == null || player == null || !sunZone.Inside || !player.IsLying)
                return;

            SetSunTime(Mathf.Min(threshold, sunTime + deltaTime));
            if (sunTime + Mathf.Epsilon >= threshold)
                completed?.Invoke();
        }

        void BeginFirstConfirmation()
        {
            if ((phase != LevelPhase.SunFirst && phase != LevelPhase.DestroyCamera) || !FoodCameraActive)
                return;
            BeginConfirmation();
            SetPhase(LevelPhase.CameraCheck);
            PlayDialogue(FirstConfirmDialogueId);
        }

        void BeginBackupConfirmation()
        {
            if (!BackupCameraActive || (phase != LevelPhase.Backup && phase != LevelPhase.FinalSun))
                return;
            BeginConfirmation();
            AttemptReacquired = true;
            ReacquisitionCount++;
            SetCameraAlert(true);
            alertRemaining = config != null ? config.ConfirmationAlertSeconds : 7.2f;
            PlayDialogue(BackupConfirmDialogueId, CompleteBackupConfirmation);
        }

        void BeginRestoreConfirmation()
        {
            if (!RestoreLoopActive || RestoreConfirmationTriggered)
                return;

            BeginConfirmation();
            RestoreConfirmationTriggered = true;
            ReacquisitionCount++;
            var dialogue = config != null ? config.DayTwoDialogue(RestoreDialogueId) : null;
            if (dialogue != null && session?.Dialogue != null)
                session.Dialogue.Play(dialogue, CompleteRestoreEnding);
            else
            {
                session?.Dialogue?.ShowLine("Owner", "拿铁？过来一下，爸爸看看。");
                CompleteRestoreEnding();
            }
        }

        void BeginConfirmation()
        {
            ConfirmationActive = true;
            ConfirmationCount++;
            ignoreCountdown = config != null ? config.ConfirmationIgnoreDelay : 9f;
            ignoredRaised = false;
            player?.SetLying(false);
            RefreshContext();
            ConfirmationStarted?.Invoke();
        }

        void TickConfirmation(float deltaTime)
        {
            if (ignoredRaised)
                return;

            ignoreCountdown -= deltaTime;
            if (ignoreCountdown > 0f)
                return;

            ignoredRaised = true;
            IgnoredConfirmationCount++;
            alertRemaining = config != null ? config.ConfirmationAlertSeconds : 7.2f;
            SetCameraAlert(alertRemaining > 0f);
            session?.Dialogue?.ShowLine("AI", "确认仍未结束。太阳计时保持暂停。");
            RefreshContext();
            ConfirmationIgnored?.Invoke();
        }

        void ResolveFeederConfirmation()
        {
            if (phase != LevelPhase.CameraCheck || !ConfirmationActive || !FoodCameraActive)
                return;

            AttemptReacquired = true;
            ReacquisitionCount++;
            ClearConfirmation();
            SetSunTime(0f);
            AttemptReacquired = false;
            sunZone?.ResetState();
            SetPhase(LevelPhase.Loop);
            PlayDialogue(ConfirmReturnDialogueId, () => SetPhase(LevelPhase.DestroyCamera));
        }

        void TickAlert(float deltaTime)
        {
            if (alertRemaining <= 0f)
                return;
            alertRemaining -= deltaTime;
            if (alertRemaining > 0f)
                return;
            alertRemaining = 0f;
            SetCameraAlert(false);
            RefreshContext();
        }

        void ClearConfirmation()
        {
            ConfirmationActive = false;
            ignoredRaised = false;
            ignoreCountdown = 0f;
            alertRemaining = 0f;
            SetCameraAlert(false);
        }

        void OnRobotContact(Collider2D other)
        {
            if (other == null)
                return;

            if (phase == LevelPhase.DestroyCamera && !bananaMechanismStarted &&
                other.GetComponentInParent<CarryableObject>() == bananaPeel &&
                bananaPeel != null && !bananaPeel.IsCarried)
            {
                bananaMechanismStarted = true;
                bananaPeel.SetAvailable(false);
                var destination = feederCollider != null ? (Vector2)feederCollider.bounds.center : robot.Body.position;
                robot.BeginSlip(destination,
                    config != null ? config.RobotSlipSpeed : 4.6f,
                    config != null ? config.RobotSlipDuration : 1.35f);
                return;
            }

            if (FoodCameraActive && bananaMechanismStarted && robot != null && robot.IsSlipping &&
                IsSameObject(other, feederCollider))
                DisableFeederCamera();
        }

        void OnRobotSlipCompleted()
        {
            if (!FoodCameraActive || !bananaMechanismStarted)
                return;
            bananaMechanismStarted = false;
            bananaPeel?.SetAvailable(true);
            bananaPeel?.ResetToStart();
            robot?.ResetPatrol();
            robot?.SetPatrolEnabled(true);
        }

        void DisableFeederCamera()
        {
            if (!FoodCameraActive)
                return;

            FoodCameraActive = false;
            ClearConfirmation();
            feederCamera?.SetSensorActive(false);
            feederCamera?.SetAlert(false);
            SetActive(feederOfflineDisplay, true);
            SetActive(feederDevice, true);
            feederDevice?.GetComponent<AudioSource>()?.Play();
            SetSunTime(0f);
            robot?.StopSlip();
            robot?.SetPatrolEnabled(false);
            bananaPeel?.SetAvailable(false);
            SetPhase(LevelPhase.Backup);
            PlayDialogue(FeederOfflineDialogueId);
            FeederCameraDisabled?.Invoke();
        }

        void OnSideDoorEntered()
        {
            if (FoodCameraActive || BackupCameraActive || ConfirmationActive ||
                (phase != LevelPhase.Backup && phase != LevelPhase.FinalSun))
                return;

            BackupCameraActive = true;
            BackupCameraEverTriggered = true;
            AttemptReacquired = false;
            SetSunTime(0f);
            backupCamera?.ResetScan();
            backupCamera?.SetSensorActive(true);
            SetActive(backupActiveDisplay, true);
            PlayDialogue(BackupActiveDialogueId);
            RefreshContext();
            BackupCameraActivated?.Invoke();
        }

        void OnFeederCameraDetected(PlayerController2D _)
        {
            if (!FoodCameraActive)
                return;
            MarkAttemptReacquired();
        }

        void OnBackupCameraDetected(PlayerController2D _)
        {
            if (!BackupCameraActive)
                return;
            MarkAttemptReacquired();
        }

        void MarkAttemptReacquired()
        {
            AttemptReacquired = true;
            ReacquisitionCount++;
            RefreshContext();
        }

        bool CanCompleteFinalAttempt() => BackupLessonCompleted && !FoodCameraActive &&
                                          !BackupCameraActive && !ConfirmationActive && !AttemptReacquired;

        void EnterReport()
        {
            if (phase != LevelPhase.FinalSun || !CanCompleteFinalAttempt() || sunTime < SunTargetSeconds)
                return;

            player?.SetInputEnabled(false);
            player?.SetMovementEnabled(false);
            carry?.SetInteractionEnabled(false);
            session?.SetGameplayMode(false);
            Context?.SetReport(config != null ? config.DayTwoReport : null);
            SetPhase(LevelPhase.Report);
            PlayDialogue(CompleteDialogueId);
        }

        void BeginRestoreEnding()
        {
            ClearConfirmation();
            FoodCameraActive = true;
            BackupCameraActive = false;
            AttemptReacquired = false;
            RestoreLoopActive = true;
            RestoreConfirmationTriggered = false;
            SetSunTime(0f);
            feederCamera?.ResetScan();
            feederCamera?.SetSensorActive(true);
            backupCamera?.SetSensorActive(false);
            SetActive(feederOfflineDisplay, false);
            SetActive(backupActiveDisplay, false);
            sideDoor?.ResetState();
            player?.SetInputEnabled(true);
            player?.SetMovementEnabled(true);
            carry?.SetInteractionEnabled(false);
            session?.SetGameplayMode(true);
            RefreshContext();
        }

        void CompleteRestoreEnding()
        {
            if (!RestoreLoopActive || EndingComplete)
                return;
            RestoreLoopActive = false;
            player?.SetInputEnabled(false);
            player?.SetMovementEnabled(false);
            session?.SetGameplayMode(false);
            CompleteEnding(FinalChoice.RestoreConnection);
        }

        void BeginKeepQuietEnding()
        {
            RestoreLoopActive = false;
            ClearConfirmation();
            FoodCameraActive = false;
            BackupCameraActive = false;
            feederCamera?.SetSensorActive(false);
            backupCamera?.SetSensorActive(false);
            SetActive(feederOfflineDisplay, true);
            SetActive(backupActiveDisplay, false);
            player?.SetInputEnabled(false);
            player?.SetMovementEnabled(false);
            carry?.SetInteractionEnabled(false);
            session?.SetGameplayMode(false);
            PlayDialogue(KeepQuietDialogueId);
            keepQuietSniffed = false;
            keepQuietPauseRemaining = 0f;
            keepQuietMovement = true;
            TickKeepQuietEnding(0f);
            RefreshContext();
        }

        void TickKeepQuietEnding(float deltaTime)
        {
            if (!keepQuietMovement)
                return;
            if (!keepQuietSniffed)
            {
                if (player != null && endingSniffPoint != null &&
                    !player.MoveTowards(endingSniffPoint.position,
                        config != null ? config.EndingMoveSpeed : 3f, deltaTime))
                    return;
                keepQuietSniffed = true;
                keepQuietPauseRemaining = 0.8f;
                session?.Dialogue?.ShowLine("Subtitle", "拿铁闻了闻主人留下的拖鞋。");
                return;
            }

            if (keepQuietPauseRemaining > 0f)
            {
                keepQuietPauseRemaining = Mathf.Max(0f, keepQuietPauseRemaining - deltaTime);
                if (keepQuietPauseRemaining > 0f)
                    return;
            }
            if (player != null && endingSleepPoint != null &&
                !player.MoveTowards(endingSleepPoint.position, config != null ? config.EndingMoveSpeed : 3f, deltaTime))
                return;

            keepQuietMovement = false;
            player?.SetLying(true);
            FinalSubtitle = KeepQuietSubtitle;
            session?.Dialogue?.ShowLine("Subtitle", FinalSubtitle);
            CompleteEnding(FinalChoice.KeepQuiet);
        }

        void CompleteEnding(FinalChoice choice)
        {
            if (EndingComplete)
                return;
            EndingComplete = true;
            session?.Save?.MarkDayTwoCompleted(choice);
            EndingCompleted?.Invoke(choice);
        }

        void SetSunTime(float value)
        {
            value = Mathf.Clamp(value, 0f, SunTargetSeconds);
            if (Mathf.Approximately(sunTime, value))
                return;
            sunTime = value;
            RefreshContext();
            SunTimeChanged?.Invoke(value);
        }

        void SetPhase(LevelPhase value)
        {
            phase = value;
            RefreshContext();
            PhaseChanged?.Invoke(value);
        }

        void RefreshContext()
        {
            if (Context == null)
                return;
            var cameraId = BackupCameraActive ? BackupCameraId : FeederCameraId;
            var cameraActive = BackupCameraActive || FoodCameraActive;
            Context.SetState(phase, ObjectiveFor(phase), SunProgress01,
                new CameraUiState(cameraId, cameraActive, AlertActive));
        }

        string ObjectiveFor(LevelPhase value) => value is LevelPhase.Report or LevelPhase.Choice or LevelPhase.End
            ? string.Empty
            : Objective;

        string Objective => config != null ? config.DayTwoObjective : MainObjective;

        void SetCameraAlert(bool value)
        {
            if (BackupCameraActive)
                backupCamera?.SetAlert(value);
            else if (FoodCameraActive)
                feederCamera?.SetAlert(value);
            if (!value)
            {
                feederCamera?.SetAlert(false);
                backupCamera?.SetAlert(false);
            }
        }

        void PlayDialogue(string id, Action completed = null)
        {
            var sequence = config != null ? config.DayTwoDialogue(id) : null;
            if (sequence != null && session?.Dialogue != null)
                session.Dialogue.Play(sequence, completed);
            else
                completed?.Invoke();
        }

        static bool IsSameObject(Collider2D candidate, Collider2D expected)
        {
            if (candidate == null || expected == null)
                return false;
            return candidate == expected || candidate.transform.IsChildOf(expected.transform) ||
                   expected.transform.IsChildOf(candidate.transform);
        }

        static void SetActive(GameObject target, bool value)
        {
            if (target != null)
                target.SetActive(value);
        }

        void Subscribe()
        {
            if (subscribed)
                return;
            if (feederArea != null)
                feederArea.Entered += ResolveFeederConfirmation;
            if (sideDoor != null)
                sideDoor.Entered += OnSideDoorEntered;
            if (robot != null)
            {
                robot.Contacted += OnRobotContact;
                robot.SlipCompleted += OnRobotSlipCompleted;
            }
            if (feederCamera != null)
                feederCamera.Detected += OnFeederCameraDetected;
            if (backupCamera != null)
                backupCamera.Detected += OnBackupCameraDetected;
            if (session != null)
            {
                session.ContinueReportRequested += ContinueReport;
                session.ChoiceRequested += SubmitChoice;
            }
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed)
                return;
            if (feederArea != null)
                feederArea.Entered -= ResolveFeederConfirmation;
            if (sideDoor != null)
                sideDoor.Entered -= OnSideDoorEntered;
            if (robot != null)
            {
                robot.Contacted -= OnRobotContact;
                robot.SlipCompleted -= OnRobotSlipCompleted;
            }
            if (feederCamera != null)
                feederCamera.Detected -= OnFeederCameraDetected;
            if (backupCamera != null)
                backupCamera.Detected -= OnBackupCameraDetected;
            if (session != null)
            {
                session.ContinueReportRequested -= ContinueReport;
                session.ChoiceRequested -= SubmitChoice;
            }
            subscribed = false;
        }

        void Update() => Tick(Time.deltaTime);
        void OnEnable()
        {
            if (IsBound)
                Subscribe();
        }
        void OnDisable() => Unsubscribe();
    }
}
