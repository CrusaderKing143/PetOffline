using System;
using System.Collections;
using PetOffline.Core;
using UnityEngine;

namespace PetOffline.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class LevelOneFlowController : LevelFlowController
    {
        const string CameraBId = "B";

        [SerializeField, RequiredReference] LevelConfigSO config;
        [SerializeField, RequiredReference] PlayerController2D player;
        [SerializeField, RequiredReference] CarryController carry;
        [SerializeField, RequiredReference] CarryableObject slipper;
        [SerializeField, RequiredReference] CarryableObject pillow;
        [SerializeField, RequiredReference] CarryGoalZone2D cameraAGoal;
        [SerializeField, RequiredReference] CarryGoalZone2D dogBedGoal;
        [SerializeField, RequiredReference] CameraVisionSensor2D cameraB;
        [SerializeField, RequiredReference] Transform playerSpawn;
        [SerializeField, RequiredReference] Transform pillowRetrySpawn;
        [SerializeField, RequiredReference] Transform endingSpeakerPoint;
        [SerializeField, RequiredReference] Transform endingBedPoint;

        GameSession session;
        Coroutine endingRoutine;
        LevelPhase phase;
        float bossCallCountdown;
        float bossResponseRemaining;
        float safeWindowRemaining;
        float alertRemaining;
        bool subscribed;
        bool openingReady;

        public LevelPhase Phase => phase;
        public bool IsBound { get; private set; }
        public bool OpeningReady => openingReady;
        public bool ShoesCompleted { get; private set; }
        public bool PillowCompleted { get; private set; }
        public bool BossCallActive { get; private set; }
        public float BossCallRemaining => BossCallActive ? bossResponseRemaining : bossCallCountdown;
        public bool SafeWindowActive => safeWindowRemaining > 0f;
        public bool AlertActive => alertRemaining > 0f;
        public int ShoesResetCount { get; private set; }
        public int PillowResetCount { get; private set; }
        public int SuccessfulBossCalls { get; private set; }
        public int MissedBossCalls { get; private set; }

        public event Action<LevelPhase> PhaseChanged;
        public event Action<LevelPhase> TaskReset;
        public event Action BossCallStarted;
        public event Action<bool> BossCallResolved;

        public void Configure(
            LevelSceneContext context,
            LevelConfigSO levelConfig,
            PlayerController2D latte,
            CarryController carryController,
            CarryableObject ownerSlipper,
            CarryableObject bossPillow,
            CarryGoalZone2D shoeGoal,
            CarryGoalZone2D pillowGoal,
            CameraVisionSensor2D hostileCamera,
            Transform latteSpawn,
            Transform pillowRetry,
            Transform speakerPoint,
            Transform bedPoint)
        {
            Unsubscribe();
            base.Configure(context);
            config = levelConfig;
            player = latte;
            carry = carryController;
            slipper = ownerSlipper;
            pillow = bossPillow;
            cameraAGoal = shoeGoal;
            dogBedGoal = pillowGoal;
            cameraB = hostileCamera;
            playerSpawn = latteSpawn;
            pillowRetrySpawn = pillowRetry;
            endingSpeakerPoint = speakerPoint;
            endingBedPoint = bedPoint;
            if (IsBound && isActiveAndEnabled)
                Subscribe();
        }

        public override void Bind(GameSession owner)
        {
            if (endingRoutine != null)
                StopCoroutine(endingRoutine);

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

            TickCameraWindows(deltaTime);
            if (phase != LevelPhase.TaskShoes && phase != LevelPhase.TaskPillow)
                return;

            if (BossCallActive)
            {
                bossResponseRemaining -= deltaTime;
                if (bossResponseRemaining <= 0f)
                    ResolveBossCall(false);
                return;
            }

            bossCallCountdown -= deltaTime;
            if (bossCallCountdown <= 0f)
                StartBossCall();
        }

        public void NotifyCameraDetection()
        {
            if (phase == LevelPhase.TaskShoes && carry != null && carry.Held == slipper)
                ResetShoesTask();
            else if (phase == LevelPhase.TaskPillow && carry != null && carry.Held == pillow)
                ResetPillowTask();
        }

        public void ContinueReport()
        {
            if (phase != LevelPhase.Report)
                return;

            SetPhase(LevelPhase.Ending);
            session?.SetGameplayMode(false);
            player?.SetInputEnabled(false);
            player?.SetMovementEnabled(false);
            carry?.SetInteractionEnabled(false);
            endingRoutine = StartCoroutine(PlayEnding());
        }

        void InitializeLevel()
        {
            ShoesCompleted = false;
            PillowCompleted = false;
            ShoesResetCount = 0;
            PillowResetCount = 0;
            SuccessfulBossCalls = 0;
            MissedBossCalls = 0;
            openingReady = false;
            CancelBossCall();
            safeWindowRemaining = 0f;
            alertRemaining = 0f;

            if (carry != null && carry.Held != null)
                carry.ForceDrop(player != null ? player.Position : Vector2.zero);

            slipper?.SetAvailable(true);
            slipper?.ResetToStart();
            pillow?.SetAvailable(false);
            pillow?.ResetToStart();
            cameraAGoal?.ResetProgress();
            dogBedGoal?.ResetProgress();
            if (cameraB != null)
            {
                cameraB.SetSensorActive(true);
                cameraB.ResetScan();
            }

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

            Context?.Configure(LevelId.Day1, ObjectiveFor(LevelPhase.Opening));
            Context?.SetReport(null);
            SetPhase(LevelPhase.Opening);

            if (session?.Dialogue != null && config?.OpeningDialogue != null)
                session.Dialogue.Play(config.OpeningDialogue, MarkOpeningReady);
            else
                MarkOpeningReady();
        }

        void MarkOpeningReady() => openingReady = true;

        void OnBarked()
        {
            if (phase == LevelPhase.Opening && openingReady)
            {
                EnterShoesTask();
                return;
            }

            if (BossCallActive && (phase == LevelPhase.TaskShoes || phase == LevelPhase.TaskPillow))
            {
                ResolveBossCall(true);
                return;
            }

            if (phase == LevelPhase.FinalBark)
                EnterReport();
        }

        void EnterShoesTask()
        {
            bossCallCountdown = config != null ? config.FirstBossCallDelay : 14f;
            player?.SetMovementEnabled(true);
            carry?.SetInteractionEnabled(true);
            SetPhase(LevelPhase.TaskShoes);
        }

        void OnShoesGoalCompleted()
        {
            if (phase != LevelPhase.TaskShoes)
                return;

            ShoesCompleted = true;
            slipper?.SetAvailable(false);
            pillow?.SetAvailable(true);
            SetPhase(LevelPhase.TaskPillow);
        }

        void OnPillowGoalCompleted()
        {
            if (phase != LevelPhase.TaskPillow)
                return;

            PillowCompleted = true;
            pillow?.SetAvailable(false);
            CancelBossCall();
            ClearCameraWindows();
            SetPhase(LevelPhase.FinalBark);
        }

        void EnterReport()
        {
            player?.SetInputEnabled(false);
            player?.SetMovementEnabled(false);
            carry?.SetInteractionEnabled(false);
            session?.SetGameplayMode(false);
            Context?.SetReport(config != null ? config.DayOneReport : null);
            SetPhase(LevelPhase.Report);
        }

        void StartBossCall()
        {
            BossCallActive = true;
            bossResponseRemaining = config != null ? config.BarkResponseSeconds : 3.6f;
            session?.Dialogue?.ShowLine("Boss", "拿铁？汪一声回应一下。");
            RefreshContext();
            BossCallStarted?.Invoke();
        }

        void ResolveBossCall(bool success)
        {
            if (!BossCallActive)
                return;

            BossCallActive = false;
            bossResponseRemaining = 0f;
            bossCallCountdown = config != null ? config.SubsequentBossCallDelay : 26f;
            if (success)
            {
                SuccessfulBossCalls++;
                session?.Dialogue?.ShowLine("Owner", "听见了，拿铁在。");
                alertRemaining = 0f;
                cameraB?.SetAlert(false);
                safeWindowRemaining = config != null ? config.SafeWindowSeconds : 3f;
                cameraB?.SetDetectionSuppressed(safeWindowRemaining > 0f);
            }
            else
            {
                MissedBossCalls++;
                session?.Dialogue?.ShowLine("AI", "未收到回应，正在提高监控灵敏度。");
                safeWindowRemaining = 0f;
                cameraB?.SetDetectionSuppressed(false);
                alertRemaining = config != null ? config.AlertWindowSeconds : 7f;
                cameraB?.SetAlert(alertRemaining > 0f);
            }
            RefreshContext();
            BossCallResolved?.Invoke(success);
        }

        void TickCameraWindows(float deltaTime)
        {
            if (safeWindowRemaining > 0f)
            {
                safeWindowRemaining -= deltaTime;
                if (safeWindowRemaining <= 0f)
                {
                    safeWindowRemaining = 0f;
                    cameraB?.SetDetectionSuppressed(false);
                    RefreshContext();
                }
            }

            if (alertRemaining <= 0f)
                return;

            alertRemaining -= deltaTime;
            if (alertRemaining <= 0f)
            {
                alertRemaining = 0f;
                cameraB?.SetAlert(false);
                RefreshContext();
            }
        }

        void ResetShoesTask()
        {
            ShoesResetCount++;
            var spawn = playerSpawn != null ? (Vector2)playerSpawn.position : player != null ? player.Position : Vector2.zero;
            if (carry != null && carry.Held == slipper)
                carry.ForceDrop(spawn);
            slipper?.SetAvailable(true);
            slipper?.ResetToStart();
            player?.ResetTo(spawn);
            cameraAGoal?.ResetProgress();
            ResetCameraAfterDetection();
            TaskReset?.Invoke(LevelPhase.TaskShoes);
        }

        void ResetPillowTask()
        {
            PillowResetCount++;
            var playerPosition = playerSpawn != null ? (Vector2)playerSpawn.position : player != null ? player.Position : Vector2.zero;
            var pillowPosition = pillowRetrySpawn != null ? (Vector2)pillowRetrySpawn.position : pillow != null ? (Vector2)pillow.transform.position : Vector2.zero;
            if (carry != null && carry.Held == pillow)
                carry.ForceDrop(pillowPosition);
            pillow?.SetAvailable(true);
            pillow?.ResetTo(pillowPosition);
            player?.ResetTo(playerPosition);
            dogBedGoal?.ResetProgress();
            ResetCameraAfterDetection();
            TaskReset?.Invoke(LevelPhase.TaskPillow);
        }

        void ResetCameraAfterDetection()
        {
            safeWindowRemaining = 0f;
            alertRemaining = 0f;
            cameraB?.ResetScan();
            RefreshContext();
        }

        void ClearCameraWindows()
        {
            safeWindowRemaining = 0f;
            alertRemaining = 0f;
            cameraB?.SetDetectionSuppressed(false);
            cameraB?.SetAlert(false);
        }

        void CancelBossCall()
        {
            BossCallActive = false;
            bossResponseRemaining = 0f;
            bossCallCountdown = 0f;
        }

        IEnumerator PlayEnding()
        {
            yield return MovePlayerTo(endingSpeakerPoint);
            if (config != null && config.EndingSpeakerWaitSeconds > 0f)
                yield return new WaitForSeconds(config.EndingSpeakerWaitSeconds);
            yield return MovePlayerTo(endingBedPoint);
            if (config != null && config.EndingBedWaitSeconds > 0f)
                yield return new WaitForSeconds(config.EndingBedWaitSeconds);

            endingRoutine = null;
            SetPhase(LevelPhase.Complete);
            session?.SetGameplayMode(false);
            session?.CompleteDayOne();
        }

        IEnumerator MovePlayerTo(Transform destination)
        {
            if (player == null || destination == null)
                yield break;

            var speed = config != null ? config.EndingMoveSpeed : 3f;
            while (!player.MoveTowards(destination.position, speed))
                yield return new WaitForFixedUpdate();
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

            var progress = phase == LevelPhase.TaskShoes && cameraAGoal != null ? cameraAGoal.Progress01 : 0f;
            var cameraState = new CameraUiState(CameraBId, cameraB != null && cameraB.SensorActive, cameraB != null && cameraB.IsAlert);
            Context.SetState(phase, ObjectiveFor(phase), progress, cameraState);
        }

        string ObjectiveFor(LevelPhase value)
        {
            if (BossCallActive && (value == LevelPhase.TaskShoes || value == LevelPhase.TaskPillow))
                return "老板点名：按 Space 汪叫回应";

            return value switch
            {
                LevelPhase.Opening => config != null ? config.OpeningObjective : "等待会议连接",
                LevelPhase.TaskShoes => config != null ? config.ShoesObjective : "把主人的拖鞋放到摄像头 A 前",
                LevelPhase.TaskPillow => config != null ? config.PillowObjective : "把老板抱枕送回狗窝",
                LevelPhase.FinalBark => config != null ? config.FinalBarkObjective : "按 Space 汪叫回应会议",
                _ => string.Empty
            };
        }

        void OnGoalProgressChanged(float _) => RefreshContext();
        void OnCameraDetected(PlayerController2D _) => NotifyCameraDetection();
        void OnHeldChanged(CarryableObject item)
        {
            if (item != null && cameraB != null && cameraB.DetectionReady)
                NotifyCameraDetection();
        }

        void Subscribe()
        {
            if (subscribed)
                return;
            if (player != null)
                player.Barked += OnBarked;
            if (carry != null)
                carry.HeldChanged += OnHeldChanged;
            if (cameraAGoal != null)
            {
                cameraAGoal.Completed += OnShoesGoalCompleted;
                cameraAGoal.ProgressChanged += OnGoalProgressChanged;
            }
            if (dogBedGoal != null)
                dogBedGoal.Completed += OnPillowGoalCompleted;
            if (cameraB != null)
                cameraB.Detected += OnCameraDetected;
            if (session != null)
                session.ContinueReportRequested += ContinueReport;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed)
                return;
            if (player != null)
                player.Barked -= OnBarked;
            if (carry != null)
                carry.HeldChanged -= OnHeldChanged;
            if (cameraAGoal != null)
            {
                cameraAGoal.Completed -= OnShoesGoalCompleted;
                cameraAGoal.ProgressChanged -= OnGoalProgressChanged;
            }
            if (dogBedGoal != null)
                dogBedGoal.Completed -= OnPillowGoalCompleted;
            if (cameraB != null)
                cameraB.Detected -= OnCameraDetected;
            if (session != null)
                session.ContinueReportRequested -= ContinueReport;
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
