using System;
using System.IO;
using HandArchery.Core;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.Core;
using Mediapipe.Tasks.Vision.HandLandmarker;
using UnityEngine;

namespace HandArchery.Input
{
    // CPU VIDEO is synchronous: the Image is disposed before its backing texture is reused.
    public sealed class MediaPipeHandInput : MonoBehaviour, IHandInputSource
    {
        [SerializeField] string cameraName = "";
        [SerializeField] bool mirrorViewport = true;
        [Tooltip("MediaPipe selfie handedness correction for an unmirrored camera; calibrate with the physical left hand.")]
        [SerializeField] bool swapHandedness = true;
        [SerializeField, Range(1,30)] int inferenceFps = 15;
        readonly LatestHandFrame cache = new LatestHandFrame();
        HandLandmarker task;
        HandLandmarkerResult result;
        WebCamTexture cameraTexture;
        Texture2D pixelsTexture;
        Color32[] sourcePixels, uprightPixels;
        long lastTimestamp = -1;
        double nextCapture, cameraDeadline;
        double lastCaptureAt = double.NegativeInfinity;
        string status = "Not started";
        bool waitingForCamera;

        public string StatusMessage => status;
        public int DeviceCount { get; private set; }
        public int CapturedFrames { get; private set; }
        public int BothHandsFrames { get; private set; }
        public int UpdateCalls { get; private set; }
        public int UpdatedCameraFrames { get; private set; }
        public double MaximumInferenceSeconds { get; private set; }
        public double LatestFrameAge => cache.TryGetLatest(out var frame) ? Time.realtimeSinceStartupAsDouble-frame.TimestampSeconds : double.PositiveInfinity;
        public bool CameraStreaming { get; private set; }
        // Borrowed for the local HUD only. This provider alone owns camera lifetime.
        public WebCamTexture PreviewTexture => cameraTexture;
        public bool CameraStarting => waitingForCamera;
        public bool PreviewReady => CameraStreaming && cameraTexture != null && cameraTexture.isPlaying &&
            cameraTexture.width > 16 && cameraTexture.height > 16 &&
            Time.realtimeSinceStartupAsDouble - lastCaptureAt <= .5;
        public string PreviewStatus => PreviewReady ? "실시간 카메라" : waitingForCamera ? "카메라 준비 중" :
            NativeReady && DeviceCount == 0 ? "카메라 없음 · 연결 후 입력 재시작" : "카메라 중단 · 입력 재시작";
        public bool NativeReady => task != null;
        public bool MirrorViewport { get => mirrorViewport; set => mirrorViewport = value; }
        public bool SwapHandedness { get => swapHandedness; set => swapHandedness = value; }
        public bool TryGetLatest(out HandInputFrame frame) => cache.TryGetLatest(out frame);

        void OnEnable() => Restart();
        void OnDisable() => StopInput();
        void OnDestroy() => ReleaseResources();

