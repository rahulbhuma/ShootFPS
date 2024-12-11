using System;
using UnityEngine;
using NeoFPS.ModularFirearms;

namespace NeoFPS.ModularFirearms
{
    [Serializable]
    public abstract class ModularFirearmPayloadSettingsBase
    {
#if UNITY_EDITOR
        [SerializeField, HideInInspector]
        protected bool m_Expanded = false;
#endif

        public abstract int GetAmmoCount();

        public abstract int attachmentCount { get; }
        public abstract string GetAttachmentSocket(int index);
        public abstract Guid GetAttachmentID(int index);
    }

    [Serializable]
    public class ModularFirearmPayloadSettings : ModularFirearmPayloadSettingsBase
    {
        [SerializeField, Tooltip("How should the ammo count be calculated")]
        private AmmoCountMethod m_AmmoCountMethod = AmmoCountMethod.FixedAmount;

        [SerializeField, Tooltip("The amount of ammo to set for the gun's magazine count on pick up.")]
        private int m_AmmoCount = 1;
        [SerializeField, Tooltip("The minimum amount (inclusive) of ammo to set for the gun's magazine count on pick up.")]
        private int m_AmmoMin = 1;
        [SerializeField, Tooltip("The maximum amount (inclusive) of ammo to set for the gun's magazine count on pick up.")]
        private int m_AmmoMax = 30;

        [SerializeField, Tooltip("The attachments that should be set on the firearm on pickup.")]
        private AttachmentInfo[] m_Attachments = { };

        [Serializable]
        private struct AttachmentInfo
        {
            public string socketName;
            public ModularFirearmAttachment attachment;
        }

        private enum AmmoCountMethod
        {
            FixedAmount,
            Random,
            Maximum,
            NotSpecified
        }

        public override int GetAmmoCount()
        {
            switch (m_AmmoCountMethod)
            {
                case AmmoCountMethod.FixedAmount:
                    return m_AmmoCount;
                case AmmoCountMethod.Random:
                    return UnityEngine.Random.Range(m_AmmoMin, m_AmmoMax + 1);
                case AmmoCountMethod.Maximum:
                    return int.MaxValue;
                default:
                    return -1;
            }
        }

        public override int attachmentCount
        {
            get { return m_Attachments.Length; }
        }

        public override Guid GetAttachmentID(int index)
        {
            var attachment = m_Attachments[index].attachment;
            if (attachment != null)
                return attachment.attachmentID;
            else
                return Guid.Empty;
        }

        public override string GetAttachmentSocket(int index)
        {
            return m_Attachments[index].socketName;
        }
    }
}
