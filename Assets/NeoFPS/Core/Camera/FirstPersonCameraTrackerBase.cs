using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    public abstract class FirstPersonCameraTrackerBase : MonoBehaviour
    {
        [SerializeField, Tooltip("If the first person camera is set to null, should the event fire with null or Camera.main as the parameter.")]
        private bool m_UseMainCameraIfNull = true;

        protected void Subscribe(bool check = true)
        {
            FirstPersonCameraBase.onCurrentCameraChanged += OnCurrentCameraChanged;
            if (check)
                OnCurrentCameraChanged(FirstPersonCameraBase.current);
        }

        protected void Unsubscribe()
        {
            FirstPersonCameraBase.onCurrentCameraChanged -= OnCurrentCameraChanged;
        }

        private void OnCurrentCameraChanged(FirstPersonCameraBase cam)
        {
            if (cam != null)
                OnFirstPersonCameraChanged(cam.unityCamera);
            else
            {
                if (m_UseMainCameraIfNull)
                    OnFirstPersonCameraChanged(Camera.main);
                else
                    OnFirstPersonCameraChanged(null);
            }
        }

        protected abstract void OnFirstPersonCameraChanged(Camera camera);
    }
}