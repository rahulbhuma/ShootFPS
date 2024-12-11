using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS.ModularFirearms
{
    [DisallowMultipleComponent]
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-modularfirearmattachmentsystem.html")]
    [RequireComponent(typeof(ModularFirearm))]
    public class ModularFirearmAttachmentSystem : MonoBehaviour
    {
        [SerializeField, Tooltip("An event fired whenever one of the attachment system's sockets change attachment.")]
        private UnityEvent m_OnAttachmentChanged = null;

        List<ModularFirearmAttachmentSocket> m_Sockets = new List<ModularFirearmAttachmentSocket>();

        public event UnityAction onSocketsChanged;

        public event UnityAction onAttachmentChanged
        {
            add { m_OnAttachmentChanged.AddListener(value); }
            remove { m_OnAttachmentChanged.RemoveListener(value); }
        }

        public ModularFirearm firearm
        {
            get;
            private set;
        }

        public int numSockets
        {
            get { return m_Sockets.Count; }
        }

        protected void Awake()
        {
            firearm = GetComponent<ModularFirearm>();
        }

        public bool TryAttach(string socketName, ModularFirearmAttachment attachmentPrefab)
        {
            if (socketName == string.Empty)
                return TryAttach(attachmentPrefab);
            else
            {
                if (attachmentPrefab == null)
                    return false;

                int index = GetSocketIndex(socketName);
                if (index != -1 && m_Sockets[index].CanAttach(attachmentPrefab))
                    return m_Sockets[index].Attach(attachmentPrefab.attachmentID);

                return false;
            }
        }

        public bool TryAttach(ModularFirearmAttachment attachmentPrefab)
        {
            if (attachmentPrefab == null)
                return false;

            // Check slot index is within range
            // Check attachment is one of valid items
            var socket = GetValidSocketForAttachment(attachmentPrefab);
            if (socket != null)
                return socket.Attach(attachmentPrefab.attachmentID);
            else
                return false;
        }

        public void RegisterSocket(ModularFirearmAttachmentSocket socket)
        {
            if (socket == null)
                return;

            int existing = GetSocketIndex(socket.socketName);
            if (existing  != -1)
            {
                m_Sockets[existing].onAttachmentChanged -= OnSocketAttachmentChanged;
                m_Sockets[existing].TransferAttachmentTo(socket);
                m_Sockets[existing].OnSocketDisconnected();
                m_Sockets[existing] = socket;
                socket.OnSocketConnected();
                socket.onAttachmentChanged += OnSocketAttachmentChanged;
            }
            else
            {
                m_Sockets.Add(socket);
                socket.onAttachmentChanged += OnSocketAttachmentChanged;
            }

            // Fire sockets changed event
            onSocketsChanged?.Invoke();
        }

        public void UnregisterSocket(ModularFirearmAttachmentSocket socket)
        {
            if (socket == null)
                return;

            int existing = GetSocketIndex(socket.socketName);
            if (existing != -1 && m_Sockets[existing] == socket)
            {
                int last = m_Sockets.Count - 1;
                //m_Sockets[existing].RemoveAttachment(); // Needed???
                m_Sockets[existing].onAttachmentChanged -= OnSocketAttachmentChanged;
                m_Sockets[existing].OnSocketDisconnected();
                m_Sockets[existing] = m_Sockets[last];
                m_Sockets.RemoveAt(last);

                // Fire sockets changed event
                onSocketsChanged?.Invoke();
            }
        }

        public bool IsSocketRegistered(ModularFirearmAttachmentSocket socket)
        {
            for (int i = 0; i < m_Sockets.Count; ++i)
            {
                if (m_Sockets[i] == socket)
                    return true;
            }

            return false;
        }

        public ModularFirearmAttachmentSocket GetSocket(int index)
        {
            return m_Sockets[index];
        }

        public ModularFirearmAttachmentSocket GetSocket(string socketName)
        {
            int index = GetSocketIndex(socketName);
            if (index != -1)
                return GetSocket(index);
            else
                return null;
        }

        int GetSocketIndex(string socketName)
        {
            for (int i = 0; i < m_Sockets.Count; ++i)
            {
                if (string.CompareOrdinal(m_Sockets[i].socketName, socketName) == 0)
                    return i;
            }
            return -1;
        }

        void OnSocketAttachmentChanged(ModularFirearmAttachment attachment)
        {
            m_OnAttachmentChanged.Invoke();
        }

        public ModularFirearmAttachmentSocket GetValidSocketForAttachment(ModularFirearmAttachment attachmentPrefab)
        {
            for (int i = 0; i < m_Sockets.Count; ++i)
            {
                if (m_Sockets[i].CanAttach(attachmentPrefab))
                    return m_Sockets[i];
            }
            return null;
        }

        public bool CanAttach(ModularFirearmAttachment attachmentPrefab)
        {
            return GetValidSocketForAttachment(attachmentPrefab) != null && attachmentPrefab.CheckRequirements(this);
        }

        public bool CanAttach(string socketName, ModularFirearmAttachment attachmentPrefab)
        {
            int index = GetSocketIndex(socketName);
            return index != -1 && m_Sockets[index].CanAttach(attachmentPrefab) && attachmentPrefab.CheckRequirements(this);
        }
    }
}