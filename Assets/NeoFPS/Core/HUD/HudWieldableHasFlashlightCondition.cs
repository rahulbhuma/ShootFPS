using NeoFPS.ModularFirearms;
using NeoFPS.WieldableTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/hudref-mb-hudwieldablehasflashlightcondition.html")]
    public class HudWieldableHasFlashlightCondition : WieldableHudBase
    {
        [SerializeField, Tooltip("The objects to show when the selected wieldable has a flashlight attached.")]
        private GameObject[] m_ConditionalObjects = null;

        private IWieldableFlashlight m_Flashlight = null;

        protected override void AttachToSelection(IQuickSlotItem wieldable)
        {
            m_Flashlight = wieldable.GetComponentInChildren<IWieldableFlashlight>();
        }

        protected override void DetachFromSelection(IQuickSlotItem wieldable)
        {
            m_Flashlight = null;
        }

        protected override void ResetUI()
        {
            foreach (var go in m_ConditionalObjects)
            {
                if (go != null)
                    go.SetActive(m_Flashlight != null);
            }
        }
    }
}