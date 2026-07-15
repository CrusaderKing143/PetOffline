using System;
using PetOffline.Core;
using UnityEngine;

namespace PetOffline.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CameraVisionSensor2D : MonoBehaviour
    {
        [SerializeField, RequiredReference] CameraScanConfigSO config;
        [SerializeField, RequiredReference] PlayerController2D target;
        [SerializeField, RequiredReference] Transform scanPivot;
        [SerializeField, RequiredReference] LineRenderer worldCone;
        [SerializeField] LayerMask occlusionMask;

        float currentAngle;
        float scanDirection = 1f;
        float sampleCountdown;
        float visibleSeconds;
        Quaternion baseLocalRotation = Quaternion.identity;
        bool sensorActive = true;
        bool alert;
        bool detectionSuppressed;
        bool detectionLatched;

        public float CurrentAngle => currentAngle;
        public bool SensorActive => sensorActive;
        public bool IsAlert => alert;
        public bool DetectionSuppressed => detectionSuppressed;
        public bool TargetVisible { get; private set; }
        public float VisibleSeconds => visibleSeconds;
        public bool DetectionReady => TargetVisible && config != null && visibleSeconds + Mathf.Epsilon >= config.DetectionHoldSeconds;
        public int DetectionCount { get; private set; }
        public event Action<PlayerController2D> Detected;
        public event Action<bool> VisibilityChanged;

        public void Configure(
            CameraScanConfigSO scanConfig,
            PlayerController2D player,
            Transform pivot,
            LineRenderer cone,
            LayerMask visionOcclusionMask)
        {
            config = scanConfig;
            target = player;
            scanPivot = pivot;
            worldCone = cone;
            occlusionMask = visionOcclusionMask;
            if (scanPivot != null)
                baseLocalRotation = scanPivot.localRotation;
            ResetScan();
        }

        public void SetSensorActive(bool value)
        {
            sensorActive = value;
            if (!value)
                ResetDetectionState();
            if (worldCone != null)
                worldCone.enabled = value;
        }

        public void SetAlert(bool value)
        {
            alert = value;
            RefreshWorldCone();
        }

        public void SetDetectionSuppressed(bool value)
        {
            detectionSuppressed = value;
            if (value)
                ResetDetectionState();
        }

        public void ResetScan()
        {
            if (config == null)
                return;

            currentAngle = config.InitialAngle;
            scanDirection = 1f;
            sampleCountdown = 0f;
            alert = false;
            detectionSuppressed = false;
            ResetDetectionState();
            ApplyAngle();
            RefreshWorldCone();
        }

        public bool CanSeeTarget()
        {
            if (!sensorActive || detectionSuppressed || target == null || scanPivot == null || config == null)
                return false;

            var toTarget = target.Position - (Vector2)scanPivot.position;
            var range = config.Range * (alert ? config.AlertRangeMultiplier : 1f);
            if (toTarget.sqrMagnitude > range * range)
                return false;

            var fieldOfView = Mathf.Min(179f, config.FieldOfView * (alert ? config.AlertFieldOfViewMultiplier : 1f));
            if (Vector2.Angle(scanPivot.right, toTarget) > fieldOfView * 0.5f)
                return false;

            return Physics2D.Linecast(scanPivot.position, target.Position, occlusionMask).collider == null;
        }

        public void Tick(float deltaTime)
        {
            if (!sensorActive || config == null || scanPivot == null || deltaTime <= 0f)
                return;

            if (!detectionSuppressed)
                AdvanceScan(deltaTime);
            sampleCountdown -= deltaTime;
            while (sampleCountdown <= 0f)
            {
                sampleCountdown += config.SampleInterval;
                SampleTarget();
            }
            RefreshWorldCone();
        }

        public void ResetDetectionState()
        {
            visibleSeconds = 0f;
            detectionLatched = false;
            SetTargetVisible(false);
        }

        void SampleTarget()
        {
            var visible = CanSeeTarget();
            SetTargetVisible(visible);
            if (!visible)
            {
                visibleSeconds = 0f;
                detectionLatched = false;
                return;
            }

            visibleSeconds += config.SampleInterval;
            if (detectionLatched || visibleSeconds + Mathf.Epsilon < config.DetectionHoldSeconds)
                return;

            detectionLatched = true;
            DetectionCount++;
            Detected?.Invoke(target);
        }

        void SetTargetVisible(bool value)
        {
            if (TargetVisible == value)
                return;
            TargetVisible = value;
            VisibilityChanged?.Invoke(value);
        }

        void AdvanceScan(float deltaTime)
        {
            var speed = config.ScanDegreesPerSecond * (alert ? config.AlertSpeedMultiplier : 1f);
            if (speed <= 0f || Mathf.Approximately(config.MinimumAngle, config.MaximumAngle))
                return;

            currentAngle += scanDirection * speed * deltaTime;
            if (currentAngle >= config.MaximumAngle)
            {
                currentAngle = config.MaximumAngle;
                scanDirection = -1f;
            }
            else if (currentAngle <= config.MinimumAngle)
            {
                currentAngle = config.MinimumAngle;
                scanDirection = 1f;
            }
            ApplyAngle();
        }

        void ApplyAngle()
        {
            if (scanPivot != null)
                scanPivot.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, currentAngle);
        }

        public void RefreshWorldCone()
        {
            if (worldCone == null || scanPivot == null || config == null)
                return;

            var segments = config.ConeSegments;
            var range = config.Range * (alert ? config.AlertRangeMultiplier : 1f);
            var fieldOfView = Mathf.Min(179f, config.FieldOfView * (alert ? config.AlertFieldOfViewMultiplier : 1f));
            var origin = scanPivot.position;
            var forwardAngle = Mathf.Atan2(scanPivot.right.y, scanPivot.right.x) * Mathf.Rad2Deg;
            worldCone.useWorldSpace = true;
            worldCone.positionCount = segments + 3;
            worldCone.SetPosition(0, origin);
            for (var i = 0; i <= segments; i++)
            {
                var angle = forwardAngle - fieldOfView * 0.5f + fieldOfView * i / segments;
                var direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                var hit = Physics2D.Raycast(origin, direction, range, occlusionMask);
                worldCone.SetPosition(i + 1, (Vector2)origin + direction * (hit.collider != null ? hit.distance : range));
            }
            worldCone.SetPosition(segments + 2, origin);
            var color = alert ? config.AlertConeColor : config.NormalConeColor;
            worldCone.startColor = color;
            worldCone.endColor = color;
        }

        void FixedUpdate() => Tick(Time.fixedDeltaTime);
        void Awake()
        {
            if (scanPivot != null)
                baseLocalRotation = scanPivot.localRotation;
            ResetScan();
        }
    }
}
