using UnityEngine;

namespace HandArchery.Input
{
    public static class CameraPixelTransform
    {
        // WebCamTexture arrays are bottom-up. MediaPipe expects top-down RGB rows.
        // Rotate pixels before inference, so landmark coordinates need no second rotation.
        public static void Upright(Color32[] source, Color32[] destination, int width, int height,
            int clockwiseDegrees, bool verticallyMirrored)
        {
            int angle = ((clockwiseDegrees % 360) + 360) % 360;
            if (angle % 90 != 0) throw new System.ArgumentOutOfRangeException(nameof(clockwiseDegrees));
            if (source.Length != width * height || destination.Length != source.Length)
                throw new System.ArgumentException("Pixel buffer dimensions do not match");
            int outputWidth = angle == 90 || angle == 270 ? height : width;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int sourceY = verticallyMirrored ? y : height-1-y;
                int dx = x, dy = y;
                switch (angle)
                {
                    case 90: dx = height-1-y; dy = x; break;
                    case 180: dx = width-1-x; dy = height-1-y; break;
                    case 270: dx = y; dy = width-1-x; break;
                }
                destination[dy * outputWidth + dx] = source[sourceY * width + x];
            }
        }
    }
}
