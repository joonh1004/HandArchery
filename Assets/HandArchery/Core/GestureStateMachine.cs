using System;
namespace HandArchery.Core
{
    public sealed class GestureStateMachine
    {
        private readonly GestureSettings settings;
        private bool hasSequence;
        private long lastSequence;
        private double lastTimestamp = double.NegativeInfinity;
        private double lastNow = double.NegativeInfinity;
        private double openSince = double.NaN;
        private double cooldownUntil;
        private float startDistance;
        private int releaseFrames;

        public GestureStateMachine(GestureSettings settings = null)
        {
            GestureSettings source = settings ?? new GestureSettings();
            if (!Finite(source.PinchCloseRatio) || source.PinchCloseRatio < 0 ||
                !Finite(source.PinchReleaseRatio) || source.PinchReleaseRatio <= source.PinchCloseRatio ||
                !Finite(source.OpenStabilitySeconds) || source.OpenStabilitySeconds < 0 ||
                !Finite(source.MaximumFrameAgeSeconds) || source.MaximumFrameAgeSeconds <= 0 ||
                source.ReleaseConfirmationFrames < 2 || !Finite(source.MinimumDrawPower) ||
                source.MinimumDrawPower <= 0 || source.MinimumDrawPower > 1 ||
                !Finite(source.DrawDistanceRange) || source.DrawDistanceRange <= 0 ||
                !Finite(source.CooldownSeconds) || source.CooldownSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(settings), "Gesture settings must be finite and preserve safe thresholds.");
            // Snapshot configuration: callers may retain/edit their settings object for a later machine.
            this.settings = new GestureSettings
            {
                BowIsLeft = source.BowIsLeft,
                PinchCloseRatio = source.PinchCloseRatio,
                PinchReleaseRatio = source.PinchReleaseRatio,
                OpenStabilitySeconds = source.OpenStabilitySeconds,
                MaximumFrameAgeSeconds = source.MaximumFrameAgeSeconds,
                ReleaseConfirmationFrames = source.ReleaseConfirmationFrames,
                MinimumDrawPower = source.MinimumDrawPower,
                DrawDistanceRange = source.DrawDistanceRange,
                CooldownSeconds = source.CooldownSeconds
            };
        }
        public GesturePhase State { get; private set; }
        public float DrawPower { get; private set; }
        public float AimX { get; private set; }
        public float AimY { get; private set; }
        public ShotCommand? Update(HandInputFrame frame, double now)
        {
            bool olderSequence = hasSequence && frame.Sequence < lastSequence;
            bool freshSequence = !hasSequence || frame.Sequence > lastSequence;
            if (freshSequence) { lastSequence = frame.Sequence; hasSequence = true; }

            bool validNow = Finite(now) && now >= 0 && now >= lastNow;
            if (validNow) lastNow = now;
            double capture = frame.TimestampSeconds;
            // Safety checks deliberately precede duplicate filtering. Cached input can become stale.
            if (!validNow || !Finite(capture) || capture < 0 || capture > now ||
                capture < lastTimestamp || now - capture > settings.MaximumFrameAgeSeconds ||
                olderSequence || !ValidHand(frame.Left) || !ValidHand(frame.Right))
            {
                Cancel();
                return null;
            }
            if (!freshSequence) return null;
            lastTimestamp = capture;

            TrackedHand bow = settings.BowIsLeft ? frame.Left : frame.Right;
            TrackedHand draw = settings.BowIsLeft ? frame.Right : frame.Left;
            AimX = bow.X; AimY = bow.Y;
            if (State == GesturePhase.Cooldown)
            {
                if (now < cooldownUntil) return null;
                State = GesturePhase.WaitingForHands;
                openSince = double.NaN;
            }

            if (State == GesturePhase.WaitingForHands)
            {
                if (draw.PinchRatio < settings.PinchReleaseRatio) { openSince = double.NaN; return null; }
                if (double.IsNaN(openSince)) openSince = Math.Max(capture, cooldownUntil);
                if (capture - openSince >= settings.OpenStabilitySeconds) State = GesturePhase.Ready;
                return null;
            }
            if (State == GesturePhase.Ready)
            {
                if (draw.PinchRatio <= settings.PinchCloseRatio)
                {
                    startDistance = Distance(bow, draw);
                    DrawPower = 0;
                    releaseFrames = 0;
                    State = GesturePhase.Drawing;
                }
                return null;
            }

            if (draw.PinchRatio < settings.PinchReleaseRatio)
            {
                // Only held input contributes draw. Opening the hand can move landmarks
                // apart, so neither release confirmation may create or increase power.
                float power = (Distance(bow, draw) - startDistance) / settings.DrawDistanceRange;
                if (power > DrawPower) DrawPower = power > 1 ? 1 : power;
                releaseFrames = 0;
                return null;
            }
            releaseFrames++;
            if (releaseFrames < settings.ReleaseConfirmationFrames) return null;
            if (DrawPower < settings.MinimumDrawPower) { Cancel(); return null; }

            ShotCommand shot = new ShotCommand(AimX, AimY, DrawPower);
            State = GesturePhase.Cooldown;
            cooldownUntil = now + settings.CooldownSeconds;
            DrawPower = 0;
            releaseFrames = 0;
            openSince = double.NaN;
            return shot;
        }

        public void Reset()
        {
            State = GesturePhase.WaitingForHands;
            DrawPower = 0; AimX = 0; AimY = 0;
            hasSequence = false; lastSequence = 0;
            lastTimestamp = double.NegativeInfinity;
            lastNow = double.NegativeInfinity;
            openSince = double.NaN;
            cooldownUntil = 0; startDistance = 0; releaseFrames = 0;
        }

        private void Cancel()
        {
            // A tracking glitch must not shorten a shot's existing cooldown.
            if (State != GesturePhase.Cooldown) State = GesturePhase.WaitingForHands;
            DrawPower = 0; startDistance = 0; releaseFrames = 0; openSince = double.NaN;
        }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        private static bool ValidHand(TrackedHand hand)
        {
            return hand.Tracked && Finite(hand.X) && hand.X >= 0 && hand.X <= 1 &&
                Finite(hand.Y) && hand.Y >= 0 && hand.Y <= 1 && Finite(hand.PinchRatio) && hand.PinchRatio >= 0;
        }
        private static float Distance(TrackedHand a, TrackedHand b)
        { double x = a.X - b.X; double y = a.Y - b.Y; return (float)Math.Sqrt(x * x + y * y); }
    }
}
