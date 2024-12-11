using System;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/interactionref-mb-inventoryinteractionhandler.html")]
    [RequireComponent(typeof(CharacterInteractionHandler))]
    public class InventoryInteractionHandler : MonoBehaviour
    {
        [SerializeField, Tooltip("What to do when the character interacts with an object")]
        private OnInteract m_OnInteract = OnInteract.SelectNothing;

        private IQuickSlots m_QuickSlots = null;
        private bool m_Interacting = false;

        public enum OnInteract
        {
            SelectNothing,
            SelectHands
        }

        private void Awake()
        {
            m_QuickSlots = GetComponent<IQuickSlots>();
            if (m_QuickSlots != null)
            {
                var interactionHandler = GetComponent<CharacterInteractionHandler>();
                interactionHandler.onInteractionStarted += OnInteractionStarted;
                interactionHandler.onInteractionSucceeded += OnInteractionEnded;
                interactionHandler.onInteractionCancelled += OnInteractionEnded;
            }
        }

        private void OnInteractionStarted(ICharacter character, IInteractiveObject interactable, float delay)
        {
            m_Interacting = true;
            if (m_OnInteract == OnInteract.SelectHands)
                m_QuickSlots.LockSelectionToBackupItem(this, false);
            else
                m_QuickSlots.LockSelectionToNothing(this, false);
        }

        private void OnInteractionEnded(ICharacter character, IInteractiveObject interactable)
        {
            if (m_Interacting)
            {
                m_Interacting = false;
                m_QuickSlots.UnlockSelection(this);
            }
        }
    }
}
