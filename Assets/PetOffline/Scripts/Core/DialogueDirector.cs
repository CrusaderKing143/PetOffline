using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PetOffline.Core
{
    public sealed class DialogueDirector : MonoBehaviour
    {
        Coroutine playing;
        Action completion;
        readonly Queue<DialogueRequest> queued = new();
        bool completing;

        readonly struct DialogueRequest
        {
            public DialogueRequest(DialogueSequenceSO sequence, Action onComplete)
            {
                Sequence = sequence;
                OnComplete = onComplete;
            }

            public DialogueSequenceSO Sequence { get; }
            public Action OnComplete { get; }
        }

        public bool IsPlaying => playing != null || completing || queued.Count > 0;
        public event Action<string, string> LineShown;
        public event Action SequenceFinished;

        public void Play(DialogueSequenceSO sequence, Action onComplete = null)
        {
            if (playing != null || completing || queued.Count > 0)
            {
                queued.Enqueue(new DialogueRequest(sequence, onComplete));
                return;
            }

            StartSequence(sequence, onComplete);
        }

        void StartSequence(DialogueSequenceSO sequence, Action onComplete)
        {
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
            queued.Clear();
            completing = false;
        }

        void CompleteSequence()
        {
            var callback = completion;
            completion = null;
            completing = true;
            SequenceFinished?.Invoke();
            callback?.Invoke();
            completing = false;
            if (playing != null || queued.Count == 0)
                return;
            var next = queued.Dequeue();
            StartSequence(next.Sequence, next.OnComplete);
        }

        void OnDisable() => Stop();
    }
}
