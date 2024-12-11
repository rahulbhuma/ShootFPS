using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using NeoFPS.Constants;
using UnityEngine.Events;
using NeoSaveGames.Serialization;
using NeoSaveGames;

namespace NeoFPS
{
	/// <summary>
	/// The base class for all player inventory behaviours
	/// </summary>
	/// <seealso cref="IInventory"/>
	/// <seealso cref="IQuickSlots"/>
	public abstract class FpsInventoryBase : MonoBehaviour, IInventory, IQuickSlots, INeoSerializableComponent
	{
		protected virtual void Awake ()
		{
            if (m_WieldableRoot != null)
            {
                m_WieldableRoot.localScale = new Vector3(m_WieldableRootScale, m_WieldableRootScale, m_WieldableRootScale);
                wieldableRootNsgo = m_WieldableRoot.GetComponent<NeoSerializedGameObject>();
            }
        }

        protected virtual void Start()
        {
            InitialiseContents();
        }

        #if UNITY_EDITOR
        protected virtual void OnValidate ()
		{
			if (m_DropTransform == null && m_WieldableRoot != null)
				m_DropTransform = m_WieldableRoot;

            if (m_StartingOrder.Length != m_SlotCount)
            {
                int[] newStartingOrder = new int[m_SlotCount];

                if (m_SlotCount > m_StartingOrder.Length)
                {
                    // Copy old values
                    int i = 0;
                    for (; i < m_StartingOrder.Length; ++i)
                        newStartingOrder[i] = m_StartingOrder[i];
                    // Set remaining values
                    for (; i < newStartingOrder.Length; ++i)
                        newStartingOrder[i] = i;
                }
                else
                {
                    // Pool available indices
                    List<int> unClaimed = new List<int>(m_SlotCount);
                    for (int i = 0; i < m_SlotCount; ++i)
                        unClaimed.Add(i);
                    // Store clashes
                    List<int> clashes = new List<int>(m_SlotCount);
                    // Iterate through and check/copy
                    for (int i = 0; i < m_SlotCount; ++i)
                    {
                        // Check if the old entry is valid
                        if (m_StartingOrder[i] < m_SlotCount && unClaimed.Contains(m_StartingOrder[i]))
                        {
                            // Apply and remove from unclaimed
                            newStartingOrder[i] = m_StartingOrder[i];
                            unClaimed.Remove(m_StartingOrder[i]);
                        }
                        else
                        {
                            // Store the clashing index
                            clashes.Add(i);
                        }
                    }
                    // Resolve the clashes using the unclaimed values
                    for (int i = 0; i < clashes.Count; ++i)
                    {
                        newStartingOrder[clashes[i]] = unClaimed[0];
                        unClaimed.RemoveAt(0);
                    }
                }

                m_StartingOrder = newStartingOrder;
            }
        }
		#endif

		#region IInventory implementation

		[SerializeField, NeoObjectInHierarchyField(true, required = true), Tooltip("The transform to set as the parent of any objects added to the inventory.")]
		private Transform m_WieldableRoot = null;

        [SerializeField, Range(0.1f, 1f), Tooltip("A scale value for the wieldable root and any child items. Used to prevent weapons clipping into the scenery.")]
        private float m_WieldableRootScale = 0.5f;

		[SerializeField, NeoObjectInHierarchyField(true), Tooltip("A proxy transform used to set the drop position and rotation when a wieldable item is dropped.")]
        private Transform m_DropTransform = null;

        [SerializeField, Tooltip("The velocity of any dropped items relative to the character forward direction.")]
		private Vector3 m_DropVelocity = new Vector3 (0f, 2f, 3f);
		
		protected abstract DuplicateEntryBehaviourFull duplicateBehaviour { get; }
		
		public event UnityAction<IInventoryItem> onItemAdded;
		public event UnityAction<IInventoryItem> onItemRemoved;
		
		protected void OnItemRemoved(IInventoryItem item)
		{
			if (onItemRemoved != null)
				onItemRemoved(item);
		}

		public abstract void ClearAllItems(UnityAction<IInventoryItem> onClearAction);
		public abstract IInventoryItem GetItem(int itemIdentifier);

		protected abstract void AddItemReference(FpsInventoryItemBase item);
		protected abstract void RemoveItemReference(FpsInventoryItemBase item);

        protected virtual void CheckInitialised()
        {
        }

        protected virtual bool CanAddItem(IInventoryItem item)
        {
            return true;
        }

