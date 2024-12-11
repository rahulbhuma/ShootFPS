using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;
using UnityEngine.Events;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/interactionref-mb-lockeddoorinteractiveobject.html")]
    public class LockedDoorInteractiveObject : InteractiveObject
    {
		[SerializeField, Tooltip("The door to open (will accept any door that inherits from `DoorBase`).")]
        private DoorBase m_Door = null;
        
        [SerializeField, HideInInspector]
        private string m_LockID = "demo_lock";

        [SerializeField, Tooltip("A range of IDs for this lock. If the player has any of these lock IDs in their inventory keyring then the door can be unlocked. If no IDs are provided then the door must be unlocked via events or the API.")]
        private string[] m_LockIds = { };

        [SerializeField, Tooltip("Should the door be locked on start.")]
        private bool m_StartLocked = true;

        [SerializeField, Tooltip("Should the door be opened when it's unlocked.")]
        private bool m_OpenOnUnlock = false;

        [SerializeField, Tooltip("The tooltip action to use when the door is locked. Use the open action toolrip for the other tooltip action.")]
        private string m_TooltipLockedAction = "Unlock";

        [SerializeField, Tooltip("An event that is triggered when the object is used while locked (does not fire when unlocking).")]
        private UnityEvent m_OnUsedWhileLocked = new UnityEvent();

        public event UnityAction<ICharacter> onUsedWhileLocked;
        private string m_TooltipOpenAction = string.Empty;
        private bool m_CanUnlock = true;

        public UnityEvent onUsedWhileLockedUnityEvent
        {
            get { return m_OnUsedWhileLocked; }
        }

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

                m_CanUnlock = false;
                for (int i = 0; i < m_LockIds.Length; ++i)
                {
                    if (!string.IsNullOrWhiteSpace(m_LockIds[i]))
                    {
                        m_CanUnlock = true;
                        break;
                    }
                }

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
            if (m_Door.isLocked && m_CanUnlock)
            {
                m_TooltipOpenAction = tooltipAction;
                tooltipAction = m_TooltipLockedAction;
            }
            else
            {
                tooltipAction = m_TooltipOpenAction;
            }
        }

        public override void Interact(ICharacter character)
        {
            base.Interact(character);

            bool open = true;

            if (m_Door.isLocked)
            {
                if (m_CanUnlock)
                {
                    bool unlock = false;

                    var inventory = character.GetComponent<IInventory>();
                    if (inventory != null)
                    {
                        var keyRing = inventory.GetItem(FpsInventoryKey.KeyRing) as IKeyRing;
                        if (keyRing != null)
                        {
                            for (int i = 0; i < m_LockIds.Length; ++i)
                            {
                                if (keyRing.ContainsKey(m_LockIds[i]))
                                {
                                    unlock = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (unlock)
                    {
                        m_Door.Unlock();

                        tooltipAction = m_TooltipOpenAction;

                        if (!m_OpenOnUnlock)
                            open = false;
                    }
                    else
                    {
                        m_OnUsedWhileLocked.Invoke();
                        onUsedWhileLocked?.Invoke(character);
                    }
                }
                else
                {
                    m_OnUsedWhileLocked.Invoke();
                    onUsedWhileLocked?.Invoke(character);
                }
            }

            if (open)
            {
                switch (m_Door.state)
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
}