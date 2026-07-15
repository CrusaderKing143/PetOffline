using System;
using PetOffline.Core;
using UnityEngine;

namespace PetOffline.Gameplay
{
    public sealed class LevelSceneContext : MonoBehaviour, ILevelViewModel
    {
        [SerializeField] LevelId level;
        [SerializeField] LevelPhase phase;
        [SerializeField] string objective = string.Empty;
        [SerializeField, Range(0f, 1f)] float progress01;
        [SerializeField] string cameraId = string.Empty;
        [SerializeField] bool cameraActive;
        [SerializeField] bool cameraAlert;
        [SerializeField] ReportDefinitionSO report;

        public LevelId Level => level;
        public LevelPhase Phase => phase;
        public string Objective => objective;
        public float Progress01 => progress01;
        public CameraUiState CameraState => new(cameraId, cameraActive, cameraAlert);
        public ReportDefinitionSO Report => report;
        public event Action Changed;

        public void Configure(LevelId value, string initialObjective)
        {
            level = value;
            objective = initialObjective;
            phase = value == LevelId.Day1 ? LevelPhase.Opening : LevelPhase.Start;
            progress01 = 0f;
            cameraId = string.Empty;
            cameraActive = false;
            cameraAlert = false;
            report = null;
        }

        public void SetState(LevelPhase value, string newObjective, float progress, CameraUiState camera)
        {
            phase = value;
            objective = newObjective;
            progress01 = Mathf.Clamp01(progress);
            cameraId = camera.CameraId;
            cameraActive = camera.Active;
            cameraAlert = camera.Alert;
            Changed?.Invoke();
        }

        public void SetReport(ReportDefinitionSO value)
        {
            report = value;
            Changed?.Invoke();
        }
    }
}
