using UnityEngine;

namespace PetOffline.Core
{
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float masterVolume = 1f;
        AudioSource uiSource;

        public float MasterVolume => masterVolume;

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            AudioListener.volume = masterVolume;
        }

        public void Play(AudioCueDefinitionSO cue, AudioSource target = null)
        {
            if (cue == null || cue.Clip == null)
                return;

            var source = target != null ? target : EnsureUiSource();
            source.clip = cue.Loop ? cue.Clip : null;
            source.volume = cue.Volume;
            source.pitch = cue.Pitch;
            source.spatialBlend = target != null ? cue.SpatialBlend : 0f;
            source.loop = cue.Loop;
            source.maxDistance = cue.MaxDistance;
            source.ignoreListenerPause = cue.PlayWhenPaused;
            if (cue.Loop)
                source.Play();
            else
                source.PlayOneShot(cue.Clip);
        }

        AudioSource EnsureUiSource()
        {
            if (uiSource != null)
                return uiSource;
            uiSource = GetComponent<AudioSource>();
            if (uiSource == null)
                uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
            uiSource.spatialBlend = 0f;
            return uiSource;
        }
    }
}
