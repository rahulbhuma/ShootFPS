using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inventoryref-mb-healplayerinstantaction.html")]
    public class HealPlayerInstantAction : FpsInventoryInstantUseAction, IDamageSource
    {
        [SerializeField, Tooltip("The (max) amount of health to add to the wielder's health manager.")]
        private float m_HealAmount = 25f;

        private IHealthManager m_HealthManager = null;

        public override bool canUse
        {
            get { return m_HealthManager == null || !Mathf.Approximately(m_HealthManager.health, m_HealthManager.healthMax); }
        }

        private DamageFilter m_OutDamageFilter = DamageFilter.AllDamageAllTeams;
        public DamageFilter outDamageFilter
        {
            get { return m_OutDamageFilter; }
            set { m_OutDamageFilter = value; }
        }

        public IController controller
        {
            get;
            private set;
        }

        public Transform damageSourceTransform
        {
            get;
            private set;
        }

        public string description
        {
            get { return "Heal"; }
        }

        public override void Initialise(FpsInventoryItemBase item)
        {
            base.Initialise(item);

            var character = item.GetComponentInParent<ICharacter>();
            if (character != null)
            {
                controller = character.controller;
                damageSourceTransform = character.transform;
            }
            else
            {
                controller = null;
                damageSourceTransform = item.transform;
            }

            m_HealthManager = item.GetComponentInParent<IHealthManager>();
        }

        public override void PerformAction()
        {
            m_HealthManager?.AddHealth(m_HealAmount);
        }
    }
}