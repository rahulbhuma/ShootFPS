using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inventoryref-mb-rechargeshieldsinstantaction.html")]
    public class RechargeShieldsInstantAction : FpsInventoryInstantUseAction
    {
        [SerializeField, Tooltip("The number of shield steps to recharge.")]
        private int m_StepCount = 1;

        private ShieldManager m_ShieldManager = null;

        public override bool canUse
        {
            get { return m_ShieldManager == null || !m_ShieldManager.isFull; }
        }

        public override void Initialise(FpsInventoryItemBase item)
        {
            base.Initialise(item);

            m_ShieldManager = item.GetComponentInParent<ShieldManager>();
        }

        public override void PerformAction()
        {
            m_ShieldManager?.FillShieldSteps(m_StepCount);
        }
    }
}