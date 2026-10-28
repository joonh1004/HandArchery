using HandArchery.Input;
using UnityEngine;
using UnityEngine.UI;

namespace HandArchery.Presentation
{
    // Samples the existing input camera directly; never starts, stops or owns it.
    public sealed class CameraPreviewView : RawImage
    {
        int rotation;
        bool verticalMirror, viewportMirror;

        public bool Show(MediaPipeHandInput source)
        {
            WebCamTexture current = source == null ? null : source.PreviewTexture;
            if (source == null || !source.PreviewReady || current == null)
            {
                texture = null;
                enabled = false; // Leave the black backing visible instead of a frozen frame.
                return false;
            }
            int angle = ((current.videoRotationAngle % 360) + 360) % 360;
            if (angle % 90 != 0) { texture = null; enabled = false; return false; }
            if (rotation != angle || verticalMirror != current.videoVerticallyMirrored || viewportMirror != source.MirrorViewport)
            {
                rotation = angle; verticalMirror = current.videoVerticallyMirrored; viewportMirror = source.MirrorViewport;
                SetVerticesDirty();
            }
            texture = current;
            var bounds = ((RectTransform)transform.parent).rect;
            CameraPreviewGeometry.Fit(bounds.width, bounds.height, current.width, current.height, rotation,
                out float width, out float height);
            rectTransform.sizeDelta = new Vector2(width, height);
            enabled = true;
            return true;
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            base.OnPopulateMesh(mesh);
            var bounds = GetPixelAdjustedRect();
            if (bounds.width <= 0 || bounds.height <= 0) return;
            var vertex = new UIVertex();
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                float x = (vertex.position.x - bounds.xMin) / bounds.width;
                float y = (vertex.position.y - bounds.yMin) / bounds.height;
                CameraPreviewGeometry.SourceUv(x, y, rotation, verticalMirror, viewportMirror, out float u, out float v);
                vertex.uv0 = new Vector2(u, v);
                mesh.SetUIVertex(vertex, i);
            }
        }
    }
}
