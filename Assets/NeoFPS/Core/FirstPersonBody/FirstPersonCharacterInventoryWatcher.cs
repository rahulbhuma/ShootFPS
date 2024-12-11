using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [RequireComponent (typeof (Animator))]
    public class FirstPersonCharacterInventoryWatcher : MonoBehaviour
    {
        private Animator m_Animator = null;

        public AnimatorOverrideController overrideController
        {
            get;
            private set;
        }

        protected void Awake()
        {
            // Get animator
            m_Animator = GetComponent<Animator>();

            // Check character animator is not already using an override controller
            if (m_Animator.runtimeAnimatorController is AnimatorOverrideController)
            {
                Debug.LogError("Attempting to use the first person character wieldable animation overrides when your character already has an override controller set. This is not supported", gameObject);
            }
            else
            {
                // Apply override controller
                overrideController = new AnimatorOverrideController(m_Animator.runtimeAnimatorController);
                m_Animator.runtimeAnimatorController = overrideController;

                // Register with wieldable selection changed event
                var quickSlots = GetComponentInParent<IQuickSlots>();
                if (quickSlots != null)
                {
                    quickSlots.onSelectionChanged += OnItemSelectionChanged;
                    OnItemSelectionChanged(0, quickSlots.selected);
                }
            }
        }

        private void OnItemSelectionChanged(int quickSlot, IQuickSlotItem wieldable)
        {
            // Get overrides from wieldable
            WieldableItemBodyAnimOverrides overrides = null;
            if (wieldable as Component != null)
                overrides = wieldable.GetComponent<WieldableItemBodyAnimOverrides>();

            // Apply overrides
            if (overrides != null)
                overrideController.ApplyOverrides(overrides.overrides);
        }
    }
}
