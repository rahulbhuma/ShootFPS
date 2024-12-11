using NeoFPS.Samples;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeoFPS.ModularFirearms
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-modularfirearmattachmentuipopup.html")]
    public class ModularFirearmAttachmentUIPopup : ModularFirearmAttachmentUIPopupBase
    {
        [SerializeField, Tooltip("The picker UI element used to select the attachment (should be a child of this object)")]
        private MultiInputMultiChoice m_SocketPickerPrototype = null;

        [SerializeField, Tooltip("The button UI element used to close the popup (should be a child of this object).")]
        private MultiInputButton m_CompletedButton = null;

        private List<SocketUI> m_SocketEntries = new List<SocketUI>();
        private Transform m_ContainerTransform = null;
        private Dictionary<string, Guid> m_Memory = new Dictionary<string, Guid>();

        private class SocketUI
        {
            public ModularFirearmAttachmentSystem attachmentSystem = null;
            public ModularFirearmAttachmentSocket socket = null;
            public MultiInputMultiChoice picker = null;
            public string sortString = string.Empty;

            private List<ModularFirearmAttachment> m_FilteredAttachmnents = new List<ModularFirearmAttachment>();
            private Dictionary<string, Guid> m_Memory = null;
            public int m_Indent = 0;

            public void Initialise(ModularFirearmAttachmentUIPopup popup, ModularFirearmAttachmentSocket s, MultiInputMultiChoice p)
            {
                m_Memory = popup.m_Memory;
                attachmentSystem = popup.attachmentSystem;
                socket = s;
                picker = p;

                picker.label = socket.socketName;

                sortString = GetSortString(s);
                picker.indentLevel = m_Indent;

                // Get the valid attachments for this socket
                socket.GetFilteredAttachments(m_FilteredAttachmnents);

                // Get the attachment options for the socket
                string[] options = new string[m_FilteredAttachmnents.Count];
                for (int j = 0; j < options.Length; ++j)
                {
                    var attachment = m_FilteredAttachmnents[j];
                    if (attachment != null)
                        options[j] = attachment.listName;
                    else
                        options[j] = socket.nullAttachmentName;
                }
                picker.options = options;

                // Get the current index
                picker.index = socket.currentAttachmentIndex;

                // Add an event handler to notify the socket to change
                picker.onIndexChanged.AddListener(OnPickerIndexChanged);

                // Enable the new picker
                picker.gameObject.SetActive(true);
            }

            void OnPickerIndexChanged(int index)
            {
                // Apply the attachment
                if (m_FilteredAttachmnents[index] != null)
                    socket.Attach(m_FilteredAttachmnents[index].attachmentID);
                else
                    socket.RemoveAttachment();

                // Store the selection in memory
                if (socket.currentAttachment != null)
                    m_Memory[socket.socketName] = socket.currentAttachment.attachmentID;
                else
                    m_Memory[socket.socketName] = Guid.Empty;
            }

            private string GetSortString(ModularFirearmAttachmentSocket s)
            {
                // Iterate up through hierarchy to get parent sockets
                Transform t = s.transform.parent;
                while (t != null && t != attachmentSystem.transform)
                {
                    var parentSocket = t.GetComponent<ModularFirearmAttachmentSocket>();
                    if (parentSocket != null) // Recursive function
                    {
                        ++m_Indent;
                        return string.Format("{0}z{1}", GetSortString(parentSocket), s.socketName);
                    }

                    t = t.parent;
                }

                // No parent. Return the name of this
                return s.socketName;
            }
        }

        public override Selectable startingSelection
        {
            get { return m_CompletedButton; }
        }

        protected override void CreateAttachmentUI()
        {
            m_ContainerTransform = m_SocketPickerPrototype.transform.parent;
            m_SocketPickerPrototype.gameObject.SetActive(false);

            // Position the popup correctly
            var rt = transform as RectTransform;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            // Add event handler to completed button
            if (m_CompletedButton != null)
                m_CompletedButton.onClick.AddListener(Back);

            // Build the sockets list
            for (int i = 0; i < attachmentSystem.numSockets; ++i)
            {
                // Create a picker for the socket
                var socket = attachmentSystem.GetSocket(i);
                CreateSocketPicker(socket);
            }

            // Store the current attachments (in case sockets are removed and re-added)
            for (int i = 0; i < m_SocketEntries.Count; ++i)
            {
                var s = m_SocketEntries[i].socket;
                if (s.currentAttachment == null)
                    m_Memory.Add(s.socketName, Guid.Empty);
                else
                    m_Memory.Add(s.socketName, s.currentAttachment.attachmentID);
            }

            // Sort the UI elements
            SortUIElements();
        }

        void CreateSocketPicker(ModularFirearmAttachmentSocket socket)
        {
            // Instantiate a new picker for it
            var picker = Instantiate(m_SocketPickerPrototype, m_ContainerTransform);

            var socketUI = new SocketUI();
            socketUI.Initialise(this, socket, picker);
            m_SocketEntries.Add(socketUI);
        }

        protected override void OnSocketsChanged()
        {
            // Remove all socket UI elements besides the one that cause the sockets change
            for (int i = m_SocketEntries.Count - 1; i >= 0; --i)
            {
                if (!attachmentSystem.IsSocketRegistered(m_SocketEntries[i].socket))
                {
                    Destroy(m_SocketEntries[i].picker.gameObject);
                    m_SocketEntries.RemoveAt(i);
                }
            }

            // Add in the new sockets
            for (int i = 0; i < attachmentSystem.numSockets; ++i)
            {
                // Get the socket
                var socket = attachmentSystem.GetSocket(i);

                // Skip if we already have a UI for this socket
                if (DoesSocketUIExist(socket))
                    continue;

                // Apply recorded attachment
                if (m_Memory.TryGetValue(socket.socketName, out Guid id))
                {
                    if (id == Guid.Empty)
                        socket.RemoveAttachment();
                    else
                        socket.Attach(id);
                }

                // Create the socket picker
                CreateSocketPicker(socket);
            }

            // Sort the UI elements
            SortUIElements();
        }

        bool DoesSocketUIExist(ModularFirearmAttachmentSocket socket)
        {
            for (int i = 0; i < m_SocketEntries.Count; ++i)
            {
                if (m_SocketEntries[i].socket == socket)
                    return true;
            }

            return false;
        }

        void SortUIElements()
        {
            // Sort based on sort strings
            m_SocketEntries.Sort((x, y) => { return string.Compare(x.sortString, y.sortString); });

            // Reorder in the hierarchy
            for (int i = 0; i < m_SocketEntries.Count; ++i)
                m_SocketEntries[i].picker.transform.SetAsLastSibling();

            // Push completed button to the end of the list and add event handler
            if (m_CompletedButton != null)
                m_CompletedButton.transform.SetAsLastSibling();
        }
    }
}
