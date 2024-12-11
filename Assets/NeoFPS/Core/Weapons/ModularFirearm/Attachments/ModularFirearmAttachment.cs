using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS.ModularFirearms
{
    [DisallowMultipleComponent]
    public abstract class ModularFirearmAttachment : MonoBehaviour
    {
        [SerializeField, Tooltip("The name that will be used in an attachments UI where the type is unspecified (eg \"High Capacity Magazine\").")]
        private string m_DisplayName = "Unnamed Attachment";
        [SerializeField, Tooltip("A shorter name that might be used in a list where the type is already known (eg \"High Capacity\" when the list is all magazines).")]
        private string m_ListName = "Unnamed";
        [HideInInspector, SerializeField]
        private string m_Guid = string.Empty;

        private Guid m_AttachmentGuid = Guid.Empty;

        public Guid attachmentID
        {
            get
            {
                if (m_AttachmentGuid == Guid.Empty)
                    m_AttachmentGuid = Guid.Parse(m_Guid);
                return m_AttachmentGuid;
            }
        }

        public string displayName
        {
            get { return m_DisplayName; }
        }

        public string listName
        {
            get { return m_ListName; }
        }

        protected void OnValidate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                string guid = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(this));
                if (!string.IsNullOrEmpty(guid) && m_Guid != guid)
                    m_Guid = guid;
            }
#endif
        }

        public abstract bool CheckRequirements(ModularFirearmAttachmentSystem attachmentSystem);
        public virtual void OnConnectedToSocket(ModularFirearmAttachmentSocket socket) { }
        public virtual void OnDisconnectedFromSocket(ModularFirearmAttachmentSocket socket) { }
    }
}