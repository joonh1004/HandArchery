using System;
using HandArchery.Core;
using HandArchery.Gameplay;
using HandArchery.Validation;
using UnityEngine;
using UnityEngine.Rendering;
using CameraInput = HandArchery.Input.MediaPipeHandInput;

namespace HandArchery.Presentation
{
    public sealed class ArcheryExperience : MonoBehaviour
    {
        [SerializeField] Material rangeMaterial;
        IHandInputSource input;
        CameraInput cameraInput;
        ArcherySmokeRunner smoke;
        ArcheryHud hud;
        Camera view;
        HandInputFrame latest;
        bool hasFrame;
        bool applicationFocused = true, applicationPaused;
        public ArcherySession Session { get; private set; }
        public ArcheryTarget Target { get; private set; }
        public Camera View => view;
        public bool IsSynthetic => smoke != null;
        public bool Mirror => cameraInput != null && cameraInput.MirrorViewport;
        public bool SwapHands => cameraInput != null && cameraInput.SwapHandedness;
        public string InputStatus { get; private set; } = "입력을 준비하고 있습니다.";
        public string ProviderDetail => input == null ? "준비 중" : input.StatusMessage;
        public string CoordinateStatus => !hasFrame ? "좌표 대기" : "왼손 " + Coordinates(latest.Left) + "  /  오른손 " + Coordinates(latest.Right);
        public string SmokeResult => smoke == null ? "" : smoke.Result;
        public void SetRangeMaterial(Material value) { rangeMaterial = value; }
        static string Coordinates(TrackedHand hand) => hand.Tracked ? "(" + hand.X.ToString("F2") + ", " + hand.Y.ToString("F2") + ")" : "미인식";

        void Awake()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            if (rangeMaterial == null) throw new InvalidOperationException("Range material missing. Regenerate scene with SceneFactory.CreateAndSave().");
            RangePrimitives.Configure(rangeMaterial);
            var world = new GameObject("Outdoor archery range"); world.transform.SetParent(transform);
            Target = OutdoorRange.Create(world.transform);
            var cameraObject = new GameObject("Archery camera"); cameraObject.transform.SetParent(transform);
            cameraObject.tag = "MainCamera";
            view = cameraObject.AddComponent<Camera>(); cameraObject.AddComponent<AudioListener>();
            view.transform.position = new Vector3(0, 2.1f, 0);
            view.transform.LookAt(Target.Center);
            view.fieldOfView = 49; view.nearClipPlane = .08f; view.farClipPlane = 120;
            view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = new Color(.63f,.76f,.78f);
            var sun = new GameObject("Afternoon sun").AddComponent<Light>(); sun.transform.SetParent(transform);
            sun.type = LightType.Directional; sun.intensity = 2.2f; sun.color = new Color(1,.94f,.82f);
            sun.transform.rotation = Quaternion.Euler(42,-32,0); sun.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.5f,.58f,.59f);
            RenderSettings.fog = true; RenderSettings.fogColor = view.backgroundColor; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 35; RenderSettings.fogEndDistance = 95;
            Session = gameObject.AddComponent<ArcherySession>(); Session.Initialize(Target, new Vector3(-.48f,1.7f,1.1f));
            var bow = new GameObject("Practice bow").AddComponent<BowView>(); bow.transform.SetParent(transform); bow.Initialize(Session);
            bool synthetic = Array.IndexOf(Environment.GetCommandLineArgs(), "--handarchery-smoke") >= 0;
            if (synthetic)
            {
                smoke = gameObject.AddComponent<ArcherySmokeRunner>(); input = smoke;
            }
            else
            {
                // No automatic developer fallback: a camera error remains a visible camera error.
                cameraInput = gameObject.AddComponent<CameraInput>(); input = cameraInput;
            }
            hud = gameObject.AddComponent<ArcheryHud>(); hud.Build(this);
            if (smoke != null) smoke.Run(this);
        }
        void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            hasFrame = input != null && input.TryGetLatest(out latest);
            if (!hasFrame) latest = new HandInputFrame(0, now, default, default);
            Session.Tick(latest, now); // core also checks duplicate, stale and invalid input every update
            double age = now - latest.TimestampSeconds;
            bool fresh = hasFrame && age >= 0 && age <= Session.Settings.MaximumFrameAgeSeconds && !double.IsNaN(age);
            InputStatus = !Session.HasFocus ? "포커스 없음 · 양손 준비가 다시 필요합니다" :
                cameraInput != null && cameraInput.NativeReady && cameraInput.DeviceCount == 0 ? "카메라가 없습니다. 연결한 뒤\n입력 재시작을 눌러 주세요." :
                !hasFrame ? "카메라 입력 대기 · 입력 재시작을 사용할 수 있습니다" :
                !fresh ? "입력이 오래되어 당김을 취소했습니다.\n계속되면 입력 재시작을 눌러 주세요." :
                latest.Left.Tracked && latest.Right.Tracked ? "양손 인식 중 · 조준할 수 있습니다" : "양손을 카메라 안에 보여 주세요 · 손 소실 시 발사 취소";
        }
        public void StartRound() { Session.StartRound(); }
        public void RestartInput()
        {
            Session.CancelGesture();
            if (cameraInput != null) cameraInput.Restart();
        }
        public void ToggleMirror() { if (cameraInput != null) { Session.CancelGesture(); cameraInput.MirrorViewport = !cameraInput.MirrorViewport; } }
        public void ToggleHandLabels() { if (cameraInput != null) { Session.CancelGesture(); cameraInput.SwapHandedness = !cameraInput.SwapHandedness; } }
        public void ToggleBowHand()
        {
            Session.CancelGesture(); Session.Settings.BowIsLeft = !Session.Settings.BowIsLeft;
            // Construct again so implementations that snapshot settings stay consistent.
            Session.RebuildGesture();
        }
        void OnApplicationFocus(bool focused) { applicationFocused = focused; if (Session != null) Session.SetFocus(applicationFocused && !applicationPaused); }
        void OnApplicationPause(bool paused) { applicationPaused = paused; if (Session != null) Session.SetFocus(applicationFocused && !applicationPaused); }
        void OnDestroy() { if (cameraInput != null) cameraInput.StopInput(); }
    }
}
