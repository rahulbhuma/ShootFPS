using System;
using System.Collections;
using System.Collections.Generic;
using NeoFPS.ModularFirearms;
using NeoSaveGames.Serialization;
using UnityEngine;

namespace NeoFPS
{
    [Serializable]
    public class ModularFirearmPayload : INeoSerializableObject
    {
        [Min(-1), Tooltip("The number of bullets in the magazine to apply to the weapon's reloader module.")]
        public int magazineCount = -1;

        [SerializeField, HideInInspector] // Needs a custom inspector to get these working nicely
        private List<string> m_Sockets = null;
        [SerializeField, HideInInspector] // Needs a custom inspector to get these working nicely
        private List<Guid> m_Attachments = null;

        public virtual void BuildFromFirearm(IModularFirearm firearm)
        {
            if (firearm == null)
                return;

            // Get the current magazine count
            if (firearm.reloader != null)
                magazineCount = firearm.reloader.currentMagazine;
            else
                magazineCount = -1;

            // Get attachments from existing firearm
            var attachmentSystem = firearm.GetComponent<ModularFirearmAttachmentSystem>();
            if (attachmentSystem != null)
            {
                m_Sockets = new List<string>(attachmentSystem.numSockets);
                m_Attachments = new List<Guid>(attachmentSystem.numSockets);

                for (int i = 0; i < attachmentSystem.numSockets; ++i)
                {
                    var socket = attachmentSystem.GetSocket(i);
                    m_Sockets.Add(socket.socketName);
                    if (socket.currentAttachment != null)
                        m_Attachments.Add(socket.currentAttachment.attachmentID);
                    else
                        m_Attachments.Add(Guid.Empty);
                }
            }
        }

        public virtual void BuildFromSettings(ModularFirearmPayloadSettingsBase settings)
        {
            if (settings == null)
                return;

            // Get the magazine count
            magazineCount = settings.GetAmmoCount();

            // Get attachments options
            m_Sockets = new List<string>(settings.attachmentCount);
            m_Attachments = new List<Guid>(settings.attachmentCount);
            for (int i = 0; i < settings.attachmentCount; ++i)
            {
                m_Sockets.Add(settings.GetAttachmentSocket(i));
                m_Attachments.Add(settings.GetAttachmentID(i));
            }
        }

        public virtual void ApplyToFirearmImmediate(IModularFirearm firearm)
        {
            // Apply attachments to the new weapon
            if (m_Sockets != null)
            {
                var attachmentSystem = firearm.GetComponent<ModularFirearmAttachmentSystem>();
                for (int i = 0; i < m_Sockets.Count; ++i)
                {
                    var socket = attachmentSystem.GetSocket(m_Sockets[i]);
                    if (socket != null)
                    {
                        if (m_Attachments[i] == Guid.Empty)
                        {
                            socket.RemoveAttachment();
                        }
                        else
                        {
                            if (!socket.Attach(m_Attachments[i]))
                                Debug.LogFormat("Failed to add attachment to socket {0}. Attachment with Guid was not registered as a know attachment for the socket.", socket.socketName);
                        }
                    }
                    else
                    {
                        Debug.Log("Couldn't find socket with socket name: " + m_Sockets[i]);
                    }
                }
            }
        }

        public virtual void ApplyToFirearmDeferred(IModularFirearm firearm)
        {
            // Apply magazine count later in case the reloader was part of an attachment
            // and not immediately available
            if (magazineCount != -1)
                firearm.reloader.currentMagazine = magazineCount;
        }

        #region SAVE GAMES

        private static readonly NeoSerializationKey k_MagazineCountKey = new NeoSerializationKey("magazineCount");
        private static readonly NeoSerializationKey k_NumSocketsKey = new NeoSerializationKey("numSockets");
        private static readonly NeoSerializationKey k_SocketsKey = new NeoSerializationKey("sockets");
        private static readonly NeoSerializationKey k_AttachmentsKey = new NeoSerializationKey("attachments");

        public virtual void WriteProperties(INeoSerializer writer)
        {
            // Magazine count
            writer.WriteValue(k_MagazineCountKey, magazineCount);

            // Attachments
            if (m_Sockets != null)
            {
                writer.WriteValue(k_NumSocketsKey, m_Sockets.Count);
                writer.WriteValues(k_SocketsKey, m_Sockets);
                writer.WriteValues(k_AttachmentsKey, m_Attachments);
            }
        }

        public virtual void ReadProperties(INeoDeserializer reader)
        {
            // Magazine count
            reader.TryReadValue(k_MagazineCountKey, out int count, magazineCount);
            magazineCount = count;

            // Attachments
            if (reader.TryReadValue(k_NumSocketsKey, out int numSockets, 0) && numSockets > 0)
            {
                m_Sockets = new List<string>(numSockets);
                m_Attachments = new List<Guid>(numSockets);

                reader.TryReadValues(k_SocketsKey, m_Sockets);
                reader.TryReadValues(k_AttachmentsKey, m_Attachments);
            }
        }

        #endregion
    }
}
