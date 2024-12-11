using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS.CharacterMotion.Conditions
{
    [MotionGraphElement("Character/WieldableSelected")]
    public class WieldableSelectedCondition : MotionGraphCondition
    {
        [SerializeField, FpsInventoryKey]
        private int m_ItemKey = 0;
        [SerializeField]
        private Selection m_Selected = Selection.Selected;

        private IInventory m_Inventory = null; 

        private enum Selection
        {
            Selected,
            NotSelected
        }

        public override void Initialise(IMotionController c)
        {
            base.Initialise(c);

            // Get the character inventory
            m_Inventory = c.GetComponent<IInventory>();
        }

        public override bool CheckCondition(MotionGraphConnectable connectable)
        {
            if (m_ItemKey != 0 && m_Inventory != null)
            {
                // Get the item with the key
                var wieldable = m_Inventory.GetItem(m_ItemKey) as IQuickSlotItem;

                // Check if owned and selected
                if (wieldable != null)
                {
                    var isSelected = wieldable.isSelected ? Selection.Selected : Selection.NotSelected;
                    return isSelected == m_Selected;
                }
                else
                    return m_Selected == Selection.NotSelected;
            }
            return false;
        }
    }
}