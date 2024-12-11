using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/interactionref-mb-doorinteractiveobject.html")]
    public class DoorInteractiveObject : InteractiveObject
    {
		[SerializeField, Tooltip("The door to open (will accept any door that inherits from `DoorBase`).")]
        private DoorBase m_Door = null;

        public override void Interact(ICharacter character)
        {
            base.Interact(character);

            if (m_Door == null)
                return;

            switch(m_Door.state)
            {
                case DoorState.Closed:
                    m_Door.Open(m_Door.reversible && !m_Door.IsTransformInFrontOfDoor(character.transform));
                    break;
                case DoorState.Closing:
                    m_Door.Open(m_Door.normalisedOpen < -0.001f);
                    break;
                default:
                    m_Door.Close();
                    break;
            }
        }
    }
}