using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inventoryref-mb-consumeiteminstantaction.html")]
    public class ConsumeItemInstantAction : FpsInventoryInstantUseAction
    {
        [SerializeField, Tooltip("How many of the item should be consumed when triggered.")]
        private int m_ConsumeAmount = 1;

        public override void PerformAction()
        {
            item.quantity -= m_ConsumeAmount;
        }
    }
}