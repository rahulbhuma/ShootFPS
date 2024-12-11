using System;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/hudref-mb-hudinventorystandardpc.html")]
    [RequireComponent(typeof(CanvasGroup))]
    public class HudInventoryStandardPC : HudInventory
    {
        [SerializeField, Tooltip("A prototype of a single quick-slot item for duplicating.")]
        private HudInventoryItemStandard m_ItemPrototype = null;

        [SerializeField, Tooltip("The padding transform to pad the layout group and push the item slots together.")]
        private Transform m_EndPadding = null;

        [SerializeField, Tooltip("Ranges of quick slot numbers that will be visible in this HUD inventory.")]
        private IntRange[] m_ShowRanges = { new IntRange { min = 0, max = 9 } };

        private HudInventoryItemStandard[] m_Slots = null;
        private int m_SelectedIndex = -1;

        protected override void OnValidate()
        {
            base.OnValidate();
            if (m_ItemPrototype == null)
                m_ItemPrototype = GetComponentInChildren<HudInventoryItemStandard>();

            // Check show ranges are valid and in order
            if (m_ShowRanges.Length < 1)
                m_ShowRanges = new IntRange[] { new IntRange { min = 0, max = 9 } };
            else
            {
                int lastIndex = -1;
                for (int i = 0; i < m_ShowRanges.Length; ++i)
                {
                    var range = m_ShowRanges[i];
                    bool dirty = false;

                    // Check min vs last range max
                    if (range.min <= lastIndex)
                    {
                        range.min = lastIndex + 1;
                        dirty = true;
                    }
                    // Check max vs min
                    if (range.max < range.min)
                    {
                        range.max = range.min;
                        dirty = true;
                    }

                    // Reapply if changed
                    if (dirty)
                        m_ShowRanges[i] = range;

                    // Record max for next range
                    lastIndex = range.max;
                }
            }
        }

        protected override void Start()
        {
            base.Start();
            if (m_ItemPrototype == null)
                Debug.LogError("Inventory HUD has no slot prototype set up.");
        }

        protected override bool InitialiseSlots()
        {
            if (m_ItemPrototype == null)
                return false;
            else
                m_ItemPrototype.gameObject.SetActive(false);

            // Check slots array is the correct size
            if (m_Slots == null)
                m_Slots = new HudInventoryItemStandard[inventory.numSlots];
            else
            {
                if (m_Slots.Length != inventory.numSlots)
                {
                    var old = m_Slots;
                    m_Slots = new HudInventoryItemStandard[inventory.numSlots];

                    int i = 0; 
                    for (; i < m_Slots.Length && i < old.Length; ++i)
                        m_Slots[i] = old[i];
                    for (; i < old.Length; ++i)
                    {
                        if (old[i] != null)
                            Destroy(old[i].gameObject);
                    }
                }
            }

            // Apply entries
            for (int i = 0; i < m_Slots.Length; ++i)
            {
                // Check if i should be visible
                if (ShouldSlotBeVisible(i))
                {
                    // Create if null
                    if (m_Slots[i] == null)
                    {
                        m_Slots[i] = Instantiate(m_ItemPrototype, m_ItemPrototype.transform.parent);
                        m_Slots[i].slotIndex = i;
                        m_Slots[i].gameObject.SetActive(true);
                    }

                    // Set as last sibling
                    m_Slots[i].transform.SetAsLastSibling();
                }
                else
                {
                    // Destroy if currently set
                    if (m_Slots[i] != null)
                    {
                        Destroy(m_Slots[i].gameObject);
                        m_Slots[i] = null;
                    }
                }
            }

            // Reset padding (for layout group)
            if (m_EndPadding != null)
                m_EndPadding.SetAsLastSibling();

            return true;
        }

        bool ShouldSlotBeVisible(int slot)
        {
            for (int i = 0; i < m_ShowRanges.Length; ++i)
                if (m_ShowRanges[i].min <= slot && m_ShowRanges[i].max >= slot)
                    return true;

            return false;
        }

        protected override void SetSlotItem(int slot, IQuickSlotItem item)
        {
            if (m_Slots[slot] != null)
                m_Slots[slot].SetItem(item);
        }

        protected override void ClearContents()
        {
            for (int i = 0; i < m_Slots.Length; ++i)
            {
                if (m_Slots[i] != null)
                {
                    m_Slots[i].SetItem(null);
                    m_Slots[i].selected = false;
                }
            }
            m_SelectedIndex = -1;
            TriggerTimeout();
        }

        protected override void OnSelectSlot(int index)
        {
            // Check if it's an actual change
            if (index == m_SelectedIndex)
                return;

            // Deselect old
            if (m_SelectedIndex != -1 && m_Slots[m_SelectedIndex] != null)
                m_Slots[m_SelectedIndex].selected = false;

            // Set new
            m_SelectedIndex = index;

            // Select new
            if (m_SelectedIndex != -1 && m_Slots[m_SelectedIndex] != null)
                m_Slots[m_SelectedIndex].selected = true;

            base.OnSelectSlot(index);
        }
    }
}