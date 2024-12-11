using UnityEngine;

namespace NeoFPS
{
    public static class DamageEvents
    {
        public delegate void DamageHandlerHitDelegate(IDamageHandler handler, IDamageSource source, Vector3 hitPoint, DamageResult result, float damage);

        public static event DamageHandlerHitDelegate onDamageHandlerHit;

        public static bool hasSubscribers
        {
            get { return onDamageHandlerHit != null; }
        }

        public static void ReportDamageHandlerHit(IDamageHandler handler, IDamageSource source, Vector3 hitPoint, DamageResult result, float damage)
        {
            onDamageHandlerHit?.Invoke(handler, source, hitPoint, result, damage);
        }
    }
}
