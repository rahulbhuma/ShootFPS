using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;
using NeoFPS.Samples;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/interactionref-mb-keypadinteractiveobject.html")]
    public class KeypadInteractiveObject : InteractiveObject
    {
		[SerializeField, Tooltip("The door to open (will accept any door that inherits from `DoorBase`).")]
        private DoorBase m_Door = null;

        [SerializeField, Tooltip("The keypad popup to show.")]
        private KeypadPopup m_KeypadPopup = null;

        [SerializeField, Tooltip("The passcode of the door.")]
        private int[] m_PassCode = { 4, 5, 1 };

        [SerializeField, HideInInspector]
        private string m_LockID = "demo_lock";

        [SerializeField, Tooltip("A range of IDs for this lock. If the player knows any of these lock codes then the digits will be shown with the popup")]
        private string[] m_LockIds = { };

        [SerializeField, Tooltip("Should the door be locked on start.")]
        private bool m_StartLocked = true;

        protected override void OnValidate()
        {
            base.OnValidate();

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

        protected override void Awake()
        {
            base.Awake();

            if (m_Door == null)
            {
                interactable = false;
            }
            else
            {
                m_Door.onIsLockedChanged += OnDoorIsLockedChanged;

                if (m_StartLocked)
                    m_Door.LockSilent();

                OnDoorIsLockedChanged();
            }
        }

        protected void OnDestroy()
        {
            if (m_Door != null)
                m_Door.onIsLockedChanged -= OnDoorIsLockedChanged;
        }

        void OnDoorIsLockedChanged()
        {
            interactable = m_Door.isLocked;
        }

        void UnlockDoor()
        {
            m_Door.Unlock();
        }

        public override void Interact(ICharacter character)
        {
            base.Interact(character);
            
            if (m_Door.isLocked)
            {
                // Check if the keycode is already known
                bool known = false;
                var inventory = character.GetComponent<IInventory>();
                if (inventory != null)
                {
                    var keyring = inventory.GetItem(FpsInventoryKey.KeyRing) as KeyRing;
                    if (keyring != null)
                    {
                        for (int i = 0; i < m_LockIds.Length; ++i)
                            known |= keyring.ContainsKey(m_LockIds[i]);
                    }
                }

                // Show the popup
                var popup = PrefabPopupContainer.ShowPrefabPopup(m_KeypadPopup);
                popup.Initialise(m_PassCode, UnlockDoor, null, known);
            }
        }
    }
}