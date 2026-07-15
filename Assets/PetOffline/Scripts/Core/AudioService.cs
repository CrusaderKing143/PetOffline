using UnityEngine;

namespace PetOffline.Core
{
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float masterVolume = 1f;

        public float MasterVolume => masterVolume;

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            AudioListener.volume = masterVolume;
        }
    }
}
