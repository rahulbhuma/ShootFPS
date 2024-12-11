using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using NeoFPS.Constants;
using NeoSaveGames;
using NeoSaveGames.Serialization;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inventoryref-mb-fpsinventoryswappable.html")]
    public class FpsInventorySwappable : FpsInventoryBase
	{
		[SerializeField, Tooltip("What to do when trying to add an item to the inventory that already exists.")]
		private DuplicateEntryBehaviourFull m_DuplicateBehaviour = DuplicateEntryBehaviourFull.Reject;

		[SerializeField, Tooltip("What to do when replacing an old item with a new one.")]
        private SwapAction m_SwapAction = SwapAction.Drop;

        [SerializeField, Range(0, 10), Tooltip("The number of quick slots available for each category.")]
        private int[] m_GroupSizes = new int[0];

		private Dictionary<int, List<FpsInventoryItemBase>> m_Items = null;
		private Dictionary<int, int> m_SaveSlotsMap = null;
		private IQuickSlotItem[] m_Slots = null;
        private ItemGroup[] m_ItemGroups = null;
        private bool m_Initialised = false;
		private bool m_Swapping = false;

		public event UnityAction<FpsSwappableCategory, bool> onCategoryFullChanged;

		protected override DuplicateEntryBehaviourFull duplicateBehaviour
		{
			get { return m_DuplicateBehaviour; }
		}
	    
		#if UNITY_EDITOR
	    protected override void OnValidate ()
	    {
		    base.OnValidate ();
			CheckCategoryCount();
        }
		#endif

	    public SwapAction swapAction
	    {
		    get { return m_SwapAction; }
	    }

	    public class ItemGroup : INeoSerializableObject
        {
		    private FpsInventorySwappable m_Inventory;
		    public int startIndex { get; private set; }
		    public int count { get; private set; }
		    private int[] m_History;

		    public ItemGroup (FpsInventorySwappable inventory, int startIndex, int count)
		    {
			    m_Inventory = inventory;
			    this.startIndex = startIndex;
			    this.count = count;
			    m_History = new int[this.count];
			    for (int i = 0; i < this.count; ++i)
				    m_History[i] = -1;
		    }

			public int GetNumAvailableSlots()
			{
				// Check for empty slots
				int available = 0;
				for (int i = 0; i < count; ++i)
				{
					if (m_Inventory.m_Slots[startIndex + i] == null)
						++available;
				}
				return available;
			}
		    
		    public int GetAvailableSlot ()
		    {
			    // Check for empty slots
			    for (int i = 0; i < count; ++i)
			    {
				    if (m_Inventory.m_Slots[startIndex + i] == null)
					    return startIndex + i;
			    }
			    
			    // Get last selected or first in list if none selected
			    return m_History[0] != -1 ? m_History[0] : startIndex;
		    }

		    public void OnSelectSlot (int slot)
            {
                // Get the current index of the slot
                int oldIndex = 0;
			    for (; oldIndex < count - 1; ++oldIndex)
			    {
				    if (m_History [oldIndex] == slot)
					    break;
			    }

                // No change required if first in history
                if (oldIndex == 0)
                    return;

				// Shift the other slots back in the history
				for (int i = oldIndex; i > 0; --i)
					m_History [i] = m_History [i - 1];

			    // Set the slot as first
			    m_History [0] = slot;
            }

		    public void OnClearSlot (int slot)
		    {
			    // Get the current index of the slot
			    int oldIndex = -1;
			    for (int i = 0; i < count; ++i)
			    {
				    if (m_History[i] == slot)
				    {
					    oldIndex = i;
					    break;
				    }
			    }
			    
			    // No change required if not found
			    if (oldIndex == -1)
				    return;

			    // Shift the other slots forward in the history
			    for (int i = oldIndex; i < count - 1; ++i)
				    m_History[i] = m_History[i + 1];
			    
			    // Clear the last slot
			    m_History[count - 1] = -1;
		    }

            public void WriteProperties(INeoSerializer writer)
            {
                writer.WriteValues(k_HistoryKey, m_History);
            }

            public void ReadProperties(INeoDeserializer reader)
            {
                int[] results = null;
                if (reader.TryReadValues(k_HistoryKey, out results, m_History))
                {
                    for (int i = 0; i < Mathf.Min(results.Length, m_History.Length); ++i)
                        m_History[i] = results[i];
                }
            }
        }

        private static readonly NeoSerializationKey k_HistoryKey = new NeoSerializationKey("history");
		private static readonly NeoSerializationKey k_SlotMapKey = new NeoSerializationKey("slotMap");

		protected override void Awake ()
		{
			CheckCategoryCount();

			// Clamp group sizes, as range does not work until edited in inspector
			for (int i = 0; i < m_GroupSizes.Length; ++i)
				m_GroupSizes[i] = Mathf.Clamp(m_GroupSizes[i], 0, 10);

			base.Awake ();

			Initialise();
		}

		protected override void CheckInitialised()
		{
			CheckCategoryCount();

			base.CheckInitialised();

			Initialise();
		}

		void CheckCategoryCount()
        {
			// Resize group count based on constant
			if (m_GroupSizes.Length != FpsSwappableCategory.count)
			{
				int[] newGroupSizes = new int[FpsSwappableCategory.count];
				int i = 0;
				for (; i < newGroupSizes.Length && i < m_GroupSizes.Length; ++i)
					newGroupSizes[i] = m_GroupSizes[i];
				for (; i < newGroupSizes.Length; ++i)
					newGroupSizes[i] = 1;
				m_GroupSizes = newGroupSizes;
			}
		}

        void Initialise()
        {
            if (!m_Initialised)
			{
				m_Items = new Dictionary<int, List<FpsInventoryItemBase>>();
				m_Slots = new IQuickSlotItem[numSlots];

                m_ItemGroups = new ItemGroup[m_GroupSizes.Length];
                int slotIndex = 0;
                for (int i = 0; i < m_GroupSizes.Length; ++i)
                {
                    m_ItemGroups[i] = new ItemGroup(this, slotIndex, m_GroupSizes[i]);
                    slotIndex += m_GroupSizes[i];
                }

                m_Initialised = true;
			}
        }

        protected override bool CanAddItem(IInventoryItem item)
        {
			var swappable = item as ISwappable;
			if (swappable != null)
				return m_GroupSizes[swappable.category] > 0;
			else
			{
				var wieldable = item as FpsInventoryWieldable;
				if (wieldable != null && wieldable.quickSlot != -1)
				{
					Debug.LogError("Attempting to add non-swappable weapon to swappable inventory: " + item.gameObject.name);
					return false;
				}
				else
					return true;
			}
        }

        protected override void AddItemReference(FpsInventoryItemBase item)
		{
			List<FpsInventoryItemBase> container;
			if (m_Items.TryGetValue(item.itemIdentifier, out container))
				container.Add(item);
			else
			{
				container = new List<FpsInventoryItemBase>(1);
				container.Add(item);
				m_Items.Add(item.itemIdentifier, container);
			}
		}

		protected override void RemoveItemReference(FpsInventoryItemBase item)
		{
			List<FpsInventoryItemBase> container;
			if (m_Items.TryGetValue(item.itemIdentifier, out container))
			{
				container.Remove(item);
				if (container.Count == 0)
					m_Items.Remove(item.itemIdentifier);
			}
		}

		public override void ClearAllItems(UnityAction<IInventoryItem> onClearAction)
		{
			foreach (var itemsContainer in m_Items.Values)
			{
				for (int i = 0; i < itemsContainer.Count; ++i)
				{
					if (itemsContainer != null)
					{
						// Callbacks
						itemsContainer[i].OnRemoveFromInventory();
						OnItemRemoved(itemsContainer[i]);
						if (onClearAction != null)
							onClearAction(itemsContainer[i]);
					}
				}
			}
			m_Items.Clear();
		}

		public override IInventoryItem GetItem(int itemIdentifier)
		{
			List<FpsInventoryItemBase> result;
			if (m_Items.TryGetValue(itemIdentifier, out result) && result.Count > 0)
				return result[0];
			else
				return null;
		}

		public override void GetItemsSorted(List<IInventoryItem> output, InventoryCallbacks.FilterItem filter, Comparison<IInventoryItem> compare)
		{
			CheckInitialised();

			if (output == null)
				return;

			output.Clear();

			// Get all items (filtered if a filter is provided)
			foreach (var itemContainer in m_Items.Values)
			{
				if (itemContainer == null)
					continue;

				for (int i = 0; i < itemContainer.Count; ++i)
				{
					if (filter != null && !filter(itemContainer[i]))
						continue;
					output.Add(itemContainer[i]);
				}
			}

			// Sort the results if a comparison is provided
			if (compare != null)
				output.Sort(compare);
		}

		public override bool ContainsItem(FpsInventoryItemBase item)
		{
			foreach (var itemContainer in m_Items.Values)
			{
				if (itemContainer == null)
					continue;

				for (int i = 0; i < itemContainer.Count; ++i)
				{
					if (itemContainer[i] == item)
						return true;
				}
			}

			return false;
		}

		public int GetSlotForItem (ISwappable item)
	    {
		    if (item.category < 0 || item.category >= m_ItemGroups.Length)
			    return -1;

			if (m_SaveSlotsMap != null && wieldableRootNsgo != null)
			{
				var nsgo = item.GetComponent<NeoSerializedGameObject>();
				if (nsgo != null)
				{
					int id = wieldableRootNsgo.serializedChildren.GetSerializationKeyForObject(nsgo);
					if (m_SaveSlotsMap.ContainsKey(id))
						return m_SaveSlotsMap[id];
				}
			}

		    return m_ItemGroups[item.category].GetAvailableSlot ();
	    }

	    private int GetGroupForSlot (int slot)
	    {
		    for (int i = 0; i < m_ItemGroups.Length - 1; ++i)
		    {
			    if (m_ItemGroups[i].startIndex <= slot && m_ItemGroups[i + 1].startIndex > slot)
				    return i;
		    }
		    return m_ItemGroups.Length - 1;
	    }
		
		public override void SetSlotItem(int slot, IQuickSlotItem item)
		{
			if (slot < 0 || slot >= m_Slots.Length || m_Slots[slot] == item)
				return;

			int groupIndex = GetGroupForSlot(slot);
			if (groupIndex == -1)
				return;

			// Check if category was full beforehand
			bool before = m_ItemGroups[groupIndex].GetNumAvailableSlots() == 0;

			// Choose what to do with old object
			IQuickSlotItem old = m_Slots[slot];
			if (old != null)
			{
				if (item == null)
				{
					m_ItemGroups[groupIndex].OnClearSlot (slot);
				}
				else
				{
					m_Swapping = true;

					if (swapAction == SwapAction.Drop)
						DropItem(old);
					else
					{
						RemoveItem(old as FpsInventoryItemBase);
						Destroy(old.gameObject);
					}
				}
			}
			
			// Set slot item
			m_Slots [slot] = item;
			OnSlotItemChanged(slot, item);

			// Check if category is full after and fire event if it's changed
			if (!m_Swapping)
			{
				bool after = m_ItemGroups[groupIndex].GetNumAvailableSlots() == 0;
				if (before != after)
					onCategoryFullChanged?.Invoke(groupIndex, after);
			}
        }

        protected override void SwitchSelectionOnRemove(IInventoryItem removed)
        {
			if (!m_Swapping)
				base.SwitchSelectionOnRemove(removed);
			else
				SetSelected(null, -2, true, false);

			// Reset swapping flag
			m_Swapping = false;
		}

        public override IQuickSlotItem GetSlotItem(int slot)
		{
			return m_Slots [slot];
		}

		public override void ClearSlots()
		{
			for (int i = 0; i < m_Slots.Length; ++i)
				SetSlotItem (i, null);
		}

		protected override bool CheckInstantUse(int index)
		{
			if (index < 0 || index >= numSlots)
				return false;

			var qs = m_Slots[index];
			if (qs != null && qs.isUsable)
			{
				qs.UseItem();
				return true;
			}
			else
				return false;
		}

		public override bool IsSlotSelectable(int index)
		{
			if (index < 0 || index >= numSlots)
				return false;
			if (selected == m_Slots[index])
				return false;
			if (m_Slots [index] == null)
				return false;
			return m_Slots [index].isSelectable;
		}
		
		protected override bool SelectSlotInternal(int slot)
        {
            // Properly wrap the number (should be 1-SlotCount)
            if (slot < 0 || slot >= numSlots)
				return false;

			// Check if valid & set
			if (IsSlotSelectable(slot))
            {
                SetSelected(m_Slots[slot], slot, false, false);
                int group = GetGroupForSlot (slot);
				if (group != -1)
					m_ItemGroups[group].OnSelectSlot (slot);
				return true;
			}
			else
				return false;
		}

        protected override IEnumerator AutoSwitchSlotDelayed(int slot)
        {
            yield return null;

			SelectSlotInternal(slot);

            // NB: Might be worth making a variant of the base class one that swaps
            // item priority for group priority instead. Only switch if group has
            // higher or equal priority (eg. gun > grenade)
        }

        public override void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            base.WriteProperties(writer, nsgo, saveMode);

			// Write groups
			for (int i = 0; i < m_ItemGroups.Length; ++i)
			{
				writer.PushContext(SerializationContext.ObjectNeoFormatted, i);
				m_ItemGroups[i].WriteProperties(writer);
				writer.PopContext(SerializationContext.ObjectNeoFormatted);
			}

			// Write slot map
			if (wieldableRootNsgo != null)
			{
				var map = new List<Vector2Int>(m_Slots.Length);
				for (int i = 0; i < m_Slots.Length; ++i)
				{
					if (m_Slots[i] != null)
					{
						var itemNsgo = m_Slots[i].GetComponent<NeoSerializedGameObject>();
						if (itemNsgo != null)
						{
							int id = wieldableRootNsgo.serializedChildren.GetSerializationKeyForObject(itemNsgo);
							map.Add(new Vector2Int(id, m_Slots[i].quickSlot));
						}
					}
				}
				writer.WriteValues(k_SlotMapKey, map);
			}
        }

		public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
		{
			Initialise();

			// Read slot map
			Vector2Int[] mapEntries;
			if (reader.TryReadValues(k_SlotMapKey, out mapEntries, null))
            {
				m_SaveSlotsMap = new Dictionary<int, int>(mapEntries.Length);
				for (int i = 0; i < mapEntries.Length; ++i)
					m_SaveSlotsMap.Add(mapEntries[i].x, mapEntries[i].y);
            }

			// Read groups
			for (int i = 0; i < m_ItemGroups.Length; ++i)
			{
				reader.PushContext(SerializationContext.ObjectNeoFormatted, i);
				m_ItemGroups[i].ReadProperties(reader);
				reader.PopContext(SerializationContext.ObjectNeoFormatted, i);
            }

            base.ReadProperties(reader, nsgo);

            // Discard slot map. It's only needed during base.ReadProperties
            m_SaveSlotsMap = null;
		}
    }
}