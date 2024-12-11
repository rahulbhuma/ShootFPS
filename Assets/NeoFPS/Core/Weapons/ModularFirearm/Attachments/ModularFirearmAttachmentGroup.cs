using System;
using UnityEngine;

namespace NeoFPS.ModularFirearms
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-so-modularfirearmattachmentgroup.html")]
    [CreateAssetMenu(fileName = "AttachmentGroup_New", menuName = "NeoFPS/Inventory/Firearm Attachment Group", order = NeoFpsMenuPriorities.inventory_attachmentGroup)]
    public class ModularFirearmAttachmentGroup : ScriptableObject
    {
        [SerializeField, Tooltip("The attachment prefabs in this group along with position offsets from their socket (local space).")]
        private AttachmentOption[] m_Attachments = { };

        public AttachmentOption[] attachments
        {
            get { return m_Attachments; }
        }
    }

    [Serializable]
    public struct AttachmentOption
    {
        public ModularFirearmAttachment attachment;
        public Vector3 offset;
    }
}