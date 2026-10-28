using HandArchery.Presentation;
using UnityEngine;

namespace HandArchery.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArrowFlight : MonoBehaviour
    {
        ArcherySession session;
        Rigidbody body;
        Collider tip;
        double expiresAt;
        bool resolved;
        public int ShotId { get; private set; }
        public bool IsResolved => resolved;

        public static ArrowFlight Spawn(ArcherySession owner, int id, Vector3 origin, Vector3 aim, float power)
        {
            var go = new GameObject("Arrow " + id);
            go.transform.SetPositionAndRotation(origin, Quaternion.LookRotation((aim - origin).normalized));
            var flight = go.AddComponent<ArrowFlight>();
            flight.session = owner; flight.ShotId = id;
            flight.body = go.GetComponent<Rigidbody>();
            flight.body.useGravity = false; // deliberate straight beginner aim; no body/string tension simulation
            flight.body.mass = .025f;
            flight.body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            flight.body.interpolation = RigidbodyInterpolation.Interpolate;
            var collider = go.AddComponent<SphereCollider>(); collider.radius = .025f; collider.center = Vector3.zero;
            flight.tip = collider;
            // Root denotes arrow tip; tail trails behind to keep collider and visual hit aligned.
            RangePrimitives.Shape("Shaft", PrimitiveType.Cylinder, go.transform, new Vector3(0, 0, -.4f),
                new Vector3(.018f, .4f, .018f), Quaternion.Euler(90, 0, 0), RangePrimitives.Wood);
            RangePrimitives.Shape("Tip", PrimitiveType.Sphere, go.transform, Vector3.zero,
                new Vector3(.04f, .04f, .10f), Quaternion.identity, RangePrimitives.Metal);
            for (int i = 0; i < 3; i++)
            {
                var feather = RangePrimitives.Shape("Fletching", PrimitiveType.Cube, go.transform, new Vector3(0, 0, -.7f),
                    new Vector3(.11f, .012f, .15f), Quaternion.Euler(0, 0, i * 120), RangePrimitives.Gold);
            }
            flight.body.linearVelocity = (aim - origin).normalized * Mathf.Lerp(22f, 42f, Mathf.Clamp01(power));
            flight.expiresAt = Time.realtimeSinceStartupAsDouble + 6;
            return flight;
        }
        void Update()
        {
            if (!resolved && (Time.realtimeSinceStartupAsDouble >= expiresAt || transform.position.z > 29 || transform.position.y < -.5f)) ResolveMiss();
        }
        void OnCollisionEnter(Collision collision)
        {
            ResolveContact(collision.collider, collision.contactCount > 0 ? collision.GetContact(0).point : transform.position);
        }
        // Shared by the physical callback and the explicitly opted-in duplicate-contact smoke check.
        public void ResolveContact(Collider other, Vector3 point)
        {
            if (resolved) return;
            resolved = true; Freeze();
            var target = other == null ? null : other.GetComponentInParent<ArcheryTarget>();
            if (target != null) target.ResolveHit(ShotId, target.NormalizedRadius(point));
            else session.ResolveShot(ShotId, 0);
        }
        void ResolveMiss()
        {
            if (resolved) return;
            resolved = true; Freeze(); session.ResolveShot(ShotId, 0);
        }
        void Freeze()
        {
            if (tip != null) tip.enabled = false;
            if (body == null) return;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.isKinematic = true;
            }
        }
        public void Discard() { resolved = true; Freeze(); }
    }
}
