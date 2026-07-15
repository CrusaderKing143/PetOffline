using System;
using PetOffline.Core;
using UnityEngine;

namespace PetOffline.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class RobotPatrol : MonoBehaviour
    {
        [SerializeField, RequiredReference] Rigidbody2D body;
        [SerializeField, RequiredReference] Collider2D robotCollider;
        [SerializeField, RequiredReference] SpriteRenderer spriteRenderer;
        [SerializeField, RequiredReference] Transform[] waypoints;
        [SerializeField, Min(0.01f)] float speed = 1.22f;

        Vector2 startPosition;
        Vector2 slipDestination;
        float slipSpeed;
        float slipRemaining;
        bool patrolBeforeSlip;
        bool slipping;
        bool patrolEnabled = true;

        public int CurrentWaypointIndex { get; private set; }
        public bool PatrolEnabled => patrolEnabled;
        public bool IsSlipping => slipping;
        public Vector2 SlipDestination => slipDestination;
        public Rigidbody2D Body => body;
        public Collider2D Collider => robotCollider;
        public event Action<CarryableObject> CarryablePushed;
        public event Action<Collider2D> Contacted;
        public event Action SlipCompleted;

        public void Configure(
            Rigidbody2D rigidbody2D,
            Collider2D collider2D,
            SpriteRenderer renderer,
            Transform[] path,
            float moveSpeed = 1.22f)
        {
            body = rigidbody2D;
            robotCollider = collider2D;
            spriteRenderer = renderer;
            waypoints = path;
            speed = Mathf.Max(0.01f, moveSpeed);
            if (body != null)
            {
                body.gravityScale = 0f;
                body.constraints |= RigidbodyConstraints2D.FreezeRotation;
                body.bodyType = RigidbodyType2D.Kinematic;
                body.useFullKinematicContacts = true;
                startPosition = body.position;
            }
        }

        public void SetPatrolEnabled(bool value) => patrolEnabled = value;

        public void ResetPatrol()
        {
            StopSlip(false);
            CurrentWaypointIndex = 0;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.position = startPosition;
            }
        }

        public void Tick(float deltaTime)
        {
            if (body == null || deltaTime <= 0f)
                return;

            if (IsSlipping)
            {
                if (slipRemaining <= 0f)
                {
                    StopSlip();
                    return;
                }
                var slipNext = Vector2.MoveTowards(body.position, slipDestination, slipSpeed * deltaTime);
                body.MovePosition(slipNext);
                slipRemaining -= deltaTime;
                return;
            }

            if (!patrolEnabled || waypoints == null || waypoints.Length == 0)
                return;

            var waypoint = waypoints[CurrentWaypointIndex];
            if (waypoint == null)
            {
                CurrentWaypointIndex = (CurrentWaypointIndex + 1) % waypoints.Length;
                return;
            }

            var next = Vector2.MoveTowards(body.position, waypoint.position, speed * deltaTime);
            body.MovePosition(next);
            if (((Vector2)waypoint.position - next).sqrMagnitude <= 0.0025f)
                CurrentWaypointIndex = (CurrentWaypointIndex + 1) % waypoints.Length;
        }

        public void BeginSlip(Vector2 destination, float moveSpeed, float duration)
        {
            if (body == null)
                return;

            if (!IsSlipping)
                patrolBeforeSlip = patrolEnabled;
            slipping = true;
            patrolEnabled = false;
            slipDestination = destination;
            slipSpeed = Mathf.Max(0.01f, moveSpeed);
            slipRemaining = Mathf.Max(0.01f, duration);
            body.linearVelocity = Vector2.zero;
        }

        public void StopSlip() => StopSlip(true);

        void StopSlip(bool notify)
        {
            var wasSlipping = IsSlipping;
            slipping = false;
            slipRemaining = 0f;
            if (body != null)
                body.linearVelocity = Vector2.zero;
            if (wasSlipping)
                patrolEnabled = patrolBeforeSlip;
            if (notify && wasSlipping)
                SlipCompleted?.Invoke();
        }

        public bool TryPush(Collider2D other)
        {
            var carryable = other != null ? other.GetComponentInParent<CarryableObject>() : null;
            if (carryable == null || !carryable.PushFrom(body.position))
                return false;

            CarryablePushed?.Invoke(carryable);
            return true;
        }

        void HandleContact(Collider2D other)
        {
            if (other == null)
                return;
            Contacted?.Invoke(other);
            TryPush(other);
        }

        void OnCollisionEnter2D(Collision2D collision) => HandleContact(collision.collider);
        void OnTriggerEnter2D(Collider2D other) => HandleContact(other);
        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        void Awake()
        {
            if (body != null)
                startPosition = body.position;
        }
    }
}
