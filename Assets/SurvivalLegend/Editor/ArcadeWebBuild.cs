using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SurvivalLegend.Editor
{
    [InitializeOnLoad]
    public static class ArcadeWebBuild
    {
        public const string Output = "Builds/ArcadeWebGL";
        public const string StatusFile = "Logs/ArcadeWebBuild.txt";
        public const string RequestFile = "Temp/ArcadeWebBuild.request";

        static ArcadeWebBuild() { EditorApplication.update += ProcessRequest; }

        // File queue lets automation return immediately instead of retrying a long RPC build.
        public static void Queue()
        {
            Directory.CreateDirectory("Temp");
            File.WriteAllText(RequestFile, DateTime.UtcNow.ToString("O"));
            EditorApplication.QueuePlayerLoopUpdate();
        }

        static void ProcessRequest()
        {
            if (!File.Exists(RequestFile) || EditorApplication.isCompiling ||
                EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer) return;
            File.Delete(RequestFile);
            Build();
        }

        [MenuItem("Survival Legend/Build Arcade WebGL")]
        public static void Build()
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(StatusFile, "Building " + DateTime.UtcNow.ToString("O"));
            var compression = PlayerSettings.WebGL.compressionFormat;
            var fallback = PlayerSettings.WebGL.decompressionFallback;
            var width = PlayerSettings.defaultScreenWidth;
            var height = PlayerSettings.defaultScreenHeight;
            try
            {
                if (EditorApplication.isPlaying)
                    throw new InvalidOperationException("Stop Play Mode before building.");
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;
                PlayerSettings.defaultScreenWidth = 1280;
                PlayerSettings.defaultScreenHeight = 720;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/SurvivalLegend/Scenes/SurvivalLegendGameObjects.unity" },
                    locationPathName = Output,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None
                });
                var summary = report.summary;
                File.WriteAllText(StatusFile, summary.result + "\nBytes=" + summary.totalSize +
                    "\nErrors=" + summary.totalErrors + "\nWarnings=" + summary.totalWarnings +
                    "\nDuration=" + summary.totalTime);
                if (summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Arcade build failed: " + summary.result);
            }
            catch (Exception error)
            {
                File.AppendAllText(StatusFile, "\n" + error);
                throw;
            }
            finally
            {
                PlayerSettings.WebGL.compressionFormat = compression;
                PlayerSettings.WebGL.decompressionFallback = fallback;
                PlayerSettings.defaultScreenWidth = width;
                PlayerSettings.defaultScreenHeight = height;
            }
        }
    }
}
