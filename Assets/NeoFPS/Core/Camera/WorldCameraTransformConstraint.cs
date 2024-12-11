using NeoFPS.SinglePlayer;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    public class WorldCameraTransformConstraint : FpsInput, IFirstPersonCameraPositionConstraint, IFirstPersonCameraRotationConstraint
    {
        [SerializeField, Tooltip("The target transform of the constraint.")]
        private Transform m_TargetTransform = null;
        [SerializeField, Tooltip("The time taken to blend the character camera to this constraint's target position and rotation.")]
        private float m_BlendIn = 0.5f;
        [SerializeField, Tooltip("The time taken to blend the character camera out from this constraint's target position / rotation to its neutral position / rotation on the character.")]
        private float m_BlendOut = 0.5f;
        [SerializeField, Tooltip("Higher priority constraints will override lower priority ones.")]
        private int m_Priority = 1000;

        [Header("Inventory")]

        [SerializeField, Tooltip("Should the player character's weapon be lowered while the camera constraint is active.")]
        private bool m_LowerWeapon = true;
        [SerializeField, Tooltip("The time taken to blend the character camera out from this constraint's target position / rotation to its neutral position / rotation on the character.")]
        private float m_InventoryDelay = 0.5f;

        [Header("UI")]

        [SerializeField, Tooltip("Should the mouse cursor be visible.")]
        private bool m_ShowCursor = false;
        [SerializeField, Tooltip("Should the in game HUD be hidden while the camera constraint is active.")]
        private bool m_HideHUD = true;

        [Header("Events")]

        [SerializeField, Tooltip("An event fired when the camera constraint is applied.")]
        private UnityEvent m_OnShow = new UnityEvent();
        [SerializeField, Tooltip("An event fired when the camera constraint is removed.")]
        private UnityEvent m_OnHide = new UnityEvent();

        private FirstPersonCameraTransformConstraints m_Constraints = null;
        private IQuickSlots m_QuickSlots = null;
        private Coroutine m_ActiveCoroutine = null;
        private bool m_PreviousMouseCursor = false;

        public event UnityAction onShow
        {
            add { m_OnShow.AddListener(value); }
            remove { m_OnShow.RemoveListener(value); }
        }

        public event UnityAction onHide
        {
            add { m_OnHide.AddListener(value); }
            remove { m_OnHide.RemoveListener(value); }
        }

        public Object owner { get { return this; } }

        public override FpsInputContext inputContext
        {
            get { return FpsInputContext.Cutscene; }
        }

        public float positionStrength
        {
            get { return 1f; }
        }

        public float rotationStrength
        {
            get { return 1f; }
        }

        public bool positionConstraintActive
        {
            get;
            set;
        }

        public bool rotationConstraintActive
        {
            get;
            set;
        }

        protected void Reset()
        {
            m_TargetTransform = transform;
        }

        protected override void OnAwake()
        {
            if (m_TargetTransform == null)
                m_TargetTransform = transform;
        }

        protected override void OnEnable()
        {
            // Removed base to control push/pop of input context
        }

        protected override void OnDisable()
        {
            // Remove to control push/pop of input context

            RemoveConstraintInstant();
        }

        public void ApplyConstraint()
        {
            // Remove the old constraints if required
            if (m_Constraints != null)
                RemoveConstraintInstant();

            // Get the current character's camera constraints
            var character = FpsSoloCharacter.localPlayerCharacter;
            if (character != null)
                m_Constraints = character.fpCamera.GetComponentInParent<FirstPersonCameraTransformConstraints>();

            if (m_Constraints != null)
            {
                // Push the input context to block input
                PushContext();

                // Lower the weapon and wait before applying constraints
                if (m_LowerWeapon)
                {
                    m_QuickSlots = character.quickSlots;
                    if (m_QuickSlots != null)
                        m_ActiveCoroutine = StartCoroutine(LowerWeaponThenApplyConstraints());
                }
                else
                {
                    // No need to wait
                    FinishApplyingConstraints();
                }
            }
        }

        void FinishApplyingConstraints()
        {
            // Add the constraints
            m_Constraints.AddPositionConstraint(this, m_Priority, m_BlendIn);
            m_Constraints.AddRotationConstraint(this, m_Priority, m_BlendIn);

            // Hide the HUD
            if (m_HideHUD)
                HudHider.HideHUD();

            // Fire event
            m_OnShow.Invoke();
        }

        public void RemoveConstraintInstant()
        {
            RemoveConstraintInternal(true);
        }

        public void RemoveConstraint()
        {
            RemoveConstraintInternal(false);
        }

        void RemoveConstraintInternal(bool instant)
        {
            if (m_ActiveCoroutine != null)
            {
                StopCoroutine(m_ActiveCoroutine);
                m_ActiveCoroutine = null;
            }

            if (m_Constraints != null)
            {
                // Remove the constraints
                m_Constraints.RemovePositionConstraint(this, m_BlendOut);
                m_Constraints.RemoveRotationConstraint(this, m_BlendOut);
                m_Constraints = null;

                // Fire event
                m_OnHide.Invoke();

                // Show the HUD
                if (m_HideHUD)
                    HudHider.ShowHUD();

                // Unlock the inventory selection
                if (m_QuickSlots != null)
                {
                    if (isActiveAndEnabled && !instant)
                        m_ActiveCoroutine = StartCoroutine(RemoveConstraintsThenRaiseWeapon());
                    else
                    {
                        m_QuickSlots.UnlockSelection(this);
                        m_QuickSlots = null;
                    }
                }

                PopContext();
            }
        }

        IEnumerator LowerWeaponThenApplyConstraints()
        {
            m_QuickSlots.LockSelectionToNothing(this, false);

            yield return new WaitForSeconds(m_InventoryDelay);

            FinishApplyingConstraints();

            m_ActiveCoroutine = null;
        }

        IEnumerator RemoveConstraintsThenRaiseWeapon()
        {
            yield return new WaitForSeconds(m_BlendOut);

            m_QuickSlots.UnlockSelection(this);
            m_QuickSlots = null;

            m_ActiveCoroutine = null;
        }

        public Vector3 GetConstraintPosition(Transform relativeTo)
        {
            return relativeTo.InverseTransformPoint(m_TargetTransform.position);
        }

        public Quaternion GetConstraintRotation(Transform relativeTo)
        {
            return Quaternion.Inverse(relativeTo.rotation) * m_TargetTransform.rotation;
        }

        protected override void OnGainFocus()
        {
            base.OnGainFocus();
            m_PreviousMouseCursor = NeoFpsInputManager.captureMouseCursor;
            NeoFpsInputManager.captureMouseCursor = !m_ShowCursor;
        }

        protected override void OnLoseFocus()
        {
            base.OnLoseFocus();
            NeoFpsInputManager.captureMouseCursor = m_PreviousMouseCursor;
        }

        protected override void UpdateInput()
        {
            // Required to get around a bug where disabling character motion input always shows cursor
            // Needs refactoring for 1.2
            if (NeoFpsInputManager.captureMouseCursor == false && !m_ShowCursor)
                NeoFpsInputManager.captureMouseCursor = true;

            // Could add a cancel / skip here
        }
    }
}