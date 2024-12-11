using NeoSaveGames;
using NeoSaveGames.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS.ModularFirearms
{
    [DisallowMultipleComponent]
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-modularfirearmattachmentsocket.html")]
    public class ModularFirearmAttachmentSocket : MonoBehaviour, INeoSerializableComponent
    {
        [SerializeField, Tooltip("The name for this socket (eg Optics or Muzzle). This will be used to identify the socket to the attachment system, but could also be used in any attachment UI.")]
        private string m_SocketName = "Socket";

        [Header("Attachments")]

        [SerializeField, Tooltip("Where is the default attachment taken from.")]
        private DefaultAttachmentSource m_DefaultAttachmentSource = DefaultAttachmentSource.None;
        [SerializeField, Tooltip("The default attachment index from the attachmnet asset above")]
        private int m_DefaultAttachment = 0;
        [SerializeField, Tooltip("A reusable group of attachments that can be attached to this socket. Will be appended to the attachments list below.")]
        private ModularFirearmAttachmentGroup m_AttachmentGroup = null;
        [SerializeField, Tooltip("An array of available attachments. The attachments from the attachment group above will be appended to this.")]
        private AttachmentOption[] m_Attachments = { };

        [Header("Contents Change")]

        [SerializeField, Tooltip("(Optional) An object that will be active when the attachment slot is filled. Examples would be a mounting rail or folded iron sights.")]
        private GameObject m_SocketFilledObject;
        [SerializeField, Tooltip("(Optional) An object that will be active when the attachement slot is empty. This could be a fallback attachment if required.")]
        private GameObject m_SocketEmptyObject;
        [SerializeField, Tooltip("The display name that should be used for the attachment when the socket is empty (null attachmnet assigned).")]
        private string m_NullAttachmentName = "Empty";
        [SerializeField, Tooltip("An event fired when the attachment is changed.")]
        private AttachmentChangedEvent m_OnAttachmentChanged;

        private static List<AttachmentOption> s_CombinedAttachments = new List<AttachmentOption>(16);

        private NeoSerializedGameObject m_NSGO = null;
        private bool m_HasNull = false;
        private bool m_Initialised = false;
        private int m_AdjustedDefault = -1;

        private enum DefaultAttachmentSource
        {
            None,
            This,
            Group,
            Random
        }

        public string socketName
        {
            get { return m_SocketName; }
        }

        public ModularFirearmAttachmentSystem attachmentSystem
        {
            get;
            private set;
        }

        public string nullAttachmentName
        {
            get { return m_NullAttachmentName; }
        }

        public int numAttachments
        {
            get { return m_Attachments.Length; }
        }

        public int currentAttachmentIndex
        {
            get;
            private set;
        }

        public ModularFirearmAttachment currentAttachment
        {
            get;
            private set;
        }

        public event UnityAction<ModularFirearmAttachment> onAttachmentChanged
        {
            add { m_OnAttachmentChanged.AddListener(value); }
            remove { m_OnAttachmentChanged.RemoveListener(value); }
        }

        protected void Awake()
        {
            currentAttachmentIndex = -1;

            m_NSGO = GetComponent<NeoSerializedGameObject>();

            // Filter and compile attachments to local array...

            bool dirty = false;

            dirty |= CompileAttachments(m_Attachments, false, m_DefaultAttachmentSource == DefaultAttachmentSource.This);
            if (m_AttachmentGroup != null)
                dirty |= CompileAttachments(m_AttachmentGroup.attachments, true, m_DefaultAttachmentSource == DefaultAttachmentSource.Group);

            if (m_Attachments.Length != s_CombinedAttachments.Count)
                m_Attachments = new AttachmentOption[s_CombinedAttachments.Count];

            if (dirty)
            {
                for (int i = 0; i < s_CombinedAttachments.Count; ++i)
                    m_Attachments[i] = s_CombinedAttachments[i];
            }

            s_CombinedAttachments.Clear();

            // Get the parent attachment system
            if (attachmentSystem == null)
                attachmentSystem = GetComponentInParent<ModularFirearmAttachmentSystem>();

            // Register with system
            if (attachmentSystem == null)
                enabled = false;
        }

        protected void Start()
        {
            attachmentSystem.RegisterSocket(this);

            if (!m_Initialised)
            {
                var existing = GetComponentInChildren<ModularFirearmAttachment>();
                if (existing != null)
                {
                    currentAttachment = existing;
                    currentAttachmentIndex = GetIndexForAttachment(existing);
                }
                else
                {
                    ApplyStartingAttachment();
                }

                m_Initialised = true;

                // React to socket empty / filled
                if (m_SocketFilledObject != null)
                    m_SocketFilledObject.SetActive(currentAttachment != null);
                if (m_SocketEmptyObject != null)
                    m_SocketEmptyObject.SetActive(currentAttachment == null);
            }
        }

        protected void OnDestroy()
        {
            // Unregister from system (?)
            if (attachmentSystem != null)
                attachmentSystem.UnregisterSocket(this);
        }

        protected virtual void ApplyStartingAttachment()
        {
            if (m_DefaultAttachmentSource == DefaultAttachmentSource.Random && m_Attachments.Length > 0)
            {
                AttachByIndex(UnityEngine.Random.Range(0, m_Attachments.Length));
            }
            else
            {
                if (m_AdjustedDefault != -1)
                    AttachByIndex(m_AdjustedDefault);
            }
        }

        bool CompileAttachments(AttachmentOption[] attachments, bool isGroup, bool defaultSource)
        {
            bool dirty = false;
            for (int i = 0; i < attachments.Length; ++i)
            {
                // Special case for null handling
                if (attachments[i].attachment == null)
                {
                    if (!m_HasNull)
                    {
                        // Record adjusted default index
                        if (defaultSource && i == m_DefaultAttachment)
                            m_AdjustedDefault = s_CombinedAttachments.Count;

                        // Add
                        s_CombinedAttachments.Add(new AttachmentOption());

                        m_HasNull = true;
                    }
                    else
                    {
                        dirty = true;
                    }
                }
                else
                {
                    bool found = CombinedAttachmentsContains(attachments[i].attachment);
                    if (found)
                    {
                        // Dirty if it's a duplicate in local attachments
                        dirty |= !isGroup;
                    }
                    else
                    {
                        // Dirty if it's not included in local attachments
                        dirty |= isGroup;

                        // Record adjusted default index
                        if (defaultSource && i == m_DefaultAttachment)
                            m_AdjustedDefault = s_CombinedAttachments.Count;

                        // Add
                        s_CombinedAttachments.Add(attachments[i]);
                    }
                }
            }
            return dirty;
        }

        bool CombinedAttachmentsContains(ModularFirearmAttachment attachment)
        {
            for (int i = 0; i < s_CombinedAttachments.Count; ++i)
            {
                if (s_CombinedAttachments[i].attachment == attachment)
                    return true;
            }
            return false;
        }

        public void GetFilteredAttachments(List<ModularFirearmAttachment> output)
        {
            for (int i = 0; i < m_Attachments.Length; ++i)
            {
                if (m_Attachments[i].attachment == null || m_Attachments[i].attachment.CheckRequirements(attachmentSystem))
                    output.Add(m_Attachments[i].attachment);
            }
        }

        public virtual void OnSocketConnected()
        {
        }

        public virtual void OnSocketDisconnected()
        {
        }

        public void RemoveAttachment()
        {
            if (currentAttachment != null)
            {
                // Signal attachment change pending
                PreAttachmentChange(currentAttachment, true);

                // Signal disconnected
                currentAttachment.OnDisconnectedFromSocket(this);

                // Destroy the existing attachment
                Destroy(currentAttachment.gameObject);

                // Reset properties
                currentAttachment = null;
                currentAttachmentIndex = -1;

                // Signal change
                OnAttachmentChanged(null, false);
            }

            m_Initialised = true;
        }

        public ModularFirearmAttachment GetAttachmentAtIndex(int index)
        {
            return m_Attachments[index].attachment;
        }

        int GetIndexForAttachment(ModularFirearmAttachment attachmentOrPrefab)
        {
            if (attachmentOrPrefab == null)
                return -1;

            for (int i = 0; i < m_Attachments.Length; ++i)
            {
                if (m_Attachments[i].attachment != null && m_Attachments[i].attachment.attachmentID == attachmentOrPrefab.attachmentID)
                    return i;
            }

            return -1;
        }

        public bool CanAttach(ModularFirearmAttachment attachmentPrefab)
        {
            if (attachmentPrefab == null)
                return false;

            for (int i = 0; i < m_Attachments.Length; ++i)
            {
                if (m_Attachments[i].attachment != null && m_Attachments[i].attachment.attachmentID == attachmentPrefab.attachmentID)
                    return true;
            }

            return false;
        }

        public bool Attach (int index)
        {
            if (index < 0 || index >= m_Attachments.Length)
                return false;

            AttachByIndex(index);
            return true;
        }

        public bool Attach(Guid guid)
        {
            for (int i = 0; i < m_Attachments.Length; ++i)
            {
                var attachment = m_Attachments[i].attachment;
                if (attachment != null && attachment.attachmentID == guid)
                {
                    AttachByIndex(i);
                    return true;
                }
            }

            return false;
        }

        public bool Attach(ModularFirearmAttachment attachmentPrefab, Vector3 offset)
        {
            bool canAttach = CanAttach(attachmentPrefab);

            if (canAttach)
                AttachInternal(attachmentPrefab, offset);

            currentAttachmentIndex = GetIndexForAttachment(attachmentPrefab);

            return canAttach;
        }

        void AttachInternal(ModularFirearmAttachment attachmentPrefab, Vector3 offset)
        {
            bool changed = false;
            bool wasNull = false;
            ModularFirearmAttachment instance = null;
            if (currentAttachment != null)
            {
                if (attachmentPrefab == null)
                {
                    // Signal disconnected and destroy old one
                    currentAttachment.OnDisconnectedFromSocket(this);
                    currentAttachment.gameObject.SetActive(false);

                    // Signal attachment change pending
                    PreAttachmentChange(currentAttachment, true);

                    // Destroy the old one
                    Destroy(currentAttachment.gameObject);

                    // Reset properties
                    currentAttachment = null;

                    changed = true;
                }   
                else
                {
                    if (currentAttachment.attachmentID != attachmentPrefab.attachmentID)
                    {
                        // Signal disconnected and disable old one
                        currentAttachment.OnDisconnectedFromSocket(this);
                        currentAttachment.gameObject.SetActive(false);

                        // Signal attachment change pending
                        PreAttachmentChange(currentAttachment, false);

                        // Instantiate the new one
                        instance = InstantiateAttachment(attachmentPrefab, offset);

                        // Destroy the old one
                        Destroy(currentAttachment.gameObject);

                        // Connect
                        currentAttachment = instance;
                        currentAttachment.OnConnectedToSocket(this);

                        changed = true;
                    }
                }
            }
            else
            {
                wasNull = true;

                if (attachmentPrefab != null)
                {
                    // Signal attachment change pending
                    PreAttachmentChange(null, false);

                    // Instantiate the new one
                    instance = InstantiateAttachment(attachmentPrefab, offset);

                    // Connect
                    currentAttachment = instance;
                    currentAttachment.OnConnectedToSocket(this);

                    changed = true;
                }
            }

            if (changed)
                OnAttachmentChanged(instance, wasNull);

            if (!m_Initialised)
                m_Initialised = true;
        }

        void AttachByIndex(int index)
        {
            AttachInternal(m_Attachments[index].attachment, m_Attachments[index].offset);
            currentAttachmentIndex = index;
        }

        protected virtual ModularFirearmAttachment InstantiateAttachment(ModularFirearmAttachment attachmentPrefab, Vector3 offset)
        {
            // Get the target (world) position / rotation
            var rotation = transform.rotation;
            var position = transform.TransformPoint(offset);

            // Instantiate
            ModularFirearmAttachment instance;
            if (m_NSGO != null)
                instance = m_NSGO.InstantiatePrefab(attachmentPrefab, position, rotation);
            else
                instance = Instantiate(attachmentPrefab, position, rotation, transform);

            // Reset scale
            instance.transform.localScale = Vector3.one;

            // Get the index
            int index = -1;
            for (int i = 0; i < m_Attachments.Length; ++i)
            {
                if (m_Attachments[i].attachment == attachmentPrefab)
                {
                    index = i;
                    break;
                }
            }
            currentAttachmentIndex = index;

            return instance;
        }

        public void TransferAttachmentTo(ModularFirearmAttachmentSocket other)
        {
            other.RemoveAttachment();

            if (currentAttachment != null)
            {
                // Get the transform and current offset
                var attachmentTransform = currentAttachment.transform;
                var offset = attachmentTransform.localPosition;

                // Signal disconnected
                currentAttachment.OnDisconnectedFromSocket(this);

                // Change parent (save system compatible)
                bool transferred = false;
                if (m_NSGO != null)
                {
                    var attachmentNSGO = currentAttachment.GetComponent<NeoSerializedGameObject>();
                    var otherNSGO = other.GetComponent<NeoSerializedGameObject>();
                    if (otherNSGO != null && attachmentNSGO != null)
                    {
                        attachmentNSGO.SetParent(otherNSGO);
                        transferred = true;
                    }
                }

                // Remove other attachment
                bool otherWasNull = true;
                other.PreAttachmentChange(other.currentAttachment, false);
                if (other.currentAttachment != null)
                {
                    other.currentAttachment.OnDisconnectedFromSocket(other);
                    Destroy(other.gameObject);
                    otherWasNull = false;
                }

                // Change parent (fallback)
                if (!transferred)
                    attachmentTransform.SetParent(other.transform);

                // Reapply offset
                attachmentTransform.localPosition = offset;
                attachmentTransform.localRotation = Quaternion.identity;
                attachmentTransform.localScale = Vector3.one;

                // Set the attachment
                other.currentAttachment = currentAttachment;
                other.currentAttachmentIndex = other.GetIndexForAttachment(currentAttachment);

                // Signal connected
                currentAttachment.OnConnectedToSocket(other);
                other.OnAttachmentChanged(currentAttachment, otherWasNull);

                // Reset the attachment here
                currentAttachment = null;
                currentAttachmentIndex = -1;
                OnAttachmentChanged(null, false);
            }
        }

        protected virtual void PreAttachmentChange(ModularFirearmAttachment from, bool toNull)
        {
            // React if socket is about to be filled
            // NB: This is done before in case the filled object has a module that will be replaced
            if (m_SocketFilledObject != null)
                m_SocketFilledObject.SetActive(!toNull);
        }

        protected virtual void OnAttachmentChanged(ModularFirearmAttachment attachment, bool wasNull)
        {
            m_OnAttachmentChanged.Invoke(attachment);

            // React to socket empty / not
            // NB: This is done after in case it means adding a firearm module
            if (m_SocketEmptyObject != null)
                m_SocketEmptyObject.SetActive(attachment == null);
        }

        private static readonly NeoSerializationKey k_HasAttachmentKey = new NeoSerializationKey("hasAttachment");

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            if (m_Initialised)
                writer.WriteValue(k_HasAttachmentKey, currentAttachment != null);
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            if (reader.TryReadValue(k_HasAttachmentKey, out bool hasAttachment, false))
            {
                if (hasAttachment)
                {
                    currentAttachment = GetComponentInChildren<ModularFirearmAttachment>();
                    currentAttachmentIndex = GetIndexForAttachment(currentAttachment);

                    // React to socket empty / filled
                    if (m_SocketFilledObject != null)
                        m_SocketFilledObject.SetActive(currentAttachment != null);
                    if (m_SocketEmptyObject != null)
                        m_SocketEmptyObject.SetActive(currentAttachment == null);

                    m_Initialised = true;
                }
                else
                    RemoveAttachment();
            }
        }
    }
}