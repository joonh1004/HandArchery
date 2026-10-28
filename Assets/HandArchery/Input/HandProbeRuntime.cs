using System;
using System.Collections;
using Mediapipe.Tasks.Vision.HandLandmarker;
using UnityEngine;

namespace HandArchery.Input
{
    // Diagnostics only. Never displays or writes raw camera pixels.
    public sealed class HandProbeRuntime : MonoBehaviour
    {
        MediaPipeHandInput input;
        bool automatic;
        string nativeStatus = "Not checked";
        WebCamTexture diagnosticCamera;
        string cameraOnlyStatus = "";

        public static void ValidateNative()
        {
            var texture = new Texture2D(64,64,TextureFormat.RGBA32,false);
            try
            {
                texture.SetPixels32(new Color32[64*64]); texture.Apply();
                using (var landmarker = (IDisposable)MediaPipeHandInput.CreateTask())
                using (var image = new Mediapipe.Image(texture))
                {
                    var task = (HandLandmarker)landmarker;
                    var result = HandLandmarkerResult.Alloc(2);
                    // The actual v0.16.3 API returns false when no hands are found;
                    // this is a successful blank inference, not a native failure.
                    bool detected = task.TryDetectForVideo(image,1,null,ref result);
                    if (detected || (result.handLandmarks != null && result.handLandmarks.Count != 0))
                        throw new Exception("Unexpected hand in blank image");
                    Debug.Log("HAND NATIVE PASS: CPU VIDEO numHands=2, model buffer, blank inference, dispose");
                }
            }
            finally { if (Application.isPlaying) Destroy(texture); else DestroyImmediate(texture); }
        }

        IEnumerator Start()
        {
            automatic = Array.IndexOf(Environment.GetCommandLineArgs(),"--hand-probe-auto") >= 0;
            // Hidden automated diagnostics must keep updating while another app has focus.
            if (automatic) Application.runInBackground = true;
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--hand-probe-camera-only") >= 0)
            {
                nativeStatus = "CAMERA ONLY: MediaPipe not initialized or called";
                yield return RunCameraOnly();
                yield break;
            }
            try { ValidateNative(); nativeStatus = "PASS CPU / two-hand task / blank image"; }
            catch (Exception e)
            {
                nativeStatus = "FAIL "+e.GetType().Name;
                Debug.LogError("HAND NATIVE FAIL: "+e);
                if (automatic) Application.Quit(2);
                yield break;
            }
            input = gameObject.AddComponent<MediaPipeHandInput>();
            if (!automatic) yield break;
            yield return new WaitForSecondsRealtime(12);
            bool first = input.NativeReady && input.CameraStreaming && input.CapturedFrames >= 5 && input.LatestFrameAge < 1;
            LogCameraCycle(1);
            input.StopInput();
            yield return new WaitForSecondsRealtime(1);
            input.Restart();
            yield return new WaitForSecondsRealtime(12);
            bool second = input.NativeReady && input.CameraStreaming && input.CapturedFrames >= 5 && input.LatestFrameAge < 1;
            LogCameraCycle(2);
            input.StopInput();
            Debug.Log("HAND PROBE END: native=true; twoCameraCycles="+(first && second)+"; physical handedness/pinch accuracy requires human verification");
            Application.Quit(first && second ? 0 : 3);
        }

        void LogCameraCycle(int cycle) => Debug.Log($"HAND CAMERA CYCLE{cycle}: devices={input.DeviceCount}; streaming={input.CameraStreaming}; frames={input.CapturedFrames}; bothHandsFrames={input.BothHandsFrames}; updates={input.UpdateCalls}; cameraUpdates={input.UpdatedCameraFrames}; maximumCaptureInferenceSeconds={input.MaximumInferenceSeconds:F3}; latestAge={input.LatestFrameAge:F3}; status={input.StatusMessage}");

