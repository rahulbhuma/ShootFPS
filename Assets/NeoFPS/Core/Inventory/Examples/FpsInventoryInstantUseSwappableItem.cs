using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;
using UnityEngine.Events;
using UnityEngine.Serialization;
using System;
using NeoSaveGames.Serialization;
using NeoSaveGames;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inventoryref-mb-fpsinventoryinstantuseswappableitem.html")]
    public class FpsInventoryInstantUseSwappableItem : FpsInventoryItemBase, IQuickSlotItem, ISwappable
    {
        [SerializeField, FpsInventoryKey, Tooltip("The item key for this weapon.")]
        private int m_InventoryID = 0;

        [SerializeField, Tooltip("The image to use in the inventory HUD.")]
        private Sprite m_DisplayImage = null;

        [SerializeField, Tooltip("The wieldable category.")]
        private FpsSwappableCategory m_Category = FpsSwappableCategory.Firearm;

        [SerializeField, Tooltip("The maximum quantity you can hold.")]
        private int m_MaxQuantity = 1;

        [SerializeField, Tooltip("The prefab to spawn when the wieldable item is dropped.")]
        private FpsInventoryWieldableDrop m_DropObject = null;

        [SerializeField, Tooltip("The actions to perform when the instant use item is triggered.")]
        private FpsInventoryInstantUseActionCollection m_Actions;

        private static readonly NeoSerializationKey k_QuickSlotKey = new NeoSerializationKey("quickSlot");

        private int m_QuickSlot = -2;

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            // Validate quantities
            if (m_MaxQuantity < 1)
                m_MaxQuantity = 1;

            base.OnValidate();
        }

#endif

        public FpsSwappableCategory category
        {
            get { return m_Category; }
        }

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

            if (addResult != InventoryAddResult.Full)
                return;

            // Check for correct inventory type
            FpsInventorySwappable cast = i as FpsInventorySwappable;
            if (cast != null)
            {
                // Ask for quickslot from inventory if not set (by reading from save game)
                if (m_QuickSlot == -2)
                    m_QuickSlot = cast.GetSlotForItem(this);

                // Assign to slot if valid
                if (m_QuickSlot != -1)
                    fpsInventory.SetSlotItem(m_QuickSlot, this);
            }
            else
                m_QuickSlot = -1;
        }

        public override void OnRemoveFromInventory()
        {
            if (m_QuickSlot >= 0)
                fpsInventory.SetSlotItem(quickSlot, null);
            m_QuickSlot = -2;
            base.OnRemoveFromInventory();
        }

        public override void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            base.WriteProperties(writer, nsgo, saveMode);
            writer.WriteValue(k_QuickSlotKey, m_QuickSlot);
        }

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);
            reader.TryReadValue(k_QuickSlotKey, out m_QuickSlot, m_QuickSlot);
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