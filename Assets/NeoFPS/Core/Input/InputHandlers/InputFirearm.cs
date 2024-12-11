using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using NeoFPS.ModularFirearms;
using System;

namespace NeoFPS
{
	[HelpURL("https://docs.neofps.com/manual/inputref-mb-inputfirearm.html")]
	[RequireComponent (typeof (IModularFirearm))]
	public class InputFirearm : FpsInput
    {
		[SerializeField, Tooltip("The key for a switch property on the character's motion graph to set when aiming. This allows slowing movement, preventing jumping, etc.")]
        private string m_AimingKey = "aiming";
        [SerializeField, Tooltip("The key for a switch property on the character's motion graph to read which prevents the character from aiming down sights (eg when falling or sprinting).")]
        private string m_BlockAimKey = "blockAim";

        protected IModularFirearm m_Firearm = null;
		protected MonoBehaviour m_FirearmBehaviour = null;
		protected bool m_IsPlayer = false;
		protected bool m_IsAlive = false;
		protected int m_AimingKeyHash = -1;
        protected int m_BlockAimKeyHash = -1;
        protected ICharacter m_Character = null;
		protected AnimatedWeaponInspect m_Inspect = null;
		protected SwitchParameter m_AimingProperty = null;
        protected SwitchParameter m_BlockAimProperty = null;
        protected bool m_EnabledFiring = false;

        public override FpsInputContext inputContext
        {
            get { return FpsInputContext.Character; }
        }
		
		protected override void OnAwake()
		{
			m_Firearm = GetComponent<IModularFirearm>();
            m_Inspect = GetComponentInChildren<AnimatedWeaponInspect>(true);
            m_FirearmBehaviour = m_Firearm as MonoBehaviour;
            if (!string.IsNullOrWhiteSpace(m_AimingKey))
                m_AimingKeyHash = Animator.StringToHash(m_AimingKey);
            if (!string.IsNullOrWhiteSpace(m_BlockAimKey))
                m_BlockAimKeyHash = Animator.StringToHash(m_BlockAimKey);
        }

        protected override void OnEnable()
        {
            m_Character = m_Firearm.wielder;
			if (m_Character != null)
			{
				if (m_Character.motionController != null)
				{
					MotionGraphContainer motionGraph = m_Character.motionController.motionGraph;
					if (m_AimingKeyHash != -1)
						m_AimingProperty = motionGraph.GetSwitchProperty(m_AimingKeyHash);
                    if (m_BlockAimKeyHash != -1)
                        m_BlockAimProperty = motionGraph.GetSwitchProperty(m_BlockAimKeyHash);
                }
				else
				{
					m_AimingProperty = null;
					m_BlockAimProperty = null;
                }

				m_Character.onControllerChanged += OnControllerChanged;
				m_Character.onIsAliveChanged += OnIsAliveChanged;
				OnControllerChanged(m_Character, m_Character.controller);
				OnIsAliveChanged(m_Character, m_Character.isAlive);
			}
			else
			{
				m_IsPlayer = false;
				m_IsAlive = false;
				m_AimingProperty = null;
				m_BlockAimProperty = null;
            }

			m_EnabledFiring = isInputActive && hasFocus && GetButton(FpsInputButton.PrimaryFire);

			FpsSettings.keyBindings.onRebind += OnRebindKeys;
		}

        protected override void OnDisable()
		{
			base.OnDisable();

			if (m_Character != null)
			{
				m_Character.onControllerChanged -= OnControllerChanged;
				m_Character.onIsAliveChanged -= OnIsAliveChanged;
			}
			m_IsPlayer = false;
			m_IsAlive = false;
			m_AimingProperty = null;

			FpsSettings.keyBindings.onRebind -= OnRebindKeys;
			
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
            {
                PopContext();
				if (m_Firearm.trigger != null)
					m_Firearm.trigger.Release();
                if (m_AimingProperty != null)
                    m_AimingProperty.on = false;
            }
		}

        protected override void OnLoseFocus()
        {
            base.OnLoseFocus();
			m_Firearm.trigger.Release();
			m_Firearm.aimToggleHold.Hold(false);
        }

        void OnRebindKeys(FpsInputButton button, bool primary, KeyCode to)
        {
			if (button == FpsInputButton.AimToggle || button == FpsInputButton.Aim)
				m_Firearm.aimToggleHold.on = false;
		}

        protected override void UpdateInput()
		{
			if (m_Firearm == null || !m_FirearmBehaviour.enabled)
				return;

			if (m_Character != null && !m_Character.allowWeaponInput)
				return;
			
            // Fire
            if (GetButtonDown(FpsInputButton.PrimaryFire) || (m_EnabledFiring && GetButton(FpsInputButton.PrimaryFire)))
            {
				if (m_Firearm.trigger.blocked && m_Firearm.reloader.interruptable)
					m_Firearm.reloader.Interrupt();
				else
					m_Firearm.trigger.Press();
            }
			if (GetButtonUp (FpsInputButton.PrimaryFire))
				m_Firearm.trigger.Release();
			if (GetButtonDown (FpsInputButton.SwitchWeaponModes))
				m_Firearm.SwitchMode();

            // Reload
            if (GetButtonDown(FpsInputButton.Reload))
            {
                if (m_Firearm.trigger.cancelOnReload)
                    m_Firearm.trigger.Cancel();
                else
                    m_Firearm.Reload();
            }

			// Aim
			if (m_BlockAimProperty == null || !m_BlockAimProperty.on)
			{
				m_Firearm.aimToggleHold.SetInput(
					GetButtonDown(FpsInputButton.AimToggle),
					GetButton(FpsInputButton.Aim)
					);
			}
			else
				m_Firearm.aimToggleHold.on = false;

            if (m_AimingProperty != null)
                m_AimingProperty.on = m_Firearm.aimToggleHold.on;

			// Flashlight
			if (GetButtonDown(FpsInputButton.Flashlight))
            {
				var flashlight = GetComponentInChildren<IWieldableFlashlight>(false);
				if (flashlight != null)
					flashlight.Toggle();
            }

			// Optics
			if (GetButtonDown(FpsInputButton.OpticsLightPlus))
			{
				var optics = GetComponentInChildren<IOpticsBrightnessControl>(false);
				if (optics != null)
					optics.IncrementBrightness();
			}
            if (GetButtonDown(FpsInputButton.OpticsLightMinus))
            {
                var optics = GetComponentInChildren<IOpticsBrightnessControl>(false);
                if (optics != null)
                    optics.DecrementBrightness();
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

			m_EnabledFiring = false;

			AdditionalFirearmInput();
		}

		protected virtual void AdditionalFirearmInput()
		{ }
	}
}