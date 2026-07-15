using PetOffline.Core;
using UnityEngine;

namespace PetOffline.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController2D), typeof(AudioSource), typeof(Animator))]
    public sealed class LatteVisual2D : MonoBehaviour
    {
        [SerializeField, RequiredReference] PlayerController2D player;
        [SerializeField, RequiredReference] Transform visualRoot;
        [SerializeField, RequiredReference] Transform mouthAnchor;
        [SerializeField, RequiredReference] AudioSource audioSource;
        [SerializeField] AudioCueDefinitionSO barkCue;

        static AudioClip barkClip;
        bool subscribed;

        public AudioCueDefinitionSO BarkCue => barkCue;

        public void Configure(
            PlayerController2D owner,
            Transform visuals,
            Transform mouth,
            AudioSource source,
            AudioCueDefinitionSO cue = null)
        {
            Unsubscribe();
            player = owner;
            visualRoot = visuals;
            mouthAnchor = mouth;
            audioSource = source;
            barkCue = cue;
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.65f;
                audioSource.minDistance = 1f;
                audioSource.maxDistance = 12f;
            }
            if (isActiveAndEnabled)
                Subscribe();
        }

        void Update()
        {
            if (player == null || visualRoot == null)
                return;

            var angle = Vector2.SignedAngle(Vector2.down, player.Facing);
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
            var moving = player.Body != null && player.Body.linearVelocity.sqrMagnitude > 0.02f;
            var bob = moving && !player.IsLying ? Mathf.Sin(Time.time * 13f) * 0.035f : 0f;
            visualRoot.localPosition = new Vector3(0f, bob, 0f);
            visualRoot.localScale = player.IsLying ? new Vector3(1.25f, 0.72f, 1f) : Vector3.one;
            if (mouthAnchor != null)
                mouthAnchor.localPosition = player.Facing * 0.5f;
        }

        void OnBarked()
        {
            if (audioSource == null)
                return;
            if (barkCue != null && barkCue.Clip != null)
            {
                audioSource.pitch = barkCue.Pitch;
                audioSource.spatialBlend = barkCue.SpatialBlend;
                audioSource.maxDistance = barkCue.MaxDistance;
                audioSource.PlayOneShot(barkCue.Clip, barkCue.Volume);
                return;
            }
            if (barkClip == null)
                barkClip = CreateBarkClip();
            audioSource.PlayOneShot(barkClip, 0.75f);
        }

        static AudioClip CreateBarkClip()
        {
            const int sampleRate = 22050;
            const float duration = 0.22f;
            var samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = (float)i / sampleRate;
                var envelope = Mathf.Exp(-t * 13f) * Mathf.Clamp01(t * 90f);
                var frequency = Mathf.Lerp(360f, 150f, t / duration);
                var tone = Mathf.Sin(t * frequency * Mathf.PI * 2f);
                var grit = Mathf.PerlinNoise(i * 0.19f, 0.37f) * 2f - 1f;
                data[i] = (tone * 0.72f + grit * 0.28f) * envelope * 0.5f;
            }
            var clip = AudioClip.Create("Latte_Bark_Procedural", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        void Subscribe()
        {
            if (subscribed || player == null)
                return;
            player.Barked += OnBarked;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || player == null)
                return;
            player.Barked -= OnBarked;
            subscribed = false;
        }

        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();
    }
}
