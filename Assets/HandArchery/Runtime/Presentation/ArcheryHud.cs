using HandArchery.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HandArchery.Presentation
{
    public sealed class ArcheryHud : MonoBehaviour
    {
        static readonly Color Ink = new Color(.91f,.94f,.90f);
        static readonly Color Muted = new Color(.68f,.76f,.72f);
        static readonly Color Gold = new Color(.94f,.76f,.34f);
        static readonly Color Panel = new Color(.055f,.105f,.09f,.94f);
        static readonly Color AimCyan = new Color(.12f,.94f,1f);
        ArcheryExperience experience;
        Font font;
        RectTransform canvasRect, reticle;
        GameObject intro, result, diagnostics;
        Text state, recognition, provider, score, remaining, power, resultScore, badge, mirror, swap, bow, previewStatus, diagnosticsButtonText;
        CameraPreviewView cameraPreview;
        Image powerFill;
        Button restartInput, mirrorButton, swapButton, bowButton;
        public void Build(ArcheryExperience owner)
        {
            experience = owner;
            font = Resources.Load<Font>("Fonts/NotoSansCJKkr-Regular");
            if (font == null) throw new System.InvalidOperationException("Korean font missing: Resources/Fonts/NotoSansCJKkr-Regular.otf");
            var canvasObject = new GameObject("HandArchery Korean UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600,900); scaler.matchWidthOrHeight = 0;
            canvasRect = canvasObject.GetComponent<RectTransform>();
            if (EventSystem.current == null)
            {
                var system = new GameObject("UI Event System", typeof(EventSystem)); system.transform.SetParent(transform, false);
                system.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            var header = Box(canvasRect, "Experience heading", new Vector2(26,-24), new Vector2(475,218), new Vector2(0,1));
            Label(header,"OUTDOOR PRACTICE  /  양궁 체험",new Vector2(22,-15),new Vector2(430,30),18,Gold);
            Label(header,"손으로 당기고, 놓아 보세요",new Vector2(22,-50),new Vector2(440,42),26,Ink);
            state = Label(header,"시작 전",new Vector2(22,-98),new Vector2(430,38),20,Gold);
            recognition = Label(header,"입력 준비 중",new Vector2(22,-142),new Vector2(430,56),17,Muted);

            var scores = Box(canvasRect,"Score card",new Vector2(-26,-24),new Vector2(265,190),new Vector2(1,1));
            Label(scores,"MY ROUND  /  나의 기록",new Vector2(20,-15),new Vector2(230,30),18,Muted);
            score = Label(scores,"0 / 50",new Vector2(20,-48),new Vector2(230,67),43,Gold);
            remaining = Label(scores,"남은 화살 5발",new Vector2(20,-129),new Vector2(230,55),18,Ink);

            var preview = Box(canvasRect,"Local camera preview",new Vector2(26,24),new Vector2(424,330),new Vector2(0,0));
            var previewFrame = Rect(preview,"Camera letterbox",new Vector2(12,-8),new Vector2(400,300),new Vector2(0,1));
            var backing = previewFrame.gameObject.AddComponent<Image>(); backing.color = Color.black; backing.raycastTarget = false;
            var previewImage = Rect(previewFrame,"Live camera image",Vector2.zero,new Vector2(400,300),new Vector2(.5f,.5f));
            cameraPreview = previewImage.gameObject.AddComponent<CameraPreviewView>(); cameraPreview.raycastTarget = false;
            cameraPreview.enabled = false;
            previewStatus = Label(preview,"카메라 준비 중",new Vector2(12,-309),new Vector2(400,20),16,Muted);

            var controls = Box(canvasRect,"Input controls",new Vector2(-26,24),new Vector2(350,300),new Vector2(1,0));
            power = Label(controls,"당김 힘 0%",new Vector2(18,-10),new Vector2(315,30),20,Ink);
            var track = Rect(controls,"Power track",new Vector2(18,-49),new Vector2(314,10),new Vector2(0,1));
            track.gameObject.AddComponent<Image>().color = new Color(.22f,.29f,.25f);
            var fill = Rect(track,"Power fill",Vector2.zero,new Vector2(314,10),new Vector2(0,1));
            powerFill = fill.gameObject.AddComponent<Image>(); powerFill.color = Gold;
            restartInput = Action(controls,"입력 재시작",new Vector2(18,-76),new Vector2(314,39),owner.RestartInput,out _);
            mirrorButton = Action(controls,"",new Vector2(18,-125),new Vector2(151,38),owner.ToggleMirror,out mirror);
            swapButton = Action(controls,"",new Vector2(181,-125),new Vector2(151,38),owner.ToggleHandLabels,out swap);
            bowButton = Action(controls,"",new Vector2(18,-173),new Vector2(314,38),owner.ToggleBowHand,out bow);
            Action(controls,"다시 하기",new Vector2(18,-221),new Vector2(314,44),owner.StartRound,out _);
            Action(canvasRect,"입력 진단 보기",new Vector2(26,-254),new Vector2(220,42),ToggleDiagnostics,out diagnosticsButtonText);
            var diagnosticPanel = Box(canvasRect,"Optional input diagnostics",new Vector2(26,-308),new Vector2(475,190),new Vector2(0,1));
            diagnostics = diagnosticPanel.gameObject;
            Label(diagnosticPanel,"입력 진단",new Vector2(18,-14),new Vector2(300,30),20,Gold);
            Action(diagnosticPanel,"닫기",new Vector2(365,-12),new Vector2(92,32),ToggleDiagnostics,out _);
            provider = Label(diagnosticPanel,"",new Vector2(18,-56),new Vector2(439,120),17,Ink);
            diagnostics.SetActive(false);
            badge = Label(canvasRect,"",new Vector2(0,-20),new Vector2(620,72),20,Gold,new Vector2(.5f,1)); badge.alignment = TextAnchor.MiddleCenter;

            reticle = Rect(canvasRect,"Aim reticle",Vector2.zero,new Vector2(30,30),new Vector2(.5f,.5f));
            ReticleBar(new Vector2(-11,0),new Vector2(8,2)); ReticleBar(new Vector2(11,0),new Vector2(8,2));
            ReticleBar(new Vector2(0,-11),new Vector2(2,8)); ReticleBar(new Vector2(0,11),new Vector2(2,8));
            ReticleBar(Vector2.zero,new Vector2(4,4));

            intro = Overlay("시작 안내", "천천히, 다섯 발의 양궁", out var introPanel);
            Label(introPanel,"양손을 펼쳐 준비합니다.\n왼손으로 조준하고 오른 엄지·검지를 붙입니다.\n두 손 사이를 옆으로 벌려 시위를 당깁니다.\n오른 엄지·검지를 벌리면 한 발이 발사됩니다.",new Vector2(35,-102),new Vector2(580,130),22,Ink);
            Label(introPanel,"손이 가려지면 당김은 취소됩니다. 왼쪽 아래에서 내 모습을 확인하세요. 영상은 미리보기만 하며 저장·전송하지 않습니다.",new Vector2(35,-244),new Vector2(580,62),18,Muted);
            Action(introPanel,"체험 시작",new Vector2(35,-328),new Vector2(580,62),owner.StartRound,out _);
            result = Overlay("라운드 결과", "다섯 발, 수고하셨습니다", out var resultPanel);
            resultScore = Label(resultPanel,"0 / 50점",new Vector2(35,-105),new Vector2(580,85),52,Gold);
            resultScore.alignment = TextAnchor.MiddleCenter;
            Label(resultPanel,"다시 도전할 때는 양손을 펼쳐 준비해 주세요.\n첫 시도는 천천히 조준하는 것만으로 충분합니다.",new Vector2(35,-213),new Vector2(580,80),22,Ink);
            Action(resultPanel,"한 번 더 도전",new Vector2(35,-328),new Vector2(580,62),owner.StartRound,out _);
            result.SetActive(false);
            badge.transform.SetAsLastSibling(); // input provenance remains readable over start/result overlays
        }
        void LateUpdate()
        {
            if (experience == null) return;
            var session = experience.Session;
            intro.SetActive(!session.HasStarted); result.SetActive(session.Round.IsFinished);
            state.text = !session.HasStarted ? "준비되면 체험 시작" : session.Round.IsFinished ? "라운드 완료" : !session.HasFocus ? "당김 취소 · 다시 준비" :
                session.Round.RemainingShots == 0 ? "화살 결과 확인 중" : Phase(session.Gesture.State);
            recognition.text = experience.InputStatus;
            if (diagnostics.activeSelf) provider.text = experience.ProviderDetail + "\n\n" + experience.CoordinateStatus;
            score.text = session.Round.TotalScore + " / 50";
            remaining.text = "남은 화살 " + session.Round.RemainingShots + "발" + (session.Round.ShotsInFlight > 0 ? "  ·  비행 중 " + session.Round.ShotsInFlight : "");
            float value = session.Gesture.DrawPower;
            power.text = "당김 힘 " + Mathf.RoundToInt(value * 100) + "%";
            powerFill.rectTransform.sizeDelta = new Vector2(314 * Mathf.Clamp01(value),10);
            string bowHand = session.Settings.BowIsLeft ? "왼손" : "오른손";
            bool previewLive = cameraPreview.Show(experience.CameraPreviewSource);
            previewStatus.text = experience.IsSynthetic ? "합성 검사 모드 · 카메라 미리보기 없음" :
                previewLive ? "실시간 카메라 · 거울 " + (experience.Mirror ? "켜짐" : "꺼짐") :
                experience.CameraPreviewSource == null ? "카메라 준비 중" :
                experience.CameraPreviewSource.PreviewReady ? "카메라 방향 확인 필요 · 입력 재시작" : experience.CameraPreviewSource.PreviewStatus;
            mirror.text = "거울 " + (experience.Mirror ? "켜짐" : "꺼짐");
            swap.text = "좌우손 " + (experience.SwapHands ? "교환" : "기본");
            bow.text = "활손: " + bowHand + " → " + (session.Settings.BowIsLeft ? "오른손" : "왼손") + " 전환";
            bool real = !experience.IsSynthetic;
            restartInput.interactable = real; mirrorButton.interactable = real; swapButton.interactable = real; bowButton.interactable = real;
            badge.text = experience.IsSynthetic ? "개발용 합성 입력  ·  실제 손 검증 아님\n" + experience.SmokeResult : "실제 카메라 모드  ·  미리보기만 · 저장·전송 없음";
            resultScore.text = session.Round.TotalScore + " / 50점";
            reticle.gameObject.SetActive(session.HasStarted && !session.Round.IsFinished);
            if (reticle.gameObject.activeSelf)
            {
                Vector3 screen = experience.View.WorldToScreenPoint(session.AimWorld);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 point);
                reticle.anchoredPosition = point;
            }
        }
        static string Phase(GesturePhase phase)
        {
            switch (phase)
            {
                case GesturePhase.Ready: return "준비 완료 · 핀치로 시위 잡기";
                case GesturePhase.Drawing: return "당기는 중 · 손가락을 벌려 발사";
                case GesturePhase.Cooldown: return "한 발 발사 · 손을 펼쳐 다시 준비";
                default: return "양손을 펼쳐 준비하세요";
            }
        }
        void ToggleDiagnostics()
        {
            diagnostics.SetActive(!diagnostics.activeSelf);
            diagnosticsButtonText.text = diagnostics.activeSelf ? "입력 진단 닫기" : "입력 진단 보기";
        }
        GameObject Overlay(string name, string heading, out RectTransform panel)
        {
            var overlay = Rect(canvasRect,name,Vector2.zero,Vector2.zero,new Vector2(.5f,.5f));
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.offsetMin = Vector2.zero; overlay.offsetMax = Vector2.zero;
            overlay.gameObject.AddComponent<Image>().color = new Color(.025f,.045f,.04f,.60f);
            panel = Box(overlay,name + " card",Vector2.zero,new Vector2(650,430),new Vector2(.5f,.5f));
            Label(panel,heading,new Vector2(35,-30),new Vector2(580,48),32,Gold);
            return overlay.gameObject;
        }
        RectTransform Box(Transform parent,string name,Vector2 position,Vector2 size,Vector2 anchor)
        {
            var rect = Rect(parent,name,position,size,anchor); rect.gameObject.AddComponent<Image>().color = Panel; return rect;
        }
        static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size,Vector2 anchor)
        {
            var rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }
        Text Label(Transform parent,string value,Vector2 position,Vector2 size,int fontSize,Color color,Vector2? anchor = null)
        {
            var rect = Rect(parent,"Text",position,size,anchor ?? new Vector2(0,1));
            var text = rect.gameObject.AddComponent<Text>(); text.font = font; text.text = value; text.fontSize = fontSize;
            text.color = color; text.raycastTarget = false; text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        Button Action(Transform parent,string value,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction callback,out Text label)
        {
            var rect = Rect(parent,"Button " + value,position,size,new Vector2(0,1));
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.21f,.35f,.28f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(callback);
            var colors = button.colors; colors.highlightedColor = new Color(1, .93f, .70f); colors.pressedColor = new Color(.65f,.78f,.62f); button.colors = colors;
            label = Label(rect,value,Vector2.zero,size,20,Ink); label.alignment = TextAnchor.MiddleCenter;
            return button;
        }
        void ReticleBar(Vector2 position,Vector2 size)
        {
            var outline = Rect(reticle,"Aim mark black outline",position,size + new Vector2(4,4),new Vector2(.5f,.5f));
            var backing = outline.gameObject.AddComponent<Image>(); backing.color = Color.black; backing.raycastTarget = false;
            var rect = Rect(reticle,"Aim mark",position,size,new Vector2(.5f,.5f));
            rect.gameObject.AddComponent<Image>().color = AimCyan; rect.GetComponent<Image>().raycastTarget = false;
        }
    }
}
