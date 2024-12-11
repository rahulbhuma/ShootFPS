using NeoFPS.Samples;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NeoFPS.ModularFirearms
{
    public abstract class ModularFirearmAttachmentUIPopupBase : MonoBehaviour, IPrefabPopup
    {
        private UnityAction m_OnCompleted = null;

        public ModularFirearmAttachmentSystem attachmentSystem
        {
            get;
            private set;
        }

        public abstract Selectable startingSelection
        {
            get;
        }

        public BaseMenu menu
        {
            get;
            private set;
        }

        public bool cancellable
        {
            get { return true; }
        }

        public virtual bool showBackground
        {
            get { return false; }
        }

        public void Initialise(ModularFirearmAttachmentSystem attachmentSystem, UnityAction onCompleted)
        {
            this.attachmentSystem = attachmentSystem;

            m_OnCompleted = onCompleted;

            attachmentSystem.onSocketsChanged += OnSocketsChanged;

            CreateAttachmentUI();
        }

        protected abstract void CreateAttachmentUI();
        protected abstract void OnSocketsChanged();

        public void OnShow(BaseMenu m)
        {
            menu = m;
        }

        public void Back()
        {
            // Hide
            menu.ShowPopup(null);
        }

        void OnDisable()
        {
            if (attachmentSystem != null)
                attachmentSystem.onSocketsChanged -= OnSocketsChanged;
            m_OnCompleted?.Invoke();
        }
    }
}
