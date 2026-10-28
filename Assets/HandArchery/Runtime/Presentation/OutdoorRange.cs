using HandArchery.Gameplay;
using UnityEngine;

namespace HandArchery.Presentation
{
    public static class OutdoorRange
    {
        public static ArcheryTarget Create(Transform root)
        {
            var grass = RangePrimitives.Material(new Color(.24f, .38f, .28f));
            var lane = RangePrimitives.Material(new Color(.52f, .50f, .35f));
            var darkGreen = RangePrimitives.Material(new Color(.13f, .24f, .20f));
            RangePrimitives.Shape("Grass", PrimitiveType.Plane, root, new Vector3(0, -.08f, 12), new Vector3(9, 1, 10), Quaternion.identity, grass, true);
            RangePrimitives.Shape("Shooting lane", PrimitiveType.Cube, root, new Vector3(0, -.045f, 11), new Vector3(5.2f, .045f, 26), Quaternion.identity, lane);
            RangePrimitives.Shape("Shooting line", PrimitiveType.Cube, root, new Vector3(0, 0, 1), new Vector3(5.2f, .02f, .12f), Quaternion.identity, RangePrimitives.Gold);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 7; i++)
                {
                    var position = new Vector3(side * 6, .85f, i * 5);
                    RangePrimitives.Shape("Fence post", PrimitiveType.Cylinder, root, position, new Vector3(.13f, .9f, .13f), Quaternion.identity, RangePrimitives.Wood);
                    RangePrimitives.Shape("Fence rail", PrimitiveType.Cube, root, position + new Vector3(0, .25f, 2.5f), new Vector3(.12f, .12f, 5.1f), Quaternion.identity, RangePrimitives.Wood);
                }
                for (int i = 0; i < 8; i++)
                {
                    RangePrimitives.Shape("Tree trunk", PrimitiveType.Cylinder, root, new Vector3(side * (9 + i % 2 * 3), 1, 4 + i * 5), new Vector3(.5f, 1, .5f), Quaternion.identity, RangePrimitives.Wood);
                    RangePrimitives.Shape("Tree canopy", PrimitiveType.Sphere, root, new Vector3(side * (9 + i % 2 * 3), 3.2f, 4 + i * 5), new Vector3(3.6f, 4.2f, 3.6f), Quaternion.identity, darkGreen);
                }
            }
            var targetObject = new GameObject("Ten-ring target"); targetObject.transform.SetParent(root, false);
            targetObject.transform.position = new Vector3(0, 2.5f, 22);
            var target = targetObject.AddComponent<ArcheryTarget>();
            var collider = targetObject.AddComponent<BoxCollider>(); collider.center = new Vector3(0, 0, .08f); collider.size = new Vector3(3.6f, 3.6f, .16f);
            Color[] colors = { new Color(.94f, .94f, .87f), new Color(.96f, .96f, .9f), new Color(.10f,.13f,.16f), new Color(.13f,.16f,.19f),
                new Color(.13f,.40f,.65f), new Color(.15f,.45f,.72f), new Color(.74f,.20f,.17f), new Color(.83f,.24f,.19f),
                new Color(.94f,.72f,.18f), new Color(1,.82f,.25f) };
            // Separate coplanar bands by a few millimetres to avoid z fighting; collision plane stays single.
            for (int ring = 0; ring < 10; ring++)
            {
                float diameter = target.Radius * 2 * (10 - ring) / 10f;
                RangePrimitives.Shape("Score ring " + (ring + 1), PrimitiveType.Cylinder, targetObject.transform,
                    new Vector3(0, 0, -.005f - ring * .004f), new Vector3(diameter, .007f, diameter), Quaternion.Euler(90, 0, 0), RangePrimitives.Material(colors[ring]));
            }
            for (int ring = 1; ring <= 10; ring++)
            {
                var points = new Vector3[97];
                for (int segment = 0; segment < points.Length; segment++)
                {
                    float angle = segment * Mathf.PI * 2 / 96;
                    points[segment] = new Vector3(Mathf.Cos(angle) * target.Radius * ring / 10,
                        Mathf.Sin(angle) * target.Radius * ring / 10, -.054f);
                }
                RangePrimitives.Line("Ring boundary " + ring, targetObject.transform, points, .009f, RangePrimitives.Metal);
            }
            RangePrimitives.Shape("Target support left", PrimitiveType.Cube, root, new Vector3(-1.1f, 1.1f, 22.3f), new Vector3(.16f, 2.4f, .16f), Quaternion.Euler(0, 0, -15), RangePrimitives.Wood);
            RangePrimitives.Shape("Target support right", PrimitiveType.Cube, root, new Vector3(1.1f, 1.1f, 22.3f), new Vector3(.16f, 2.4f, .16f), Quaternion.Euler(0, 0, 15), RangePrimitives.Wood);
            RangePrimitives.Shape("Backstop", PrimitiveType.Cube, root, new Vector3(0, 2.6f, 25), new Vector3(8, 5.2f, .16f), Quaternion.identity, darkGreen, true);
            return target;
        }
    }
}
