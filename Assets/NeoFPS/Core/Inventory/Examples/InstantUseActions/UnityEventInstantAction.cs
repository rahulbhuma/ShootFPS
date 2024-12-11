using NeoFPS.Constants;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inventoryref-mb-unityeventinstantaction.html")]
    public class UnityEventInstantAction : FpsInventoryInstantUseAction
    {
        [SerializeField, Tooltip("The event to fire on using the instant use item.")]
        private UnityEvent m_OnUsed = null;

        public override void PerformAction()
        {
            m_OnUsed.Invoke();
        }
    }
}