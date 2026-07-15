using UnityEngine;

namespace PetOffline.Gameplay
{
    [CreateAssetMenu(fileName = "CameraScan_", menuName = "Pet Offline/Camera Scan Config")]
    public sealed class CameraScanConfigSO : ScriptableObject
    {
        [SerializeField] float minimumAngle = -43f;
        [SerializeField] float maximumAngle = 43f;
        [SerializeField] float initialAngle;
        [SerializeField, Min(0f)] float scanDegreesPerSecond = 28f;
        [SerializeField, Min(0.01f)] float range = 7.1f;
        [SerializeField, Range(1f, 179f)] float fieldOfView = 54f;
        [SerializeField, Min(1f)] float alertSpeedMultiplier = 1.75f;
        [SerializeField, Min(1f)] float alertRangeMultiplier = 1.19f;
        [SerializeField, Min(1f)] float alertFieldOfViewMultiplier = 1.5f;
        [SerializeField, Min(0.01f)] float sampleInterval = 0.1f;
        [SerializeField, Min(0f)] float detectionHoldSeconds = 0.24f;
        [SerializeField, Range(4, 64)] int coneSegments = 18;
        [SerializeField] Color normalConeColor = new(0.15f, 0.65f, 1f, 0.45f);
        [SerializeField] Color alertConeColor = new(1f, 0.18f, 0.12f, 0.65f);

        public float MinimumAngle => minimumAngle;
        public float MaximumAngle => maximumAngle;
        public float InitialAngle => initialAngle;
        public float ScanDegreesPerSecond => scanDegreesPerSecond;
        public float Range => range;
        public float FieldOfView => fieldOfView;
        public float AlertSpeedMultiplier => alertSpeedMultiplier;
        public float AlertRangeMultiplier => alertRangeMultiplier;
        public float AlertFieldOfViewMultiplier => alertFieldOfViewMultiplier;
        public float SampleInterval => sampleInterval;
        public float DetectionHoldSeconds => detectionHoldSeconds;
        public int ConeSegments => coneSegments;
        public Color NormalConeColor => normalConeColor;
        public Color AlertConeColor => alertConeColor;

        public void Configure(
            float minAngle,
            float maxAngle,
            float startAngle,
            float degreesPerSecond,
            float viewRange,
            float viewAngle,
            float alertSpeed = 1.75f,
            float alertRange = 1.19f,
            float alertFieldOfView = 1.5f,
            float sensingInterval = 0.1f,
            float detectionHold = 0.24f)
        {
            minimumAngle = Mathf.Min(minAngle, maxAngle);
            maximumAngle = Mathf.Max(minAngle, maxAngle);
            initialAngle = Mathf.Clamp(startAngle, minimumAngle, maximumAngle);
            scanDegreesPerSecond = Mathf.Max(0f, degreesPerSecond);
            range = Mathf.Max(0.01f, viewRange);
            fieldOfView = Mathf.Clamp(viewAngle, 1f, 179f);
            alertSpeedMultiplier = Mathf.Max(1f, alertSpeed);
            alertRangeMultiplier = Mathf.Max(1f, alertRange);
            alertFieldOfViewMultiplier = Mathf.Max(1f, alertFieldOfView);
            sampleInterval = Mathf.Max(0.01f, sensingInterval);
            detectionHoldSeconds = Mathf.Max(0f, detectionHold);
        }
    }
}
