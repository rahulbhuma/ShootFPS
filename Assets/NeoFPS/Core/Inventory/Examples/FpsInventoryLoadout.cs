using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inventoryref-so-fpsinventoryloadout.html")]
    [CreateAssetMenu(fileName = "FpsInventoryLoadout", menuName = "NeoFPS/Inventory/Loadout", order = NeoFpsMenuPriorities.inventory_database)]
    public class FpsInventoryLoadout : ScriptableObject
    {
        [SerializeField, Tooltip("The items the character inventory should contain on spawn")]
        private FpsInventoryItemBase[] m_Items = { };

        public FpsInventoryItemBase[] items
        {
            get { return m_Items; }
        }

        public static FpsInventoryLoadout CreateLoadout(FpsInventoryItemBase[] items)
        {
            var result = CreateInstance<FpsInventoryLoadout>();
            result.m_Items = items;
            return result;
        }

        public static FpsInventoryLoadout CreateLoadout(List<FpsInventoryItemBase> items)
        {
            var result = CreateInstance<FpsInventoryLoadout>();
            result.m_Items = items.ToArray();
            return result;
        }
    }
}
