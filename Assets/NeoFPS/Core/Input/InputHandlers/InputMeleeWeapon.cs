﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;

namespace NeoFPS
{
	[HelpURL("https://docs.neofps.com/manual/inputref-mb-inputmeleeweapon.html")]
	[RequireComponent (typeof (IMeleeWeapon))]
	public class InputMeleeWeapon : FpsInput
	{
		private IMeleeWeapon m_MeleeWeapon = null;
        private ICharacter m_Character = null;
        private AnimatedWeaponInspect m_Inspect = null;
        private bool m_IsPlayer = false;
		private bool m_IsAlive = false;

        public override FpsInputContext inputContext
        {
            get { return FpsInputContext.Character; }
        }

        protected override void OnAwake()
        {
            m_MeleeWeapon = GetComponent<IMeleeWeapon>();
            m_Inspect = GetComponentInChildren<AnimatedWeaponInspect>(true);
        }

        protected override void OnEnable()
        {
			// Get the wielding character
			m_Character = m_MeleeWeapon.wielder;
            if (m_Character != null)
            {
                // Attach event handlers
                m_Character.onControllerChanged += OnControllerChanged;
                m_Character.onIsAliveChanged += OnIsAliveChanged;
                OnControllerChanged(m_Character, m_Character.controller);
                OnIsAliveChanged(m_Character, m_Character.isAlive);
            }
            else
            {
                m_IsPlayer = false;
                m_IsAlive = false;
            }
		}

		protected override void OnDisable ()
		{
			base.OnDisable();

			if (m_Character != null)
			{
				m_Character.onControllerChanged -= OnControllerChanged;
				m_Character.onIsAliveChanged -= OnIsAliveChanged;
			}
			
            // Inspect
            if (m_Inspect != null)
                m_Inspect.inspecting = false;
		}

		void OnControllerChanged (ICharacter character, IController controller)
		{
			m_IsPlayer = (controller != null && controller.isPlayer);
			if (m_IsPlayer && m_IsAlive)
				PushContext();
			else
                PopContext();
		}

		void OnIsAliveChanged (ICharacter character, bool alive)
		{
			m_IsAlive = alive;
			if (m_IsPlayer && m_IsAlive)
				PushContext();
            else
                PopContext();
        }

        protected override void OnLoseFocus()
		{
			m_MeleeWeapon.PrimaryRelease();
			m_MeleeWeapon.SecondaryRelease();
        }

        protected override void UpdateInput()
		{
			if (m_Character != null && !m_Character.allowWeaponInput)
                return;

			if (GetButtonDown (FpsInputButton.PrimaryFire))
                m_MeleeWeapon.PrimaryPress();
			if (GetButtonUp(FpsInputButton.PrimaryFire))
				m_MeleeWeapon.PrimaryRelease();

			if (GetButtonDown(FpsInputButton.SecondaryFire))
				m_MeleeWeapon.SecondaryPress();
			if (GetButtonUp(FpsInputButton.SecondaryFire))
				m_MeleeWeapon.SecondaryRelease();

			// Inspect
			if (m_Inspect != null)
            {
                if (m_Inspect.toggle)
                {
                    if (GetButtonDown(FpsInputButton.Inspect))
                        m_Inspect.inspecting = !m_Inspect.inspecting;
                }
                else
                    m_Inspect.inspecting = GetButton(FpsInputButton.Inspect);
            }
        }
	}
}