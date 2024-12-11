using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeoFPS.SinglePlayer;
using System;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/hudref-mb-hudinteractiontooltip.html")]
    public class HudInteractionTooltip : PlayerCharacterHudBase
    {
        [SerializeField, Tooltip("The UI text element to show the highlighted object's name.")]
        private Text m_NameText = null;

        [SerializeField, Tooltip("The UI text element to show the input action required (press or hold).")]
        private Text m_InputActionText = null;

        [SerializeField, Tooltip("The UI text element to show the interaction result (eg pick up).")]
        private Text m_InteractionText = null;

        [SerializeField, Tooltip("The verb to use for holding the use button.")]
        private string m_HoldOption = "Hold";

        [SerializeField, Tooltip("The verb to use for pressing/tapping the use button.")]
        private string m_PressOption = "Press";

        [SerializeField, Tooltip("The verb to use for the action when that's performed when using the item.")]
        private string m_DefaultAction = "interact";

        private CharacterInteractionHandler m_Interact = null;
        private IInteractiveObject m_Highlighted = null;
        
        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Unsubscribe from character
            if (m_Interact != null)
            {
                m_Interact.onHighlightedChanged -= OnHighlightedChanged;
                m_Interact = null;
            }
        }

        public override void OnPlayerCharacterChanged(ICharacter character)
        {
            if (m_Interact != null)
                m_Interact.onHighlightedChanged -= OnHighlightedChanged;

            if (character as Component == null)
                m_Interact = null;
            else
                m_Interact = character.GetComponent<CharacterInteractionHandler>();

            if (m_Interact != null)
            {
                m_Interact.onHighlightedChanged += OnHighlightedChanged;
                OnHighlightedChanged(character, m_Interact.highlighted);
            }

            gameObject.SetActive(false);
        }

        private void OnHighlightedChanged(ICharacter character, IInteractiveObject highlighted)
        {
            // Detach event handler
            if (m_Highlighted != null)
                m_Highlighted.onTooltipChanged -= OnTooltipChanged;

            // Assign
            m_Highlighted = highlighted;
            if (m_Highlighted == null)
            {
                // None highlighted, hide tooltip
                gameObject.SetActive(false);
            }
            else
            {
                // Reset tooltip
                m_Highlighted.onTooltipChanged += OnTooltipChanged;
                OnTooltipChanged();
            }
        }

        void OnTooltipChanged()
        {
            // Check if tooltip should be hidden
            if ( m_Highlighted.tooltipName == string.Empty)
            {
                gameObject.SetActive(false);
            }
            else
            {
                // Show
                gameObject.SetActive(true);
                
                // Apply label
                m_NameText.text = m_Highlighted.tooltipName;

                // Get input action
                m_InputActionText.text = m_Highlighted.holdDuration > 0f ? m_HoldOption : m_PressOption;

                // Get resulting interaction
                if (!string.IsNullOrEmpty(m_Highlighted.tooltipAction))
                    m_InteractionText.text = m_Highlighted.tooltipAction;
                else
                    m_InteractionText.text = m_DefaultAction;
            }
        }
    }
}