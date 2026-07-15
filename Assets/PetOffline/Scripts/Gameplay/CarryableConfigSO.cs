using UnityEngine;

namespace PetOffline.Gameplay
{
    [CreateAssetMenu(fileName = "Carryable_", menuName = "Pet Offline/Carryable Config")]
    public sealed class CarryableConfigSO : ScriptableObject
    {
        [SerializeField] bool heavy;
        [SerializeField, Range(0.1f, 1f)] float carrySpeedMultiplier = 0.85f;
        [SerializeField, Min(0f)] float robotPushDistance;

        public bool Heavy => heavy;
        public bool DropsOnBark => heavy;
        public float CarrySpeedMultiplier => carrySpeedMultiplier;
        public float RobotPushDistance => robotPushDistance;

        public void Configure(bool isHeavy, float speedMultiplier, float pushDistance)
        {
            heavy = isHeavy;
            carrySpeedMultiplier = Mathf.Clamp(speedMultiplier, 0.1f, 1f);
            robotPushDistance = Mathf.Max(0f, pushDistance);
        }
    }
}
