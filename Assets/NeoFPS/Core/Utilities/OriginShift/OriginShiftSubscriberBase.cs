using System;
using UnityEngine;

namespace NeoFPS
{
    public abstract class OriginShiftSubscriberBase : MonoBehaviour, IOriginShiftSubscriber
    {
        private void Awake()
        {
            if (subscriptionPeriod == OriginShift.SubscriptionPeriod.ObjectLifecycle)
                RegisterSubscriber(true);
        }

        private void OnDestroy()
        {
            if (subscriptionPeriod == OriginShift.SubscriptionPeriod.ObjectLifecycle)
                UnregisterSubscriber();
        }

        private void OnEnable()
        {
            if (subscriptionPeriod == OriginShift.SubscriptionPeriod.EnabledOnly)
                RegisterSubscriber(false);
        }

        private void OnDisable()
        {
            if (subscriptionPeriod == OriginShift.SubscriptionPeriod.EnabledOnly)
                UnregisterSubscriber();
        }

        void RegisterSubscriber(bool applyOffset)
        {
            if (OriginShift.system != null)
            {
                OriginShift.system.AddSubscriber(this);
                //if (applyOffset)
                //    OnOffsetChanged(OriginShift.system.currentOffset);
            }

            OriginShift.onOriginShiftSystemChanged += OnFloatingOriginSystemChanged;
        }

        void UnregisterSubscriber()
        {
            if (OriginShift.system != null)
            {
                OriginShift.system.RemoveSubscriber(this);
            }

            OriginShift.onOriginShiftSystemChanged -= OnFloatingOriginSystemChanged;
        }

        private void OnFloatingOriginSystemChanged(OriginShift system)
        {
            if (system != null)
                system.AddSubscriber(this);
        }

        private void OnOffsetChanged(Vector3 offset)
        {
            if (offset != Vector3.zero)
                transform.position += offset;
        }

        protected abstract OriginShift.SubscriptionPeriod subscriptionPeriod { get; }

        public abstract void ApplyOffset(Vector3 offset);
    }
}