using HandArchery.Core;
using HandArchery.Gameplay;
using UnityEngine;

namespace HandArchery.Presentation
{
    public sealed class BowView : MonoBehaviour
    {
        ArcherySession session;
        LineRenderer bowstring, nockedArrow;
        readonly Vector3 upper = new Vector3(-.02f, .68f, -.07f);
        readonly Vector3 lower = new Vector3(-.02f, -.68f, -.07f);
        public void Initialize(ArcherySession owner)
        {
            session = owner; transform.position = owner.MuzzlePosition;
            RangePrimitives.Line("Bow limbs", transform, new[] { lower, new Vector3(-.1f,-.45f,.08f), new Vector3(-.16f,0,.17f), new Vector3(-.1f,.45f,.08f), upper }, .045f, RangePrimitives.Wood);
            RangePrimitives.Shape("Bow grip", PrimitiveType.Cylinder, transform, new Vector3(-.16f, 0, .17f), new Vector3(.075f,.14f,.075f), Quaternion.identity, RangePrimitives.Metal);
            bowstring = RangePrimitives.Line("Bow string", transform, new[] { upper, Vector3.zero, lower }, .007f, RangePrimitives.Gold);
            nockedArrow = RangePrimitives.Line("Nocked arrow", transform, new[] { Vector3.zero, Vector3.forward * .7f }, .015f, RangePrimitives.Gold);
        }
        void LateUpdate()
        {
            if (session == null) return;
            transform.rotation = Quaternion.LookRotation(session.AimWorld - transform.position);
            float pull = session.Gesture.DrawPower;
            var anchor = new Vector3(0, 0, -.06f - pull * .35f);
            bowstring.SetPosition(1, anchor);
            nockedArrow.SetPosition(0, anchor); nockedArrow.SetPosition(1, Vector3.forward * .7f);
            nockedArrow.enabled = session.HasStarted && session.Round.RemainingShots > 0 && session.Gesture.State != GesturePhase.Cooldown;
        }
    }
}
