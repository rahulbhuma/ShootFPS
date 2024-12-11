using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;
using UnityEngine.Events;
using UnityEngine.Serialization;
using NeoSaveGames.Serialization;
using NeoSaveGames;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inventoryref-mb-fpsinventorywieldableswappable.html")]
    public class FpsInventoryWieldableSwappable : FpsInventoryItemBase, IQuickSlotItem, ISwappable
    {
        [SerializeField, HideInInspector]
        private FpsInventoryKey m_ItemKey = FpsInventoryKey.Undefined;

        [SerializeField, FpsInventoryKey, Tooltip("The item key for this weapon.")]
        private int m_InventoryID = 0;

        [SerializeField, Tooltip("The image to use in the inventory HUD.")]
        private Sprite m_DisplayImage = null;

        [SerializeField, Tooltip("The maximum quantity you can hold.")]
        private int m_MaxQuantity = 1;

        [SerializeField, Tooltip("What to do when the item is deselected.")]
        private WieldableDeselectAction m_DeselectAction = WieldableDeselectAction.DeactivateGameObject;

        [SerializeField, Tooltip("An event called when the wieldable is selected. Use this to enable components, etc.")]
        private UnityEvent m_OnSelect = new UnityEvent();

        [SerializeField, Tooltip("An event called when the wieldable is deselected. Use this to disable components, etc.")]
        private UnityEvent m_OnDeselect = new UnityEvent();

        [SerializeField, Tooltip("The prefab to spawn when the wieldable item is dropped.")]
        private FpsInventoryWieldableDrop m_DropObject = null;

        [SerializeField, Tooltip("The wieldable category.")]
        private FpsSwappableCategory m_Category = FpsSwappableCategory.Firearm;

        private static readonly NeoSerializationKey k_QuickSlotKey = new NeoSerializationKey("quickSlot");

        private Coroutine m_DeselectionCoroutine = null;
        private Waitable m_DeselectionWaitable = null;
        private bool m_DestroyOnDeselect = false;
        private int m_QuickSlot = -2;

        public event UnityAction onSelect
        {
            add { m_OnSelect.AddListener(value); }
            remove { m_OnSelect.RemoveListener(value); }
        }

        public event UnityAction onDeselect
        {
            add { m_OnDeselect.AddListener(value); }
            remove { m_OnDeselect.RemoveListener(value); }
        }

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            // Validate quantities
            if (m_MaxQuantity < 1)
                m_MaxQuantity = 1;

            base.OnValidate();

            CheckID();
        }