		public InventoryAddResult AddItem(IInventoryItem item)
        {
            CheckInitialised();

            // Check if item is valid
            if (item.itemIdentifier == 0)
            {
                Debug.LogError("Attempting to add inventory item with no ID set: " + (item as UnityEngine.Object));
                return InventoryAddResult.Rejected;
            }

            // Test if can add item
            if (!CanAddItem(item))
                return InventoryAddResult.Rejected;

            // Check if already in inventory
            var existing = (FpsInventoryItemBase)GetItem(item.itemIdentifier);
            if (existing != null)
            {
                // In and same type
                if (existing.itemIdentifier == item.itemIdentifier && existing.maxQuantity > 1)
                {
                    return AppendItem(existing, item);
                }
                else
                {
                    switch (duplicateBehaviour)
                    {
                        case DuplicateEntryBehaviourFull.Reject:
                            return InventoryAddResult.Rejected;
                        case DuplicateEntryBehaviourFull.DestroyOld:
                            RemoveItem(existing);
                            Destroy(existing.gameObject);
                            break;
                        case DuplicateEntryBehaviourFull.DropOld:
                            var qsi = existing as IQuickSlotItem;
                            if (qsi != null)
                                DropItem(qsi);
                            else
                            {
                                RemoveItem(existing);
                                Destroy(existing.gameObject);
                            }
                            break;
                        case DuplicateEntryBehaviourFull.Allow:
                            break;
                    }
                }
			}

            FpsInventoryItemBase fpsItem = item as FpsInventoryItemBase;
			if (fpsItem != null)
			{
                // Needs a root transform for GameObject inventory items
                if (m_WieldableRoot == null)
                {
                    return InventoryAddResult.Rejected;
                }

                // Get transform
                Transform t = fpsItem.transform;

                // Attach using serialization system
                bool attached = false;
                if (wieldableRootNsgo != null)
                {
                    var itemNsgo = fpsItem.GetComponent<NeoSerializedGameObject>();
                    if (itemNsgo != null)
                    {
                        attached = true;
                        itemNsgo.SetParent(wieldableRootNsgo);
                    }
                }

                // Attach using transform if failed
                if (!attached)
                    t.SetParent(m_WieldableRoot);

                // Localise transform
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;

                // Callbacks
                item.OnAddToInventory (this, InventoryAddResult.Full);
				if (onItemAdded != null)
					onItemAdded(item);

                // Deactivate the object
                fpsItem.gameObject.SetActive (false);

                // Set item
                AddItemReference(fpsItem);

                return InventoryAddResult.Full;
			}
			else
			{
				Debug.LogError ("Inventory can only accept components inherited from FpsInventoryItemBase.");
				return InventoryAddResult.Rejected;
			}
        }

        public InventoryAddResult AddItemFromPrefab(GameObject prefab)
        {
            CheckInitialised();

            if (prefab == null || m_WieldableRoot == null)
                return InventoryAddResult.Rejected;

            // Check if item is valid
            var prefabItem = prefab.GetComponent<IInventoryItem>();
            if (prefabItem == null || prefabItem.itemIdentifier == 0)
                return InventoryAddResult.Rejected;

            // Check if already in inventory
            var existing = (FpsInventoryItemBase)GetItem(prefabItem.itemIdentifier);
            if (existing != null)
            {
                // In and same type
                if (existing.itemIdentifier == prefabItem.itemIdentifier && existing.maxQuantity > 1)
                {
                    return AppendPrefab(existing, prefabItem);
                }
                else
                {
                    switch (duplicateBehaviour)
                    {
                        case DuplicateEntryBehaviourFull.Reject:
                            return InventoryAddResult.Rejected;
                        case DuplicateEntryBehaviourFull.DestroyOld:
                            RemoveItem(existing);
                            Destroy(existing.gameObject);
                            break;
                        case DuplicateEntryBehaviourFull.DropOld:
                            var qsi = existing as IQuickSlotItem;
                            if (qsi != null)
                                DropItem(qsi);
                            else
                            {
                                RemoveItem(existing);
                                Destroy(existing.gameObject);
                            }
                            break;
                        case DuplicateEntryBehaviourFull.Allow:
                            break;
                    }
                }
            }
            
            var fpsItem = prefabItem as FpsInventoryItemBase;
            if (fpsItem != null)
            {
                Transform t;

                // Create instance
                if (wieldableRootNsgo != null)
                {
                    fpsItem = wieldableRootNsgo.InstantiatePrefab(fpsItem);
                    t = fpsItem.transform;
                }
                else
                {
                    fpsItem = Instantiate(fpsItem);
                    t = fpsItem.transform;
                    t.SetParent(m_WieldableRoot);
                }
                
                // Localise transform
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;

                // Callbacks
                fpsItem.OnAddToInventory(this, InventoryAddResult.Full);
                if (onItemAdded != null)
                    onItemAdded(fpsItem);

                // Deactivate the object
                fpsItem.gameObject.SetActive(false);

                // Set item
                AddItemReference(fpsItem);

                return InventoryAddResult.Full;
            }
            else
            {
                Debug.LogError("FpsInventoryQuickSwitch can only accept FpsInventoryItem components or components inherited from it.");
                return InventoryAddResult.Rejected;
            }

        }

        protected InventoryAddResult AppendItem(IInventoryItem existing, IInventoryItem item)
        {
            // At limit, so reject item
            if (existing.quantity >= existing.maxQuantity)
                return InventoryAddResult.Rejected;
            else
            {
                // Can take more
                int total = existing.quantity + item.quantity;
                if (total > existing.maxQuantity)
                {
                    // Limit reached. Reduce item quantity to excess and leave
                    existing.quantity = existing.maxQuantity;
                    int excess = total - existing.maxQuantity;
                    item.quantity = excess;
                    item.OnAddToInventory(this, InventoryAddResult.Partial);
                    return InventoryAddResult.Partial;
                }
                else
                {
                    // Limit not reached.
                    item.quantity = 0;
                    existing.quantity = total;
                    item.OnAddToInventory(this, InventoryAddResult.AppendedFull);
                    return InventoryAddResult.AppendedFull; // Caller responsible for destruction, etc
                }
            }
        }

