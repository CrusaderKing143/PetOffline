using System;
using PetOffline.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PetOffline.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class PlayerController2D : MonoBehaviour
    {
        [SerializeField, RequiredReference] Rigidbody2D body;
        [SerializeField, RequiredReference] SpriteRenderer spriteRenderer;
        [SerializeField, Min(0.01f)] float moveSpeed = 3.25f;

        InputAction moveAction;
        InputAction interactAction;
        InputAction barkAction;
        InputAction pushAction;
        InputAction lieAction;
        Vector2 moveInput;
        Vector2 facing = Vector2.down;
        Vector2 slideDirection;
        float carrySpeedMultiplier = 1f;
        float slideSpeedMultiplier = 1f;
        float slideRemaining;
        bool inputEnabled;
        bool movementEnabled = true;
        bool lying;

        public Rigidbody2D Body => body;
        public SpriteRenderer SpriteRenderer => spriteRenderer;
        public Vector2 Position => body != null ? body.position : (Vector2)transform.position;
        public Vector2 Facing => facing;
        public bool InputEnabled => inputEnabled;
        public bool MovementEnabled => movementEnabled;
        public bool IsSliding => slideRemaining > 0f;
        public bool IsLying => lying;
        public float MoveSpeed => moveSpeed;
        public event Action InteractPressed;
        public event Action Barked;
        public event Action PushPressed;
        public event Action<bool> LieChanged;

        public void Configure(Rigidbody2D rigidbody2D, SpriteRenderer renderer, float speed = 3.25f)
        {
            body = rigidbody2D;
            spriteRenderer = renderer;
            moveSpeed = Mathf.Max(0.01f, speed);
            if (body == null)
                return;

            body.gravityScale = 0f;
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        public void BindInput(InputRouter input)
        {
            var gameplay = input != null ? input.Gameplay : null;
            moveAction = gameplay?.FindAction("Move");
            interactAction = gameplay?.FindAction("Interact");
            barkAction = gameplay?.FindAction("Bark");
            pushAction = gameplay?.FindAction("Push");
            lieAction = gameplay?.FindAction("Lie");
        }

        public void SetInputEnabled(bool value)
        {
            inputEnabled = value;
            if (!value)
            {
                moveInput = Vector2.zero;
                SetLying(false);
                CancelSlide();
            }
        }

        public void SetMovementEnabled(bool value)
        {
            movementEnabled = value;
            if (!value)
            {
                moveInput = Vector2.zero;
                SetLying(false);
                StopBody();
            }
        }

        public void SetCarrySpeedMultiplier(float value) => carrySpeedMultiplier = Mathf.Clamp(value, 0.1f, 1f);

        public void Bark()
        {
            if (inputEnabled)
                Barked?.Invoke();
        }

        public void Push()
        {
            if (inputEnabled && movementEnabled && !IsSliding)
                PushPressed?.Invoke();
        }

        public void Slide(Vector2 direction, float speedMultiplier, float duration)
        {
            if (IsSliding)
                return;

            slideDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : facing;
            if (slideDirection.sqrMagnitude < 0.001f)
                slideDirection = Vector2.right;
            slideSpeedMultiplier = Mathf.Max(1f, speedMultiplier);
            slideRemaining = Mathf.Max(0.01f, duration);
            SetLying(false);
        }

        public void CancelSlide()
        {
            slideRemaining = 0f;
            StopBody();
        }

        public void ResetTo(Vector2 worldPosition)
        {
            CancelSlide();
            if (body != null)
                body.position = worldPosition;
            else
                transform.position = worldPosition;
        }

        public bool MoveTowards(Vector2 worldPosition, float speed) =>
            MoveTowards(worldPosition, speed, Time.fixedDeltaTime);

        public bool MoveTowards(Vector2 worldPosition, float speed, float deltaTime)
        {
            if (body == null)
                return true;

            StopBody();
            var next = Vector2.MoveTowards(body.position, worldPosition,
                Mathf.Max(0f, speed) * Mathf.Max(0f, deltaTime));
            body.MovePosition(next);
            var delta = worldPosition - next;
            if (delta.sqrMagnitude > 0.0001f)
                facing = delta.normalized;
            return delta.sqrMagnitude <= 0.0004f;
        }

        void Update()
        {
            if (!inputEnabled)
                return;

            moveInput = movementEnabled && !IsSliding && moveAction != null
                ? moveAction.ReadValue<Vector2>()
                : Vector2.zero;
            if (moveInput.sqrMagnitude > 1f)
                moveInput.Normalize();
            if (moveInput.sqrMagnitude > 0.001f)
                facing = moveInput.normalized;

            SetLying(movementEnabled && !IsSliding && lieAction != null && lieAction.IsPressed());
            if (movementEnabled && !IsSliding && interactAction != null && interactAction.WasPressedThisFrame())
                InteractPressed?.Invoke();
            if (barkAction != null && barkAction.WasPressedThisFrame())
                Bark();
            if (pushAction != null && pushAction.WasPressedThisFrame())
                Push();
        }

        void FixedUpdate()
        {
            if (body == null)
                return;

            if (slideRemaining > 0f)
            {
                slideRemaining -= Time.fixedDeltaTime;
                body.linearVelocity = slideDirection * (moveSpeed * slideSpeedMultiplier);
                if (slideRemaining <= 0f)
                    StopBody();
                return;
            }

            body.linearVelocity = inputEnabled && movementEnabled && !lying
                ? moveInput * (moveSpeed * carrySpeedMultiplier)
                : Vector2.zero;
        }

        public void SetLying(bool value)
        {
            if (lying == value)
                return;
            lying = value;
            LieChanged?.Invoke(value);
        }

        void StopBody()
        {
            if (body != null)
                body.linearVelocity = Vector2.zero;
        }

        void OnDisable()
        {
            moveInput = Vector2.zero;
            SetLying(false);
            CancelSlide();
        }
    }
}
