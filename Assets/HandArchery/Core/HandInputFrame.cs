namespace HandArchery.Core
{
    public readonly struct TrackedHand
    {
        public bool Tracked { get; }
        public float X { get; }
        public float Y { get; }
        public float PinchRatio { get; }
        public TrackedHand(bool tracked, float x, float y, float pinchRatio)
        { Tracked = tracked; X = x; Y = y; PinchRatio = pinchRatio; }
    }

    public readonly struct HandInputFrame
    {
        public long Sequence { get; }
        public double TimestampSeconds { get; }
        public TrackedHand Left { get; }
        public TrackedHand Right { get; }
        public HandInputFrame(long sequence, double timestampSeconds, TrackedHand left, TrackedHand right)
        { Sequence = sequence; TimestampSeconds = timestampSeconds; Left = left; Right = right; }
    }
}
