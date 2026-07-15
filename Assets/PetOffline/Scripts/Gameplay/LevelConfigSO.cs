using PetOffline.Core;
using UnityEngine;

namespace PetOffline.Gameplay
{
    [CreateAssetMenu(fileName = "Level_", menuName = "Pet Offline/Level Config")]
    public sealed class LevelConfigSO : ScriptableObject
    {
        [Header("Day 1 Content")]
        [SerializeField, RequiredReference] DialogueSequenceSO openingDialogue;
        [SerializeField, RequiredReference] ReportDefinitionSO dayOneReport;
        [SerializeField] string openingObjective = "等待会议连接";
        [SerializeField] string shoesObjective = "把主人的拖鞋放到摄像头 A 前";
        [SerializeField] string pillowObjective = "把老板抱枕送回狗窝";
        [SerializeField] string finalBarkObjective = "按 Space 汪叫回应会议";

        [Header("Day 1 Timing")]
        [SerializeField, Min(0.01f)] float shoeGoalHoldSeconds = 2f;
        [SerializeField, Min(0.01f)] float firstBossCallDelay = 14f;
        [SerializeField, Min(0.01f)] float subsequentBossCallDelay = 26f;
        [SerializeField, Min(0.01f)] float barkResponseSeconds = 3.6f;
        [SerializeField, Min(0f)] float safeWindowSeconds = 3f;
        [SerializeField, Min(0f)] float alertWindowSeconds = 7f;

        [Header("World Motion")]
        [SerializeField, Min(0.01f)] float playerMoveSpeed = 3.25f;
        [SerializeField, Min(0.01f)] float robotPatrolSpeed = 1.22f;
        [SerializeField, Min(1f)] float slipSpeedMultiplier = 1.6f;
        [SerializeField, Min(0.01f)] float slipDuration = 0.9f;
        [SerializeField, Min(0.01f)] float endingMoveSpeed = 3f;
        [SerializeField, Min(0f)] float endingSpeakerWaitSeconds = 1.5f;
        [SerializeField, Min(0f)] float endingBedWaitSeconds = 1f;

        public DialogueSequenceSO OpeningDialogue => openingDialogue;
        public ReportDefinitionSO DayOneReport => dayOneReport;
        public string OpeningObjective => openingObjective;
        public string ShoesObjective => shoesObjective;
        public string PillowObjective => pillowObjective;
        public string FinalBarkObjective => finalBarkObjective;
        public float ShoeGoalHoldSeconds => shoeGoalHoldSeconds;
        public float FirstBossCallDelay => firstBossCallDelay;
        public float SubsequentBossCallDelay => subsequentBossCallDelay;
        public float BarkResponseSeconds => barkResponseSeconds;
        public float SafeWindowSeconds => safeWindowSeconds;
        public float AlertWindowSeconds => alertWindowSeconds;
        public float PlayerMoveSpeed => playerMoveSpeed;
        public float RobotPatrolSpeed => robotPatrolSpeed;
        public float SlipSpeedMultiplier => slipSpeedMultiplier;
        public float SlipDuration => slipDuration;
        public float EndingMoveSpeed => endingMoveSpeed;
        public float EndingSpeakerWaitSeconds => endingSpeakerWaitSeconds;
        public float EndingBedWaitSeconds => endingBedWaitSeconds;

        public void ConfigureObjectives(string opening, string shoes, string pillow, string finalBark)
        {
            openingObjective = opening ?? string.Empty;
            shoesObjective = shoes ?? string.Empty;
            pillowObjective = pillow ?? string.Empty;
            finalBarkObjective = finalBark ?? string.Empty;
        }

        public void Configure(
            DialogueSequenceSO opening,
            ReportDefinitionSO report,
            float shoeHoldSeconds = 2f,
            float firstCallDelay = 14f,
            float nextCallDelay = 26f,
            float responseSeconds = 3.6f,
            float safeSeconds = 3f,
            float alertSeconds = 7f)
        {
            openingDialogue = opening;
            dayOneReport = report;
            shoeGoalHoldSeconds = Mathf.Max(0.01f, shoeHoldSeconds);
            firstBossCallDelay = Mathf.Max(0.01f, firstCallDelay);
            subsequentBossCallDelay = Mathf.Max(0.01f, nextCallDelay);
            barkResponseSeconds = Mathf.Max(0.01f, responseSeconds);
            safeWindowSeconds = Mathf.Max(0f, safeSeconds);
            alertWindowSeconds = Mathf.Max(0f, alertSeconds);
        }

        public void ConfigureWorldMotion(
            float slideMultiplier = 1.6f,
            float slideSeconds = 0.9f,
            float endingSpeed = 3f,
            float speakerWaitSeconds = 1.5f,
            float bedWaitSeconds = 1f,
            float moveSpeed = 3.25f,
            float patrolSpeed = 1.22f)
        {
            playerMoveSpeed = Mathf.Max(0.01f, moveSpeed);
            robotPatrolSpeed = Mathf.Max(0.01f, patrolSpeed);
            slipSpeedMultiplier = Mathf.Max(1f, slideMultiplier);
            slipDuration = Mathf.Max(0.01f, slideSeconds);
            endingMoveSpeed = Mathf.Max(0.01f, endingSpeed);
            endingSpeakerWaitSeconds = Mathf.Max(0f, speakerWaitSeconds);
            endingBedWaitSeconds = Mathf.Max(0f, bedWaitSeconds);
        }
    }
}
