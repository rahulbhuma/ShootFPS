using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS.SinglePlayer
{
    [Serializable]
    public class LoadoutBuilderData
    {
        [Tooltip("")]
        public LoadoutBuilderSlot[] slots = { };

        [Tooltip("")]
        public FpsInventoryItemBase[] fixedItems = { };

        private List<FpsInventoryItemBase> m_ItemList = null;

        public FpsInventoryLoadout GetLoadout()
        {
            // Initialise list
            if (m_ItemList == null)
                m_ItemList = new List<FpsInventoryItemBase>(slots.Length + fixedItems.Length);
            else
                m_ItemList.Clear();

            // Add selected items
            for (int i = 0; i < slots.Length; ++i)
            {
                var item = slots[i].GetOption(slots[i].currentOption);
                if (item != null)
                    m_ItemList.Add(item);
            }

            // Add fixed items
            for (int i = 0; i < fixedItems.Length; ++i)
            {
                if (fixedItems[i] != null)
                    m_ItemList.Add(fixedItems[i]);
            }

            // Create loadout
            return FpsInventoryLoadout.CreateLoadout(m_ItemList);
        }
    }

    [Serializable]
    public class LoadoutBuilderSlot : ILoadoutBuilderSlot
    {
        [SerializeField, Tooltip("The name used for the multi-choice selector for this inventory slot")]
        private string m_DisplayName = string.Empty;

        [SerializeField, Tooltip("The different weapon / item options for available for this slot")]
        private FpsInventoryItemBase[] m_Options = { };

        private int m_CurrentOption = 0;

        public string displayName
        {
            get { return m_DisplayName; }
        }

        public int numOptions
        {
            get { return m_Options.Length; }
        }

        public int currentOption
        {
            get { return m_CurrentOption; }
            set { m_CurrentOption = Mathf.Clamp(value, 0, m_Options.Length - 1); }
        }

        public FpsInventoryItemBase GetOption(int index)
        {
            if (index >= 0 && index < m_Options.Length)
                return m_Options[index];
            else
                return null;
        }
    }
}
