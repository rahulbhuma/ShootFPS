using UnityEngine;

namespace NeoFPS.ModularFirearms
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-delegatedtargettrackingammoeffect.html")]
    public class DelegatedTargetTrackingAmmoEffect : BaseAmmoEffect
    {
        [SerializeField, Tooltip("The targeting system to assign the tagged transform to.")]
        private TransformTargetingSystem m_TargetingSystem = null;

        [SerializeField, ComponentOnObject(typeof(BaseAmmoEffect), false, false), Tooltip("An optional secondary ammo effect to allow the tracking bullet to deal damage, etc.")]
        private BaseAmmoEffect m_SecondaryEffect = null;

        [SerializeField, Tooltip("The object tags that can be targeted.")]
        private string m_ValidObjectTag = string.Empty;

        protected BaseAmmoEffect secondaryAmmoEffect
        {
            get { return m_SecondaryEffect; }
        }

        public override void Hit(RaycastHit hit, Vector3 rayDirection, float totalDistance, float speed, IDamageSource damageSource)
        {
            // Apply the hit effect
            if (secondaryAmmoEffect != null)
                secondaryAmmoEffect.Hit(hit, rayDirection, totalDistance, speed, damageSource);

            // Tag the target
            if (m_ValidObjectTag == string.Empty || hit.collider.transform.gameObject.CompareTag(m_ValidObjectTag))
                TagTarget(hit.collider.transform, hit.point);

            // If you want to add conditions for tagging:
            // - Derive a new behaviour from this class
            // - Override this method, but don't call the base method
            // - Copy / paste the secondary ammo effect lines
            // - Perform your tests, and then call TagTarget if they pass
            // (I didn't want too many methods taking raycast hits as parameters since they're chunky)
        }

        protected void TagTarget(Transform t, Vector3 hitPoint)
        {
            // Get the relative position
            Vector3 relativePosition = hitPoint - t.position;
            relativePosition = Quaternion.Inverse(t.rotation) * relativePosition;

            // Apply to active trackers
            m_TargetingSystem.SetTargetTransform(t, relativePosition);
        }
    }
}