        protected InventoryAddResult AppendPrefab(IInventoryItem existing, IInventoryItem prefab)
        {
            // At limit, so reject item
            if (existing.quantity >= existing.maxQuantity)
                return InventoryAddResult.Rejected;
            else
            {
                // Can take more
                int total = existing.quantity + prefab.quantity;
                if (total > existing.maxQuantity)
                {
                    // Limit reached
                    existing.quantity = existing.maxQuantity;
                    return InventoryAddResult.Partial;
                }
                else
                {
                    // Limit not reached
                    existing.quantity = total;
                    return InventoryAddResult.AppendedFull; // Caller responsible for destruction, etc
                }
            }
        }

        public void RemoveItem(IInventoryItem item)
        {
            CheckInitialised();

            if (item == null || item.itemIdentifier == 0)
				return;

			var result = item as FpsInventoryItemBase;
            if (result == null || !ContainsItem(result))
                return;

			// Check if selected
			bool wasSelected = false;
			var qsi = result as IQuickSlotItem;
			if (qsi != null)
			{
				wasSelected = (selected == qsi);
            }

            if (wasSelected)
            {
                qsi.OnDeselectInstant();

                // Callbacks
                result.OnRemoveFromInventory();
                OnItemRemoved(result);

                // Remove
                RemoveItemReference(result);

                // Switch selection if required
                SwitchSelectionOnRemove(item);
            }
            else
            {
                // Callbacks
                result.OnRemoveFromInventory();
                OnItemRemoved(result);

                // Remove
                RemoveItemReference(result);
            }
		}

        protected virtual void SwitchSelectionOnRemove(IInventoryItem removed)
        {
            if (m_SlotCount > 1)
            {
                if (!SwitchSelection() && backupSlot != null)
                    SetSelected(backupSlot, -1, false, false);

                if (selected == removed)
                    SetSelected(null, -2, true, false);
            }
            else
            {
                if (!SelectStartingSlot())
                    SetSelected(null, -2, true, false);
            }
        }
		
		public void RemoveItem(int itemIdentifier, UnityAction<IInventoryItem> onClearAction)
        {
            CheckInitialised();

            if (itemIdentifier == 0)
				return;
            
			var result = (FpsInventoryItemBase)GetItem (itemIdentifier);
			if (result == null)
				return;
                        
			// Check if selected
			bool wasSelected = false;
			var qsi = result as IQuickSlotItem;
			if (qsi != null)
			{
				wasSelected = (selected == qsi);
				if (wasSelected)
					qsi.OnDeselect ();
			}
            
			// Callbacks
			result.OnRemoveFromInventory ();
			OnItemRemoved (result);
			if (onClearAction != null)
				onClearAction (result);

			// Remove
			RemoveItemReference(result);

            // Switch selection if required
            if (wasSelected)
            {
                if (!SwitchSelection() && backupSlot != null)
                    SetSelected(backupSlot, -1, false, false);

                if (selected == qsi)
                    SetSelected(null, -2, true, false);
            }
        }

        public void GetItems(List<IInventoryItem> output)
        {
            GetItemsSorted(output, null, null);
        }

        public void GetItems(List<IInventoryItem> output, InventoryCallbacks.FilterItem filter)
        {
            GetItemsSorted(output, filter, null);
        }

        public abstract void GetItemsSorted(List<IInventoryItem> output, InventoryCallbacks.FilterItem filter, Comparison<IInventoryItem>  compare);

        public abstract bool ContainsItem(FpsInventoryItemBase item);

        public IInventoryItem[] GetItems()
        {
            var results = new List<IInventoryItem>();
            GetItemsSorted(results, null, null);
            return results.ToArray();
        }

        public IInventoryItem[] GetItems(InventoryCallbacks.FilterItem filter)
        {
            var results = new List<IInventoryItem>();
            GetItemsSorted(results, filter, null);
            return results.ToArray();
        }

        public IInventoryItem[] GetItemsSorted(InventoryCallbacks.FilterItem filter, Comparison<IInventoryItem> compare)
        {
            var results = new List<IInventoryItem>();
            GetItemsSorted(results, filter, compare);
            return results.ToArray();
        }

        #endregion

        #region IQuickSlots implementation

        [SerializeField, Tooltip("The selection method for the starting slot.")]
        private HolsterAction m_HolsterAction = HolsterAction.BackupItem;

        [SerializeField, Tooltip("If selecting an empty slot, switch to the backup item.")]
        private bool m_EmptyAsBackup = false;

        [SerializeField, Min(1), Tooltip("The number of item quick slots.")]
		private int m_SlotCount = 10;

        [SerializeField, Tooltip("The selection method for the starting slot.")]
        private StartingSlot m_StartingSlotChoice = StartingSlot.Ascending;

		[SerializeField, Tooltip("This array specifies the selection order on start. The highest on the list that exists will be the starting selection.")]
		private int[] m_StartingOrder = {0,1,2,3,4,5,6,7,8,9};

        [SerializeField, Tooltip("This array specifies the selection order on start. The highest on the list that exists will be the starting selection.")]
        private AutoSwitchStyle m_AutoSwitchStyle = AutoSwitchStyle.StartingOrder;

