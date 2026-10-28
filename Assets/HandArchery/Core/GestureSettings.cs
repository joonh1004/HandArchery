namespace HandArchery.Core
{
    public enum GesturePhase { WaitingForHands, Ready, Drawing, Cooldown }

    public sealed class GestureSettings
    {
        public bool BowIsLeft { get; set; } = true;
        public float PinchCloseRatio { get; set; } = .35f;
        public float PinchReleaseRatio { get; set; } = .65f;
        public double OpenStabilitySeconds { get; set; } = .2;
        public double MaximumFrameAgeSeconds { get; set; } = .25;
        public int ReleaseConfirmationFrames { get; set; } = 2;
        public float MinimumDrawPower { get; set; } = .1f;
        public float DrawDistanceRange { get; set; } = .30f;
        public double CooldownSeconds { get; set; } = .35;
    }
}
