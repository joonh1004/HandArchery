using System;
using System.Collections.Generic;
using System.Text;
using HandArchery.Core;

namespace HandArchery.EditorTests
{
    public sealed class CoreTestCase
    {
        public string Name;
        public bool Passed;
        public string Error;
    }

    public sealed class CoreTestResult
    {
        public readonly List<CoreTestCase> Cases = new List<CoreTestCase>();
        public int Passed { get { int count = 0; foreach (CoreTestCase c in Cases) if (c.Passed) count++; return count; } }
        public int Failed { get { return Cases.Count - Passed; } }
        public string ToJson()
        {
            StringBuilder b = new StringBuilder("{\"total\":" + Cases.Count + ",\"passed\":" + Passed + ",\"failed\":" + Failed + ",\"cases\":[");
            for (int i = 0; i < Cases.Count; i++)
            {
                if (i > 0) b.Append(',');
                CoreTestCase c = Cases[i];
                b.Append("{\"name\":\"").Append(Escape(c.Name)).Append("\",\"passed\":").Append(c.Passed ? "true" : "false").Append(",\"error\":\"").Append(Escape(c.Error)).Append("\"}");
            }
            return b.Append("]}").ToString();
        }
        private static string Escape(string value)
        { return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t"); }
    }

    // Tests use literal hand trajectories and public outcomes, with no Unity dependency.
    public static class CoreTestSuite
    {
        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
        private static void Near(float actual, float expected, string message)
        { Check(Math.Abs(actual - expected) < .00001f, message + ": expected " + expected + ", got " + actual); }
        private static void Case(CoreTestResult result, string name, Action body)
        {
            CoreTestCase item = new CoreTestCase { Name = name };
            try { body(); item.Passed = true; }
            catch (Exception ex) { item.Error = ex.GetType().Name + ": " + ex.Message; }
            result.Cases.Add(item);
        }
        private static HandInputFrame Frame(long sequence, double time, float pinch = .8f, float drawX = .3f, bool left = true, bool right = true)
        { return new HandInputFrame(sequence, time, new TrackedHand(left, .2f, .5f, .8f), new TrackedHand(right, drawX, .5f, pinch)); }
        private static GestureStateMachine Drawing(float peakX = .6f)
        {
            GestureStateMachine g = new GestureStateMachine();
            g.Update(Frame(1, 0), 0);
            g.Update(Frame(2, .21), .21);
            g.Update(Frame(3, .22, .2f), .22);
            g.Update(Frame(4, .23, .2f, peakX), .23);
            return g;
        }
        private static ShotCommand Fire(GestureStateMachine g)
        {
            Check(!g.Update(Frame(5, .24, .8f, .6f), .24).HasValue, "first release must not fire");
            ShotCommand? shot = g.Update(Frame(6, .25, .8f, .6f), .25);
            Check(shot.HasValue, "two fresh releases must fire");
            return shot.Value;
        }
        private static void NoShotAfterCancellation(GestureStateMachine g)
        {
            Check(!g.Update(Frame(20, .7, .8f, .6f), .7).HasValue, "reacquired open must not fire");
            Check(!g.Update(Frame(21, .71, .8f, .6f), .71).HasValue, "release after cancel must not fire");
            Check(g.State != GesturePhase.Drawing, "cancel must discard draw");
        }

