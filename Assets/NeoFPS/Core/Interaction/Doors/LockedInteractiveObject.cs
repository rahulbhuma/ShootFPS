using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;
using NeoSaveGames.Serialization;
using UnityEngine.Events;
using NeoSaveGames;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/interactionref-mb-lockedinteractiveobject.html")]
    public class LockedInteractiveObject : MonoBehaviour, IInteractiveObject
    {
        [SerializeField, HideInInspector]
        private string m_LockID = "demo_lock";

        [SerializeField, Tooltip("A range of IDs for this lock. If the player has any of these lock IDs in their inventory keyring then the lock can be unlocked. If no IDs are provided then the lock must be unlocked via events or the API.")]
        private string[] m_LockIds = { };

        [SerializeField, Tooltip("The name of the item in the HUD tooltip.")]
        private string m_TooltipName = string.Empty;

        [SerializeField, Tooltip("A description of the action for use in the HUD tooltip, eg pick up.")]
        private string m_TooltipAction = string.Empty;

        [SerializeField, Tooltip("Can the object be interacted with immediately.")]
        private bool m_InteractableOnStart = true;

        [SerializeField, Tooltip("An event that is triggered when the object is used.")]
        private UnityEvent m_OnUsed = new UnityEvent();

        [SerializeField, Tooltip("An event that is triggered when the object is used while locked (does not fire when unlocking).")]
        private UnityEvent m_OnUsedWhileLocked = new UnityEvent();

        [SerializeField, Tooltip("An event that is triggered when the player looks directly at the object.")]
        private UnityEvent m_OnCursorEnter = new UnityEvent();

        [SerializeField, Tooltip("An event that is triggered when the player looks away from the object.")]
        private UnityEvent m_OnCursorExit = new UnityEvent();

        private static readonly NeoSerializationKey k_InteractableKey = new NeoSerializationKey("interactable");

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

        public string tooltipAction
        {
            get { return m_TooltipAction; }
            protected set
            {
                m_TooltipAction = value;
                if (onTooltipChanged != null)
                    onTooltipChanged();
            }
        }

        public event UnityAction<ICharacter> onUsed;
        public event UnityAction<ICharacter> onUsedWhileLocked;

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
        public UnityEvent onUsedWhileLockedUnityEvent
        {
            get { return m_OnUsedWhileLocked; }
        }
        public UnityEvent onCursorEnterUnityEvent
        {
            get { return m_OnCursorEnter; }
        }
        public UnityEvent onCursorExitUnityEvent
        {
            get { return m_OnCursorExit; }
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

        protected virtual void Awake()
        {
            m_Collider = GetComponent<Collider>();
            if (m_Collider != null)
                m_Collider.enabled = m_Interactable;
        }

        protected virtual void Start()
        { 
            // Check if lock IDs are valid
            bool canUnlock = false;
            for (int i = 0; i < m_LockIds.Length; ++i)
            {
                if (!string.IsNullOrWhiteSpace(m_LockIds[i]))
                {
                    canUnlock = true;
                    break;
                }
            }

            if (canUnlock)
                interactable = m_InteractableOnStart;
            else
            {
                Debug.LogWarning("LockedInteractiveObject has invalid lock ID. Disabling interaction." + gameObject);
                interactable = false;
            }
        }

        protected virtual void OnHighlightedChanged(bool h)
        {
            if (h)
                m_OnCursorEnter.Invoke();
            else
                m_OnCursorExit.Invoke();
        }

        public bool HasKey(ICharacter character)
        {
            var inventory = character.GetComponent<IInventory>();
            if (inventory != null)
            {
                var keyRing = inventory.GetItem(FpsInventoryKey.KeyRing) as IKeyRing;
                if (keyRing != null)
                {
                    for (int i = 0; i < m_LockIds.Length; ++i)
                    {
                        if (keyRing.ContainsKey(m_LockIds[i]))
                            return true;
                    }
                }
            }

            return false;
        }

        public virtual void Interact(ICharacter character)
        {
            if (HasKey(character))
                InteractSuccess(character);
            else
                InteractLocked(character);
        }

        public virtual void InteractSuccess(ICharacter character)
        {
            m_OnUsed.Invoke();
            onUsed?.Invoke(character);
        }

        public virtual void InteractLocked(ICharacter character)
        {
            m_OnUsedWhileLocked.Invoke();
            onUsedWhileLocked?.Invoke(character);
        }

        public virtual void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValue(k_InteractableKey, interactable);
        }

        public virtual void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            bool result = true;
            if (reader.TryReadValue(k_InteractableKey, out result, true))
                interactable = result;
        }
    }
}