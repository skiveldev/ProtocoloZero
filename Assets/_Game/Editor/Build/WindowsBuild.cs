using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ProtocoloZero.Build
{
    public static class WindowsBuild
    {
        public static void Run()
        {
            int exitCode = 1;
            try
            {
                string root = Directory.GetParent(Application.dataPath).FullName;
                if (!Directory.Exists(Path.Combine(root, "Assets", "_Game")))
                    throw new DirectoryNotFoundException("Owned project root is required.");
                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                    throw new System.InvalidOperationException("Installed Windows x64 module is required.");
                if (PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone)
                    != ScriptingImplementation.Mono2x)
                    throw new System.InvalidOperationException("Expected installed Mono backend; no settings changed.");
                string output = Path.Combine(root, "Builds", "Windows");
                Directory.CreateDirectory(output);
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/_Game/Scenes/ProtocoloZero_Nivel01.unity" },
                    locationPathName = Path.Combine(output, "ProtocoloZero.exe"),
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development
                });
                var summary = report.summary;
                Debug.Log($"[PA4-BUILD] result={summary.result} errors={summary.totalErrors} " +
                    $"warnings={summary.totalWarnings} seconds={summary.totalTime.TotalSeconds:F2} bytes={summary.totalSize}");
                File.WriteAllText(Path.Combine(output, "build-report.json"), JsonUtility.ToJson(new BuildReceipt
                {
                    result = summary.result.ToString(), errors = summary.totalErrors,
                    warnings = summary.totalWarnings, seconds = summary.totalTime.TotalSeconds,
                    bytes = summary.totalSize, development = true, backend = "Mono", target = "StandaloneWindows64"
                }, true));
                exitCode = summary.result == BuildResult.Succeeded ? 0 : 1;
            }
            catch (System.Exception error) { Debug.LogException(error); }
            finally { EditorApplication.Exit(exitCode); }
        }

        [System.Serializable]
        private sealed class BuildReceipt
        {
            public string result, backend, target;
            public int errors, warnings;
            public double seconds;
            public ulong bytes;
            public bool development;
        }
    }
}
