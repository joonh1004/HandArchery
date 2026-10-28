using HandArchery.Core;
namespace HandArchery.Input
{
    public sealed class LatestHandFrame : IHandInputSource
    {
        public string StatusMessage { get; private set; } = "Not started";
        HandInputFrame latest;
        long sequence;
        public void Publish(double capturedAt, TrackedHand left, TrackedHand right, string status)
        {
            if (double.IsNaN(capturedAt) || double.IsInfinity(capturedAt) || capturedAt < 0)
                throw new System.ArgumentOutOfRangeException(nameof(capturedAt));
            latest = new HandInputFrame(++sequence,capturedAt,left,right);
            StatusMessage = status;
        }
        public bool TryGetLatest(out HandInputFrame frame) { frame = latest; return sequence > 0; }
    }
}
