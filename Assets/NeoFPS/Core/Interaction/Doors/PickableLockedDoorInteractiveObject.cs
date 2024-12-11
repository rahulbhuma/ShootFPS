using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;
using UnityEngine.Serialization;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/interactionref-mb-pickablelockeddoorinteractiveobject.html")]
    public class PickableLockedDoorInteractiveObject : InteractiveObject
    {
		[SerializeField, Tooltip("The door to open (will accept any door that inherits from `DoorBase`).")]
        private DoorBase m_Door = null;

        [SerializeField, HideInInspector]
        private string m_LockID = "demo_lock";

        [SerializeField, Tooltip("A range of IDs for this lock. If the player has any of these lock IDs in their inventory keyring then the lock can be unlocked. If no IDs are provided then the lock must be unlocked via events or the API.")]
        private string[] m_LockIds = { };

        [SerializeField, FpsInventoryKey, Tooltip("A range of inventory IDs for lockpicks in the character inventory. If you have one of these then you can pick the lock.")]
        private int[] m_LockpickItems = { FpsInventoryKey.Lockpick };

        [SerializeField, FormerlySerializedAs("m_LockpickID"), Tooltip("The ID for the lockpick UI you want to use. This allows for multiple lockpick minigame styles in a single scene.")]
        private string m_LockpickUI = "default";
		
        [SerializeField, Tooltip("The difficulty of this specific lock.")]
        private float m_LockpickDifficulty = 0.5f;

        [SerializeField, Tooltip("Does the character require a lockpick item in their inventory.")]
        private bool m_RequiresPickItem = true;

        [SerializeField, Tooltip("Should the door be locked on start.")]
        private bool m_StartLocked = true;

        [SerializeField, Tooltip("Should the door be opened when it's unlocked.")]
        private bool m_OpenOnUnlock = false;

        [SerializeField, Tooltip("The tooltip action (verb) to show when the door is locked.")]
        private string m_TooltipLockedAction = "Unlock";

        private string m_TooltipOpenAction = string.Empty;

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
            if (m_Door.isLocked)
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
                    // Check for lockpick item if required
                    IInventoryItem pickItem = null;
                    if (m_RequiresPickItem && inventory != null)
                    {
                        for(int i = 0;i< m_LockpickItems.Length; ++i)
                        {
                            var lockPickItem = inventory.GetItem(m_LockpickItems[i]);
                            if (lockPickItem != null && lockPickItem.quantity > 0)
                            {
                                pickItem = lockPickItem;
                                break;
                            }
                        }
                    }

                    // Open lockpick popup
                    if (pickItem != null && LockpickPopup.ShowLockpickPopup(m_LockpickUI, GetLockpickDifficulty(), character, pickItem, LockPickSuccess, LockPickCancelled))
                        open = false;
                }
            }

            if (open)
            {
                if (m_Door.state == DoorState.Closed || m_Door.state == DoorState.Closing)
                    m_Door.Open(m_Door.reversible && !m_Door.IsTransformInFrontOfDoor(character.motionController.localTransform));
                else
                    m_Door.Close();
            }
        }

        protected virtual float GetLockpickDifficulty()
        {
            return m_LockpickDifficulty;
        }

        void LockPickSuccess(ICharacter character)
        {
            m_Door.Unlock();
            tooltipAction = m_TooltipOpenAction;
        }

        void LockPickCancelled()
        {
        }
    }
}