        public event UnityAction<int, IQuickSlotItem> onSelectionChanged;
		public event UnityAction<int, IQuickSlotItem> onSlotItemChanged;
		public event UnityAction<IQuickSlotItem> onItemDropped;

        private Waitable m_DeselectWaitable = null;
        private Coroutine m_SelectionCoroutine = null;
        private int[] m_InternalStartingOrder = null;
        private int m_Holstered = -1;
        private Coroutine m_LockCoroutine = null;
        private UnityEngine.Object m_LockObject = null;
        private int m_PreLockedItem = -1;
        private int m_CurrentSlot = -1;

        public enum AutoSwitchStyle
        {
            StartingOrder,
            AlwaysSwitch,
            NeverSwitch
        }

        public enum HolsterAction
        {
            BackupItem,
            Nothing
        }

        public IQuickSlotItem backupSlot { get; private set; }

		private IQuickSlotItem m_Selected = null;
        public IQuickSlotItem selected
		{
			get { return m_Selected; }
		}

        protected bool emptyAsBackupItem
        {
            get { return m_EmptyAsBackup; }
        }

        protected bool isSelectionLocked
        {
            get { return m_LockObject != null; }
        }

        IEnumerator SlowSwitch(bool silent, int slot)
        {
            while (m_DeselectWaitable != null && !m_DeselectWaitable.isComplete)
                yield return null;

            if (m_Selected != null)
                m_Selected.OnSelect();

            if (!silent)
                OnSelectionChanged(slot);

            m_SelectionCoroutine = null;
        }

		public abstract void SetSlotItem(int slot, IQuickSlotItem item);
		public abstract IQuickSlotItem GetSlotItem(int slot);
		public abstract void ClearSlots();
		public abstract bool IsSlotSelectable(int index);
		protected abstract bool SelectSlotInternal(int slot);
        protected abstract bool CheckInstantUse(int slot);

        protected void SetSelected(IQuickSlotItem item, int slot, bool instant, bool silent)
        {
            CheckInitialised();

            if (m_Selected != item)
            {
                if (m_SelectionCoroutine != null)
                {
                    StopCoroutine(m_SelectionCoroutine);
                    m_SelectionCoroutine = null;
                }

                if (instant)
                {
                    if (m_Selected != null)
                        m_Selected.OnDeselectInstant();

                    m_Selected = item;

                    if (m_Selected != null)
                        m_Selected.OnSelect();

                    if (!silent)
                        OnSelectionChanged(slot);
                }
                else
                {
                    Waitable waitable = null;
                    if (m_Selected != null)
                        waitable = m_Selected.OnDeselect();

                    m_Selected = item;

                    if (waitable != null)
                    {
                        m_DeselectWaitable = waitable;
                        m_SelectionCoroutine = StartCoroutine(SlowSwitch(silent, slot));
                    }
                    else
                    {
                        if (m_Selected != null)
                            m_Selected.OnSelect();

                        if (!silent)
                            OnSelectionChanged(slot);
                    }
                }

                m_CurrentSlot = slot;
            }
            else
            {
                if (!silent && emptyAsBackupItem && isBackupItemSelected)
                {
                    OnSelectionChanged(slot);
                    m_CurrentSlot = slot;
                }
            }
        }

        public bool isBackupItemSelected
        {
            get { return m_Selected != null && m_Selected.quickSlot == -1; }
        }

        public bool isNothingSelected
        {
            get { return m_Selected == null; }
        }

        protected void OnSelectionChanged(int slot)
		{
			if (onSelectionChanged != null)
				onSelectionChanged(slot, selected);
		}

		protected void OnSlotItemChanged(int slot, IQuickSlotItem item)
		{
			if (onSlotItemChanged != null)
				onSlotItemChanged(slot, item);
		}

		public virtual bool SelectSlot (int slot)
        {
            CheckInitialised();

            if (CheckInstantUse(slot))
            {
                return false;
            }
            else
            {
                if (m_LockObject != null)
                    return false;

                if (slot == -1 || m_LockObject)
                {
                    if (m_HolsterAction == HolsterAction.BackupItem)
                        return SelectBackupItem(true, false);
                    else
                        return SelectNothing(true, false);
                }

                if (!SelectSlotInternal(slot))
                {
                    if (m_EmptyAsBackup)
                    {
                        if (selected != null && selected != backupSlot)
                            m_Holstered = selected.quickSlot;
                        SetSelected(backupSlot, slot, false, false);

                        return true;
                    }
                    else
                        return false;
                }
                else
                    return true;
            }
		}

        public virtual bool SelectNothing(bool toggle, bool silent)
        {
            CheckInitialised();

            if (m_LockObject != null || backupSlot == null)
                return false;

            if (selected == null)
            {
                if (toggle)
                {
                    // Select previous
                    if (m_Holstered == -1)
                        return SelectStartingSlot();
                    else
                        SelectSlotInternal(m_Holstered);
                    // If changing SelectSlotInternal to SelectSlot, will need to update stacked inventory
                    // Since SelectSlot takes 0-9 and calls internal 0-99 (which is what holstering records)
                }
                else
                    return false;
            }
            else
            {
                m_Holstered = selected.quickSlot;
                SetSelected(null, -2, false, silent);
            }

            return true;
        }