        IEnumerator RunCameraOnly()
        {
            bool passed = true;
            for(int cycle=1;cycle<=2;cycle++)
            {
                var devices = WebCamTexture.devices;
                if(devices.Length==0) { Debug.Log("HAND CAMERA ONLY: devices=0"); passed=false; break; }
                diagnosticCamera = new WebCamTexture(devices[0].name,640,480,30);
                diagnosticCamera.Play();
                int updates=0,frames=0; double lastFrame=double.NegativeInfinity;
                Color32[] buffer = null;
                double deadline = Time.realtimeSinceStartupAsDouble+12;
                while(Time.realtimeSinceStartupAsDouble < deadline)
                {
                    updates++;
                    if(diagnosticCamera.didUpdateThisFrame && diagnosticCamera.width>16 && diagnosticCamera.height>16)
                    {
                        int count=diagnosticCamera.width*diagnosticCamera.height;
                        if(buffer==null || buffer.Length!=count) buffer=new Color32[count];
                        diagnosticCamera.GetPixels32(buffer);
                        frames++; lastFrame=Time.realtimeSinceStartupAsDouble;
                    }
                    cameraOnlyStatus=$"Camera-only cycle {cycle}; Unity updates {updates}; fresh pixels {frames}";
                    yield return null;
                }
                double age=Time.realtimeSinceStartupAsDouble-lastFrame;
                Debug.Log($"HAND CAMERA ONLY CYCLE{cycle}: devices={devices.Length}; updates={updates}; frames={frames}; age={age:F3}; isPlaying={diagnosticCamera.isPlaying}; width={diagnosticCamera.width}; height={diagnosticCamera.height}; inference=false");
                passed &= frames>=5 && age<1;
                diagnosticCamera.Stop(); Destroy(diagnosticCamera); diagnosticCamera=null;
                yield return new WaitForSecondsRealtime(1);
            }
            Debug.Log("HAND CAMERA ONLY END: twoCycles="+passed+"; MediaPipe never initialized");
            if(automatic) Application.Quit(passed?0:3);
        }

        void OnDestroy()
        {
            if(diagnosticCamera!=null) { diagnosticCamera.Stop(); Destroy(diagnosticCamera); diagnosticCamera=null; }
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20,20,Mathf.Min(Screen.width-40,850),Screen.height-40),GUI.skin.box);
            GUILayout.Label("HandArchery INPUT PROBE (no video display or recording)");
            GUILayout.Label("Native: "+nativeStatus);
            if(!string.IsNullOrEmpty(cameraOnlyStatus)) GUILayout.Label(cameraOnlyStatus);
            if (input != null)
            {
                GUILayout.Label(input.StatusMessage+$" | cameras {input.DeviceCount} | frames {input.CapturedFrames}");
                input.MirrorViewport = GUILayout.Toggle(input.MirrorViewport,"Mirror viewport X (selfie control)");
                input.SwapHandedness = GUILayout.Toggle(input.SwapHandedness,"Swap model labels (calibrate physical left/right)");
                if (input.TryGetLatest(out var frame))
                {
                    GUILayout.Label($"Sequence {frame.Sequence}; age {Time.realtimeSinceStartupAsDouble-frame.TimestampSeconds:F3}s");
                    GUILayout.Label($"LEFT: tracked={frame.Left.Tracked} x={frame.Left.X:F3} y={frame.Left.Y:F3} pinch={frame.Left.PinchRatio:F3}");
                    GUILayout.Label($"RIGHT: tracked={frame.Right.Tracked} x={frame.Right.X:F3} y={frame.Right.Y:F3} pinch={frame.Right.PinchRatio:F3}");
                }
                if (GUILayout.Button("Restart camera / model")) input.Restart();
                if (GUILayout.Button("Stop camera")) input.StopInput();
            }
            GUILayout.Label("Verify physical LEFT only, RIGHT only, then both; move up/right; pinch/release; hide hands.\nNo game shots are produced by this diagnostic scene.");
            GUILayout.EndArea();
            if(input != null && input.TryGetLatest(out var current))
            { DrawPoint(current.Left,Color.green,"L"); DrawPoint(current.Right,Color.cyan,"R"); }
        }
        static void DrawPoint(HandArchery.Core.TrackedHand hand,Color color,string label)
        {
            if (!hand.Tracked) return;
            var previous = GUI.color; GUI.color = color;
            GUI.Label(new Rect(hand.X*Screen.width, (1-hand.Y)*Screen.height,80,30),"+ "+label);
            GUI.color = previous;
        }
    }
}
