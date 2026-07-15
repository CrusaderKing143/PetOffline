using System;
using System.Collections.Generic;
using UnityEngine;

namespace PetOffline.Core
{
    [Serializable]
    public struct DialogueLine
    {
        [SerializeField] string speakerId;
        [SerializeField, TextArea] string text;
        [SerializeField, Min(0f)] float duration;

        public DialogueLine(string speakerId, string text, float duration)
        {
            this.speakerId = speakerId ?? string.Empty;
            this.text = text ?? string.Empty;
            this.duration = Mathf.Max(0f, duration);
        }

        public string SpeakerId => speakerId;
        public string Text => text;
        public float Duration => duration;
    }

    [CreateAssetMenu(fileName = "Dialogue_", menuName = "Pet Offline/Dialogue Sequence")]
    public sealed class DialogueSequenceSO : ScriptableObject
    {
        [SerializeField] string sequenceId = string.Empty;
        [SerializeField] bool pauseGameplay;
        [SerializeField] bool skippable = true;
        [SerializeField] string completionEventId = string.Empty;
        [SerializeField] DialogueLine[] lines = Array.Empty<DialogueLine>();

        public string SequenceId => sequenceId;
        public bool PauseGameplay => pauseGameplay;
        public bool Skippable => skippable;
        public string CompletionEventId => completionEventId;
        public IReadOnlyList<DialogueLine> Lines => lines;

        public void Configure(
            string id,
            DialogueLine[] content,
            bool pausesGameplay = false,
            bool canSkip = true,
            string completionId = "")
        {
            sequenceId = id ?? string.Empty;
            lines = content ?? Array.Empty<DialogueLine>();
            pauseGameplay = pausesGameplay;
            skippable = canSkip;
            completionEventId = completionId ?? string.Empty;
        }
    }
}