        public virtual bool SelectBackupItem(bool toggle, bool silent)
        {
            CheckInitialised();

            if (m_LockObject != null || backupSlot == null)
                return false;

            if (selected == backupSlot)
            {
                if (toggle)
                {
                    // Select previous
                    if (m_Holstered == -1)
                        return SelectStartingSlot();
                    else
                        SelectSlotInternal(m_Holstered);
                    // If changing SelectSlotInternal to SelectSlot, will need to update stacked inventory
                    // Since SelectSlot takes 0-9 and calls internal 0-99 (which is what holstering records)
                }
                else
                    return false;
            }
            else
            {
                if (selected != null)
                    m_Holstered = selected.quickSlot;
                SetSelected(backupSlot, -1, false, silent);
            }

            return true;
        }

		public bool SelectStartingSlot ()
        {
            CheckInitialised();

            if (m_LockObject != null)
                return false;

            // Select the starting slot (note, if none are valid then none will be selected)
            switch (m_StartingSlotChoice)
			{
				case StartingSlot.Ascending:
					for (int i = 0; i < m_SlotCount; ++i)
					{
						if (SelectSlotInternal(i))
							return true;
					}
					break;
				case StartingSlot.Descending:
					for (int i = m_SlotCount - 1; i >= 0; --i)
					{
						if (SelectSlotInternal(i))
							return true;
					}
					break;
				case StartingSlot.CustomOrder:
					for (int i = 0; i < m_SlotCount; ++i)
					{
						if (SelectSlotInternal(m_StartingOrder[i]))
							return true;
					}
					break;
			}

            // Check for the backup item
            if (backupSlot != null)
            {
                SetSelected(backupSlot, -1, false, false);
                return true;
			}

            return false;
		}

        public virtual bool SelectNextSlot ()
        {
            CheckInitialised();

            if (m_LockObject != null)
                return false;

            if (selected != null)
            {
                if (m_SlotCount > 1)
                {
                    // Get the next slot
                    int index = selected.quickSlot;

                    // Keep cycling until a valid slot is found (limited to number of slots)
                    for (int i = 0; i < m_SlotCount; ++i)
                    {
                        index = WrapSlotIndex(index + 1);
                        // Select the slot if possible
                        if (SelectSlotInternal(index))
                            return true;
                    }

                    //// No valid slots found
                    //// Check for the backup item
                    //if (backupSlot != null)
                    //{
                    //    SetSelected(backupSlot, -1, false, false);
                    //    return true;
                    //}
                }

				return false;
			}
			else
				return SelectStartingSlot ();
		}

		public virtual bool SelectPreviousSlot ()
        {
            CheckInitialised();

            if (m_LockObject != null)
                return false;

            if (selected != null)
            {
                if (m_SlotCount > 1)
                {
                    // Get the previous slot
                    int index = selected.quickSlot;

                    // Keep cycling until a valid slot is found (limited to number of slots)
                    for (int i = 0; i < m_SlotCount; ++i)
                    {
                        index = WrapSlotIndex(index - 1);
                        // Select the slot if possible
                        if (SelectSlotInternal(index))
                            return true;
                    }

                    //// No valid slots found
                    //// Check for the backup item
                    //if (backupSlot != null)
                    //{
                    //    SetSelected(backupSlot, -1, false, false);
                    //    return true;
                    //}
                }

				return false;
			}
			else
				return SelectStartingSlot ();
		}

		public int numSlots
		{
			get { return m_SlotCount; }
		}

        public bool isLocked
        {
            get { return m_LockObject != null; }
        }

        public virtual void AutoSwitchSlot (int slot)
        {
            if (m_LockObject != null)
                return;

            // Check if this is the backup item
            if (slot == -1 && backupSlot != null)
            {
                SetSelected(backupSlot, -1, false, false);
                return;
			}

            if (initialisedContents && isActiveAndEnabled)
                StartCoroutine(AutoSwitchSlotDelayed(slot));
		}

		protected virtual IEnumerator AutoSwitchSlotDelayed (int slot)
		{
			yield return null;

			// Properly wrap the number (should be 1-SlotCount)
			slot = WrapSlotIndex (slot);

			if (selected == null)
				SelectSlotInternal(slot);
			else
            {
                if (FpsSettings.gameplay.autoSwitchWeapons)
                {
                    switch (m_AutoSwitchStyle)
                    {
                        case AutoSwitchStyle.StartingOrder:
                            {
                                // Check if valid + better than current & set
                                if (IsSlotSelectable(slot))
                                {
                                    int currentSlotIndex = selected.quickSlot;
                                    for (int i = 0; i < m_StartingOrder.Length; ++i)
                                    {
                                        if (m_StartingOrder[i] == currentSlotIndex)
                                            break;
                                        if (m_StartingOrder[i] == slot)
                                        {
                                            SelectSlotInternal(slot);
                                            break;
                                        }
                                    }
                                }
                            }
                            break;
                        case AutoSwitchStyle.AlwaysSwitch:
                            {
                                if (IsSlotSelectable(slot))
                                    SelectSlotInternal(slot);
                            }
                            break;
                    }
                }
			}
		}

