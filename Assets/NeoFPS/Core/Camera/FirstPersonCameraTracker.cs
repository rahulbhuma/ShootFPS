using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/fpcamref-mb-firstpersoncameratracker.html")]
    public class FirstPersonCameraTracker : FirstPersonCameraTrackerBase
    {
        [SerializeField, Tooltip("When should the event handler subscribe. Using Enable will only connect the event handler up while the object or component is enabled.")]
        private When m_When = When.Enable;
        [SerializeField, Tooltip("Should the camera be checked immediately when subscribing or wait for the first camera changed event.")]
        private bool m_CheckOnSubscribe = true;

        [Header("Events")]

        [SerializeField, Tooltip("A unity event called when the first person camera changes.")]
        private CameraEvent m_OnCameraChanged = null;
        [SerializeField, Tooltip("A unity event called when the first person camera changes.")]
        private TransformEvent m_OnCameraTransformChanged = null;

        [Serializable]
        private class CameraEvent : UnityEvent<Camera> { }
        [Serializable]
        private class TransformEvent : UnityEvent<Transform> { }

        enum When
        {
            Enable,
            Awake,
            Start
        }

        protected virtual void Awake()
        {
            if (m_When == When.Awake)
                Subscribe(m_CheckOnSubscribe);
        }

        protected virtual void Start()
        {
            if (m_When == When.Start)
                Subscribe(m_CheckOnSubscribe);
        }

        protected virtual void OnEnable()
        {
            if (m_When == When.Enable)
                Subscribe(m_CheckOnSubscribe);
        }

        protected virtual void OnDisable()
        {
            if (m_When == When.Enable)
                Unsubscribe();
        }

        protected virtual void OnDestroy()
        {
            if (m_When != When.Enable)
                Unsubscribe();
        }

        protected override void OnFirstPersonCameraChanged(Camera camera)
        {
            m_OnCameraChanged.Invoke(camera);
            m_OnCameraTransformChanged.Invoke(camera?.transform);
        }
    }
}