using UnityEngine;

namespace HandArchery.Presentation
{
    public static class RangePrimitives
    {
        static Material template;
        public static Material Wood { get; private set; }
        public static Material Metal { get; private set; }
        public static Material Gold { get; private set; }
        public static void Configure(Material litTemplate)
        {
            template = litTemplate;
            Wood = Material(new Color(.32f, .19f, .10f));
            Metal = Material(new Color(.24f, .29f, .30f));
            Gold = Material(new Color(.87f, .66f, .22f));
        }
        public static Material Material(Color color)
        {
            var value = new Material(template) { color = color };
            if (value.HasProperty("_BaseColor")) value.SetColor("_BaseColor", color);
            return value;
        }
        public static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 localPosition,
            Vector3 scale, Quaternion rotation, Material material, bool collision = false)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = localPosition;
            go.transform.localRotation = rotation; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            var collider = go.GetComponent<Collider>();
            if (!collision && collider != null) { collider.enabled = false; Object.Destroy(collider); }
            return go;
        }
        public static LineRenderer Line(string name, Transform parent, Vector3[] points, float width, Material material)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false; line.sharedMaterial = material;
            line.startWidth = width; line.endWidth = width; line.positionCount = points.Length;
            line.numCapVertices = 4; line.numCornerVertices = 4;
            line.SetPositions(points); return line;
        }
    }
}