		public virtual bool SwitchSelection ()
		{
			return SelectNextSlot (); 
		}

		public void DropSelected ()
        {
            if (m_LockObject == null)
                DropItem(selected);
		}

        public void DropAllWieldables()
        {
            if (m_LockObject == null)
            {
                var wieldables = GetItems(x => { return x is IQuickSlotItem; });
                foreach(var i in wieldables)
                {
                    var wieldable = i as IQuickSlotItem;
                    if (!wieldable.isSelected)
                        DropItem(wieldable);
                }
                DropItem(selected);
            }
        }

		protected void DropItem (IQuickSlotItem qsi)
		{
			if (qsi != null && qsi.isDroppable && qsi != backupSlot)
			{
				// Fire onDropped event
				if (onItemDropped != null)
					onItemDropped(qsi);

                // Remove from inventory
                var item = qsi as FpsInventoryItemBase;
                RemoveItem(item);

                // Get the item to handle its own drop
                if (m_DropTransform != null)
                	qsi.DropItem(m_DropTransform.position, m_DropTransform.forward, m_WieldableRoot.rotation * m_DropVelocity);
				else
                	qsi.DropItem(m_WieldableRoot.position, m_WieldableRoot.forward, m_WieldableRoot.rotation * m_DropVelocity);

				// Destroy the inventory item
				Destroy (item.gameObject);
			}
		}

		private int WrapSlotIndex (int slot)
		{
			while (slot < 0)
				slot += m_SlotCount;
			while (slot >= m_SlotCount)
				slot -= m_SlotCount;
			return slot;
		}

		protected int[] GetStartingSlotOrder ()
		{
			if (m_InternalStartingOrder == null)
			{
				m_InternalStartingOrder = new int[m_SlotCount + 1];
				switch (m_StartingSlotChoice)
				{
					case StartingSlot.Ascending:
						for (int i = 0; i < m_SlotCount; ++i)
							m_InternalStartingOrder [i] = i;
						break;
					case StartingSlot.Descending:
						for (int i = 0; i < m_SlotCount; ++i)
							m_InternalStartingOrder [i] = m_SlotCount - 1 - i;
						break;
					case StartingSlot.CustomOrder:
						{
							// Create list of available indices
							List<int> available = new List<int> (m_SlotCount);
							for (int i = 0; i < m_SlotCount; ++i)
								available.Add (i);
							// Set indices from list
							int done = 0;
							for (int i = 0; i < m_StartingOrder.Length; ++i)
							{
								// Check if not already used
								if (available.Contains (m_StartingOrder[i]))
								{
									m_InternalStartingOrder [i] = m_StartingOrder [i];
									available.Remove (m_StartingOrder [i]);
									++done;
								}
							}
							// Fill remaining slots with available
							for (int i = 0; i < m_SlotCount - done; ++i)
								m_InternalStartingOrder [i + done] = available[i];
						}
						break;
				}
                m_InternalStartingOrder[m_SlotCount] = -1;
            }
			return m_InternalStartingOrder;
		}
        
        public bool LockSelectionToSlot(int index, UnityEngine.Object o)
        {
            // Check if lock is valid
            if (o == null || m_LockObject != null)
                return false;

            // Cancel lock/unlock event from this frame
            if (m_LockCoroutine != null)
            {
                StopCoroutine(m_LockCoroutine);
                m_LockCoroutine = null;
            }

            // Check if selectable
            var item = GetSlotItem(index);
            if (item == null || !item.isSelectable)
                return false;

            // Delayed lock (allows cancellation by another lock event on the same frame)
            m_LockCoroutine = StartCoroutine(DelayedLockSelectionToSlot(index, o));

            return true;
        }

        IEnumerator DelayedLockSelectionToSlot(int index, UnityEngine.Object o)
        {
            yield return null;

            // Select the new object and record the old one
            m_PreLockedItem = m_CurrentSlot;
            SelectSlotInternal(index);
            m_LockObject = o;

            m_LockCoroutine = null;
        }

        public bool LockSelectionToBackupItem(UnityEngine.Object o, bool silent)
        {
            // Check if lock is valid
            if (o == null || m_LockObject != null)
                return false;

            // Cancel lock/unlock event from this frame
            if (m_LockCoroutine != null)
            {
                StopCoroutine(m_LockCoroutine);
                m_LockCoroutine = null;
            }
            
            // Delayed lock (allows cancellation by another lock event on the same frame)
            m_LockCoroutine = StartCoroutine(DelayedLockSelectionToBackupItem(silent, o));

            return true;
        }

        IEnumerator DelayedLockSelectionToBackupItem(bool silent, UnityEngine.Object o)
        {
            yield return null;

            // Select the new object and record the old one
            m_PreLockedItem = m_CurrentSlot;
            SelectBackupItem(false, silent);
            m_LockObject = o;

            m_LockCoroutine = null;
        }

        public bool LockSelectionToNothing(UnityEngine.Object o, bool silent)
        {
            // Check if lock is valid
            if (o == null || m_LockObject != null)
                return false;

            // Cancel lock/unlock event from this frame
            if (m_LockCoroutine != null)
                StopCoroutine(m_LockCoroutine);

            // Delayed lock (allows cancellation by another lock event on the same frame)
            m_LockCoroutine = StartCoroutine(DelayedLockSelectionToNothing(silent, o));

            return true;
        }

