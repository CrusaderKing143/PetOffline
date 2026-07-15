using System;
using System.Collections;
using UnityEngine;

namespace PetOffline.Core
{
    public sealed class DialogueDirector : MonoBehaviour
    {
        Coroutine playing;
        Action completion;

        public bool IsPlaying => playing != null;
        public event Action<string, string> LineShown;
        public event Action SequenceFinished;

        public void Play(DialogueSequenceSO sequence, Action onComplete = null)
        {
            Stop();
            completion = onComplete;
            if (sequence == null || sequence.Lines.Count == 0)
            {
                CompleteSequence();
                return;
            }

            playing = StartCoroutine(PlayRoutine(sequence));
        }

        IEnumerator PlayRoutine(DialogueSequenceSO sequence)
        {
            for (var i = 0; i < sequence.Lines.Count; i++)
            {
                var line = sequence.Lines[i];
                ShowLine(line.SpeakerId, line.Text);
                if (line.Duration > 0f)
                    yield return new WaitForSecondsRealtime(line.Duration);
                else
                    yield return null;
            }

            playing = null;
            CompleteSequence();
        }

        public void ShowLine(string speaker, string line) => LineShown?.Invoke(speaker, line);

        public void FinishSequence()
        {
            if (playing == null)
                return;

            StopCoroutine(playing);
            playing = null;
            CompleteSequence();
        }

        public void Stop()
        {
            if (playing != null)
                StopCoroutine(playing);
            playing = null;
            completion = null;
        }

        void CompleteSequence()
        {
            var callback = completion;
            completion = null;
            SequenceFinished?.Invoke();
            callback?.Invoke();
        }

        void OnDisable() => Stop();
    }
}
