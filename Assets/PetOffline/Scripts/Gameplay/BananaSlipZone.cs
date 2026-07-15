using PetOffline.Core;
using UnityEngine;

namespace PetOffline.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class BananaSlipZone : MonoBehaviour
    {
        [SerializeField, RequiredReference] Collider2D slipTrigger;
        [SerializeField, RequiredReference] SpriteRenderer spriteRenderer;
        [SerializeField, RequiredReference] LevelConfigSO levelConfig;
        [SerializeField] Vector2 localSlideDirection = Vector2.right;

        public int ActivationCount { get; private set; }

        public void Configure(
            Collider2D trigger,
            SpriteRenderer renderer,
            LevelConfigSO config,
            Vector2 slideDirection)
        {
            slipTrigger = trigger;
            spriteRenderer = renderer;
            levelConfig = config;
            localSlideDirection = slideDirection;
            if (slipTrigger != null)
                slipTrigger.isTrigger = true;
        }

        public bool Activate(PlayerController2D player)
        {
            if (player == null || levelConfig == null || player.IsSliding)
                return false;

            var direction = localSlideDirection.sqrMagnitude > 0.001f
                ? (Vector2)transform.TransformDirection(localSlideDirection).normalized
                : player.Facing;
            player.Slide(direction, levelConfig.SlipSpeedMultiplier, levelConfig.SlipDuration);
            ActivationCount++;
            return true;
        }

        void OnTriggerEnter2D(Collider2D other) => Activate(other.GetComponentInParent<PlayerController2D>());
    }
}
