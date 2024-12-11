using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS.ModularFirearms
{
    [DisallowMultipleComponent]
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-modularfirearmattachment.html")]
    public class StandardModularFirearmAttachment : ModularFirearmAttachment
    {
        [SerializeField, Tooltip("(Optional) If this is set then the character must have an instance of this item (or an item with matching inventory ID) in their inventory before attaching to their weapon.")]
        private FpsInventoryItemBase m_RequiredInventoryItem = null;

        private IInventory m_Inventory = null;
        private ModularFirearmAttachmentSocket m_Socket = null;

        public override bool CheckRequirements(ModularFirearmAttachmentSystem attachmentSystem)
        {
            // Passes if there's no inventory requirement
            if (m_RequiredInventoryItem == null)
                return true;

            // Get the wielder's inventory (passes if wielder doesn't have one)
            var inventory = attachmentSystem.firearm.wielder?.GetComponent<IInventory>();
            if (inventory == null)
                return true;

            // Check if inventory contains the item
            var item = inventory.GetItem(m_RequiredInventoryItem.itemIdentifier);
            return item != null && item.quantity > 0;
        }

        public override void OnConnectedToSocket(ModularFirearmAttachmentSocket socket)
        {
            base.OnConnectedToSocket(socket);

            if (m_RequiredInventoryItem == null)
                return;

            m_Inventory = socket.attachmentSystem.firearm.wielder?.GetComponent<IInventory>();
            if (m_Inventory == null)
                return;

            m_Socket = socket;
            m_Inventory.onItemRemoved += OnItemRemoved;
        }

        public override void OnDisconnectedFromSocket(ModularFirearmAttachmentSocket socket)
        {
            base.OnDisconnectedFromSocket(socket);

            if (m_Inventory != null)
                m_Inventory.onItemRemoved -= OnItemRemoved;
            m_Inventory = null;

            m_Socket = null;
        }

        void OnItemRemoved(IInventoryItem item)
        {
            if (item.itemIdentifier == m_RequiredInventoryItem.itemIdentifier)
                m_Socket.RemoveAttachment();
        }
    }
}