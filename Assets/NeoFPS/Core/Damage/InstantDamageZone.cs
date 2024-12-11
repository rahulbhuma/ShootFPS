using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/healthref-mb-instantdamagezone.html")]
    public class InstantDamageZone : CharacterTriggerZone, IDamageSource
    {
        [SerializeField, Tooltip("The amount of damage to apply to the player character per second.")]
        private float m_Damage = 10f;
        [SerializeField, Tooltip("The type of damage to apply.")]
        private DamageType m_DamageType = DamageType.Default;
        [SerializeField, Tooltip("A description of the damage to use in logs, etc.")]
        private string m_DamageDescription = "Damage Zone";
        [SerializeField, Tooltip("If this is true, then the damage zone will apply damage directly to the health manager of the character, instead of going through the damage handlers which could modify the results (eg armour, shields, etc).")]
        private bool m_BypassDamageHandler = false;

        private DamageFilter m_OutDamageFilter = DamageFilter.AllDamageAllTeams;

        protected override void OnCharacterEntered(ICharacter c)
        {
            base.OnCharacterEntered(c);

            if (!m_BypassDamageHandler && c.gameObject.TryGetComponent(out IDamageHandler dh))
                dh.AddDamage(m_Damage, this);
            else
            {
                if (c.gameObject.TryGetComponent(out IHealthManager hm))
                    hm.AddDamage(m_Damage, false, this);
            }
        }

        protected void Awake()
        {
            m_OutDamageFilter.SetDamageType(m_DamageType);
        }

        #region IDamageSource IMPLEMENTATION

        public DamageFilter outDamageFilter
        {
            get { return new DamageFilter(m_DamageType, DamageTeamFilter.All); }
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