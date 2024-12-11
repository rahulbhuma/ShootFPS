using NeoFPS.Constants;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    public abstract class HudInventoryItemTracker : PlayerCharacterHudBase
    {
        [SerializeField, HideInInspector]
        private FpsInventoryKey m_ItemKey = FpsInventoryKey.Undefined;

        [SerializeField, FpsInventoryKey, Tooltip("The inventory ID of the item to track.")]
        private int m_InventoryID = 0;

        private FpsInventoryBase m_Inventory = null;

        protected IInventoryItem item
        {
            get;
            private set;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            CheckID();
        }
#endif
        
        void CheckID()
        {
            if (m_ItemKey != FpsInventoryKey.Undefined)
            {
                if (m_InventoryID == 0)
                    m_InventoryID = m_ItemKey;
                m_ItemKey = FpsInventoryKey.Undefined;
            };
        }

        protected override void Awake()
        {
            base.Awake();
            CheckID();
        }

        public override void OnPlayerCharacterChanged(ICharacter character)
        {
            // Detach from old inventory
            if (m_Inventory != null)
            {
                m_Inventory.onItemAdded -= OnItemAdded;
                m_Inventory.onItemRemoved -= OnItemRemoved;
            }

            // Detach from old item
            if (item != null)
            {
                item.onQuantityChange -= OnQuantityChanged;
                item = null;
            }

            // Set new inventory
            if (character as Component != null)
                m_Inventory = character.GetComponent<FpsInventoryBase>();
            else
                m_Inventory = null;

            // Attach to new inventory
            if (m_Inventory != null)
            {
                m_Inventory.onItemAdded += OnItemAdded;
                m_Inventory.onItemRemoved += OnItemRemoved;

                item = m_Inventory.GetItem(m_InventoryID);
                if (item != null)
                    item.onQuantityChange += OnQuantityChanged;

                OnQuantityChanged();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        protected abstract void OnQuantityChanged();

        private void OnItemAdded(IInventoryItem added)
        {
            if (added.itemIdentifier == m_InventoryID)
            {
                if (item != null)
                    item.onQuantityChange -= OnQuantityChanged;
                item = added;
                if (item != null)
                    item.onQuantityChange += OnQuantityChanged;
            }

            OnQuantityChanged();
        }

        private void OnItemRemoved(IInventoryItem removed)
        {
            if (removed.itemIdentifier == m_InventoryID)
            {
                if (item != null)
                    item.onQuantityChange -= OnQuantityChanged;
                item = null;
            }

            OnQuantityChanged();
        }
    }
}
