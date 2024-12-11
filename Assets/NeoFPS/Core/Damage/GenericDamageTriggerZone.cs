using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/healthref-mb-genericdamagetriggerzone.html")]
    public class GenericDamageTriggerZone : MonoBehaviour, IDamageSource
    {
        [SerializeField, Tooltip("The amount of damage to apply to the player character per second.")]
        private float m_DamagePerSecond = 10f;
        [SerializeField, Tooltip("The type of damage to apply.")]
        private DamageType m_DamageType = DamageType.Default;
        [SerializeField, Tooltip("A description of the damage to use in logs, etc.")]
        private string m_DamageDescription = "Damage Zone";

        private DamageFilter m_OutDamageFilter = DamageFilter.AllDamageAllTeams;

        private Dictionary<int, IDamageHandler> m_DamageHandlers = new Dictionary<int, IDamageHandler>();

        public float damagePerSecond
        {
            get { return m_DamagePerSecond; }
            protected set { m_DamagePerSecond = value; }
        }

        public DamageType damageType
        {
            get { return m_DamageType; }
            protected set
            {
                m_DamageType = value;
                m_OutDamageFilter.SetDamageType(m_DamageType);
            }
        }

        protected void OnTriggerEnter(Collider other)
        {
            var handler = other.GetComponent<IDamageHandler>();
            if (handler != null)
                m_DamageHandlers.Add(other.GetInstanceID(), handler);
        }

        protected void OnTriggerStay(Collider other)
        {
            IDamageHandler handler;
            if (m_DamageHandlers.TryGetValue(other.GetInstanceID(), out handler))
                handler.AddDamage(m_DamagePerSecond * Time.deltaTime, this);
        }

        protected void OnTriggerExit(Collider other)
        {
            m_DamageHandlers.Remove(other.GetInstanceID());
        }

        protected void Awake()
        {
            m_OutDamageFilter.SetDamageType(m_DamageType);
        }

        #region IDamageSource IMPLEMENTATION

        public DamageFilter outDamageFilter
        {
            get { return m_OutDamageFilter;  }
            set { m_OutDamageFilter = value; }
        }

        public IController controller
        {
            get { return null; }
        }

        public Transform damageSourceTransform
        {
            get { return transform; }
        }

        public string description
        {
            get { return m_DamageDescription; }
        }

        #endregion
    }
}