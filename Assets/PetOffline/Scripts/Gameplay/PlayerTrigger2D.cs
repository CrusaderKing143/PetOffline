using System;
using PetOffline.Core;
using UnityEngine;

namespace PetOffline.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerTrigger2D : MonoBehaviour
    {
        [SerializeField, RequiredReference] Collider2D trigger;
        [SerializeField, RequiredReference] PlayerController2D target;

        public Collider2D Trigger => trigger;
        public PlayerController2D Target => target;
        public bool Inside { get; private set; }
        public event Action Entered;
        public event Action Exited;

        public void Configure(Collider2D triggerCollider, PlayerController2D player)
        {
            trigger = triggerCollider;
            target = player;
            if (trigger != null)
                trigger.isTrigger = true;
            ResetState();
        }

        public void SetInside(bool value)
        {
            if (Inside == value)
                return;
            Inside = value;
            if (value)
                Entered?.Invoke();
            else
                Exited?.Invoke();
        }

        public void ResetState() => SetInside(false);

        bool IsTarget(Collider2D other) =>
            other != null && target != null && other.GetComponentInParent<PlayerController2D>() == target;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (IsTarget(other))
                SetInside(true);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (IsTarget(other))
                SetInside(false);
        }

        void OnDisable() => ResetState();
    }
}
