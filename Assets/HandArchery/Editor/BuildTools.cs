using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HandArchery.EditorTools
{
    public static class BuildTools
    {
        [MenuItem("HandArchery/Build Windows Archery")]
        public static void BuildWindows()
        {
            try
            {
                if (!File.Exists("Assets/StreamingAssets/hand_landmarker.bytes"))
                    throw new FileNotFoundException("MediaPipe hand_landmarker.bytes missing from StreamingAssets.");
                SceneFactory.CreateAndSave();
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                PlayerSettings.runInBackground = true;
                PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                Directory.CreateDirectory("Builds/Windows");
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { SceneFactory.ScenePath },
                    locationPathName = "Builds/Windows/HandArchery.exe",
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                });
                Debug.Log("HANDARCHERY BUILD: " + report.summary.result + "; errors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings);
                if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows player build failed.");
                string fontLicense = FindFontLicense();
                Directory.CreateDirectory("Builds/Windows/ThirdParty/NotoSansCJK");
                File.Copy(fontLicense, "Builds/Windows/ThirdParty/NotoSansCJK/OFL.txt", true);
                var sourceNotice = Path.Combine(Path.GetDirectoryName(fontLicense), "SOURCE.md");
                if (File.Exists(sourceNotice)) File.Copy(sourceNotice, "Builds/Windows/ThirdParty/NotoSansCJK/SOURCE.md", true);
                const string guide = "Assets/HandArchery/Runtime/USER_GUIDE.ko.md";
                if (File.Exists(guide)) File.Copy(guide, "Builds/Windows/먼저읽기.md", true);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("HANDARCHERY BUILD FAIL: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }
        static string FindFontLicense()
        {
            const string path = "Assets/HandArchery/Runtime/Resources/Fonts/OFL.txt";
            if (!File.Exists(path)) throw new FileNotFoundException("Preserve staged Runtime folder structure and font license.", path);
            return path;
        }
    }
}
