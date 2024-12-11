using NeoFPS.Constants;
using NeoFPS.SinglePlayer;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/interactionref-mb-lockedtriggerzone.html")]
	public class LockedTriggerZone : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        private string m_LockID = "demo_lock";

        [SerializeField, Tooltip("A range of IDs for this lock. If the player has any of these lock IDs in their inventory keyring then the lock can be unlocked. If no IDs are provided then the lock must be unlocked via events or the API.")]
        private string[] m_LockIds = { };

        [SerializeField, Tooltip("The event that is fired when a character enters the trigger collider.")]
		private CharacterEvent m_OnTriggerEnter = new CharacterEvent();

		[SerializeField, Tooltip("The event that is fired when a character exits the trigger collider.")]
		private CharacterEvent m_OnTriggerExit = new CharacterEvent();

		[Serializable]
		public class CharacterEvent : UnityEvent<BaseCharacter> { }

		private BaseCharacter m_Character = null;

        protected virtual void OnValidate()
        {
            // Check if using old single ID setup
            if (!string.IsNullOrEmpty(m_LockID))
            {
                // Copy IDs over to new array 1 longer
                var temp = m_LockIds;
                m_LockIds = new string[temp.Length + 1];
                for (int i = 0; i < temp.Length; ++i)
                    m_LockIds[i] = temp[i];

                // Add old ID
                m_LockIds[temp.Length] = m_LockID;

                // Clear old ID
                m_LockID = string.Empty;
            }
        }

        protected void OnTriggerEnter(Collider other)
		{
			if (other.CompareTag("Player"))
			{
				BaseCharacter c = other.GetComponentInParent<BaseCharacter>();
				if (c != null)
				{
					var inventory = c.GetComponent<IInventory>();
					if (inventory != null)
					{
						var keyRing = inventory.GetItem(FpsInventoryKey.KeyRing) as IKeyRing;
                        if (keyRing != null)
                        {
                            for (int i = 0; i < m_LockIds.Length; ++i)
                            {
                                if (keyRing.ContainsKey(m_LockIds[i]))
                                {
                                    m_Character = c;
                                    OnCharacterEntered(c);
                                    break;
                                }
                            }
                        }
					}
				}
			}
		}

        protected void OnTriggerExit(Collider other)
		{
			if (other.CompareTag("Player"))
			{
				BaseCharacter c = other.GetComponentInParent<BaseCharacter>();
				if (c != null && c == m_Character)
					OnCharacterExited(c);
			}
		}

		protected virtual void OnCharacterEntered(BaseCharacter c)
		{
			if (m_OnTriggerEnter != null)
				m_OnTriggerEnter.Invoke(c);
		}

		protected virtual void OnCharacterExited(BaseCharacter c)
		{
			if (m_OnTriggerExit != null)
				m_OnTriggerExit.Invoke(c);
		}
	}
}