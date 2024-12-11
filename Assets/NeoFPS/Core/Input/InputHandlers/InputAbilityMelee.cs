using UnityEngine;
using NeoFPS.CharacterMotion;
using NeoFPS.ModularFirearms;

namespace NeoFPS
{
	[HelpURL("https://docs.neofps.com/manual/inputref-mb-inputabilitymelee.html")]
	[RequireComponent (typeof (IMeleeWeapon))]
	public class InputAbilityMelee : FpsInput
    {
		private IMeleeWeapon m_MeleeWeapon = null;
        //private MonoBehaviour m_FirearmBehaviour = null;
        private bool m_IsPlayer = false;
		private bool m_IsAlive = false;
		private ICharacter m_Character = null;

        public override FpsInputContext inputContext
        {
            get { return FpsInputContext.Character; }
        }
		
		protected override void OnAwake()
		{
            m_MeleeWeapon = GetComponent<IMeleeWeapon>();
            //m_FirearmBehaviour = m_Firearm as MonoBehaviour;
		}

        protected override void OnEnable()
        {
			m_Character = m_MeleeWeapon.wielder;
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
				m_MeleeWeapon.PrimaryRelease();
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
            m_MeleeWeapon.PrimaryRelease();
        }

        protected override void UpdateInput()
		{
			if (m_MeleeWeapon == null || !m_MeleeWeapon.enabled)
				return;

			if (m_Character != null && !m_Character.allowWeaponInput)
				return;
			
            // Fire
            if (GetButtonDown(FpsInputButton.Ability))
                m_MeleeWeapon.PrimaryPress();
            if (GetButtonUp (FpsInputButton.Ability))
                m_MeleeWeapon.PrimaryRelease();
        }
	}
}