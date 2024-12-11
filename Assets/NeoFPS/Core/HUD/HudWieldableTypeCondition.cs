using NeoFPS.ModularFirearms;
using NeoFPS.WieldableTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/hudref-mb-hudwieldabletypecondition.html")]
    public class HudWieldableTypeCondition : WieldableHudBase
    {
        [SerializeField, Tooltip("The object to show when the selected wieldable is a firearm")]
        private GameObject m_FirearmObject = null;
        [SerializeField, Tooltip("The object to show when the selected wieldable is a melee weapon")]
        private GameObject m_MeleeWeaponObject = null;
        [SerializeField, Tooltip("The object to show when the selected wieldable is a thrown weapon")]
        private GameObject m_ThrownWeaponObject = null;
        [SerializeField, Tooltip("The object to show when the selected wieldable is a wieldable tool")]
        private GameObject m_WielableToolObject = null;

        private IWieldable m_Wieldable = null;

        protected override void AttachToSelection(IQuickSlotItem wieldable)
        {
            m_Wieldable = wieldable.wieldable;
        }

        protected override void DetachFromSelection(IQuickSlotItem wieldable)
        {
            m_Wieldable = null;
        }

        protected override void ResetUI()
        {
            m_FirearmObject?.SetActive(m_Wieldable is IModularFirearm);
            m_MeleeWeaponObject?.SetActive(m_Wieldable is IMeleeWeapon);
            m_ThrownWeaponObject?.SetActive(m_Wieldable is IThrownWeapon);
            m_WielableToolObject?.SetActive(m_Wieldable is IWieldableTool);
        }
    }
}