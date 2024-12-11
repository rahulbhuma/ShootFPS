using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;
using NeoFPS.WieldableTools;

namespace NeoFPS
{
	[HelpURL("https://docs.neofps.com/manual/inputref-mb-inputwieldabletool.html")]
	[RequireComponent(typeof(IWieldableTool))]
	public class InputWieldableTool : FpsInput
	{
		private IWieldableTool m_Tool = null;
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
			m_Tool = GetComponent<IWieldableTool>();
            m_Inspect = GetComponentInChildren<AnimatedWeaponInspect>(true);
        }

		protected override void OnEnable()
		{
			// Attach event handlers
			m_Character = m_Tool.wielder;
			if (m_Character != null)
			{
				m_Character.onControllerChanged += OnControllerChanged;
				m_Character.onIsAliveChanged += OnIsAliveChanged;
				OnControllerChanged(m_Character, m_Character.controller);
				OnIsAliveChanged(m_Character, m_Character.isAlive);
			}
		}

		protected override void OnDisable()
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

		void OnControllerChanged(ICharacter character, IController controller)
		{
			m_IsPlayer = (controller != null && controller.isPlayer);
			if (m_IsPlayer && m_IsAlive)
				PushContext();
			else
				PopContext();
		}

		void OnIsAliveChanged(ICharacter character, bool alive)
		{
			m_IsAlive = alive;
			if (m_IsPlayer && m_IsAlive)
				PushContext();
			else
				PopContext();
		}

		protected override void OnLoseFocus()
		{
			m_Tool.PrimaryRelease();
            m_Tool.SecondaryRelease();
        }

		protected override void UpdateInput()
		{
			if (m_Character != null && !m_Character.allowWeaponInput)
				return;

			// Primary
			if (GetButtonDown(FpsInputButton.PrimaryFire))
				m_Tool.PrimaryPress();
			if (GetButtonUp(FpsInputButton.PrimaryFire))
				m_Tool.PrimaryRelease();

			// Secondary
			if (GetButtonDown(FpsInputButton.SecondaryFire))
				m_Tool.SecondaryPress();
			if (GetButtonUp(FpsInputButton.SecondaryFire))
				m_Tool.SecondaryRelease();

			// Interrupt
			if (GetButtonDown(FpsInputButton.Reload))
				m_Tool.Interrupt();

			// Flashlight
			if (GetButtonDown(FpsInputButton.Flashlight))
			{
				var flashlight = GetComponentInChildren<IWieldableFlashlight>(false);
				if (flashlight != null)
					flashlight.Toggle();
			}

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