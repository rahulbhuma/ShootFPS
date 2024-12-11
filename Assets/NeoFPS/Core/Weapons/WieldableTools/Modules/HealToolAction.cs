using UnityEngine;
using UnityEngine.Serialization;

namespace NeoFPS.WieldableTools
{
    [HelpURL("https://docs.neofps.com/manual/inputref-mb-healtoolaction.html")]
    public class HealToolAction : BaseWieldableToolModule, IDamageSource
    {
        [SerializeField, FlagsEnum, Tooltip("When should the tool apply the heal.")]
        private WieldableToolActionTiming m_Timing = WieldableToolActionTiming.Start;
        [SerializeField, Tooltip("Who/what to heal. Wielder means the character using the tool will heal themselves. Target means the tool will be used on the health manager in front of the user.")]
        private Subject m_Subject = Subject.Wielder;
        [SerializeField, Tooltip("The physics layers that the tool should check against to get a valid heal subject.")]
        private LayerMask m_TargetLayers = PhysicsFilter.LayerFilter.CharacterControllers;
        [SerializeField, Tooltip("The maximum distance in front of the character that the tool should cast to check for valid heal subjects.")]
        private float m_MaxRange = 2f;

        [Header("Start Heal")]

        [SerializeField, FormerlySerializedAs("m_HealAmount"), Tooltip("How many points to heal the subject for in the start action.")]
        private int m_StartHeal = 25;
        [SerializeField, Tooltip("How many fixed frames since the start of the action before the heal is applied.")]
        private int m_StartDelay = 0;

        [Header("Continuous Heal")]

        [SerializeField, FormerlySerializedAs("m_HealAmount"), Tooltip("How many points to heal the subject for in the continuous action.")]
        private int m_ContinuousHeal = 1;
        [SerializeField, Tooltip("The heal will be applied every nth fixed update tick for continuous heals.")]
        private int m_HealInterval = 1;
        [SerializeField, Tooltip("Should the heal be applied on the first frame of the continuous action, or should it wait for the first interval to elapse.")]
        private bool m_Instant = true;

        [Header("End Heal")]

        [SerializeField, FormerlySerializedAs("m_HealAmount"), Tooltip("How many points to heal the subject for in the end action.")]
        private int m_EndHeal = 25;

        [Header("Inventory")]
        [SerializeField, Tooltip("Should the heal consume an inventory item, and how should that work.")]
        private InventoryConsume m_InventoryConsume = InventoryConsume.NoInventoryItem;
        [SerializeField, FpsInventoryKey(required = false), Tooltip("The inventory ID of the object to consume on healing.")]
        private int m_ItemKey = 0;

        private IHealthManager m_HealthManager = null;
        private int m_ContinuousCountDown = 0;
        private int m_StartCountDown = 0;
        private IInventory m_Inventory = null;
        private IInventoryItem m_ToolItem = null;
        private IInventoryItem m_ConsumeItem = null;

        private enum Subject
        {
            Wielder,
            Target
        }

        private enum InventoryConsume
        {
            NoInventoryItem,
            ConsumeOneOnHeal,
            ConsumeHealAmount
        }

        public override bool isValid
        {
            get { return m_Timing != 0; }
        }

        public override WieldableToolActionTiming timing
        {
            get { return m_Timing; }
        }

        private DamageFilter m_OutDamageFilter = DamageFilter.AllDamageAllTeams;
        public DamageFilter outDamageFilter
        {
            get { return m_OutDamageFilter; }
            set { m_OutDamageFilter = value; }
        }

        public IController controller
        {
            get { return tool.wielder.controller; }
        }

        public Transform damageSourceTransform
        {
            get { return tool.wielder.transform; }
        }

        public override bool blocking
        {
            get { return (m_HealthManager == null || m_HealthManager.health >= m_HealthManager.healthMax); }
        }

        public string description
        {
            get { return "Heal"; }
        }

        protected void OnValidate()
        {
            // Sanity check values
            m_MaxRange = Mathf.Clamp(m_MaxRange, 0.5f, 100f);
            m_StartHeal = Mathf.Clamp(m_StartHeal, 1, 1000);
            m_StartDelay = Mathf.Clamp(m_StartDelay, 0, 500);
            m_ContinuousHeal = Mathf.Clamp(m_ContinuousHeal, 1, 1000);
            m_HealInterval = Mathf.Clamp(m_HealInterval, 1, 500);
            m_EndHeal = Mathf.Clamp(m_EndHeal, 1, 1000);

            // Set item key to nothing if not using
            if (m_InventoryConsume == InventoryConsume.NoInventoryItem)
                m_ItemKey = 0;
        }

