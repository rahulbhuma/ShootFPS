using UnityEngine;
using UnityEngine.UI;
using NeoFPS;
using System;

namespace NeoFPS.AI
{
    [HelpURL("https://docs.neofps.com/manual/hudref-mb-hudenemyhealthbar.html")]
    public class HudEnemyHealthBar : HudEnemyHealthBarBase
    {

        [SerializeField, Tooltip("The text component that should display the hit target's name")]
        private Text m_DisplayNameText = null;
        [SerializeField, Tooltip("The rect transform of the filled bar")]
        private RectTransform m_BarRect = null;
        [SerializeField, Tooltip("The canvas group to fade out after enough time has elapsed")]
        private CanvasGroup m_CanvasGroup = null;

        protected override void SetDisplayName(string displayName)
        {
            if (m_DisplayNameText != null)
                m_DisplayNameText.text = displayName;
        }

        protected override void UpdateHealthbar()
        {
            m_BarRect.localScale = new Vector2(healthManager.health / healthManager.healthMax, 1f);
        }

        protected override void SetAlpha(float alpha)
        {
            m_CanvasGroup.alpha = alpha;
        }
    }
}
