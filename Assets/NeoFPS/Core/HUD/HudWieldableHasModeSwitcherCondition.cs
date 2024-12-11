using NeoFPS.ModularFirearms;
using NeoFPS.WieldableTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/hudref-mb-hudwieldablehasmodeswitchercondition.html")]
    public class HudWieldableHasModeSwitcherCondition : WieldableHudBase
    {
        [SerializeField, Tooltip("The objects to show when the selected wieldable has a mode switcher component attached.")]
        private GameObject[] m_ConditionalObjects = null;

        private IModularFirearmModeSwitcher m_Switcher = null;

        protected override void AttachToSelection(IQuickSlotItem wieldable)
        {
            m_Switcher = wieldable.GetComponentInChildren<IModularFirearmModeSwitcher>();
        }

        protected override void DetachFromSelection(IQuickSlotItem wieldable)
        {
            m_Switcher = null;
        }

        protected override void ResetUI()
        {
            foreach (var go in m_ConditionalObjects)
            {
                if (go != null)
                    go.SetActive(m_Switcher != null);
            }
        }
    }
}