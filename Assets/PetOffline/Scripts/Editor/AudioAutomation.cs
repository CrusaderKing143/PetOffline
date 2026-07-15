using System;
using System.IO;
using System.Text;
using PetOffline.Core;
using UnityEditor;
using UnityEngine;

namespace PetOffline.Editor
{
    public static class AudioAutomation
    {
        const string AudioRoot = "Assets/PetOffline/Audio/SFX";
        const string DataRoot = "Assets/PetOffline/Data/Audio";
        const int SampleRate = 22050;

        [MenuItem("Tools/Pet Offline/Setup Generated Audio")]
        public static void SetupAudio()
        {
            Directory.CreateDirectory(AudioRoot);
            Directory.CreateDirectory(DataRoot);

            WriteWave("Bark.wav", 0.26f, Bark);
            WriteWave("Robot.wav", 2f, Robot);
            WriteWave("CameraAlert.wav", 0.8f, CameraAlert);
            WriteWave("FeederOffline.wav", 0.75f, FeederOffline);
            WriteWave("UIConfirm.wav", 0.18f, UiConfirm);
            WriteWave("UIReport.wav", 0.7f, UiReport);
            WriteWave("Ambience.wav", 4f, Ambience);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            EnsureCue("Audio_Bark.asset", "Bark", "Bark.wav", AudioBus.World, 0.75f, 1f, 0.65f, false, 12f);
            EnsureCue("Audio_Robot.asset", "Robot", "Robot.wav", AudioBus.World, 0.14f, 1f, 0.8f, true, 14f);
            EnsureCue("Audio_CameraAlert.asset", "CameraAlert", "CameraAlert.wav", AudioBus.World, 0.45f, 1f, 0.85f, false, 14f);
            EnsureCue("Audio_FeederOffline.asset", "FeederOffline", "FeederOffline.wav", AudioBus.World, 0.6f, 1f, 0.8f, false, 12f);
            EnsureCue("Audio_UIConfirm.asset", "UIConfirm", "UIConfirm.wav", AudioBus.UI, 0.3f);
            EnsureCue("Audio_UIReport.asset", "UIReport", "UIReport.wav", AudioBus.UI, 0.38f);
            EnsureCue("Audio_Ambience.asset", "Ambience", "Ambience.wav", AudioBus.World, 0.14f, 1f, 0f, true);

            AssetDatabase.SaveAssets();
            Debug.Log("[PetOffline] Generated audio cues and AudioCueDefinitionSO assets.");
        }

        public static void SetupBatch()
        {
            try
            {
                SetupAudio();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        static void EnsureCue(
            string assetName,
            string cueId,
            string waveName,
            AudioBus bus,
            float volume,
            float pitch = 1f,
            float spatialBlend = 0f,
            bool loop = false,
            float maxDistance = 16f)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioRoot}/{waveName}");
            if (clip == null)
                throw new InvalidOperationException($"Audio import failed: {waveName}");

            var path = $"{DataRoot}/{assetName}";
            var cue = AssetDatabase.LoadAssetAtPath<AudioCueDefinitionSO>(path);
            if (cue == null)
            {
                cue = ScriptableObject.CreateInstance<AudioCueDefinitionSO>();
                AssetDatabase.CreateAsset(cue, path);
            }
            cue.Configure(cueId, clip, bus, volume, pitch, spatialBlend, loop, maxDistance);
            EditorUtility.SetDirty(cue);
        }

        static void WriteWave(string fileName, float duration, Func<float, float> sample)
        {
            var count = Mathf.CeilToInt(duration * SampleRate);
            var path = Path.GetFullPath($"{AudioRoot}/{fileName}");
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream);
            var dataBytes = count * sizeof(short);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataBytes);
            writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * sizeof(short));
            writer.Write((short)sizeof(short));
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataBytes);
            for (var i = 0; i < count; i++)
                writer.Write((short)(Mathf.Clamp(sample((float)i / SampleRate), -1f, 1f) * short.MaxValue));
        }

        static float Bark(float time)
        {
            const float duration = 0.26f;
            var envelope = Mathf.Exp(-time * 12f) * Mathf.Clamp01(time * 90f);
            var frequency = Mathf.Lerp(340f, 145f, time / duration);
            var tone = Mathf.Sin(time * frequency * Mathf.PI * 2f);
            var grit = Mathf.PerlinNoise(time * 1250f, 0.37f) * 2f - 1f;
            return (tone * 0.72f + grit * 0.28f) * envelope * 0.62f;
        }

        static float Robot(float time) =>
            (Mathf.Sin(time * 70f * Mathf.PI * 2f) * 0.7f +
             Mathf.Sin(time * 140f * Mathf.PI * 2f) * 0.3f) * 0.22f;

        static float CameraAlert(float time)
        {
            var gate = Mathf.Repeat(time, 0.25f) < 0.13f ? 1f : 0f;
            return Mathf.Sin(time * 880f * Mathf.PI * 2f) * gate * 0.48f;
        }

        static float FeederOffline(float time)
        {
            var frequency = Mathf.Lerp(760f, 180f, time / 0.75f);
            return Mathf.Sin(time * frequency * Mathf.PI * 2f) * Mathf.Exp(-time * 2.5f) * 0.55f;
        }

        static float UiConfirm(float time)
        {
            var frequency = Mathf.Lerp(880f, 1380f, time / 0.18f);
            return Mathf.Sin(time * frequency * Mathf.PI * 2f) * Mathf.Exp(-time * 18f) * 0.55f;
        }

        static float UiReport(float time)
        {
            var frequency = time < 0.32f ? 660f : 880f;
            var local = time < 0.32f ? time : time - 0.32f;
            return Mathf.Sin(local * frequency * Mathf.PI * 2f) * Mathf.Exp(-local * 8f) * 0.45f;
        }

        static float Ambience(float time) =>
            (Mathf.Sin(time * 50f * Mathf.PI * 2f) * 0.7f +
             Mathf.Sin(time * 100f * Mathf.PI * 2f) * 0.3f) * 0.08f;
    }
}
