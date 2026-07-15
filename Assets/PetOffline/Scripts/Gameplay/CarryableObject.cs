using System;
using PetOffline.Core;
using UnityEngine;

namespace PetOffline.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class CarryableObject : MonoBehaviour
    {
        [SerializeField, RequiredReference] Rigidbody2D body;
        [SerializeField, RequiredReference] Collider2D objectCollider;
        [SerializeField, RequiredReference] SpriteRenderer spriteRenderer;
        [SerializeField, RequiredReference] CarryableConfigSO config;
        [SerializeField] bool available = true;

        Transform startParent;
        Vector3 startPosition;
        Quaternion startRotation;
        bool carried;

        public Rigidbody2D Body => body;
        public Collider2D Collider => objectCollider;
        public SpriteRenderer SpriteRenderer => spriteRenderer;
        public CarryableConfigSO Config => config;
        public bool IsAvailable => available;
        public bool IsCarried => carried;
        public event Action<CarryableObject, bool> CarryStateChanged;

        public void Configure(
            Rigidbody2D rigidbody2D,
            Collider2D collider2D,
            SpriteRenderer renderer,
            CarryableConfigSO carryConfig,
            bool initiallyAvailable = true)
        {
            body = rigidbody2D;
            objectCollider = collider2D;
            spriteRenderer = renderer;
            config = carryConfig;
            available = initiallyAvailable;
            if (body != null)
            {
                body.gravityScale = 0f;
                body.constraints |= RigidbodyConstraints2D.FreezeRotation;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.simulated = available;
            }
            CaptureStartPose();
        }

        public void CaptureStartPose()
        {
            startParent = transform.parent;
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        public bool BeginCarry(Transform anchor)
        {
            if (!available || carried || anchor == null)
                return false;

            carried = true;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
            }
            transform.SetParent(anchor, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            CarryStateChanged?.Invoke(this, true);
            return true;
        }

        public void Drop(Vector2 worldPosition)
        {
            if (!carried)
                return;

            carried = false;
            transform.SetParent(startParent, true);
            transform.SetPositionAndRotation(worldPosition, startRotation);
            if (body != null)
            {
                body.position = worldPosition;
                body.rotation = startRotation.eulerAngles.z;
                body.linearVelocity = Vector2.zero;
                body.simulated = available;
            }
            CarryStateChanged?.Invoke(this, false);
        }

        public void SetAvailable(bool value)
        {
            available = value;
            if (body != null && !carried)
            {
                body.linearVelocity = Vector2.zero;
                body.simulated = value;
            }
        }

        public void ResetToStart()
            => ResetTo(startPosition);

        public void ResetTo(Vector2 worldPosition)
        {
            var wasCarried = carried;
            carried = false;
            transform.SetParent(startParent, true);
            transform.SetPositionAndRotation(worldPosition, startRotation);
            if (body != null)
            {
                body.position = worldPosition;
                body.rotation = startRotation.eulerAngles.z;
                body.linearVelocity = Vector2.zero;
                body.simulated = available;
            }
            if (wasCarried)
                CarryStateChanged?.Invoke(this, false);
        }

        public bool PushFrom(Vector2 source)
        {
            if (!available || carried || body == null || config == null || config.RobotPushDistance <= 0f)
                return false;

            var direction = body.position - source;
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector2.right;
            body.position += direction.normalized * config.RobotPushDistance;
            body.linearVelocity = Vector2.zero;
            return true;
        }

        void Awake() => CaptureStartPose();
    }
}
