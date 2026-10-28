using HandArchery.Core;

namespace HandArchery.Input
{
    // Input is the upright, unmirrored MediaPipe image (top-left origin).
    public static class HandCoordinates
    {
        public static TrackedHand Convert(float x, float y, float thumbX, float thumbY,
            float indexX, float indexY, float palmAX, float palmAY, float palmBX, float palmBY,
            int width, int height, bool mirror)
        {
            float[] values = {x,y,thumbX,thumbY,indexX,indexY,palmAX,palmAY,palmBX,palmBY};
            foreach (var value in values)
                if (float.IsNaN(value) || float.IsInfinity(value)) return default;
            if (width <= 0 || height <= 0 || x < 0 || x > 1 || y < 0 || y > 1) return default;
            double palmX = ((double)palmAX-palmBX)*width, palmY = ((double)palmAY-palmBY)*height;
            double pinchX = ((double)thumbX-indexX)*width, pinchY = ((double)thumbY-indexY)*height;
            double palm = System.Math.Sqrt(palmX*palmX+palmY*palmY);
            if (palm < 1) return default;
            double ratio = System.Math.Sqrt(pinchX*pinchX+pinchY*pinchY)/palm;
            if (double.IsNaN(ratio) || double.IsInfinity(ratio) || ratio > float.MaxValue) return default;
            return new TrackedHand(true, mirror ? 1-x : x, 1-y,(float)ratio);
        }
    }
}
