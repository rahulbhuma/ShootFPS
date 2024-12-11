using System;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    [RequireComponent(typeof(Collider))]
    [HelpURL("https://docs.neofps.com/manual/healthref-mb-eventdamagehandler.html")]
    public class EventDamageHandler : MonoBehaviour, IDamageHandler, IHasHealthManager
    {
		[SerializeField, Tooltip("The value to multiply any incoming damage by. Use to reduce damage to areas like feet, or raise it for areas like the head.")]
        private float m_Multiplier = 1f;

		[SerializeField, Tooltip("Does the damage count as critical. Used to change the feedback for the damage taker and dealer.")]
        private bool m_Critical = false;

		[SerializeField, Tooltip("An event that is invoked when damage is taken.")]
        private DamageEvent m_OnDamage = new DamageEvent();

        [Serializable]
        public class DamageEvent : UnityEvent<float, Vector3, bool>
        {
        }

        private Collider m_Collider = null;
        private Transform m_LocalTransform;

        public IHealthManager healthManager
        {
            get;
            private set;
        }

        public Transform healthTransform
        {
            get;
            private set;
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            if (m_Multiplier < 0f)
                m_Multiplier = 0f;
        }
#endif
        protected void Awake()
        {
            m_LocalTransform = transform;
            healthManager = GetComponentInParent<IHealthManager>();

            if (healthManager == null)
            {
                healthTransform = transform;
                if (m_Multiplier <= 0f)
                    enabled = false;
            }
            else
                healthTransform = healthManager.transform;

            m_Collider = GetComponent<Collider>();
            Debug.Assert(m_Collider != null, "Damage handlers should only be placed on objects with a collider");
        }

        #region IDamageHandler implementation

        private DamageFilter m_InDamageFilter = DamageFilter.AllDamageAllTeams;
        public DamageFilter inDamageFilter
        {
            get { return m_InDamageFilter; }
            set { m_InDamageFilter = value; }
        }

        public DamageResult AddDamage(float damage)
        {
            if (m_Multiplier > 0f)
            {
                damage *= m_Multiplier;
                var result = m_Critical ? DamageResult.Critical : DamageResult.Standard;

                if (healthManager != null)
                    healthManager.AddDamage(damage, m_Critical);

                // Invoke event
                m_OnDamage.Invoke(damage, Vector3.zero, m_Critical);
				
                // Report damage dealt
                DamageEvents.ReportDamageHandlerHit(this, null, m_Collider.bounds.center, result, damage);

                return result;
            }
            else
                return DamageResult.Ignored;
        }

        public DamageResult AddDamage(float damage, IDamageSource source)
        {
            // Apply damage
            if (m_Multiplier > 0f && CheckDamageCollision(source))
            {
                damage *= m_Multiplier;
                var result = m_Critical ? DamageResult.Critical : DamageResult.Standard;

                if (healthManager != null)
                    healthManager.AddDamage(damage, m_Critical, source);

                // Get hit direction
                Vector3 direction = Vector3.zero;
                if (source != null && source.damageSourceTransform != null)
                    direction = Vector3.Normalize(m_LocalTransform.position - source.damageSourceTransform.position);

                // Invoke event
                m_OnDamage.Invoke(damage, direction, m_Critical);
                
                // Report damage dealt
                if (damage > 0f && source != null && source.controller != null)
                    source.controller.currentCharacter.ReportTargetHit(m_Critical);
                DamageEvents.ReportDamageHandlerHit(this, source, m_Collider.bounds.center, result, damage);

                return result;
            }
            else
                return DamageResult.Ignored;
        }

        public DamageResult AddDamage(float damage, RaycastHit hit)
        {
            if (m_Multiplier > 0f)
            {
                damage *= m_Multiplier;
                var result = m_Critical ? DamageResult.Critical : DamageResult.Standard;

                if (healthManager != null)
                    healthManager.AddDamage(damage, m_Critical, hit);

                // Invoke event
                m_OnDamage.Invoke(damage, Vector3.zero, m_Critical);

                // Report damage dealt
                DamageEvents.ReportDamageHandlerHit(this, null, hit.point, result, damage);

                return result;
            }
            else
                return DamageResult.Ignored;
        }

        public DamageResult AddDamage(float damage, RaycastHit hit, IDamageSource source)
        {
            // Apply damage
            if (m_Multiplier > 0f && CheckDamageCollision(source))
            {
                damage *= m_Multiplier;
                var result = m_Critical ? DamageResult.Critical : DamageResult.Standard;

                if (healthManager != null)
                    healthManager.AddDamage(damage, m_Critical, source, hit);

                // Get hit direction
                Vector3 direction = Vector3.zero;
                if (source != null && source.damageSourceTransform != null)
                    direction = Vector3.Normalize(m_LocalTransform.position - source.damageSourceTransform.position);

                // Invoke event
                m_OnDamage.Invoke(damage, direction, m_Critical);

                // Report damage dealt
                if (damage > 0f && source != null && source.controller != null)
                    source.controller.currentCharacter.ReportTargetHit(m_Critical);
                DamageEvents.ReportDamageHandlerHit(this, source, hit.point, result, damage);

                return result;
            }
            else
                return DamageResult.Ignored;
        }

        bool CheckDamageCollision(IDamageSource source)
        {
            return !(source != null && !source.outDamageFilter.CollidesWith(inDamageFilter, FpsGameMode.friendlyFire));
        }

        #endregion
    }
}