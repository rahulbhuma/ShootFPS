using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using NeoFPS.Samples;

namespace NeoFPS.SinglePlayer
{
    public abstract class PreSpawnPopupTab : MonoBehaviour
    {
        public abstract string tabName
        {
            get;
        }

        public FpsSoloGameCustomisable gameMode
        {
            get;
            private set;
        }

        public virtual bool Initialise(FpsSoloGameCustomisable g)
        {
            gameMode = g;
            return true;
        }

        private PopupTabState m_State = PopupTabState.Uninitialised;
        public PopupTabState state
        {
            get { return m_State; }
            protected set
            {
                if (m_State != value)
                {
                    m_State = value;
                    OnStateChanged();
                }
            }
        }

        protected virtual void OnStateChanged()
        {
            gameObject.SetActive(state != PopupTabState.Hidden);
        }

        public void Show(bool instant)
        {
            if (instant)
                state = PopupTabState.Visible;
            else
            {
                if (state != PopupTabState.Visible)
                    state = PopupTabState.Showing;
            }
        }

        public void Hide(bool instant)
        {
            if (instant)
                state = PopupTabState.Hidden;
            else
            {
                if (state != PopupTabState.Hidden)
                    state = PopupTabState.Hiding;
            }
        }
    }

    public enum PopupTabState
    {
        Uninitialised,
        Hidden,
        Showing,
        Visible,
        Hiding
    }
}