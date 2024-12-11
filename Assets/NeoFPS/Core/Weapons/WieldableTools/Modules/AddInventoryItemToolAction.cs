using NeoSaveGames;
using NeoSaveGames.Serialization;
using UnityEngine;

namespace NeoFPS.WieldableTools
{
    [HelpURL("https://docs.neofps.com/manual/inputref-mb-addinventoryitemtoolaction.html")]
    public class AddInventoryItemToolAction : BaseWieldableToolModule, INeoSerializableComponent
    {
        [SerializeField, FlagsEnum, Tooltip("When should the items be consumed.")]
        private WieldableToolActionTiming m_Timing = WieldableToolActionTiming.Start;
        [SerializeField, NeoPrefabField(required = true), Tooltip("An inventory item prefab to add to the character inventory if none is found.")]
        private FpsInventoryItemBase m_ItemPrefab = null;
        [SerializeField, Tooltip("The items should be added every nth fixed update frame in continuous mode.")]
        private int m_Interval = 1;
        [SerializeField, Tooltip("Should the item be added on the first frame of the continuous action, or should it wait for the first interval to elapse.")]
        private bool m_Instant = true;
        [SerializeField, Tooltip("The number of times to add the item.")]
        private int m_Count = 25;
        [SerializeField, Tooltip("Should the countdown between adding items be reset when you stop using the item.")]
        private bool m_ResetTimerOnStop = false;

        private IInventory m_Inventory = null;
        private IInventoryItem m_ToolItem = null;
        private int m_CountDown = 0;
        private int m_NumAdded = 0;

        public override bool isValid
        {
            get { return m_Timing != 0 && m_ItemPrefab != null && m_Inventory != null && m_NumAdded < m_Count; }
        }

        public override WieldableToolActionTiming timing
        {
            get { return m_Timing; }
        }

        protected void OnValidate()
        {
            m_Interval = Mathf.Clamp(m_Interval, 1, 500);
        }

        protected void Start()
        {
            m_ToolItem = GetComponent<IInventoryItem>();
            if (m_ToolItem != null)
            {
                m_ToolItem.onAddToInventory += OnInventoryChanged;
                m_ToolItem.onRemoveFromInventory += OnInventoryChanged;
                OnInventoryChanged();
            }
        }

        public void ResetCount()
        {
            m_NumAdded = 0;
        }

        void OnInventoryChanged()
        {
            m_Inventory = m_ToolItem.inventory;
        }

        protected void OnDisable()
        {
            m_CountDown = 0;
        }

        bool AddItem()
        {
            var item = m_Inventory.GetItem(m_ItemPrefab.itemIdentifier);
            if (item == null)
            {
                // Add new item to inventory
                if (m_Inventory.AddItemFromPrefab(m_ItemPrefab.gameObject) != InventoryAddResult.Rejected)
                {
                    // Check if count reached
                    if (++m_NumAdded == m_Count)
                        tool.Interrupt();

                    return true;
                }
            }
            else
            {
                if (item.quantity < item.maxQuantity)
                {
                    // Increment item quantity
                    item.quantity += m_ItemPrefab.quantity;

                    // Check if count reached or max quantity
                    if (++m_NumAdded == m_Count || item.quantity == item.maxQuantity)
                        tool.Interrupt();

                    return true;
                }
            }

            return false;
        }

        public override void FireStart()
        {
            // Set interval for continuous
            if (m_Instant)
                m_CountDown = 1;
            else
                m_CountDown = m_Interval;
        }

        public override void FireEnd(bool success)
        {
            // Reset countdown
            if (m_ResetTimerOnStop)
                m_CountDown = 0;
        }

        public override bool TickContinuous()
        {
            if (--m_CountDown <= 0)
            {
                m_CountDown = m_Interval;

                return AddItem();
            }
            else
                return true;
        }

        private static readonly NeoSerializationKey k_CountDownKey = new NeoSerializationKey("countdown");
        private static readonly NeoSerializationKey k_NumAddedKey = new NeoSerializationKey("numAdded");

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValue(k_CountDownKey, m_CountDown);
            writer.WriteValue(k_NumAddedKey, m_NumAdded);
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            reader.TryReadValue(k_CountDownKey, out m_CountDown, m_CountDown);
            reader.TryReadValue(k_NumAddedKey, out m_NumAdded, m_NumAdded);
        }
    }
}