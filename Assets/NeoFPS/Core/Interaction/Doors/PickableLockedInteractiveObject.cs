using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;
using NeoSaveGames.Serialization;
using UnityEngine.Events;
using NeoSaveGames;
using UnityEngine.Serialization;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/interactionref-mb-pickablelockedinteractiveobject.html")]
    public class PickableLockedInteractiveObject : MonoBehaviour, IInteractiveObject
    {
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

        [SerializeField, Tooltip("The name of the item in the HUD tooltip.")]
        private string m_TooltipName = string.Empty;

        [SerializeField, Tooltip("A description of the action for use in the HUD tooltip, eg pick up.")]
        private string m_TooltipAction = string.Empty;

        [SerializeField, Tooltip("The tooltip action (verb) to show when the object is locked.")]
        private string m_TooltipLockedAction = "Unlock";

        [SerializeField, Tooltip("Should the object be locked on start.")]
        private bool m_StartLocked = true;

        [SerializeField, Tooltip("Can the object be interacted with immediately.")]
        private bool m_InteractableOnStart = true;

        [SerializeField, Tooltip("Should the object be interacted with immediately when it's unlocked.")]
        private bool m_InteractOnUnlock = false;

        [SerializeField, Tooltip("An event that is triggered when the object is used.")]
        private UnityEvent m_OnUsed = new UnityEvent();

        [SerializeField, Tooltip("An event that is triggered when the player looks directly at the object.")]
        private UnityEvent m_OnCursorEnter = new UnityEvent();

        [SerializeField, Tooltip("An event that is triggered when the player looks away from the object.")]
        private UnityEvent m_OnCursorExit = new UnityEvent();

        private static readonly NeoSerializationKey k_InteractableKey = new NeoSerializationKey("interactable");
        private static readonly NeoSerializationKey k_LockedKey = new NeoSerializationKey("locked");

        private Collider m_Collider = null;

        public event UnityAction onTooltipChanged;

        public string tooltipName
        {
            get { return m_TooltipName; }
            protected set
            {
                m_TooltipName = value;
                if (onTooltipChanged != null)
                    onTooltipChanged();
            }
        }

        private string m_CurrentTooltipAction = string.Empty;
        public string tooltipAction
        {
            get
            {
                return m_CurrentTooltipAction;
            }
            protected set
            {
                m_CurrentTooltipAction = value;
                if (onTooltipChanged != null)
                    onTooltipChanged();
            }
        }

        public event UnityAction<ICharacter> onUsed;

        public event UnityAction onCursorEnter
        {
            add { m_OnCursorEnter.AddListener(value); }
            remove { m_OnCursorEnter.RemoveListener(value); }
        }
        public event UnityAction onCursorExit
        {
            add { m_OnCursorExit.AddListener(value); }
            remove { m_OnCursorExit.RemoveListener(value); }
        }

        public UnityEvent onUsedUnityEvent
        {
            get { return m_OnUsed; }
        }
        public UnityEvent onCursorEnterUnityEvent
        {
            get { return m_OnCursorEnter; }
        }
        public UnityEvent onCursorExitUnityEvent
        {
            get { return m_OnCursorExit; }
        }

        private bool m_Locked = false;
        public bool locked
        {
            get { return m_Locked; }
            private set
            {
                m_Locked = value;
                m_CurrentTooltipAction = (m_Locked) ? m_TooltipLockedAction : m_TooltipAction;
            }
        }

        private bool m_Highlighted = false;
        public bool highlighted
        {
            get { return m_Highlighted; }
            set
            {
                if (m_Highlighted != value)
                {
                    m_Highlighted = value;
                    OnHighlightedChanged(value);
                }
            }
        }

        private bool m_Interactable = false;
        public bool interactable
        {
            get { return m_Interactable; }
            set
            {
                m_Interactable = value;
                if (m_Collider != null)
                    m_Collider.enabled = value;
            }
        }

        public float holdDuration
        {
            get { return 0f; }
        }

        protected virtual void Awake()
        {
            m_Collider = GetComponent<Collider>();
            if (m_Collider != null)
                m_Collider.enabled = m_Interactable;
        }

        protected virtual void Start()
        {
            locked = m_StartLocked;
            interactable = m_InteractableOnStart;
        }

        protected virtual void OnHighlightedChanged(bool h)
        {
            if (h)
                m_OnCursorEnter.Invoke();
            else
                m_OnCursorExit.Invoke();
        }

        public virtual void Interact(ICharacter character)
        {
            if (locked)
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
                    locked = false;

                    tooltipAction = m_TooltipAction;

                    if (m_InteractOnUnlock)
                        OnUsed(character);
                }
                else
                {
                    // Check for lockpick item if required
                    IInventoryItem pickItem = null;
                    if (m_RequiresPickItem && inventory != null)
                    {
                        for (int i = 0; i < m_LockpickItems.Length; ++i)
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
                    if (pickItem != null)
                        LockpickPopup.ShowLockpickPopup(m_LockpickUI, GetLockpickDifficulty(), character, pickItem, LockPickSuccess, null);
                }
            }
            else
            {
                OnUsed(character);
            }
        }

        protected virtual float GetLockpickDifficulty()
        {
            return m_LockpickDifficulty;
        }

        void LockPickSuccess(ICharacter character)
        {
            locked = false;

            if (m_InteractOnUnlock)
                OnUsed(character);
        }

        protected virtual void OnUsed(ICharacter character)
        {
            m_OnUsed.Invoke();
            onUsed?.Invoke(character);
        }

        public virtual void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValue(k_InteractableKey, interactable);
            writer.WriteValue(k_LockedKey, locked);
        }

        public virtual void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            bool result = true;
            if (reader.TryReadValue(k_InteractableKey, out result, true))
                interactable = result;
            if (reader.TryReadValue(k_LockedKey, out result, m_StartLocked))
                locked = result;
        }
    }
}