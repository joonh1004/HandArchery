using System.Collections.Generic;
using HandArchery.Core;
using HandArchery.Presentation;
using UnityEngine;

namespace HandArchery.Gameplay
{
    public sealed class ArcherySession : MonoBehaviour
    {
        readonly List<ArrowFlight> arrows = new List<ArrowFlight>();
        bool disposed;
        public RoundState Round { get; } = new RoundState(5);
        public GestureSettings Settings { get; } = new GestureSettings();
        public GestureStateMachine Gesture { get; private set; }
        public bool HasStarted { get; private set; }
        public bool HasFocus { get; private set; } = true;
        public string Feedback { get; private set; } = "시작을 누르면 다섯 발의 체험이 시작됩니다.";
        public ArrowFlight LastArrow { get; private set; }
        public int ActiveArrowCount => arrows.Count;
        public ArcheryTarget Target { get; private set; }
        public Vector3 MuzzlePosition { get; private set; }
        public Vector3 AimWorld => Gesture == null || Gesture.State == GesturePhase.WaitingForHands ? Target.Center : AimPoint(Gesture.AimX, Gesture.AimY);
        public void Initialize(ArcheryTarget target, Vector3 muzzle)
        {
            Target = target; MuzzlePosition = muzzle;
            Gesture = new GestureStateMachine(Settings);
            Target.Initialize(this);
        }
        public Vector3 AimPoint(float x, float y)
        {
            // One mapping is shared by the reticle, bow orientation and projectile trajectory.
            return Target.Center + new Vector3((Mathf.Clamp01(x) - .5f) * Target.Radius * 2.5f,
                (Mathf.Clamp01(y) - .5f) * Target.Radius * 2.5f, 0);
        }
        public void StartRound()
        {
            if (disposed) return;
            CancelGesture();
            Round.Reset(); // invalidates old shot IDs before deferred GameObject destruction
            ClearArrows();
            HasStarted = true;
            Feedback = "양손을 펼쳐 준비하세요. 왼손은 조준, 오른손은 시위를 당깁니다.";
        }
        public void Tick(HandInputFrame frame, double now)
        {
            if (!HasStarted || !HasFocus || Round.IsFinished) return;
            ShotCommand? command = Gesture.Update(frame, now);
            if (!command.HasValue || !Round.TryBeginShot(out int shotId)) return;
            var shot = command.Value;
            LastArrow = ArrowFlight.Spawn(this, shotId, MuzzlePosition, AimPoint(shot.AimX, shot.AimY), shot.Power);
            arrows.Add(LastArrow);
            Feedback = Round.RemainingShots == 0 ? "마지막 화살의 결과를 기다립니다." : "한 발 발사! 손을 펼쳐 다시 준비하세요.";
        }
        public bool ResolveShot(int shotId, int score)
        {
            if (disposed) return false;
            if (!Round.ResolveShot(shotId, score)) return false;
            Feedback = score > 0 ? score + "점! 손을 펼쳐 다음 발을 준비하세요." : "이번 발은 0점입니다. 천천히 다시 조준해 보세요.";
            if (Round.IsFinished) Feedback = "다섯 발 완료! 총 " + Round.TotalScore + " / 50점";
            return true;
        }
        public void CancelGesture() { Gesture?.Reset(); }
        public void RebuildGesture() { Gesture = new GestureStateMachine(Settings); Gesture.Reset(); }
        public void SetFocus(bool focused)
        {
            HasFocus = focused; CancelGesture();
            if (HasStarted) Feedback = focused ? "화면으로 돌아왔습니다. 양손을 펼쳐 다시 준비하세요." : "화면 포커스가 없어 당김을 취소했습니다.";
        }
        void ClearArrows()
        {
            foreach (var arrow in arrows)
            {
                if (arrow == null) continue;
                // Cancel callbacks before disposal; teardown is never a scored miss.
                arrow.Discard();
                if (Application.isPlaying) Destroy(arrow.gameObject);
                else DestroyImmediate(arrow.gameObject);
            }
            arrows.Clear(); LastArrow = null;
        }
        void OnDestroy()
        {
            disposed = true;
            HasStarted = false;
            ClearArrows(); // also owns root arrow objects when only this component is removed
        }
    }
}
