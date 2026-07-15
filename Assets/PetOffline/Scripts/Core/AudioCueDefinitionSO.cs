using UnityEngine;

namespace PetOffline.Core
{
    public enum AudioBus
    {
        World,
        UI
    }

    [CreateAssetMenu(fileName = "Audio_", menuName = "Pet Offline/Audio Cue Definition")]
    public sealed class AudioCueDefinitionSO : ScriptableObject
    {
        [SerializeField] string cueId = string.Empty;
        [SerializeField] AudioClip clip;
        [SerializeField] AudioBus bus;
        [SerializeField, Range(0f, 1f)] float volume = 1f;
        [SerializeField, Range(0.1f, 3f)] float pitch = 1f;
        [SerializeField, Range(0f, 1f)] float spatialBlend;
        [SerializeField] bool loop;
        [SerializeField, Min(0f)] float maxDistance = 16f;
        [SerializeField] bool playWhenPaused;

        public string CueId => cueId;
        public AudioClip Clip => clip;
        public AudioBus Bus => bus;
        public float Volume => volume;
        public float Pitch => pitch;
        public float SpatialBlend => spatialBlend;
        public bool Loop => loop;
        public float MaxDistance => maxDistance;
        public bool PlayWhenPaused => playWhenPaused;

        public void Configure(
            string id,
            AudioClip audioClip,
            AudioBus audioBus,
            float cueVolume,
            float cuePitch = 1f,
            float cueSpatialBlend = 0f,
            bool cueLoop = false,
            float cueMaxDistance = 16f,
            bool cuePlaysWhenPaused = false)
        {
            cueId = id ?? string.Empty;
            clip = audioClip;
            bus = audioBus;
            volume = Mathf.Clamp01(cueVolume);
            pitch = Mathf.Clamp(cuePitch, 0.1f, 3f);
            spatialBlend = Mathf.Clamp01(cueSpatialBlend);
            loop = cueLoop;
            maxDistance = Mathf.Max(0f, cueMaxDistance);
            playWhenPaused = cuePlaysWhenPaused;
        }
    }
}
