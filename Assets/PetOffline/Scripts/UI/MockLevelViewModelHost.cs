using System;
using PetOffline.Core;
using UnityEngine;

namespace PetOffline.UI
{
    public sealed class MockLevelViewModelHost : MonoBehaviour, ILevelViewModel
    {
        [SerializeField, RequiredReference] UIRootController uiRoot;
        [SerializeField] ReportDefinitionSO report;

        public LevelId Level => LevelId.Day2;
        public LevelPhase Phase => LevelPhase.FinalSun;
        public string Objective => "让拿铁晒满20秒太阳";
        public float Progress01 => 0.5f;
        public CameraUiState CameraState => new("FOOD", false, false);
        public ReportDefinitionSO Report => report;
        public event Action Changed;

        public void Configure(UIRootController root) => uiRoot = root;

        void Start()
        {
            uiRoot?.Bind(this);
            Changed?.Invoke();
        }
    }
}