        public static HandLandmarker CreateTask()
        {
            // Buffer load avoids native filename encoding issues with Korean/space paths.
            byte[] model = File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath,"hand_landmarker.bytes"));
            return HandLandmarker.CreateFromOptions(new HandLandmarkerOptions(
                new BaseOptions(BaseOptions.Delegate.CPU, modelAssetBuffer:model), RunningMode.VIDEO, numHands:2));
        }

        public void Restart()
        {
            StopInput();
            CapturedFrames = 0; BothHandsFrames = 0;
            UpdateCalls = 0; UpdatedCameraFrames = 0; MaximumInferenceSeconds = 0;
            lastCaptureAt = double.NegativeInfinity;
            try
            {
                try { task = CreateTask(); }
                catch (Exception e) { Fail("Native/model initialization failed",e); return; }
                result = HandLandmarkerResult.Alloc(2);
                lastTimestamp = -1;
                var devices = WebCamTexture.devices;
                DeviceCount = devices.Length;
                if (devices.Length == 0) { SetLoss("No camera (native ready)"); return; }
                string selected = devices[0].name;
                if (!string.IsNullOrEmpty(cameraName))
                {
                    bool found = false;
                    foreach (var device in devices) if (device.name == cameraName) { selected = device.name; found = true; break; }
                    if (!found) { SetLoss("Selected camera missing (native ready)"); return; }
                }
                cameraTexture = new WebCamTexture(selected,640,480,30);
                cameraTexture.Play();
                cameraDeadline = Time.realtimeSinceStartupAsDouble+10;
                nextCapture = 0; waitingForCamera = true;
                status = "Native ready; waiting for camera frames";
            }
            catch (Exception e) { Fail("Camera startup failed", e); }
        }

        public void StopInput()
        {
            ReleaseResources();
            SetLoss("Stopped");
        }

        void Update()
        {
            if (task == null || cameraTexture == null) return;
            UpdateCalls++;
            double now = Time.realtimeSinceStartupAsDouble;
            if(CameraStreaming && now-lastCaptureAt > .5)
            { CameraStreaming = false; status = "Camera stalled; cached frame retains its original capture time"; }
            if(cameraTexture.didUpdateThisFrame) UpdatedCameraFrames++;
            if (!cameraTexture.didUpdateThisFrame || cameraTexture.width <= 16 || cameraTexture.height <= 16)
            {
                if (waitingForCamera && now > cameraDeadline)
                { ReleaseResources(); SetLoss("Camera timeout: no stream (retry available)"); }
                return; // Cached frame retains capture time; core rejects stale frames.
            }
            if (now < nextCapture) return;
            nextCapture = now+1.0/inferenceFps;
            try
            {
                int width = cameraTexture.width, height = cameraTexture.height;
                int rotation = cameraTexture.videoRotationAngle;
                bool verticalFlip = cameraTexture.videoVerticallyMirrored;
                int uprightWidth = rotation == 90 || rotation == 270 ? height : width;
                int uprightHeight = rotation == 90 || rotation == 270 ? width : height;
                EnsureBuffers(width,height,uprightWidth,uprightHeight);
                // Timestamp is sampled before the pixel copy/inference, never after detection.
                double capturedAt = Time.realtimeSinceStartupAsDouble;
                cameraTexture.GetPixels32(sourcePixels);
                CameraPixelTransform.Upright(sourcePixels,uprightPixels,width,height,rotation,verticalFlip);
                pixelsTexture.SetPixels32(uprightPixels);
                pixelsTexture.Apply(false,false);
                long timestamp = Math.Max(lastTimestamp+1,(long)(capturedAt*1000));
                lastTimestamp = timestamp;
                bool success;
                using (var image = new Mediapipe.Image(pixelsTexture))
                    success = task.TryDetectForVideo(image,timestamp,null,ref result);
                MaximumInferenceSeconds = Math.Max(MaximumInferenceSeconds,Time.realtimeSinceStartupAsDouble-capturedAt);
                TrackedHand left = default, right = default;
                if (success) ConvertResult(ref left,ref right,uprightWidth,uprightHeight);
                CameraStreaming = true; waitingForCamera = false; CapturedFrames++;
                lastCaptureAt = capturedAt;
                if (left.Tracked && right.Tracked) BothHandsFrames++;
                status = left.Tracked || right.Tracked ? "Tracking hands" : "Camera streaming; no hands";
                cache.Publish(capturedAt,left,right,status);
            }
            catch (Exception e) { Fail("Capture/inference failed",e); }
        }

        void ConvertResult(ref TrackedHand left, ref TrackedHand right, int width, int height)
        {
            if (result.handLandmarks == null || result.handedness == null) return;
            float leftConfidence = -1, rightConfidence = -1;
            for (int i = 0; i < Math.Min(result.handLandmarks.Count,result.handedness.Count); i++)
            {
                var points = result.handLandmarks[i].landmarks;
                var categories = result.handedness[i].categories;
                if (points == null || points.Count < 21 || categories == null || categories.Count == 0) continue;
                var category = categories[0];
                if (category.score < .5f || (category.categoryName != "Left" && category.categoryName != "Right")) continue;
                bool isLeft = category.categoryName == "Left";
                if (swapHandedness) isLeft = !isLeft;
                var aim = points[9]; var thumb = points[4]; var index = points[8]; var palmA = points[5]; var palmB = points[17];
                var hand = HandCoordinates.Convert(aim.x,aim.y,thumb.x,thumb.y,index.x,index.y,palmA.x,palmA.y,palmB.x,palmB.y,width,height,mirrorViewport);
                if (isLeft && category.score > leftConfidence) { left = hand; leftConfidence = category.score; }
                if (!isLeft && category.score > rightConfidence) { right = hand; rightConfidence = category.score; }
            }
        }

        void EnsureBuffers(int width,int height,int uprightWidth,int uprightHeight)
        {
            if (sourcePixels == null || sourcePixels.Length != width*height)
            { sourcePixels = new Color32[width*height]; uprightPixels = new Color32[width*height]; }
            if (pixelsTexture != null && pixelsTexture.width == uprightWidth && pixelsTexture.height == uprightHeight) return;
            if (pixelsTexture != null) Destroy(pixelsTexture);
            pixelsTexture = new Texture2D(uprightWidth,uprightHeight,TextureFormat.RGBA32,false);
        }

        void SetLoss(string message)
        { status = message; cache.Publish(Time.realtimeSinceStartupAsDouble,default,default,message); }
        void Fail(string prefix,Exception error)
        {
            ReleaseResources();
            SetLoss(prefix+": "+error.GetType().Name+" (retry available)");
            Debug.LogError("HAND INPUT "+status+"; "+error.Message);
        }
        void ReleaseResources()
        {
            CameraStreaming = false; waitingForCamera = false;
            if (cameraTexture != null) { cameraTexture.Stop(); Destroy(cameraTexture); cameraTexture = null; }
            if (task != null) { ((IDisposable)task).Dispose(); task = null; }
            if (pixelsTexture != null) { Destroy(pixelsTexture); pixelsTexture = null; }
            sourcePixels = null; uprightPixels = null;
        }
    }
}
