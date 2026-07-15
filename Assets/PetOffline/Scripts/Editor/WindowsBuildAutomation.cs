using System;
using System.IO;
using System.Linq;
using PetOffline.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PetOffline.Editor
{
    public static class WindowsBuildAutomation
    {
        [MenuItem("Tools/Pet Offline/Build/Windows Development")]
        public static void BuildDevelopment() => BuildWindows(true);

        [MenuItem("Tools/Pet Offline/Build/Windows Release")]
        public static void BuildRelease() => BuildWindows(false);

        public static void BuildDevelopmentBatch() => RunBatch(true);

        public static void BuildReleaseBatch() => RunBatch(false);

        static void BuildWindows(bool development)
        {
            var scenes = new[] { SceneNames.BootstrapPath, SceneNames.Day1Path, SceneNames.Day2Path };
            var configured = EditorBuildSettings.scenes.ToDictionary(scene => scene.path);
            if (scenes.Any(path => !configured.TryGetValue(path, out var scene) || !scene.enabled))
                throw new BuildFailedException("Windows build requires all three runtime scenes enabled.");

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var windowsRoot = Path.Combine(projectRoot, "Builds/Windows");
            if (development)
                CleanDevelopmentOutput(Path.Combine(windowsRoot, "Development"));
            else
                CleanReleaseOutput(windowsRoot);
            var outputPath = Path.Combine(windowsRoot, development
                ? "Development/PetOffline.exe"
                : "PetOffline.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var options = development
                ? BuildOptions.Development | BuildOptions.AllowDebugging
                : BuildOptions.None;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = options
            });

            var summary = report.summary;
            var evidenceDirectory = Path.Combine(projectRoot, "Artifacts/TestResults");
            Directory.CreateDirectory(evidenceDirectory);
            File.WriteAllText(Path.Combine(evidenceDirectory,
                    development ? "WindowsBuild_Development.txt" : "WindowsBuild_Release.txt"),
                $"Result: {summary.result}{Environment.NewLine}" +
                $"Output: {outputPath}{Environment.NewLine}" +
                $"SizeBytes: {summary.totalSize}{Environment.NewLine}" +
                $"Errors: {summary.totalErrors}{Environment.NewLine}" +
                $"Warnings: {summary.totalWarnings}{Environment.NewLine}" +
                $"Duration: {summary.totalTime}{Environment.NewLine}");
            if (summary.result != BuildResult.Succeeded)
                throw new BuildFailedException(
                    $"Windows {(development ? "Development" : "Release")} build failed: " +
                    $"{summary.result}, {summary.totalErrors} error(s), {summary.totalWarnings} warning(s).");

            Debug.Log($"[PetOffline] Windows {(development ? "Development" : "Release")} build complete: {outputPath}");
        }

        static void CleanReleaseOutput(string windowsRoot)
        {
            foreach (var name in new[]
                     {
                         "PetOffline.exe",
                         "PetOffline.pdb",
                         "UnityPlayer.dll",
                         "UnityCrashHandler64.exe"
                     })
            {
                var path = Path.Combine(windowsRoot, name);
                if (File.Exists(path))
                    File.Delete(path);
            }

            foreach (var name in new[]
                     {
                         "PetOffline_Data",
                         "MonoBleedingEdge",
                         "D3D12",
                         "Pet Offline_BurstDebugInformation_DoNotShip",
                         "PetOffline_BurstDebugInformation_DoNotShip",
                         "Pet Offline_BackUpThisFolder_ButDontShipItWithYourGame",
                         "PetOffline_BackUpThisFolder_ButDontShipItWithYourGame"
                     })
            {
                var path = Path.Combine(windowsRoot, name);
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
        }

        static void CleanDevelopmentOutput(string outputDirectory)
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, true);
        }

        static void RunBatch(bool development)
        {
            try
            {
                BuildWindows(development);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
