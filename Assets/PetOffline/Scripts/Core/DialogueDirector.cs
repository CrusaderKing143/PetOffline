using System;
using UnityEngine;

namespace PetOffline.Core
{
    public sealed class DialogueDirector : MonoBehaviour
    {
        public event Action<string, string> LineShown;
        public event Action SequenceFinished;

        public void ShowLine(string speaker, string line) => LineShown?.Invoke(speaker, line);
        public void FinishSequence() => SequenceFinished?.Invoke();
    }
}
