using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS.CharacterMotion.Conditions
{
    [MotionGraphElement("Character/InventoryItem")]
    public class InventoryItemCondition : MotionGraphCondition
    {
        [SerializeField, FpsInventoryKey]
        private int m_ItemKey = 0;
        [SerializeField]
        private int m_CompareValue = 0;
        [SerializeField]
        private ComparisonType m_ComparisonType = ComparisonType.EqualTo;

        private IInventory m_Inventory = null; 

        public enum ComparisonType
        {
            EqualTo,
            NotEqualTo,
            GreaterThan,
            GreaterOrEqual,
            LessThan,
            LessOrEqual
        }

        public override void OnValidate()
        {
            base.OnValidate();

            if (m_CompareValue < 0)
                m_CompareValue = 0;
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
                IInventoryItem item = m_Inventory.GetItem(m_ItemKey);
                if (item != null)
                {
                    // Check quantity
                    switch (m_ComparisonType)
                    {
                        case ComparisonType.EqualTo:
                            return item.quantity == m_CompareValue;
                        case ComparisonType.NotEqualTo:
                            return item.quantity != m_CompareValue;
                        case ComparisonType.GreaterThan:
                            return item.quantity > m_CompareValue;
                        case ComparisonType.GreaterOrEqual:
                            return item.quantity >= m_CompareValue;
                        case ComparisonType.LessThan:
                            return item.quantity < m_CompareValue;
                        case ComparisonType.LessOrEqual:
                            return item.quantity <= m_CompareValue;
                    }
                }
                else
                {
                    // Quantity is 0
                    switch (m_ComparisonType)
                    {
                        case ComparisonType.EqualTo:
                            return m_CompareValue == 0;
                        case ComparisonType.NotEqualTo:
                            return m_CompareValue != 0;
                        case ComparisonType.GreaterThan:
                            return false;
                        case ComparisonType.GreaterOrEqual:
                            return m_CompareValue == 0;
                        case ComparisonType.LessThan:
                            return m_CompareValue >= 1;
                        case ComparisonType.LessOrEqual:
                            return true;
                    }
                }
            }
            return false;
        }
    }
}