using System;
using HandArchery.Core;
using HandArchery.Gameplay;
using UnityEditor;
using UnityEngine;

namespace HandArchery.EditorTools
{
    public static class PresentationValidation
    {
        static void Require(bool value, string message) { if (!value) throw new Exception("PRESENTATION CONTRACT FAIL: " + message); }
        [MenuItem("HandArchery/Validate Presentation Contracts")]
        public static void ValidateContracts()
        {
            GameObject targetObject = null, sessionObject = null;
            try
            {
                Require(Resources.Load<Font>("Fonts/NotoSansCJKkr-Regular") != null, "packaged Korean font");
                targetObject = new GameObject("Validation target"); targetObject.transform.position = new Vector3(0, 2.5f, 22);
                var target = targetObject.AddComponent<ArcheryTarget>();
                sessionObject = new GameObject("Validation session");
                var session = sessionObject.AddComponent<ArcherySession>(); session.Initialize(target, Vector3.zero);
                Require((session.AimPoint(.5f,.5f) - target.Center).sqrMagnitude < .000001f, "reticle/arrow share center mapping");
                Require(Mathf.Abs(target.NormalizedRadius(target.Center + Vector3.right * target.Radius) - 1) < .00001f, "physical radius to normalized scoring");
                session.StartRound();
                Require(session.Round.TryBeginShot(out int id), "round begins first physical shot");
                Require(target.ResolveHit(id,0), "target resolves center through session");
                Require(session.Round.TotalScore == 10 && !target.ResolveHit(id,0), "duplicate target hit not scored twice");
                session.StartRound();
                Require(!target.ResolveHit(id,0) && session.Round.TotalScore == 0 && session.ActiveArrowCount == 0, "restart generation/cleanup");
                session.Tick(new HandInputFrame(1,0,new TrackedHand(true,.5f,.5f,1),new TrackedHand(true,.53f,.5f,1)),0);
                session.Tick(new HandInputFrame(2,.25,new TrackedHand(true,.5f,.5f,1),new TrackedHand(true,.53f,.5f,1)),.25);
                session.Tick(new HandInputFrame(3,.3,new TrackedHand(true,.5f,.5f,1),new TrackedHand(true,.53f,.5f,.2f)),.3);
                session.Tick(new HandInputFrame(4,.35,new TrackedHand(true,.5f,.5f,1),new TrackedHand(true,.95f,.5f,.2f)),.35);
                Require(session.Gesture.State == GesturePhase.Drawing && session.Gesture.DrawPower > .1f, "draw setup");
                session.SetFocus(false);
                Require(session.Gesture.State == GesturePhase.WaitingForHands && session.Gesture.DrawPower == 0, "focus loss cancels draw");
                Require(session.Round.RemainingShots == 5, "focus loss never fires");
                session.Settings.BowIsLeft = false; session.RebuildGesture();
                Require(session.Gesture.State == GesturePhase.WaitingForHands, "hand option rebuilds snapshot and requires rearm");
                Debug.Log("HANDARCHERY PRESENTATION CONTRACT PASS: 11 assertions; PlayMode lifecycle/physics/UI/native webcam must be checked separately");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                if (Application.isBatchMode) EditorApplication.Exit(1); else throw;
            }
            finally
            {
                if (sessionObject != null) UnityEngine.Object.DestroyImmediate(sessionObject);
                if (targetObject != null) UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }
    }
}