#endif

        int CheckID()
        {
            if (m_ItemKey != FpsInventoryKey.Undefined)
            {
                if (m_InventoryID == 0)
                    m_InventoryID = m_ItemKey;
                m_ItemKey = FpsInventoryKey.Undefined;
            };
            return m_InventoryID;
        }

        protected override void Awake()
        {
            wieldable = GetComponent<IWieldable>();
            base.Awake();
        }


        public FpsSwappableCategory category
        {
            get { return m_Category; }
        }

        public override int itemIdentifier
        {
            get { return CheckID(); }
        }

        public override int maxQuantity
        {
            get { return m_MaxQuantity; }
        }

        public IWieldable wieldable
        {
            get;
            private set;
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

                // Assing to slot if valid
                if (m_QuickSlot != -1)
                {
                    var old = fpsInventory.GetSlotItem(m_QuickSlot);
                    bool forceAutoSwitch = (old != null && fpsInventory.selected == old);

                    fpsInventory.SetSlotItem(m_QuickSlot, this);
                    if (forceAutoSwitch || FpsSettings.gameplay.autoSwitchWeapons)
                        fpsInventory.AutoSwitchSlot(m_QuickSlot);
                }
            }
            else
                m_QuickSlot = -1;
        }

        public override void OnRemoveFromInventory()
        {
            if (m_QuickSlot >= 0)
                fpsInventory.SetSlotItem(m_QuickSlot, null);
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

        public virtual void OnSelect()
        {
            // Stop existing deselect
            if (m_DeselectionCoroutine != null)
            {
                StopCoroutine(m_DeselectionCoroutine);
                m_DeselectionCoroutine = null;
                m_DeselectionWaitable = null;
            }

            // Perform select action
            switch (m_DeselectAction)
            {
                case WieldableDeselectAction.DeactivateGameObject:
                    gameObject.SetActive(true);
                    break;
                case WieldableDeselectAction.DisableWieldableComponent:
                    {
                        var wieldableBehaviour  = wieldable as MonoBehaviour;
                        if (wieldableBehaviour != null)
                            wieldableBehaviour.enabled = true;
                    }
                    break;
            }

            // Tell wieldable to select
            if (wieldable != null)
                wieldable.Select();

            // Invoke event
            m_OnSelect.Invoke();
        }

        public virtual void OnDeselectInstant()
        {
            if (m_DeselectionCoroutine != null)
            {
                StopCoroutine(m_DeselectionCoroutine);
                m_DeselectionCoroutine = null;
                m_DeselectionWaitable = null;
            }

            // Invoke event
            m_OnDeselect.Invoke();

            // Reset deselectable
            if (wieldable != null)
                wieldable.DeselectInstant();

            PerformDeselectAction();
        }

        public virtual Waitable OnDeselect()
        {
            if (m_DeselectionCoroutine == null)
            {
                // Invoke event
                m_OnDeselect.Invoke();

                // Reset deselectable
                if (this != null) // Check if destroyed due to drop, etc
                {
                    m_DeselectionWaitable = null;
                    if (wieldable != null)
                        m_DeselectionWaitable = wieldable.Deselect();
                    if (m_DeselectionWaitable != null && gameObject.activeInHierarchy)
                    {
                        m_DeselectionCoroutine = StartCoroutine(DelayedDeselect());
                        return m_DeselectionWaitable;
                    }
                    else
                    {
                        PerformDeselectAction();
                    }
                }

                return null;
            }
            else
                return m_DeselectionWaitable;
        }

        IEnumerator DelayedDeselect()
        {
            while (!m_DeselectionWaitable.isComplete)
                yield return null;
            m_DeselectionWaitable = null;

            PerformDeselectAction();

            m_DeselectionCoroutine = null;
        }

        void PerformDeselectAction()
        {
            // Destroy if required, or perform deselect actions
            if (m_DestroyOnDeselect)
            {
                m_DestroyOnDeselect = false;
                fpsInventory.RemoveItem(this);
                Destroy(gameObject);
            }
            else
            {
                // Perform deselect action
                switch (m_DeselectAction)
                {
                    case WieldableDeselectAction.DeactivateGameObject:
                        gameObject.SetActive(false);
                        break;
                    case WieldableDeselectAction.DisableWieldableComponent:
                        {
                            var wieldableBehaviour = wieldable as MonoBehaviour;
                            if (wieldableBehaviour != null)
                                wieldableBehaviour.enabled = false;
                        }
                        break;
                }
            }
        }

        protected override void OnZeroQuantity()
        {
            if ((FpsInventoryItemBase)fpsInventory.selected == this)
            {
                m_DestroyOnDeselect = true;
                fpsInventory.SwitchSelection();
            }
            else
            {
                // Just remove and destroy
                fpsInventory.RemoveItem(this);
                Destroy(gameObject);
            }
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
            get { return (FpsInventoryWieldableSwappable)fpsInventory.selected == this; }
        }

        public virtual bool isSelectable
        {
            get { return m_QuickSlot != -1; }
        }

        public bool isUsable
        {
            get { return false; }
        }

        public bool isDroppable
        {
            get { return m_DropObject != null; }
        }

        public virtual bool DropItem(Vector3 position, Vector3 forward, Vector3 velocity)
        {
            if (m_DropObject == null) return false;

            FpsInventoryWieldableDrop drop = null;
            
            // Instantiate through save system
            if (neoSerializedGameObject != null)
            {
                var serializedScene = neoSerializedGameObject.serializedScene;
                if (serializedScene != null)
                    drop = serializedScene.InstantiatePrefab(m_DropObject);
            }

            // Fallback if not savable, or save system failed
            if (drop == null)
                drop = Instantiate(m_DropObject);

            // Drop the item
            drop.Drop(this, position, forward, velocity);

            return true;
        }

        public void UseItem() { }

        #endregion
    }
}