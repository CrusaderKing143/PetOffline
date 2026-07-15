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
        bool patrolEnabled = true;

        public int CurrentWaypointIndex { get; private set; }
        public bool PatrolEnabled => patrolEnabled;
        public Rigidbody2D Body => body;
        public event System.Action<CarryableObject> CarryablePushed;

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
                startPosition = body.position;
            }
        }

        public void SetPatrolEnabled(bool value) => patrolEnabled = value;

        public void ResetPatrol()
        {
            CurrentWaypointIndex = 0;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.position = startPosition;
            }
        }

        public void Tick(float deltaTime)
        {
            if (!patrolEnabled || body == null || waypoints == null || waypoints.Length == 0 || deltaTime <= 0f)
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

        public bool TryPush(Collider2D other)
        {
            var carryable = other != null ? other.GetComponentInParent<CarryableObject>() : null;
            if (carryable == null || !carryable.PushFrom(body.position))
                return false;

            CarryablePushed?.Invoke(carryable);
            return true;
        }

        void OnCollisionEnter2D(Collision2D collision) => TryPush(collision.collider);
        void OnTriggerEnter2D(Collider2D other) => TryPush(other);
        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        void Awake()
        {
            if (body != null)
                startPosition = body.position;
        }
    }
}
