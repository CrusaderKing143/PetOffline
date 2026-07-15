using System;
using System.Collections.Generic;
using UnityEngine;

namespace PetOffline.Core
{
    [Serializable]
    public struct ReportField
    {
        [SerializeField] string label;
        [SerializeField, TextArea] string value;

        public ReportField(string label, string value)
        {
            this.label = label ?? string.Empty;
            this.value = value ?? string.Empty;
        }

        public string Label => label;
        public string Value => value;
    }

    [CreateAssetMenu(fileName = "Report_", menuName = "Pet Offline/Report Definition")]
    public sealed class ReportDefinitionSO : ScriptableObject
    {
        [SerializeField] string reportId = string.Empty;
        [SerializeField] string title = string.Empty;
        [SerializeField] ReportField[] fields = Array.Empty<ReportField>();
        [SerializeField, TextArea] string warning = string.Empty;
        [SerializeField] string continueLabel = "继续";
        [SerializeField, TextArea] string choicePrompt = string.Empty;
        [SerializeField] FinalChoice recommendedChoice = FinalChoice.KeepQuiet;

        public string ReportId => reportId;
        public string Title => title;
        public IReadOnlyList<ReportField> Fields => fields;
        public string Warning => warning;
        public string ContinueLabel => continueLabel;
        public string ChoicePrompt => choicePrompt;
        public FinalChoice RecommendedChoice => recommendedChoice;

        public void Configure(
            string id,
            string reportTitle,
            ReportField[] content,
            string warningText = "",
            string continueText = "继续",
            string prompt = "",
            FinalChoice recommended = FinalChoice.KeepQuiet)
        {
            reportId = id ?? string.Empty;
            title = reportTitle ?? string.Empty;
            fields = content ?? Array.Empty<ReportField>();
            warning = warningText ?? string.Empty;
            continueLabel = continueText ?? string.Empty;
            choicePrompt = prompt ?? string.Empty;
            recommendedChoice = recommended;
        }
    }
}