        IEnumerator DelayedLockSelectionToNothing(bool silent, UnityEngine.Object o)
        {
            yield return null;

            // Select the new object and record the old one
            if (m_CurrentSlot >= 0 || m_CurrentSlot == -1)
                m_PreLockedItem = m_CurrentSlot;
            SetSelected(null, -2, false, silent);
            m_LockObject = o;

            m_LockCoroutine = null;
        }

        public void UnlockSelection(UnityEngine.Object o)
        {
            if (m_LockObject == o)
            {
                // Cancel lock/unlock event from this frame
                if (m_LockCoroutine != null)
                    StopCoroutine(m_LockCoroutine);

                m_LockObject = null;

                // Delayed lock (allows cancellation by another lock event on the same frame)
                if (enabled && gameObject.activeInHierarchy)
                    m_LockCoroutine = StartCoroutine(DelayedUnlockSelection());
                else
                {
                    if (m_PreLockedItem == -1)
                        SelectBackupItem(false, false);
                    else
                        SelectSlot(m_PreLockedItem);
                    m_LockCoroutine = null;
                }
            }
        }

        IEnumerator DelayedUnlockSelection()
        {
            // Wait a few frames
            // Logic is that this will mostly be used with the motion graph behaviours,
            // and a few frames leeway can make a big difference if transitioning wall run to fall to mantle, etc
            // with a middle state of 1-2 frames that doesn't require the inventory locked
            yield return null;
            yield return null;
            yield return null;

            if (m_PreLockedItem != -1)
            {
                if (GetSlotItem(m_PreLockedItem) == null)
                    SelectSlot(m_PreLockedItem);
                else
                    SelectSlotInternal(m_PreLockedItem);
            }
            else
                SelectBackupItem(false, false);

            m_LockCoroutine = null;
        }

        #endregion

        #region STARTING STATE

        [SerializeField, NeoPrefabField, Tooltip("An item to use if no wieldables are in the inventory. This could be empty hands or an infinite weapon such as a knife.")]
		private FpsInventoryWieldable m_BackupItem = null;
        [SerializeField, Tooltip("A selection of inventory items to be added to the inventory on start.")]
        private FpsInventoryItemBase[] m_StartingItems = new FpsInventoryItemBase[0];

        private FpsInventoryItemBase[] m_AdditionalItems = null;
        private FpsInventoryItemBase[] m_AdditionalPrefabs = null;

        protected bool initialisedContents
        {
            get;
            private set;
        }

        void InitialiseContents()
        {
            if (!initialisedContents)
            {
                // Add items already children of wieldable root (backup item will be rejected due to quickslot)
                var found = m_WieldableRoot.GetComponentsInChildren<FpsInventoryItemBase>(true);
                for (int i = 0; i < found.Length; ++i)
                    AddItem(found[i]);

                // Initialise the backup item (usually hands)
                InitialiseBackupItem();

                // Add each item to the inventory
                if (m_StartingItems != null)
                    AddItemsFromPrefabArray(m_StartingItems);
                if (m_AdditionalItems != null)
                    AddItemsFromInstanceArray(m_AdditionalItems);
                if (m_AdditionalPrefabs != null)
                    AddItemsFromPrefabArray(m_AdditionalPrefabs);

                // Select the starting item
                SelectStartingSlot();

                initialisedContents = true;
            }
        }

        void InitialiseBackupItem()
        {
            if (m_BackupItem != null && backupSlot == null)
            {
                // Instantiate the prefab and add to inventory
                if (wieldableRootNsgo != null)
                    backupSlot = wieldableRootNsgo.InstantiatePrefab(m_BackupItem);
                else
                    backupSlot = Instantiate(m_BackupItem);

                var result = AddItem(backupSlot as IInventoryItem);
                if (result == InventoryAddResult.Rejected)
                    Debug.Log("Player inventory rejected backup item: " + m_BackupItem.name);
            }
        }

        public void ApplyLoadout(FpsInventoryLoadout loadout, bool replace = true)
        {
            CheckInitialised();

            if (!initialisedContents)
            {
                if (replace)
                    m_StartingItems = null;

                m_AdditionalPrefabs = loadout.items;
            }
            else
            {
                // Clear existing
                if (replace)
                    ClearAllItems(null);

                // Add each item to the inventory
                AddItemsFromPrefabArray(loadout.items);

                // Select the starting item
                if (replace)
                    SelectStartingSlot();
            }
        }

        public void ApplyLoadout(IInventoryItem[] loadout, bool prefabs, bool replace = true)
        {
            CheckInitialised();

            if (!initialisedContents)
            {
                // Convert items and set as additional starting items
                var castItems = new FpsInventoryItemBase[loadout.Length];
                for (int i = 0; i < loadout.Length; ++i)
                    castItems[i] = loadout[i] as FpsInventoryItemBase;

                if (prefabs)
                    m_AdditionalPrefabs = castItems;
                else
                    m_AdditionalItems = castItems;

                // Remove the original starting items
                if (replace)
                    m_StartingItems = null;
            }
            else
            {
                // Remove old items
                if (replace)
                    ClearAllItems(null);

                // Add each item to the inventory
                for (int i = 0; i < loadout.Length; ++i)
                {
                    var item = loadout[i] as FpsInventoryItemBase;

                    // Check if null
                    if (item == null)
                        continue;

                    // Instantiate if required
                    if (prefabs)
                        item = InstantiateItemFromPrefab(item);

                    // Add to inventory
                    var result = AddItem(item);
                    if (result == InventoryAddResult.Rejected)
                    {
                        Debug.Log("Player inventory rejected item: " + loadout[i].name);
                        if (!prefabs)
                            Destroy(item.gameObject);
                    }
                }

                // Select the starting item
                if (replace)
                    SelectStartingSlot();
            }
        }

