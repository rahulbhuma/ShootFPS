using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using NeoFPS.Constants;
using UnityEngine.Events;
using NeoSaveGames.Serialization;
using NeoSaveGames;

namespace NeoFPS
{
	[HelpURL("https://docs.neofps.com/manual/inventoryref-mb-inventoryselectionanimator.html")]
	public class InventorySelectionAnimator : MonoBehaviour
	{
		[SerializeField, Tooltip("The character's animator")]
		private Animator m_Animator = null;

		[SerializeField, Tooltip("The name of an integer parameter in the character's animator controller that should be set with the quick slot number on selection changed.")]
		private string m_SlotParameter = "quickSlot";

		private int m_SlotHash = 0;

		protected Animator animator
        {
			get { return m_Animator; }
        }

        private void Awake()
        {
			if (m_Animator != null && !string.IsNullOrWhiteSpace(m_SlotParameter))
			{
				var qs = GetComponent<IQuickSlots>();
				if (qs != null)
				{
					qs.onSelectionChanged += OnSelectionChanged;
					m_SlotHash = Animator.StringToHash(m_SlotParameter);
				}
			}
        }

        protected virtual void OnSelectionChanged(int slotID, IQuickSlotItem item)
        {
			m_Animator.SetInteger(m_SlotHash, slotID);

			// Override this to do things like setting a parameter based on item type or some component on the item
		}
    }
}
