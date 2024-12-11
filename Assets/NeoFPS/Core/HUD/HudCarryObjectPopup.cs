using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeoFPS.SinglePlayer;
using System;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/hudref-mb-hudcarryobjectpopup.html")]
    public class HudCarryObjectPopup : PlayerCharacterHudBase
    {
        [SerializeField, Tooltip("The carry state to show this popup for.")]
        private CarryState m_CarryState = CarryState.Carrying;

        private ICarrySystem m_CarrySystem = null;

        public override void OnPlayerCharacterChanged(ICharacter character)
        {
            if (m_CarrySystem != null)
            {
                m_CarrySystem.onCarryStateChanged -= OnCarryStateChanged;
                m_CarrySystem = null;
            }

            if (character != null)
                m_CarrySystem = character.GetComponent<ICarrySystem>();

            if (m_CarrySystem != null)
            {
                m_CarrySystem.onCarryStateChanged += OnCarryStateChanged;
                OnCarryStateChanged(m_CarrySystem.carryState);
            }
            else
                gameObject.SetActive(false);
        }

        private void OnCarryStateChanged(CarryState carryState)
        {
            gameObject.SetActive(carryState == m_CarryState);
        }
    }
}