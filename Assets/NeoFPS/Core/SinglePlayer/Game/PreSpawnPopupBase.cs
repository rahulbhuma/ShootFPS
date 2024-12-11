using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using NeoFPS.Samples;

namespace NeoFPS.SinglePlayer
{
    public abstract class PreSpawnPopupBase : MonoBehaviour, IPrefabPopup
    {
        private UnityAction m_OnComplete = null;

        public BaseMenu menu
        {
            get;
            private set;
        }

        public bool cancellable
        {
            get { return false; }
        }

        public bool showBackground
        {
            get { return false; }
        }

        public FpsSoloGameCustomisable gameMode
        {
            get;
            private set;
        }

        public abstract Selectable startingSelection
        {
            get;
        }

        public void OnShow(BaseMenu m)
        {
            menu = m;
        }

        public virtual void Initialise(FpsSoloGameCustomisable g, UnityAction onComplete)
        {
            gameMode = g;
            m_OnComplete = onComplete;
        }

        protected void Spawn()
        {
            m_OnComplete?.Invoke();
            menu.ShowPopup(null);
        }

        public void Back()
        {
            Spawn();
        }
    }
}