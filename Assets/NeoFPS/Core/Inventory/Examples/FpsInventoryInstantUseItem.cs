using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;
using UnityEngine.Events;
using UnityEngine.Serialization;
using System;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inventoryref-mb-fpsinventoryinstantuseitem.html")]
    public class FpsInventoryInstantUseItem : FpsInventoryItemBase, IQuickSlotItem
    {
        [SerializeField, FpsInventoryKey, Tooltip("The item key for this weapon.")]
        private int m_InventoryID = 0;

        [SerializeField, Tooltip("The image to use in the inventory HUD.")]
        private Sprite m_DisplayImage = null;

        [SerializeField, Tooltip("The quick slot the item should be placed in. If you are using a stacked inventory, remember that each stack is 10 slots (0-9 = stack 1, 10-19 = stack 2, etc).")]
        private int m_QuickSlot = -2;

        [SerializeField, Tooltip("The maximum quantity you can hold.")]
        private int m_MaxQuantity = 1;

        [SerializeField, Tooltip("The prefab to spawn when the wieldable item is dropped.")]
        private FpsInventoryWieldableDrop m_DropObject = null;

        [SerializeField, Tooltip("The actions to perform when the instant use item is triggered.")]
        private FpsInventoryInstantUseActionCollection m_Actions;

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            // Validate quantities
            if (m_MaxQuantity < 1)
                m_MaxQuantity = 1;

            // Validate Quickslot
            if (m_QuickSlot < -1)
                m_QuickSlot = -1;

            base.OnValidate();
        }

#endif

        public override int itemIdentifier
        {
            get { return m_InventoryID; }
        }

        public override int maxQuantity
        {
            get { return m_MaxQuantity; }
        }

        public override void OnAddToInventory(IInventory i, InventoryAddResult addResult)
        {
            base.OnAddToInventory(i, addResult);
            if (addResult == InventoryAddResult.Full && m_QuickSlot != -1)
                fpsInventory.SetSlotItem(m_QuickSlot, this);
        }

        public override void OnRemoveFromInventory()
        {
            if (m_QuickSlot != -1)
                fpsInventory.SetSlotItem(m_QuickSlot, null);
            base.OnRemoveFromInventory();
        }

        #region IQuickSlotItem implementation

        public IWieldable wieldable
        {
            get { return null; }
        }

        public IQuickSlots slots
        {
            get { return fpsInventory; }
        }

        public Sprite displayImage
        {
            get { return m_DisplayImage; }
        }

        public int quickSlot
        {
            get { return m_QuickSlot; }
        }

        public bool isSelected
        {
            get { return false; }
        }

        public virtual bool isSelectable
        {
            get { return false; }
        }

        public bool isDroppable
        {
            get { return m_DropObject != null; }
        }

        public virtual bool isUsable
        {
            get { return m_Actions.canUse; }
        }

        public virtual void UseItem()
        {
            m_Actions.PerformActions();
        }

        public virtual void OnSelect() { }
        public virtual void OnDeselectInstant() { }
        public virtual Waitable OnDeselect() { return null; }

        protected override void Awake()
        {
            base.Awake();

            m_Actions.Initialise(this);
        }

        protected override void OnZeroQuantity()
        {
            // Just remove and destroy
            fpsInventory.RemoveItem(this);
            Destroy(gameObject);
        }

        public virtual bool DropItem(Vector3 position, Vector3 forward, Vector3 velocity)
        {
            if (m_DropObject == null)
                return false;

            var drop = (neoSerializedGameObject != null && neoSerializedGameObject.serializedScene != null) ?
                neoSerializedGameObject.serializedScene.InstantiatePrefab(m_DropObject) :
                Instantiate(m_DropObject);

            drop.Drop(this, position, forward, velocity);

            return true;
        }

        #endregion
    }
}