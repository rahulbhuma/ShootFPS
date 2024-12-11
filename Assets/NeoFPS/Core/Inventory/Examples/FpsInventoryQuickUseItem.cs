using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;
using UnityEngine.Events;
using UnityEngine.Serialization;
using System;
using NeoFPS.ModularFirearms;
using NeoFPS.WieldableTools;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inventoryref-mb-fpsinventoryquickuseitem.html")]
    public class FpsInventoryQuickUseItem : FpsInventoryItemBase, IQuickSlotItem
    {
        [SerializeField, FpsInventoryKey, Tooltip("The item key for this weapon.")]
        private int m_InventoryID = 0;

        [SerializeField, Tooltip("The image to use in the inventory HUD.")]
        private Sprite m_DisplayImage = null;

        [SerializeField, Tooltip("The quick slot the item should be placed in. If you are using a stacked inventory, remember that each stack is 10 slots (0-9 = stack 1, 10-19 = stack 2, etc).")]
        private int m_QuickSlot = -2;

        [SerializeField, Tooltip("The maximum quantity you can hold.")]
        private int m_MaxQuantity = 1;

        [SerializeField, Tooltip("An event called when the item is used.")]
        private UnityEvent m_OnUsed = new UnityEvent();

        [SerializeField, Tooltip("The prefab to spawn when the wieldable item is dropped.")]
        private FpsInventoryWieldableDrop m_DropObject = null;

        [SerializeField, Tooltip("What to do with the equipped item while performing the quick action. Deselect will deselect it before the action starts, and reselect after. AnimatorBool will block it and set a bool on its animator.")]
        private EquippedItemAction m_EquippedItemAction = EquippedItemAction.Deselect;

        [SerializeField, Tooltip("The animator bool to set on the previously equipped item. You can use this to do things like lower one hand to use as part of this item.")]
        private string m_AnimatorBoolKey = "QuickAction";

        private enum EquippedItemAction
        {
            Deselect,
            AnimatorBool,
            Nothing
        }

        public event UnityAction onUsed
        {
            add { m_OnUsed.AddListener(value); }
            remove { m_OnUsed.RemoveListener(value); }
        }

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
            if (addResult == InventoryAddResult.Full && m_QuickSlot >= 0)
                fpsInventory.SetSlotItem(m_QuickSlot, this);
        }

        public override void OnRemoveFromInventory()
        {
            slots.UnlockSelection(this);
            if (m_QuickSlot >= 0)
                fpsInventory.SetSlotItem(m_QuickSlot, null);
            base.OnRemoveFromInventory();
        }

        #region IQuickSlotItem implementation

        public IWieldable wieldable
        {
            get;
            private set;
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
            get
            {
                return m_State == State.Idle && !slots.isLocked && (slots.selected == null || slots.selected.wieldable == null || !slots.selected.wieldable.isBlocked) && CanUseWieldable();
            }
        }

        public void UseItem()
        {
            gameObject.SetActive(true);
        }

        public virtual void OnSelect() { }
        public virtual void OnDeselectInstant() { }
        public virtual Waitable OnDeselect() { return null; }

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

        private IModularFirearm m_Firearm = null;
        private IMeleeWeapon m_MeleeWeapon = null;
        private IThrownWeapon m_ThrownWeapon = null;
        private IWieldableTool m_WieldableTool = null;

        private State m_State = State.Idle;
        private float m_Timer = 0f;
        private Waitable m_Deselect = null;

        private Animator m_EquippedAnimator = null;
        private int m_AnimatorBoolHash = -1;

		[SerializeField, FormerlySerializedAs("preSelectPause"), Tooltip("The amount of time to wait before raising the weapon (this gives time for the currently equipped weapon to deselect).")]
        private float m_PreSelectPause = 0.25f;
		[SerializeField, FormerlySerializedAs("preFirePause"), Tooltip("The amount of time after raising the weapon before starting the primary fire.")]
        private float m_PreFirePause = 0.0f;
		[SerializeField, FormerlySerializedAs("triggerHoldDuration"), Tooltip("The length of time to hold the primary fire trigger for.")]
        private float m_TriggerHoldDuration = 0f;
		[SerializeField, FormerlySerializedAs("postFirePause"), Tooltip("The time to wait after the weapon fires or performs its action before deselecting.")]
        private float m_PostFirePause = 0.0f;
		[SerializeField, FormerlySerializedAs("waitForPostFireUnblock"), Tooltip("Should the quick use item also wait for the weapon to register that it has completed its action before deselecting.")]
        private bool m_WaitForPostFireUnblock = true;

        enum State
        {
            Idle,
            PreSelect,
            Selecting,
            PreFire,
            TriggerPress,
            TriggerHold,
            TriggerRelease,
            PostFire,
            Deselecting
        }

        protected override void Awake()
        {
            base.Awake();

            // Get the wieldable (and cast it)
            wieldable = GetComponent<IWieldable>();
            m_Firearm = wieldable as IModularFirearm;
            m_MeleeWeapon = wieldable as IMeleeWeapon;
            m_ThrownWeapon = wieldable as IThrownWeapon;
            m_WieldableTool = wieldable as IWieldableTool;

            if (!string.IsNullOrWhiteSpace(m_AnimatorBoolKey))
                m_AnimatorBoolHash = Animator.StringToHash(m_AnimatorBoolKey);
        }

        protected virtual bool CanUseWieldable()
        {
            if (m_Firearm != null)
                return m_Firearm.reloader == null || !m_Firearm.reloader.empty;
            return true;
        }

        protected void OnEnable()
        {
            if (slots != null)
            {
                switch(m_EquippedItemAction)
                {
                    case EquippedItemAction.Deselect:
                        {
                            slots.LockSelectionToNothing(this, true);
                        }
                        break;
                    case EquippedItemAction.AnimatorBool:
                        {
                            if (slots.selected != null) 
                            {
                                if (m_AnimatorBoolHash != -1)
                                {
                                    // Get the equipped weapon's animator
                                    m_EquippedAnimator = slots.selected.GetComponentInChildren<Animator>();
                                    if (m_EquippedAnimator != null)
                                    {
                                        // Set the bool parameter
                                        m_EquippedAnimator.SetBool(m_AnimatorBoolHash, true);
                                    }
                                }

                                // Lock the inventory selection
                                slots.LockSelectionToSlot(slots.selected.quickSlot, this);
                                slots.selected.wieldable.AddBlocker(this);
                            }
                        }
                        break;
                }

                m_State = State.PreSelect;
                transform.GetChild(0).gameObject.SetActive(false);
                m_Timer = 0f;
            }
        }

        protected void OnDisable()
        {
            m_State = State.Idle;

            if (slots != null)
            {
                switch (m_EquippedItemAction)
                {
                    case EquippedItemAction.Deselect:
                        {
                            // Unlock the selection
                            slots.UnlockSelection(this);
                        }
                        break;
                    case EquippedItemAction.AnimatorBool:
                        {
                            // Reset the bool parameter
                            if (m_EquippedAnimator != null)
                            {
                                m_EquippedAnimator.SetBool(m_AnimatorBoolHash, false);
                                m_EquippedAnimator = null;
                            }

                            // Unlock the inventory selection
                            if (slots.selected != null)
                            {
                                slots.UnlockSelection(this);
                                slots.selected.wieldable.RemoveBlocker(this);
                            }
                        }
                        break;
                }

                m_Timer = 0f;
            }
        }

        protected void FixedUpdate()
        {
            switch (m_State)
            {
                case State.PreSelect:
                    if (m_Timer > m_PreSelectPause)
                    {
                        m_Timer = 0f;
                        m_State = State.Selecting;
                        transform.GetChild(0).gameObject.SetActive(true);
                        wieldable.Select();
                    }
                    else
                        m_Timer += Time.deltaTime;
                    return;
                case State.Selecting:
                    if (!wieldable.isBlocked)
                        m_State = State.PreFire;
                    break;
                case State.PreFire:
                    if (m_Timer > m_PreFirePause)
                    {
                        m_Timer = 0f;
                        m_State = State.TriggerPress;
                    }
                    else
                        m_Timer += Time.deltaTime;
                    return;
                case State.TriggerPress:
                    if (m_Firearm != null)
                        m_Firearm.trigger.Press();
                    if (m_MeleeWeapon != null)
                        m_MeleeWeapon.PrimaryPress();
                    if (m_ThrownWeapon != null)
                        m_ThrownWeapon.ThrowHeavy();
                    if (m_WieldableTool != null)
                        m_WieldableTool.PrimaryPress();
                    m_State = State.TriggerHold;
                    return;
                case State.TriggerHold:
                    if (m_Timer > m_TriggerHoldDuration)
                    {
                        m_Timer = 0f;
                        m_State = State.TriggerRelease;
                    }
                    else
                        m_Timer += Time.deltaTime;
                    return;
                case State.TriggerRelease:
                    if (m_Firearm != null)
                        m_Firearm.trigger.Release();
                    if (m_MeleeWeapon != null)
                        m_MeleeWeapon.PrimaryRelease();
                    if (m_WieldableTool != null)
                        m_WieldableTool.PrimaryRelease();
                    m_State = State.PostFire;
                    return;
                case State.PostFire:
                    if (m_Timer > m_PostFirePause && !(m_WaitForPostFireUnblock && wieldable.isBlocked))
                    {
                        m_Timer = 0f;
                        m_State = State.Deselecting;
                        m_Deselect = wieldable.Deselect();
                    }
                    else
                        m_Timer += Time.deltaTime;
                    return;
                case State.Deselecting:
                    if (m_Deselect == null || m_Deselect.isComplete)
                        gameObject.SetActive(false);
                    return;
                default:
                    gameObject.SetActive(false);
                    return;
            }
        }
    }
}