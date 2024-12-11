using UnityEngine;
using UnityEngine.Events;
using NeoSaveGames.Serialization;
using NeoSaveGames;
using System.Collections;

namespace NeoFPS
{
    [RequireComponent (typeof(ICarrySystem))]
	[HelpURL("https://docs.neofps.com/manual/inputref-mb-inputcarryobject.html")]
    public class InputCarryObject : CharacterInputBase
	{
		[SerializeField, Min(0f), Tooltip("The minimum scroll wheel movement per frame for the scroll input to be registered")]
		private float m_ScrollThreshold = 0.1f;

        private ICarrySystem m_CarrySystem = null;
		private bool m_LockCamera = false;

		void BlockCameraInputs(bool blocked)
		{
			if (m_LockCamera == !blocked)
			{
				m_LockCamera = blocked;

				var aim = m_Character.aimController;
				if (m_LockCamera)
				{
					aim.SetPitchConstraints(aim.pitch, aim.pitch);
					aim.SetYawConstraints(aim.forward, 0f);
				}
				else
				{
					aim.ResetPitchConstraints();
					aim.ResetYawConstraints();
				}
			}
		}

		protected override void OnAwake()
        {
            base.OnAwake();

			m_CarrySystem = GetComponent<ICarrySystem>();
		}

		protected override void UpdateInput()
		{
			bool manipulating = false;

			switch (m_CarrySystem.carryState)
			{
				case CarryState.ValidTarget:
					{
						// Pick up
						if (GetButtonDown(FpsInputButton.PickUp))
							m_CarrySystem.PickUpObject();
					}
					break;
				case CarryState.Carrying:
					{
						// Manipulate
						if (GetButton(FpsInputButton.SecondaryFire))
						{
							manipulating = true;
							Vector2 m_MouseDelta = new Vector2(GetAxis(FpsInputAxis.MouseX), GetAxis(FpsInputAxis.MouseY));
							Vector2 m_Analogue = new Vector2(GetAxis(FpsInputAxis.LookX), GetAxis(FpsInputAxis.LookY));
							m_CarrySystem.ManipulateObject(m_MouseDelta, m_Analogue);
						}

						// Drop
						if (GetButtonDown(FpsInputButton.PickUp))
							m_CarrySystem.DropObject();

						// Throw
						if (GetButtonDown(FpsInputButton.PrimaryFire))
							m_CarrySystem.ThrowObject();
					}
					break;
			}

			// Move backwards / forwards
			float scroll = GetAxis(FpsInputAxis.MouseScroll);
			if (scroll > m_ScrollThreshold || scroll < -m_ScrollThreshold)
				m_CarrySystem.PushObject(scroll, 0);

			// Block camera input if manipulating the object
			BlockCameraInputs(manipulating);
		}
	}
}