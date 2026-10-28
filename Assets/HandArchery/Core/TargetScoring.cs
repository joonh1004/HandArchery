namespace HandArchery.Core
{
    public static class TargetScoring
    {
        // Array storage fixes each boundary to binary32. Mono may retain extra
        // precision for an intermediate ring / 10f expression during comparison.
        private static readonly float[] RingBoundaries =
        { .1f, .2f, .3f, .4f, .5f, .6f, .7f, .8f, .9f, 1f };

        public static int ScoreAtNormalizedRadius(float radius)
        {
            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius < 0 || radius > 1) return 0;
            // Compare stored floats: arithmetic intermediates can demote .3f.
            // No broad epsilon: the immediately adjacent outward float belongs to the next ring.
            for (int index = 0; index < RingBoundaries.Length; index++)
                if (radius <= RingBoundaries[index]) return 10 - index;
            return 0;
        }
    }
}
