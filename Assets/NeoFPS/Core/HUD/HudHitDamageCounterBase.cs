using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS;

namespace NeoFPS.AI
{
    public abstract class HudHitDamageCounterBase : WorldSpaceHudMarkerItemBase
    {
        [SerializeField, Tooltip("The amount of time the counter should remain visible")]
        private float m_Lifetime = 2f;

        protected float lifetime
        {
            get { return m_Lifetime; }
        }

        protected float elapsed
        {
            get;
            private set;
        }

        public abstract void Initialise(Vector3 worldPos, float damage, bool critical);

        public override bool Tick()
        {
            elapsed += Time.deltaTime;
            return elapsed < m_Lifetime;
        }

        private void OnEnable()
        {
            elapsed = 0f;
        }
    }
}