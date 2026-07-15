using System;
using PetOffline.Core;
using UnityEngine;

namespace PetOffline.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CarryController : MonoBehaviour
    {
        [SerializeField, RequiredReference] PlayerController2D player;
        [SerializeField, RequiredReference] Transform mouthAnchor;
        [SerializeField] LayerMask carryableMask;
        [SerializeField] LayerMask dropBlockMask;
        [SerializeField, Min(0.01f)] float pickupRadius = 1.1f;
        [SerializeField, Min(0f)] float dropDistance = 0.55f;

        bool subscribed;
        bool interactionEnabled = true;

        public CarryableObject Held { get; private set; }
        public bool IsCarrying => Held != null;
        public bool InteractionEnabled => interactionEnabled;
        public event Action<CarryableObject> HeldChanged;

        public void Configure(
            PlayerController2D owner,
            Transform anchor,
            LayerMask mask,
            float radius = 1.1f,
            float forwardDropDistance = 0.55f,
            LayerMask blockMask = default)
        {
            Unsubscribe();
            player = owner;
            mouthAnchor = anchor;
            carryableMask = mask;
            dropBlockMask = blockMask;
            pickupRadius = Mathf.Max(0.01f, radius);
            dropDistance = Mathf.Max(0f, forwardDropDistance);
            if (isActiveAndEnabled)
                Subscribe();
        }

        public void TryInteract()
        {
            if (!interactionEnabled || player == null || player.IsSliding || mouthAnchor == null)
                return;

            if (Held != null)
            {
                DropHeld();
                return;
            }

            var hits = Physics2D.OverlapCircleAll(player.Position, pickupRadius, carryableMask);
            CarryableObject closest = null;
            var closestDistance = float.PositiveInfinity;
            for (var i = 0; i < hits.Length; i++)
            {
                var item = hits[i].GetComponentInParent<CarryableObject>();
                if (item == null || !item.IsAvailable || item.IsCarried)
                    continue;

                var distance = ((Vector2)item.transform.position - player.Position).sqrMagnitude;
                if (distance >= closestDistance)
                    continue;
                closest = item;
                closestDistance = distance;
            }

            if (closest != null)
                TryPickup(closest);
        }

        public bool TryPickup(CarryableObject item)
        {
            if (!interactionEnabled || Held != null || item == null || mouthAnchor == null || !item.BeginCarry(mouthAnchor))
                return false;

            Held = item;
            player.SetCarrySpeedMultiplier(item.Config != null ? item.Config.CarrySpeedMultiplier : 1f);
            HeldChanged?.Invoke(item);
            return true;
        }

        public void DropHeld()
        {
            if (Held == null || mouthAnchor == null || player == null)
                return;
            ForceDrop(FindSafeDropPosition());
        }

        Vector2 FindSafeDropPosition()
        {
            var facing = player.Facing.sqrMagnitude > 0.001f ? player.Facing.normalized : Vector2.down;
            var side = new Vector2(-facing.y, facing.x);
            var origin = player.Position;
            var candidates = new[]
            {
                origin + facing * dropDistance,
                origin - facing * dropDistance,
                origin + side * dropDistance,
                origin - side * dropDistance
            };
            var size = Held.Collider != null ? (Vector2)Held.Collider.bounds.size : Vector2.one * 0.4f;
            size.x = Mathf.Max(0.1f, size.x * 0.9f);
            size.y = Mathf.Max(0.1f, size.y * 0.9f);
            for (var i = 0; i < candidates.Length; i++)
                if (Physics2D.OverlapBox(candidates[i], size, 0f, dropBlockMask) == null)
                    return candidates[i];
            return origin;
        }

        public void SetInteractionEnabled(bool value) => interactionEnabled = value;

        public bool TryPushNearby()
        {
            if (!interactionEnabled || Held != null || player == null || player.IsSliding)
                return false;

            var hits = Physics2D.OverlapCircleAll(player.Position, pickupRadius, carryableMask);
            CarryableObject closest = null;
            var closestDistance = float.PositiveInfinity;
            for (var i = 0; i < hits.Length; i++)
            {
                var item = hits[i].GetComponentInParent<CarryableObject>();
                if (item == null || item.IsCarried || !item.IsAvailable)
                    continue;
                var distance = ((Vector2)item.transform.position - player.Position).sqrMagnitude;
                if (distance >= closestDistance)
                    continue;
                closest = item;
                closestDistance = distance;
            }
            return closest != null && closest.PushFrom(player.Position);
        }

        public void ForceDrop(Vector2 worldPosition)
        {
            if (Held == null)
                return;

            var item = Held;
            Held = null;
            player.SetCarrySpeedMultiplier(1f);
            item.Drop(worldPosition);
            HeldChanged?.Invoke(null);
        }

        void OnBarked()
        {
            if (Held != null && Held.Config != null && Held.Config.DropsOnBark)
                DropHeld();
        }

        void OnPushPressed() => TryPushNearby();

        void Subscribe()
        {
            if (subscribed || player == null)
                return;
            player.InteractPressed += TryInteract;
            player.Barked += OnBarked;
            player.PushPressed += OnPushPressed;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || player == null)
                return;
            player.InteractPressed -= TryInteract;
            player.Barked -= OnBarked;
            player.PushPressed -= OnPushPressed;
            subscribed = false;
        }

        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();
    }
}
