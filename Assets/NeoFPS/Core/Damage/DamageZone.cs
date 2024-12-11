using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/healthref-mb-damagezone.html")]
    public class DamageZone : CharacterTriggerZonePersistant, IDamageSource
    {
        [SerializeField, Tooltip("The amount of damage to apply to the player character per second.")]
        private float m_DamagePerSecond = 10f;
        [SerializeField, Tooltip("The type of damage to apply.")]
        private DamageType m_DamageType = DamageType.Default;
        [SerializeField, Tooltip("A description of the damage to use in logs, etc.")]
        private string m_DamageDescription = "Damage Zone";

        private DamageFilter m_OutDamageFilter = DamageFilter.AllDamageAllTeams;

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

        protected override void OnCharacterStay(ICharacter c)
        {
            var hm = c.GetComponent<IHealthManager>();
            if (hm != null)
                hm.AddDamage(m_DamagePerSecond * Time.deltaTime, false, this);

            base.OnCharacterStay(c);
        }

        protected void Awake()
        {
            m_OutDamageFilter.SetDamageType(m_DamageType);
        }

        #region IDamageSource IMPLEMENTATION

        public DamageFilter outDamageFilter
        {
            get { return m_OutDamageFilter; }
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