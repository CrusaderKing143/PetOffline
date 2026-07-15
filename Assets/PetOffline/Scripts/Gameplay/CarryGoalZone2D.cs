using System;
using PetOffline.Core;
using UnityEngine;

namespace PetOffline.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class CarryGoalZone2D : MonoBehaviour
    {
        [SerializeField, RequiredReference] Collider2D goalTrigger;
        [SerializeField, RequiredReference] CarryableObject target;
        [SerializeField, Min(0f)] float holdSeconds;

        public CarryableObject Target => target;
        public bool IsInside { get; private set; }
        public bool IsComplete { get; private set; }
        public float Elapsed { get; private set; }
        public float HoldSeconds => holdSeconds;
        public float Progress01 => IsComplete || holdSeconds <= 0f ? (IsInside ? 1f : 0f) : Elapsed / holdSeconds;
        public event Action Completed;
        public event Action<float> ProgressChanged;

        public void Configure(Collider2D trigger, CarryableObject requiredItem, float requiredSeconds)
        {
            goalTrigger = trigger;
            target = requiredItem;
            holdSeconds = Mathf.Max(0f, requiredSeconds);
            if (goalTrigger != null)
                goalTrigger.isTrigger = true;
            ResetProgress();
        }

        public void NotifyEnter(CarryableObject item)
        {
            if (IsComplete || item == null || item != target || item.IsCarried)
                return;

            IsInside = true;
            if (holdSeconds <= 0f)
                Complete();
            else
                ProgressChanged?.Invoke(Progress01);
        }

        public void NotifyExit(CarryableObject item)
        {
            if (IsComplete || item == null || item != target)
                return;

            IsInside = false;
            SetElapsed(0f);
        }

        public void Tick(float deltaTime)
        {
            if (IsComplete || !IsInside || deltaTime <= 0f)
                return;

            if (target == null || target.IsCarried)
            {
                IsInside = false;
                SetElapsed(0f);
                return;
            }

            SetElapsed(Mathf.Min(holdSeconds, Elapsed + deltaTime));
            if (Elapsed >= holdSeconds)
                Complete();
        }

        public void ResetProgress()
        {
            IsInside = false;
            IsComplete = false;
            SetElapsed(0f);
        }

        void Complete()
        {
            if (IsComplete)
                return;

            IsComplete = true;
            Elapsed = holdSeconds;
            ProgressChanged?.Invoke(1f);
            Completed?.Invoke();
        }

        void SetElapsed(float value)
        {
            if (Mathf.Approximately(Elapsed, value))
                return;
            Elapsed = value;
            ProgressChanged?.Invoke(Progress01);
        }

        void OnTriggerEnter2D(Collider2D other) => NotifyEnter(other.GetComponentInParent<CarryableObject>());
        void OnTriggerExit2D(Collider2D other) => NotifyExit(other.GetComponentInParent<CarryableObject>());
        void FixedUpdate() => Tick(Time.fixedDeltaTime);
    }
}
