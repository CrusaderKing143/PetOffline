using UnityEngine;

namespace PetOffline.Core
{
    public sealed class SaveService : MonoBehaviour
    {
        const string Prefix = "PetOffline.v1.";
        const string DayOneCompletedKey = Prefix + "DayOneCompleted";
        const string DayTwoCompletedKey = Prefix + "DayTwoCompleted";
        const string LastChoiceKey = Prefix + "LastChoice";
        const string MasterVolumeKey = Prefix + "MasterVolume";

        public bool DayOneCompleted => PlayerPrefs.GetInt(DayOneCompletedKey, 0) != 0;
        public bool DayTwoCompleted => PlayerPrefs.GetInt(DayTwoCompletedKey, 0) != 0;
        public FinalChoice LastChoice => (FinalChoice)PlayerPrefs.GetInt(LastChoiceKey, (int)FinalChoice.KeepQuiet);
        public float MasterVolume => PlayerPrefs.GetFloat(MasterVolumeKey, 1f);

        public void MarkDayOneCompleted()
        {
            PlayerPrefs.SetInt(DayOneCompletedKey, 1);
            PlayerPrefs.Save();
        }

        public void MarkDayTwoCompleted(FinalChoice choice)
        {
            PlayerPrefs.SetInt(DayTwoCompletedKey, 1);
            PlayerPrefs.SetInt(LastChoiceKey, (int)choice);
            PlayerPrefs.Save();
        }

        public void SetMasterVolume(float value)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }

        public void ClearProgress()
        {
            PlayerPrefs.DeleteKey(DayOneCompletedKey);
            PlayerPrefs.DeleteKey(DayTwoCompletedKey);
            PlayerPrefs.DeleteKey(LastChoiceKey);
            PlayerPrefs.Save();
        }
    }
}
