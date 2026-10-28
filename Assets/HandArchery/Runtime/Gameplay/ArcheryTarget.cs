using HandArchery.Core;
using UnityEngine;

namespace HandArchery.Gameplay
{
    public sealed class ArcheryTarget : MonoBehaviour
    {
        ArcherySession session;
        public float Radius { get; private set; } = 1.8f;
        public Vector3 Center => transform.position;
        public void Initialize(ArcherySession owner) { session = owner; }
        public float NormalizedRadius(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            return new Vector2(local.x, local.y).magnitude / Radius;
        }
        public bool ResolveHit(int shotId, float normalizedRadius)
        {
            return session != null && session.ResolveShot(shotId, TargetScoring.ScoreAtNormalizedRadius(normalizedRadius));
        }
    }
}
