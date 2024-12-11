using UnityEngine;
using NeoFPS.CharacterMotion;
using NeoFPS.WieldableTools;

namespace NeoFPS
{
	[HelpURL("https://docs.neofps.com/manual/inputref-mb-inputabilitywieldabletool.html")]
	[RequireComponent (typeof (IWieldableTool))]
	public class InputAbilityWieldableTool : FpsInput
    {
		private IWieldableTool m_WieldableTool = null;
        private bool m_IsPlayer = false;
		private bool m_IsAlive = false;
		private ICharacter m_Character = null;

        public override FpsInputContext inputContext
        {
            get { return FpsInputContext.Character; }
        }
		
		protected override void OnAwake()
		{
            m_WieldableTool = GetComponent<IWieldableTool>();
		}

        protected override void OnEnable()
        {
			m_Character = m_WieldableTool.wielder;
			if (m_Character != null && m_Character.motionController != null)
			{
				m_Character.onControllerChanged += OnControllerChanged;
				m_Character.onIsAliveChanged += OnIsAliveChanged;
				OnControllerChanged (m_Character, m_Character.controller);
				OnIsAliveChanged (m_Character, m_Character.isAlive);
			}
			else
			{
				m_IsPlayer = false;
				m_IsAlive = false;
			}
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
            {
                PopContext();
				m_WieldableTool.PrimaryRelease();
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

			m_IsPlayer = false;
			m_IsAlive = false;
		}

        protected override void OnLoseFocus()
        {
            m_WieldableTool.PrimaryRelease();
        }

        protected override void UpdateInput()
		{
			if (m_WieldableTool == null || !m_WieldableTool.enabled)
				return;

			if (m_Character != null && !m_Character.allowWeaponInput)
				return;
			
            // Fire
            if (GetButtonDown(FpsInputButton.Ability))
                m_WieldableTool.PrimaryPress();
			if (GetButtonUp (FpsInputButton.Ability))
                m_WieldableTool.PrimaryRelease();
        }
	}
}