using System;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/neofpsref-mb-originshifttransform.html")]
    public class OriginShiftTransform : OriginShiftSubscriberBase
    {
        [SerializeField, Tooltip("When should the component subscribe and unsubscribe from the origin shift system. ObjectLifecycle means that the component will subscribe on Awake and unsubscribe when destroyed - the object will be repositioned when inactive. EnabledOnly means the component will subscribe when enabled and unsubscribe when disabled.")]
        private OriginShift.SubscriptionPeriod m_SubscriptionPeriod = OriginShift.SubscriptionPeriod.EnabledOnly;

        protected override OriginShift.SubscriptionPeriod subscriptionPeriod
        {
            get { return m_SubscriptionPeriod; }
        }

        public override void ApplyOffset(Vector3 offset)
        {
            transform.position += offset;
        }
    }
}