        public static CoreTestResult RunAll()
        {
            CoreTestResult r = new CoreTestResult();
            Case(r, "Open stability requires elapsed fresh capture time", delegate {
                var g = new GestureStateMachine(); g.Update(Frame(1, 0), 0); g.Update(Frame(2, .19), .19);
                Check(g.State == GesturePhase.WaitingForHands, ".19 is not stable");
                g.Update(Frame(3, .21), .21); Check(g.State == GesturePhase.Ready, ".21 must ready");
            });
            Case(r, "Duplicate open frame cannot advance stability", delegate {
                var g = new GestureStateMachine(); g.Update(Frame(1, 0), 0); g.Update(Frame(1, 0), .21);
                Check(g.State == GesturePhase.WaitingForHands, "cached frame cannot ready");
            });
            Case(r, "Capture timestamp rather than receipt time controls stability", delegate {
                var g = new GestureStateMachine(); g.Update(Frame(1, 1), 1.2); g.Update(Frame(2, 1.1), 1.34);
                Check(g.State == GesturePhase.WaitingForHands, "only .1 capture seconds elapsed");
            });
            Case(r, "Initial pinched hands cannot arm or shoot", delegate {
                var g = new GestureStateMachine();
                g.Update(Frame(1, 0, .2f), 0); g.Update(Frame(2, .3, .2f, .6f), .3);
                Check(!g.Update(Frame(3, .31, .8f, .6f), .31).HasValue, "initial release");
                Check(!g.Update(Frame(4, .32, .8f, .6f), .32).HasValue, "initial release repeat");
                Check(g.State == GesturePhase.WaitingForHands, "needs fresh open stability");
            });
            Case(r, "Open stability resets on pinch", delegate {
                var g = new GestureStateMachine(); g.Update(Frame(1, 0), 0); g.Update(Frame(2, .15, .2f), .15);
                g.Update(Frame(3, .16), .16); g.Update(Frame(4, .3), .3);
                Check(g.State == GesturePhase.WaitingForHands, "interrupted open is not stable");
            });
            Case(r, "Open stability resets on missing hand", delegate {
                var g = new GestureStateMachine(); g.Update(Frame(1, 0), 0); g.Update(Frame(2, .15, .8f, .3f, false), .15);
                g.Update(Frame(3, .16), .16); g.Update(Frame(4, .3), .3);
                Check(g.State == GesturePhase.WaitingForHands, "lost hand interrupted timer");
            });
            Case(r, "Normal draw emits one shot with aim and normalized peak", delegate {
                var g = Drawing(.6f); ShotCommand shot = Fire(g);
                Near(shot.AimX, .2f, "bow aim x"); Near(shot.AimY, .5f, "bow aim y");
                // Fresh held draw at .23 reaches .6 before either release frame.
                Near(shot.Power, 1, "clamped power"); Check(g.State == GesturePhase.Cooldown, "enters cooldown");
                Check(!g.Update(Frame(7, .26, .8f, .6f), .26).HasValue, "held release cannot repeat");
            });
            Case(r, "Peak power survives hand moving inward before release", delegate {
                var g = Drawing(.45f);
                g.Update(Frame(5, .24, .2f, .31f), .24);
                g.Update(Frame(6, .25, .8f, .31f), .25);
                var shot = g.Update(Frame(7, .26, .8f, .31f), .26);
                Check(shot.HasValue, "peak retained"); Near(shot.Value.Power, .5f, ".15/.30");
            });
            Case(r, "Minimum draw is normalized power rather than viewport distance", delegate {
                var g = Drawing(.34f);
                g.Update(Frame(5, .24, .8f, .34f), .24);
                var shot = g.Update(Frame(6, .25, .8f, .34f), .25);
                Check(shot.HasValue, ".04 viewport increase exceeds normalized .1"); Near(shot.Value.Power, .13333333f, "normalized power");
            });
            Case(r, "Below minimum normalized draw cancels", delegate {
                var g = Drawing(.32f);
                g.Update(Frame(5, .24, .8f, .32f), .24);
                Check(!g.Update(Frame(6, .25, .8f, .32f), .25).HasValue, ".02/.30 is below .1");
                Check(g.State == GesturePhase.WaitingForHands, "underpowered release needs rearm");
            });
            Case(r, "No draw release cannot fire", delegate {
                var g = Drawing(.3f); g.Update(Frame(5, .24), .24);
                Check(!g.Update(Frame(6, .25), .25).HasValue, "zero draw rejected");
            });
            Case(r, "Opening hands while separating without held draw cannot create power", delegate {
                var g = Drawing(.3f);
                Check(!g.Update(Frame(5, .24, .8f, .6f), .24).HasValue, "first moved release");
                Check(!g.Update(Frame(6, .25, .8f, .6f), .25).HasValue, "release movement is not held draw");
                Check(g.State == GesturePhase.WaitingForHands, "zero held draw must rearm");
            });
            Case(r, "First release overshoot cannot increase held peak", delegate {
                var g = Drawing(.45f);
                g.Update(Frame(5, .24, .8f, .9f), .24);
                var shot = g.Update(Frame(6, .25, .8f, .45f), .25);
                Check(shot.HasValue, "held draw still fires"); Near(shot.Value.Power, .5f, "held .15/.30 only");
            });
            Case(r, "Second release overshoot cannot increase held peak", delegate {
                var g = Drawing(.45f);
                g.Update(Frame(5, .24, .8f, .45f), .24);
                var shot = g.Update(Frame(6, .25, .8f, .9f), .25);
                Check(shot.HasValue, "second release confirms"); Near(shot.Value.Power, .5f, "second release movement ignored");
            });
            Case(r, "Release overshoot followed by hysteresis interrupt retains held peak and resets confirmation", delegate {
                var g = Drawing(.45f);
                g.Update(Frame(5, .24, .8f, .9f), .24);
                g.Update(Frame(6, .25, .6f, .31f), .25);
                Check(g.State == GesturePhase.Drawing, "hysteresis band retains drawing");
                Check(!g.Update(Frame(7, .26, .8f, .45f), .26).HasValue, "interruption resets release count");
                var shot = g.Update(Frame(8, .27, .8f, .45f), .27);
                Check(shot.HasValue, "two new releases confirm"); Near(shot.Value.Power, .5f, "overshoot never entered peak");
            });
            Case(r, "Duplicate release sequence cannot become second release", delegate {
                var g = Drawing(); g.Update(Frame(5, .24, .8f, .6f), .24);
                Check(!g.Update(Frame(5, .24, .8f, .6f), .25).HasValue, "duplicate release");
                Check(g.Update(Frame(6, .26, .8f, .6f), .26).HasValue, "second fresh release");
            });
            Case(r, "Release confirmations must be consecutive across hysteresis band", delegate {
                var g = Drawing(); g.Update(Frame(5, .24, .7f, .6f), .24);
                g.Update(Frame(6, .25, .6f, .6f), .25);
                Check(!g.Update(Frame(7, .26, .7f, .6f), .26).HasValue, ".7 .6 .7 is not consecutive");
                Check(g.Update(Frame(8, .27, .7f, .6f), .27).HasValue, ".7 .7 finally fires");
            });
            Case(r, "Pinch jitter inside hysteresis band preserves drawing", delegate {
                var g = Drawing(); g.Update(Frame(5, .24, .5f, .6f), .24);
                Check(g.State == GesturePhase.Drawing, "middle ratio stays drawing");
                Check(!g.Update(Frame(6, .25, .64f, .6f), .25).HasValue, "below release threshold");
            });
            Case(r, "Stale age exact maximum remains valid", delegate {
                var g = Drawing(); g.Update(Frame(4, .23, .2f, .6f), .48);
                Check(g.State == GesturePhase.Drawing, "age .25 inclusive");
            });
            Case(r, "Stale duplicate frame cancels before sequence filtering", delegate {
                var g = Drawing(); g.Update(Frame(4, .23, .2f, .6f), .4801);
                Check(g.State == GesturePhase.WaitingForHands, "stale cached frame cancels"); NoShotAfterCancellation(g);
            });
            Case(r, "Fresh stale capture cancels rather than releasing", delegate {
                var g = Drawing(); Check(!g.Update(Frame(5, .24, .8f, .6f), .6).HasValue, "old capture"); NoShotAfterCancellation(g);
            });
            Case(r, "Older sequence cancels drawing continuity", delegate {
                var g = Drawing(); g.Update(Frame(2, .24, .8f, .6f), .24); NoShotAfterCancellation(g);
            });
            Case(r, "Hand reacquisition while pinched cannot resume old draw", delegate {
                var g = Drawing(); g.Update(Frame(5, .24, .2f, .6f, false), .24);
                g.Update(Frame(6, .25, .2f, .6f), .25);
                Check(g.State == GesturePhase.WaitingForHands, "pinched reacquisition not armed"); NoShotAfterCancellation(g);
            });
            Case(r, "Cooldown ignores pinch and requires a new open stability window", delegate {
                var g = Drawing(); Fire(g);
                g.Update(Frame(7, .59, .2f), .59); Check(g.State == GesturePhase.Cooldown, ".34 cooldown");
                g.Update(Frame(8, .61), .61); Check(g.State == GesturePhase.WaitingForHands, "cooldown expiry starts open");
                g.Update(Frame(9, .79), .79); Check(g.State == GesturePhase.WaitingForHands, "only .18 new stable seconds");
                g.Update(Frame(10, .82), .82); Check(g.State == GesturePhase.Ready, "new .21 stable seconds");
            });
            Case(r, "Invalid input cannot bypass cooldown", delegate {
                var g = Drawing(); Fire(g); g.Update(Frame(7, .26, .8f, .6f, false), .26);
                g.Update(Frame(8, .27), .27); g.Update(Frame(9, .49), .49);
                Check(g.State == GesturePhase.Cooldown, "loss preserves cooldown deadline");
            });
            Case(r, "Gesture Reset discards drawing and frame history", delegate {
                var g = Drawing(); g.Reset(); Check(g.State == GesturePhase.WaitingForHands, "reset waits"); Near(g.DrawPower, 0, "reset draw");
                g.Update(Frame(1, 0), 0); g.Update(Frame(2, .21), .21);
                Check(g.State == GesturePhase.Ready, "reset accepts restarted input clock/sequence");
            });
            Case(r, "Right bow option takes aim from right hand and pinch from left", delegate {
                var g = new GestureStateMachine(new GestureSettings { BowIsLeft = false });
                Func<long, double, float, float, HandInputFrame> f = (seq, t, p, x) => new HandInputFrame(seq, t, new TrackedHand(true, x, .5f, p), new TrackedHand(true, .8f, .4f, .8f));
                g.Update(f(1, 0, .8f, .6f), 0); g.Update(f(2, .21, .8f, .6f), .21);
                g.Update(f(3, .22, .2f, .6f), .22); g.Update(f(4, .23, .2f, .3f), .23);
                g.Update(f(5, .24, .8f, .3f), .24); var shot = g.Update(f(6, .25, .8f, .3f), .25);
                Check(shot.HasValue, "swapped hands shoot"); Near(shot.Value.AimX, .8f, "right aim"); Near(shot.Value.AimY, .4f, "right aim y");
            });
            Case(r, "Custom confirmation frames and stability settings affect behavior", delegate {
                var g = new GestureStateMachine(new GestureSettings { OpenStabilitySeconds = .1, ReleaseConfirmationFrames = 3, DrawDistanceRange = .2f });
                g.Update(Frame(1, 0), 0); g.Update(Frame(2, .11), .11); g.Update(Frame(3, .12, .2f), .12);
                g.Update(Frame(4, .13, .2f, .4f), .13); g.Update(Frame(5, .14, .8f, .4f), .14);
                Check(!g.Update(Frame(6, .15, .8f, .4f), .15).HasValue, "two insufficient when configured three");
                var shot = g.Update(Frame(7, .16, .8f, .4f), .16); Check(shot.HasValue, "third release"); Near(shot.Value.Power, .5f, "configured range");
            });
            Case(r, "Aim follows latest bow coordinate", delegate {
                var g = Drawing(); g.Update(Frame(5, .24, .8f, .6f), .24);
                var shot = g.Update(new HandInputFrame(6, .25, new TrackedHand(true, .25f, .6f, .8f), new TrackedHand(true, .6f, .5f, .8f)), .25);
                Check(shot.HasValue, "shot"); Near(shot.Value.AimX, .25f, "latest x"); Near(shot.Value.AimY, .6f, "latest y");
            });

            float[] malformed = { float.NaN, float.PositiveInfinity, float.NegativeInfinity };
            foreach (float bad in malformed)
            {
                float value = bad;
                for (int index = 0; index < 6; index++)
                {
                    int field = index;
                    Case(r, "Malformed tracked coordinate/pinch cancels field " + field + " " + value, delegate {
                        var g = Drawing();
                        var left = new TrackedHand(true, field == 0 ? value : .2f, field == 1 ? value : .5f, field == 2 ? value : .8f);
                        var right = new TrackedHand(true, field == 3 ? value : .6f, field == 4 ? value : .5f, field == 5 ? value : .8f);
                        Check(!g.Update(new HandInputFrame(5, .24, left, right), .24).HasValue, "invalid cannot fire");
                        Check(g.State == GesturePhase.WaitingForHands, "invalid cancels"); NoShotAfterCancellation(g);
                    });
                }
            }
            foreach (float bad in new[] { -.01f, 1.01f })
            {
                float value = bad;
                Case(r, "Out of viewport cancels " + value, delegate {
                    var g = Drawing(); g.Update(Frame(5, .24, .8f, value), .24); NoShotAfterCancellation(g);
                });
            }
            Case(r, "Negative pinch ratio cancels", delegate {
                var g = Drawing(); g.Update(Frame(5, .24, -.1f, .6f), .24); NoShotAfterCancellation(g);
            });
            Case(r, "Missing draw hand cancels", delegate {
                var g = Drawing(); g.Update(Frame(5, .24, .8f, .6f, true, false), .24); NoShotAfterCancellation(g);
            });
            Case(r, "Invalid hand on duplicate sequence still cancels", delegate {
                var g = Drawing(); g.Update(Frame(4, .23, .8f, float.NaN), .24); NoShotAfterCancellation(g);
            });
            double[] badTime = { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -.1, .5, .22 };
            foreach (double bad in badTime)
            {
                double value = bad;
                Case(r, "Invalid future or backward capture time cancels " + value, delegate {
                    var g = Drawing(); g.Update(Frame(5, value, .8f, .6f), .24); NoShotAfterCancellation(g);
                });
            }
            foreach (double bad in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -.1, .22 })
            {
                double value = bad;
                Case(r, "Invalid or backward now cancels " + value, delegate {
                    var g = Drawing(); g.Update(Frame(5, .24, .8f, .6f), value); NoShotAfterCancellation(g);
                });
            }
            Case(r, "Five shot round waits for every resolution", delegate {
                var round = new RoundState(); int[] ids = new int[5];
                for (int i = 0; i < 5; i++) Check(round.TryBeginShot(out ids[i]), "allowed shot " + i);
                int extra; Check(!round.TryBeginShot(out extra), "sixth rejected");
                Check(round.Capacity == 5 && round.RemainingShots == 0 && round.ShotsInFlight == 5 && !round.IsFinished, "five in flight not finished");
                int[] scores = { 10, 8, 0, 1, 5 };
                for (int i = 0; i < 5; i++) Check(round.ResolveShot(ids[i], scores[i]), "resolve " + i);
                Check(round.TotalScore == 24 && round.ShotsInFlight == 0 && round.IsFinished, "finished score 24");
            });
            Case(r, "Unknown and duplicate collisions cannot add points", delegate {
                var round = new RoundState(); int id; round.TryBeginShot(out id);
                Check(!round.ResolveShot(id + 100, 10), "unknown rejected");
                Check(round.ResolveShot(id, 7), "first collision"); Check(!round.ResolveShot(id, 10), "duplicate rejected");
                Check(round.TotalScore == 7 && round.ShotsInFlight == 0 && round.RemainingShots == 4, "one score only");
            });
            Case(r, "Reset rejects late shots and restores round", delegate {
                var round = new RoundState(); int oldId; round.TryBeginShot(out oldId); round.ResolveShot(oldId, 8);
                int oldFlying; round.TryBeginShot(out oldFlying); round.Reset();
                Check(round.TotalScore == 0 && round.RemainingShots == 5 && round.ShotsInFlight == 0 && !round.IsFinished, "clean round");
                int fresh; round.TryBeginShot(out fresh); Check(fresh != oldId && fresh != oldFlying, "ids never reused across reset");
                Check(!round.ResolveShot(oldFlying, 10) && !round.ResolveShot(oldId, 10), "late old hits ignored");
                Check(round.ResolveShot(fresh, 4) && round.TotalScore == 4, "new shot survives old hits");
            });
            Case(r, "Miss or timeout score zero completes an exhausted round", delegate {
                var round = new RoundState(1); int id; round.TryBeginShot(out id);
                Check(!round.IsFinished && round.ResolveShot(id, 0) && round.IsFinished, "zero resolves flight");
            });
            Case(r, "Invalid score cannot consume a valid in-flight shot", delegate {
                var round = new RoundState(); int id; round.TryBeginShot(out id);
                Check(!round.ResolveShot(id, -1) && !round.ResolveShot(id, 11), "scores outside 0..10 rejected");
                Check(round.ShotsInFlight == 1 && round.TotalScore == 0, "invalid score leaves flight");
                Check(round.ResolveShot(id, 6), "valid later resolution");
            });
            Case(r, "Nonpositive round capacity is rejected", delegate {
                bool rejected = false; try { new RoundState(0); } catch (ArgumentOutOfRangeException) { rejected = true; }
                Check(rejected, "zero capacity");
            });
            float[] radii = { 0, .1f, .1001f, .2f, .3f, .4f, .5f, .6f, .7f, .8f, .9f, 1, 1.0000001f, 1.001f, -.1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity };
            int[] expected = { 10, 10, 9, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 0, 0, 0, 0, 0 };
            for (int i = 0; i < radii.Length; i++)
            {
                float radius = radii[i]; int score = expected[i];
                Case(r, "Scoring radius " + radius, delegate { Check(TargetScoring.ScoreAtNormalizedRadius(radius) == score, "expected " + score); });
            }
            Case(r, "Ring immediate outward float neighbors belong to outer ring", delegate {
                Check(TargetScoring.ScoreAtNormalizedRadius(.10000001f) == 9, "next float after .1");
                Check(TargetScoring.ScoreAtNormalizedRadius(.30000004f) == 7, "next float after .3");
            });
            Case(r, "Invalid settings fail before processing input", delegate {
                bool rejected = false;
                try { new GestureStateMachine(new GestureSettings { DrawDistanceRange = 0 }); } catch (ArgumentOutOfRangeException) { rejected = true; }
                Check(rejected, "zero draw range");
                rejected = false;
                try { new GestureStateMachine(new GestureSettings { PinchCloseRatio = .7f }); } catch (ArgumentOutOfRangeException) { rejected = true; }
                Check(rejected, "close must be below release");
            });
            return r;
        }
    }
}
