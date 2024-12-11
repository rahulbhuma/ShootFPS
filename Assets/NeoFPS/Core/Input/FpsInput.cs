using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;

namespace NeoFPS
{
	public abstract class FpsInput : FpsInputBase
	{
        protected override bool isInputActive
        {
			get { return (NeoFpsInputManager.instance is NeoFpsInputManager); }
		}

#if ENABLE_LEGACY_INPUT_MANAGER

		public float GetAxis (FpsInputAxis axis)
		{
			switch (axis)
			{
				case FpsInputAxis.MouseX:
					return NeoFpsInputManager.GetMouseX();
				case FpsInputAxis.MouseY:
					return NeoFpsInputManager.GetMouseY();
				case FpsInputAxis.MouseScroll:
					return Input.GetAxis (NeoFpsInputManager.mouseScrollAxis);
				default:
					return NeoFpsInputManager.gamepad.GetAxis (axis);
			}
		}

		public float GetAxisRaw (FpsInputAxis axis)
		{
			switch (axis)
			{
				case FpsInputAxis.MouseX:
					return NeoFpsInputManager.GetMouseXRaw();
				case FpsInputAxis.MouseY:
					return NeoFpsInputManager.GetMouseYRaw();
				case FpsInputAxis.MouseScroll:
					return Input.GetAxisRaw (NeoFpsInputManager.mouseScrollAxis);
				default:
					return NeoFpsInputManager.gamepad.GetAxisRaw (axis);
			}
		}

		public bool GetButton (FpsInputButton button)
		{
			if (FpsSettings.keyBindings.GetButton(button))
				return true;
			return NeoFpsInputManager.gamepad.GetButton(button);
		}

		public bool GetButtonDown (FpsInputButton button)
		{
			if (FpsSettings.keyBindings.GetButtonDown(button))
				return !NeoFpsInputManager.gamepad.GetButton(button);
			if (NeoFpsInputManager.gamepad.GetButtonDown(button))
				return !FpsSettings.keyBindings.GetButton(button);
			return false;
		}

		public bool GetButtonUp (FpsInputButton button)
		{
			if (FpsSettings.keyBindings.GetButtonUp(button))
				return !NeoFpsInputManager.gamepad.GetButton(button);
			if (NeoFpsInputManager.gamepad.GetButtonUp(button))
				return !FpsSettings.keyBindings.GetButton(button);
			return false;
		}

#else

		private bool m_Errored = false;

		void LogError()
        {
			if (!m_Errored)
			{
				Debug.LogError(string.Format("Old input manager component ({0}) is active on object {1} and should be replaced with its input system equivalent.", GetType().Name, gameObject.name), gameObject);
				m_Errored = true;
			}
		}

		public float GetAxis (FpsInputAxis axis)
		{
			LogError();
			return 0f;
		}

		public float GetAxisRaw (FpsInputAxis axis)
		{
			LogError();
			return 0f;
		}

		public bool GetButton (FpsInputButton button)
		{
			LogError();
			return false;
		}

		public bool GetButtonDown (FpsInputButton button)
		{
			LogError();
			return false;
		}

		public bool GetButtonUp (FpsInputButton button)
		{
			LogError();
			return false;
		}

#endif
	}
}
