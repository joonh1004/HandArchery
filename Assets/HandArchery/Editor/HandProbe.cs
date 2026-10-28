using System;
using HandArchery.Core;
using HandArchery.Input;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;

namespace HandArchery.Editor
{
    public static class HandProbe
    {
        static void Require(bool condition, string message)
        { if (!condition) throw new Exception("INPUT CONTRACT FAIL: " + message); }
        static bool Near(float a, float b) => Math.Abs(a - b) < 0.0001f;
        public static void ValidateContracts()
        {
            try
            {
                var hand = HandCoordinates.Convert(.2f,.3f,.1f,.1f,.2f,.1f,.1f,.2f,.3f,.2f,1280,720,false);
                Require(hand.Tracked && Near(hand.X,.2f) && Near(hand.Y,.7f), "top-left to bottom-left exactly once");
                Require(Near(hand.PinchRatio,.5f), "pixel distance ratio");
                var vertical = HandCoordinates.Convert(.2f,.3f,.1f,.1f,.1f,.2f,.1f,.2f,.3f,.2f,1280,720,true);
                Require(Near(vertical.X,.8f) && Near(vertical.PinchRatio,.28125f), "mirror and aspect corrected ratio");
                Require(!HandCoordinates.Convert(float.NaN,0,0,0,1,1,0,0,1,1,1280,720,false).Tracked, "NaN rejected");
                Require(!HandCoordinates.Convert(.5f,.5f,0,0,1,1,0,0,0,0,1280,720,false).Tracked, "degenerate palm rejected");
                Require(!HandCoordinates.Convert(.5f,.5f,0,0,1,1,0,0,1,1,0,720,false).Tracked, "invalid dimensions rejected");
                Require(!HandCoordinates.Convert(.5f,.5f,float.MaxValue,float.MaxValue,-float.MaxValue,-float.MaxValue,0,0,.1f,.1f,1280,720,false).Tracked,"overflowing ratio rejected");
                var pixels = new [] {new Color32(3,0,0,255),new Color32(4,0,0,255),new Color32(1,0,0,255),new Color32(2,0,0,255)};
                var output = new Color32[4];
                CameraPixelTransform.Upright(pixels,output,2,2,0,false);
                Require(output[0].r==1 && output[3].r==4,"camera bottom-up to inference top-down");
                CameraPixelTransform.Upright(pixels,output,2,2,90,false);
                Require(output[0].r==3 && output[1].r==1 && output[2].r==4 && output[3].r==2,"clockwise 90 rotation");
                CameraPixelTransform.Upright(pixels,output,2,2,180,false);
                Require(output[0].r==4 && output[3].r==1,"180 rotation");
                CameraPixelTransform.Upright(pixels,output,2,2,270,false);
                Require(output[0].r==2 && output[1].r==4 && output[2].r==1 && output[3].r==3,"270 rotation");
                CameraPixelTransform.Upright(pixels,output,2,2,0,true);
                Require(output[0].r==3 && output[3].r==2,"camera vertical mirror corrected");
                var cache = new LatestHandFrame();
                Require(!cache.TryGetLatest(out _), "no fabricated startup sample");
                cache.Publish(12.125,hand,default,"Hands");
                Require(cache.TryGetLatest(out var first) && first.Sequence == 1 && first.TimestampSeconds == 12.125, "capture timestamp and sequence");
                Require(cache.TryGetLatest(out var repeat) && repeat.Sequence == first.Sequence && repeat.TimestampSeconds == first.TimestampSeconds, "read keeps original sequence and capture time");
                cache.Publish(12.25,default,default,"No hands");
                Require(cache.TryGetLatest(out var lost) && lost.Sequence == 2 && !lost.Left.Tracked && !lost.Right.Tracked, "loss is explicit new untracked sample");
                bool rejected = false;
                try { cache.Publish(double.NaN,default,default,"invalid"); } catch(ArgumentOutOfRangeException) { rejected = true; }
                Require(rejected && cache.TryGetLatest(out var intact) && intact.Sequence==2,"invalid capture does not advance sequence");
                Debug.Log("INPUT CONTRACT PASS: 17 assertions");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogError(e); EditorApplication.Exit(1); }
        }

        public static void ValidateNative()
        {
            try { HandProbeRuntime.ValidateNative(); EditorApplication.Exit(0); }
            catch(Exception e) { Debug.LogError("HAND NATIVE FAIL: "+e); EditorApplication.Exit(1); }
        }

        [MenuItem("HandArchery/Create Input Probe Scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Input probe (no camera video)").AddComponent<HandProbeRuntime>();
            var camera = new GameObject("Diagnostic Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.06f,.08f,.12f);
            System.IO.Directory.CreateDirectory("Assets/HandArchery/Input/Probe");
            EditorSceneManager.SaveScene(scene,"Assets/HandArchery/Input/Probe/HandInputProbe.unity");
            AssetDatabase.SaveAssets();
        }

        public static void BuildWindowsProbe()
        {
            try
            {
                CreateScene();
                var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new [] {"Assets/HandArchery/Input/Probe/HandInputProbe.unity"},
                    locationPathName = "Builds/HandInputProbe/HandInputProbe.exe",
                    target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
                Debug.Log($"HAND PROBE BUILD: {result.summary.result}; errors={result.summary.totalErrors}; warnings={result.summary.totalWarnings}");
                EditorApplication.Exit(result.summary.result == BuildResult.Succeeded ? 0 : 1);
            }
            catch (Exception e) { Debug.LogError(e); EditorApplication.Exit(1); }
        }
    }

    public sealed class MediaPipeLicenseBuild : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPostprocessBuild(BuildReport report)
        {
            string destination = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(report.summary.outputPath),"ThirdParty","MediaPipe");
            System.IO.Directory.CreateDirectory(destination);
            foreach(var name in new [] {"LICENSE.md","Third Party Notices.md","SOURCE.md"})
                System.IO.File.Copy(System.IO.Path.Combine("ThirdParty","MediaPipe",name),System.IO.Path.Combine(destination,name),true);
        }
    }
}
