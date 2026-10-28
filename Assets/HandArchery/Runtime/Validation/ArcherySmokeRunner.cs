using System;
using System.Collections;
using HandArchery.Core;
using HandArchery.Gameplay;
using HandArchery.Presentation;
using UnityEngine;

namespace HandArchery.Validation
{
    // Opt-in synthetic integration check. This never constitutes a real-hand acceptance test.
    public sealed class ArcherySmokeRunner : MonoBehaviour, IHandInputSource
    {
        long sequence;
        float ratio = 1f, bowX = .5f, drawX = .47f;
        HandInputFrame capturedFrame;
        bool hasCapturedFrame;
        ArcheryExperience experience;
        public string StatusMessage => "합성 검사 입력 · 실제 손 검증 아님";
        public string Result { get; private set; } = "합성 검사 진행 중";
        public bool TryGetLatest(out HandInputFrame frame)
        {
            frame = capturedFrame;
            return hasCapturedFrame;
        }
        void LateUpdate()
        {
            // Capture before the consumer's next Update samples `now`. Reads preserve age/sequence.
            capturedFrame = new HandInputFrame(++sequence, Time.realtimeSinceStartupAsDouble,
                new TrackedHand(true, bowX, .5f, 1f), new TrackedHand(true, drawX, .5f, ratio));
            hasCapturedFrame = true;
        }
        public void Run(ArcheryExperience owner) { experience = owner; StartCoroutine(Check()); }
        void Require(bool ok, string message)
        {
            if (ok) return;
            Result = "합성 검사 실패: " + message;
            Debug.LogError("HANDARCHERY SYNTHETIC FAIL: " + message);
            Application.Quit(1);
            throw new InvalidOperationException(message);
        }
        IEnumerator Check()
        {
            yield return null;
            var session = experience.Session;
            yield return WaitForStartupFocus(session);
            int oldShot = -1;
            for (int cycle = 0; cycle < 2; cycle++)
            {
                experience.StartRound();
                Require(session.Round.RemainingShots == 5 && session.Round.TotalScore == 0, "fresh round");
                if (cycle > 0) Require(!session.Round.ResolveShot(oldShot, 10), "late previous-round hit rejected");
                for (int shot = 0; shot < 5; shot++)
                {
                    bool miss = cycle == 1 && shot >= 3;
                    bowX = miss ? .99f : .5f;
                    drawX = bowX - .03f; ratio = 1f;
                    double readyDeadline = Time.realtimeSinceStartupAsDouble + 3;
                    while (session.Gesture.State != GesturePhase.Ready && Time.realtimeSinceStartupAsDouble < readyDeadline) yield return null;
                    Require(session.Gesture.State == GesturePhase.Ready && Application.isFocused && session.HasFocus,
                        "ready after fresh open (3s limit); " + Diagnostics(session));
                    ratio = .2f;
                    yield return new WaitForSecondsRealtime(.15f);
                    drawX = bowX - .45f;
                    yield return new WaitForSecondsRealtime(.2f);
                    Require(session.Gesture.DrawPower >= .1f, "draw reaches minimum; " + Diagnostics(session));
                    ratio = 1f;
                    double deadline = Time.realtimeSinceStartupAsDouble + 4;
                    while (session.Round.RemainingShots == 5 - shot && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                    Require(session.Round.RemainingShots == 4 - shot, "one release consumes exactly one shot");
                    var arrow = session.LastArrow;
                    Require(arrow != null, "rigidbody arrow spawned");
                    oldShot = arrow.ShotId;
                    // The result must wait for physics, even when the last arrow was used.
                    if (shot == 4) Require(!session.Round.IsFinished, "result waits for last arrow");
                    deadline = Time.realtimeSinceStartupAsDouble + 7;
                    while (session.Round.ShotsInFlight != 0 && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                    Require(session.Round.ShotsInFlight == 0, "physics hit/timeout resolves shot");
                    int expectedScore = (cycle == 0 ? shot + 1 : Math.Min(shot + 1,3)) * 10;
                    Require(session.Round.TotalScore == expectedScore, miss ? "outside target/backstop resolves zero" : "center trajectory scores 10");
                    int score = session.Round.TotalScore;
                    arrow.ResolveContact(experience.Target.GetComponent<Collider>(), experience.Target.Center);
                    Require(session.Round.TotalScore == score && !session.Round.ResolveShot(oldShot, 10), "duplicate collision has no effect");
                }
                Require(session.Round.IsFinished && session.Round.TotalScore == (cycle == 0 ? 50 : 30), "five-shot result including misses");
            }
            experience.StartRound();
            Require(session.ActiveArrowCount == 0 && session.Round.TotalScore == 0, "restart clears arrows and scores");
            Require(!session.Round.ResolveShot(oldShot, 10), "restart rejects late hit");
            yield return CheckSessionComponentTeardown(false);
            yield return CheckSessionComponentTeardown(true);
            yield return CheckNaturalTimeout();
            Result = "합성 검사 통과 · 실제 손 검증 별도";
            Debug.Log("HANDARCHERY SYNTHETIC PASS: 2 rounds / 8 rigidbody center hits + 2 misses / duplicate collision / stale generation / cleanup / PlayMode teardown 2 cases 14 assertions / natural 6s timeout 5 assertions; real hands NOT tested");
            yield return new WaitForSecondsRealtime(.5f);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--handarchery-smoke-quit") >= 0) Application.Quit(0);
        }
        IEnumerator WaitForStartupFocus(ArcherySession session)
        {
            Result = "합성 검사 대기 · 30초 안에 게임 창을 선택해 주세요";
            Debug.Log("HANDARCHERY SYNTHETIC WAIT: actual application + session focus, 30s limit, 0.3s stable; " + Diagnostics(session));
            double deadline = Time.realtimeSinceStartupAsDouble + 30;
            double focusedSince = double.NaN;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                double now = Time.realtimeSinceStartupAsDouble;
                if (Application.isFocused && session.HasFocus)
                {
                    if (double.IsNaN(focusedSince)) focusedSince = now;
                    if (now - focusedSince >= .3) break;
                }
                else focusedSince = double.NaN;
                yield return null;
            }
            Require(Application.isFocused && session.HasFocus && !double.IsNaN(focusedSince) &&
                Time.realtimeSinceStartupAsDouble - focusedSince >= .3, "startup focus not acquired within 30s; " + Diagnostics(session));
            Result = "합성 검사 진행 중 · 게임 창 포커스를 유지해 주세요";
            Debug.Log("HANDARCHERY SYNTHETIC FOCUS READY: " + Diagnostics(session));
        }
        string Diagnostics(ArcherySession session)
        {
            return "applicationFocus=" + Application.isFocused + "; sessionFocus=" + session.HasFocus +
                "; phase=" + session.Gesture.State + "; started=" + session.HasStarted + "; cached=" + hasCapturedFrame +
                "; sequence=" + capturedFrame.Sequence + "; age=" + (Time.realtimeSinceStartupAsDouble - capturedFrame.TimestampSeconds).ToString("F4");
        }
        IEnumerator CheckSessionComponentTeardown(bool alreadyHit)
        {
            GameObject fixtureRoot = null, ownerObject = null, arrowObject = null;
            string phase = alreadyHit ? "after hit" : "in flight";
            try
            {
                // A separate active PlayMode session, far from the main range and its colliders.
                fixtureRoot = new GameObject("PlayMode lifecycle check " + phase);
                ownerObject = new GameObject("Lifecycle owner"); ownerObject.transform.SetParent(fixtureRoot.transform);
                var targetObject = new GameObject("Lifecycle target"); targetObject.transform.SetParent(fixtureRoot.transform);
                targetObject.transform.position = new Vector3(100,10,22);
                var target = targetObject.AddComponent<ArcheryTarget>();
                var targetCollider = targetObject.AddComponent<BoxCollider>(); targetCollider.size = new Vector3(3.6f,3.6f,.16f);
                var owner = ownerObject.AddComponent<ArcherySession>();
                owner.Initialize(target,new Vector3(100,10,0)); owner.StartRound();
                yield return FireFixtureArrow(owner);
                var round = owner.Round;
                var arrow = owner.LastArrow;
                Require(arrow != null && round.RemainingShots == 4, "lifecycle real gesture/Rigidbody spawn " + phase);
                arrowObject = arrow.gameObject;
                int shotId = arrow.ShotId;
                if (alreadyHit)
                {
                    double deadline = Time.realtimeSinceStartupAsDouble + 4;
                    while (round.ShotsInFlight != 0 && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                }
                Require(alreadyHit ? arrow.IsResolved && round.TotalScore == 10 && round.ShotsInFlight == 0 :
                    !arrow.IsResolved && round.TotalScore == 0 && round.ShotsInFlight == 1, "lifecycle phase " + phase);
                int score = round.TotalScore, pending = round.ShotsInFlight;
                UnityEngine.Object.Destroy(owner); // engine callback; no reflection or manual OnDestroy
                yield return null;
                yield return null; // also settle any arrow Destroy scheduled by the session's OnDestroy
                Require(ownerObject != null && ownerObject.GetComponent<ArcherySession>() == null &&
                    !owner.HasStarted && owner.ActiveArrowCount == 0 && owner.LastArrow == null, "engine teardown callback completed " + phase);
                Require(arrowObject == null, "owned arrow GameObject removed " + phase);
                Require(round.TotalScore == score && round.ShotsInFlight == pending, "teardown neither scores nor resolves " + phase);
                Require(!owner.ResolveShot(shotId,10) && !target.ResolveHit(shotId,0), "late callbacks rejected " + phase);
                Require(round.TotalScore == score && round.ShotsInFlight == pending, "late callbacks leave round unchanged " + phase);
                Debug.Log("HANDARCHERY LIFECYCLE PASS: " + phase + "; engine Destroy callback/public state + arrow GO disposal + unchanged round; 7 assertions");
            }
            finally
            {
                if (fixtureRoot != null) UnityEngine.Object.Destroy(fixtureRoot);
                if (arrowObject != null) UnityEngine.Object.Destroy(arrowObject);
            }
        }
        IEnumerator CheckNaturalTimeout()
        {
            GameObject fixtureRoot = null, arrowObject = null;
            try
            {
                fixtureRoot = new GameObject("Natural 6 second timeout check");
                var targetObject = new GameObject("Timeout aim point without collider"); targetObject.transform.SetParent(fixtureRoot.transform);
                targetObject.transform.position = new Vector3(122,10,10);
                var target = targetObject.AddComponent<ArcheryTarget>();
                var ownerObject = new GameObject("Timeout owner"); ownerObject.transform.SetParent(fixtureRoot.transform);
                var owner = ownerObject.AddComponent<ArcherySession>();
                // Horizontal +X flight keeps z=10 and y=10, so neither early spatial-miss guard applies.
                // No collider exists on this isolated aim target. The production expiry must resolve it.
                owner.Initialize(target,new Vector3(100,10,10)); owner.StartRound();
                yield return FireFixtureArrow(owner);
                var arrow = owner.LastArrow;
                Require(arrow != null && owner.Round.RemainingShots == 4 && owner.Round.ShotsInFlight == 1, "timeout fixture real spawn");
                arrowObject = arrow.gameObject;
                int id = arrow.ShotId;
                double started = Time.realtimeSinceStartupAsDouble;
                yield return new WaitForSecondsRealtime(5);
                Require(!arrow.IsResolved && owner.Round.ShotsInFlight == 1 &&
                    Mathf.Abs(arrow.transform.position.z - 10) < .1f && Mathf.Abs(arrow.transform.position.y - 10) < .1f,
                    "timeout stays pending for first 5s away from colliders and spatial bounds");
                double deadline = started + 8;
                while (!arrow.IsResolved && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                Require(arrow.IsResolved && owner.Round.ShotsInFlight == 0 && owner.Round.TotalScore == 0, "natural timeout resolves zero");
                double elapsed = Time.realtimeSinceStartupAsDouble - started;
                Require(elapsed >= 5.7 && elapsed <= 8.5 && arrow.transform.position.z < 29 && arrow.transform.position.y > -.5f,
                    "natural timeout timing and absence of spatial miss; elapsed=" + elapsed.ToString("F2"));
                arrow.ResolveContact(null,arrow.transform.position);
                Require(!owner.ResolveShot(id,10) && owner.Round.TotalScore == 0 && owner.Round.ShotsInFlight == 0, "timeout duplicate/late hit rejected");
                Debug.Log("HANDARCHERY TIMEOUT PASS: natural 6s production expiry; elapsed=" + elapsed.ToString("F2") + "; 5 assertions");
            }
            finally
            {
                if (fixtureRoot != null) UnityEngine.Object.Destroy(fixtureRoot);
                if (arrowObject != null) UnityEngine.Object.Destroy(arrowObject);
            }
        }
        static IEnumerator FireFixtureArrow(ArcherySession owner)
        {
            yield return null; // let the engine activate the ordinary MonoBehaviour in PlayMode
            long sample = 0;
            FeedLifecycleFrame(owner,ref sample,.53f,1);
            yield return new WaitForSecondsRealtime(.25f);
            FeedLifecycleFrame(owner,ref sample,.53f,1);
            yield return null;
            FeedLifecycleFrame(owner,ref sample,.53f,.2f);
            yield return null;
            FeedLifecycleFrame(owner,ref sample,.95f,.2f);
            yield return null;
            FeedLifecycleFrame(owner,ref sample,.95f,1);
            yield return null;
            FeedLifecycleFrame(owner,ref sample,.95f,1);
        }
        static void FeedLifecycleFrame(ArcherySession owner,ref long sequence,float drawHandX,float pinch)
        {
            double now = Time.realtimeSinceStartupAsDouble;
            owner.Tick(new HandInputFrame(++sequence,now,new TrackedHand(true,.5f,.5f,1),
                new TrackedHand(true,drawHandX,.5f,pinch)),now);
        }
    }
}
