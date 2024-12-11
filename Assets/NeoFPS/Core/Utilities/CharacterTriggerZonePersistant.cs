using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/interactionref-mb-charactertriggerzonepersistant.html")]
	public class CharacterTriggerZonePersistant : MonoBehaviour
	{
		public event UnityAction<ICharacter> onTriggerEnter;
		public event UnityAction<ICharacter> onTriggerExit;
        public event UnityAction<ICharacter> onTriggerStay;

		private List<ICharacter> m_Characters = new List<ICharacter>();

        protected void OnTriggerEnter (Collider other)
		{
			if (other.CompareTag ("Player"))
			{
				ICharacter c = other.GetComponentInParent<ICharacter>();
				if (c != null)
				{
                    m_Characters.Add(c);
                    OnCharacterEntered(c);
                }
			}
		}

        protected void OnTriggerExit (Collider other)
		{
			if (other.CompareTag ("Player"))
			{
				ICharacter c = other.GetComponentInParent<ICharacter>();
				if (c != null)
				{
                    m_Characters.Remove(c);
                    OnCharacterExited(c);
				}
			}
		}

        protected void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                ICharacter c = other.GetComponentInParent<ICharacter>();
                if (c != null)
					OnCharacterStay(c);
            }
        }

        protected void OnDisable()
        {
			foreach (var c in m_Characters)
				OnCharacterExited(c);
			m_Characters.Clear();
        }

        protected virtual void OnCharacterEntered (ICharacter c)
		{
			if (onTriggerEnter != null)
				onTriggerEnter.Invoke (c);
		}

		protected virtual void OnCharacterExited (ICharacter c)
		{
			if (onTriggerExit != null)
				onTriggerExit.Invoke (c);
		}

		protected virtual void OnCharacterStay (ICharacter c)
        {
            if (onTriggerStay != null)
                onTriggerStay.Invoke(c);
        }
    }
}