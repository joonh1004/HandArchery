using System;

namespace HandArchery.Presentation
{
    // Pure geometry: invert the same vertical correction and clockwise rotation used
    // by CameraPixelTransform, then apply the viewport's horizontal mirror.
    public static class CameraPreviewGeometry
    {
        public static void SourceUv(float x, float y, int clockwiseDegrees, bool verticallyMirrored,
            bool mirrorViewport, out float u, out float v)
        {
            int angle = NormalizeRotation(clockwiseDegrees);
            float uprightX = mirrorViewport ? 1 - x : x;
            float uprightTopY = 1 - y;
            float sourceTopY;
            switch (angle)
            {
                case 90: u = uprightTopY; sourceTopY = 1 - uprightX; break;
                case 180: u = 1 - uprightX; sourceTopY = 1 - uprightTopY; break;
                case 270: u = 1 - uprightTopY; sourceTopY = uprightX; break;
                default: u = uprightX; sourceTopY = uprightTopY; break;
            }
            v = verticallyMirrored ? sourceTopY : 1 - sourceTopY;
        }

        public static void Fit(float availableWidth, float availableHeight, int sourceWidth, int sourceHeight,
            int clockwiseDegrees, out float width, out float height)
        {
            int angle = NormalizeRotation(clockwiseDegrees);
            if (availableWidth <= 0 || availableHeight <= 0 || sourceWidth <= 0 || sourceHeight <= 0)
            { width = 0; height = 0; return; }
            bool sideways = angle == 90 || angle == 270;
            float uprightWidth = sideways ? sourceHeight : sourceWidth;
            float uprightHeight = sideways ? sourceWidth : sourceHeight;
            float scale = Math.Min(availableWidth / uprightWidth, availableHeight / uprightHeight);
            width = uprightWidth * scale;
            height = uprightHeight * scale;
        }

        static int NormalizeRotation(int clockwiseDegrees)
        {
            int angle = ((clockwiseDegrees % 360) + 360) % 360;
            if (angle % 90 != 0) throw new ArgumentOutOfRangeException(nameof(clockwiseDegrees));
            return angle;
        }
    }
}