        void AddItemsFromInstanceArray(FpsInventoryItemBase[] itemArray)
        {
            CheckInitialised();

            // Add each item to the inventory
            for (int i = 0; i < itemArray.Length; ++i)
            {
                // Check if null
                if (itemArray[i] == null)
                    continue;
                
                // Add to inventory
                var result = AddItem(itemArray[i]);
                if (result == InventoryAddResult.Rejected)
                {
                    Debug.Log("Player inventory rejected item: " + itemArray[i].name);
                    Destroy(itemArray[i].gameObject);
                }
            }
        }

        void AddItemsFromPrefabArray(FpsInventoryItemBase[] itemArray)
        {
            CheckInitialised();

            // Add each item to the inventory
            for (int i = 0; i < itemArray.Length; ++i)
            {
                // Check if null
                if (itemArray[i] == null)
                    continue;

                // Instantiate the prefab and add to inventory
                var item = InstantiateItemFromPrefab(itemArray[i]);

                // Add to inventory
                var result = AddItem(item);
                if (result == InventoryAddResult.Rejected)
                    Debug.Log("Player inventory rejected item: " + item.name);
            }
        }

        FpsInventoryItemBase InstantiateItemFromPrefab(FpsInventoryItemBase source)
        {
            FpsInventoryItemBase result = null;

            // Check if inventory contents are serialized
            if (wieldableRootNsgo != null)
            {
                // Check if serialized or not
                if (source.GetComponent<NeoSerializedGameObject>() == null)
                {
                    // Warn about save system if the scene expects it
                    if (SceneSaveInfo.currentActiveScene != null)
                        Debug.LogWarning("Added wieldable to character inventory which does not have a NeoSerializedGameObject component. This means it will not work with the NeoFPS save system. Wieldable: " + source.name);

                    result = Instantiate(source);
                }
                else
                    result = wieldableRootNsgo.InstantiatePrefab(source);
            }
            else
                result = Instantiate(source);

            return result;
        }

        #endregion

        #region INeoSerializableComponent implementation

        private static readonly NeoSerializationKey k_BackupKey = new NeoSerializationKey("backup");
        private static readonly NeoSerializationKey k_SelectedKey = new NeoSerializationKey("selected");
        private static readonly NeoSerializationKey k_HolsteredKey = new NeoSerializationKey("holstered");
        private static readonly NeoSerializationKey k_PreLockedKey = new NeoSerializationKey("preLocked");
        private static readonly NeoSerializationKey k_CurrentSlotKey = new NeoSerializationKey("currentSlot");

        protected NeoSerializedGameObject wieldableRootNsgo { get; private set; }

        public virtual void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteComponentReference(k_BackupKey, backupSlot, nsgo);
            writer.WriteValue(k_HolsteredKey, m_Holstered);
            writer.WriteValue(k_CurrentSlotKey, m_CurrentSlot);
            if (selected != null)
                writer.WriteValue(k_SelectedKey, selected.quickSlot);
            if (m_LockObject != null)
                writer.WriteValue(k_PreLockedKey, m_PreLockedItem);
        }

        public virtual void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            if (initialisedContents != true)
            {
                if (wieldableRootNsgo == null && m_WieldableRoot != null)
                    wieldableRootNsgo = m_WieldableRoot.GetComponent<NeoSerializedGameObject>();

                // Add inventory items (backup item will be rejected due to quickslot)
                var found = m_WieldableRoot.GetComponentsInChildren<FpsInventoryItemBase>(true);
                for (int i = 0; i < found.Length; ++i)
                    AddItem(found[i]);
                
                // Add backup item
                IQuickSlotItem serializedItem;
                if (reader.TryReadComponentReference(k_BackupKey, out serializedItem, nsgo) && serializedItem != null)
                    backupSlot = serializedItem;

                // Select the correct slot
                int selectedSlot;
                if (reader.TryReadValue(k_SelectedKey, out selectedSlot, -1))
                {
                    if (selectedSlot == -1)
                        SetSelected(backupSlot, selectedSlot, false, false);
                    else
                        SelectSlotInternal(selectedSlot);
                }

                // Read holstered item
                reader.TryReadValue(k_HolsteredKey, out m_Holstered, -1);

                // Read current slot
                reader.TryReadValue(k_CurrentSlotKey, out m_CurrentSlot, -1);

                // Read pre-locked item
                reader.TryReadValue(k_PreLockedKey, out m_PreLockedItem, m_PreLockedItem);

                initialisedContents = true;
            }
            else
                Debug.LogError("Attempting to load inventory contents after it's been initialised: " + name);
        }

        #endregion
    }
}