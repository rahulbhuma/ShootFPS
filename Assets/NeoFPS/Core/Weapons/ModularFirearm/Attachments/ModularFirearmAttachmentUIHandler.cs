using NeoFPS.Samples;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace NeoFPS.ModularFirearms
{
    [RequireComponent(typeof(ModularFirearmAttachmentSystem))]
    public class ModularFirearmAttachmentUIHandler : AnimatedWeaponInspect
    {
        [SerializeField, Tooltip("The attachments UI popup to show when inspecting the weapon")]
        private ModularFirearmAttachmentUIPopupBase m_PopupPrefab = null;

        private ModularFirearmAttachmentSystem m_AttachmentSystem = null;
        private ModularFirearmAttachmentUIPopupBase m_PopupInstance = null;

        public override bool toggle
        {
            get { return true; }
        }

        protected override void Start()
        {
            base.Start();

            m_AttachmentSystem = GetComponent<ModularFirearmAttachmentSystem>();
        }

        protected override void OnStartInspecting()
        {
            Debug.Assert(m_PopupPrefab != null, "No firearm attachment pop-up prefab set");

            m_PopupInstance = PrefabPopupContainer.ShowPrefabPopup(m_PopupPrefab);
            m_PopupInstance.Initialise(m_AttachmentSystem, OnCompleted);

            // Block aiming and functionality while switching attachments
            m_AttachmentSystem.firearm.AddBlocker(m_PopupInstance);
            m_AttachmentSystem.firearm.AddAimBlocker(m_PopupInstance);
        }

        void OnCompleted()
        {
            inspecting = false;

            // Unblock aiming and functionality
            m_AttachmentSystem.firearm.RemoveBlocker(m_PopupInstance);
            m_AttachmentSystem.firearm.RemoveAimBlocker(m_PopupInstance);
        }
    }
}