        public override void Initialise(IWieldableTool t)
        {
            base.Initialise(t);

            // Get health manager if targeting wielder
            if (m_Subject == Subject.Wielder)
            {
                m_HealthManager = t.wielder.GetComponent<IHealthManager>();
                if (m_HealthManager == null)
                    enabled = false;
            }

            // Get item
            m_ToolItem = GetComponent<IInventoryItem>();
            if (m_ToolItem != null)
            {
                m_ToolItem.onAddToInventory += OnInventoryChanged;
                m_ToolItem.onRemoveFromInventory += OnInventoryChanged;
                OnInventoryChanged();
            }
        }


        protected void OnDisable()
        {
            m_ContinuousCountDown = 0;
        }

        protected void OnEnable()
        {
            // Set interval for continuous
            if (m_Instant)
                m_ContinuousCountDown = 1;
            else
                m_ContinuousCountDown = m_HealInterval;

            m_StartCountDown = 0;
        }

        public override void FireStart()
        {
            if (m_StartDelay == 0)
                PerformHeal(m_StartHeal, GetSubject());
            else
                m_StartCountDown = m_StartDelay + 1;
        }

        protected void FixedUpdate()
        {
            if (m_StartCountDown > 0)
            {
                if (--m_StartCountDown == 0)
                    PerformHeal(m_StartHeal, GetSubject());
            }
        }

        public override void FireEnd(bool success)
        {
            if (success)
                PerformHeal(m_EndHeal, GetSubject());

            // Set interval for continuous
            if (m_Instant)
                m_ContinuousCountDown = 1;
            else
                m_ContinuousCountDown = m_HealInterval;
        }

        public override bool TickContinuous()
        {
            if (--m_ContinuousCountDown <= 0)
            {
                var h = GetSubject();
                if (h == null)
                {
                    tool.Interrupt();
                    return false;
                }
                else
                {
                    m_ContinuousCountDown = m_HealInterval;
                    PerformHeal(m_ContinuousHeal, h);

                    if (h.health == h.healthMax)
                    {
                        tool.Interrupt();
                        return false;
                    }
                }
            }
            return true;
        }

        public override void Interrupt()
        {
            base.Interrupt();

            m_StartCountDown = 0;
        }

        void PerformHeal(int amount, IHealthManager subject)
        {
            if (subject != null)
            {
                float originalHealth = subject.health;
                subject.AddHealth(amount, this);

                if (m_ConsumeItem != null)
                {
                    switch (m_InventoryConsume)
                    {
                        case InventoryConsume.ConsumeOneOnHeal:
                            --m_ConsumeItem.quantity;
                            break;
                        case InventoryConsume.ConsumeHealAmount:
                            float healthDelta = subject.health - originalHealth;
                            m_ConsumeItem.quantity -= Mathf.FloorToInt(healthDelta);
                            break;
                    }
                }
            }
        }

        IHealthManager GetSubject()
        {
            if (m_Subject == Subject.Wielder)
                return m_HealthManager;
            else
            {
                // Check for hit
                RaycastHit hit;
                if (PhysicsExtensions.RaycastNonAllocSingle(tool.wielder.fpCamera.GetAimRay(), out hit, m_MaxRange, m_TargetLayers, tool.wielder.transform, QueryTriggerInteraction.Ignore))
                    return hit.collider.GetComponent<IHealthManager>();
                else
                    return null;
            }
        }

        void OnInventoryChanged()
        {
            if (m_Inventory != null)
            {
                m_Inventory.onItemAdded -= OnItemAdded;
                m_Inventory.onItemRemoved -= OnItemRemoved;
            }

            m_Inventory = m_ToolItem.inventory;

            if (m_Inventory == null)
                m_ConsumeItem = null;
            else
            {
                m_Inventory.onItemAdded += OnItemAdded;
                m_Inventory.onItemRemoved += OnItemRemoved;
                m_ConsumeItem = m_Inventory.GetItem(m_ItemKey);
            }
        }

        private void OnItemAdded(IInventoryItem item)
        {
            if (item.itemIdentifier == m_ItemKey)
                m_ConsumeItem = item;
        }

        private void OnItemRemoved(IInventoryItem item)
        {
            if (item.itemIdentifier == m_ItemKey)
            {
                m_ConsumeItem = null;
                if (m_ContinuousCountDown > 0)
                    tool.Interrupt();
            }
        }
    }
}