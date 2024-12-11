using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/fpcamref-mb-camerashakeoneshot.html")]
    public class CameraShakeOneShot : MonoBehaviour
    {
        [SerializeField, Tooltip("The strength of the camera (and other) shake due to the explosion.")]
        private float m_ShakeStrength = 0.5f;

        [SerializeField, Tooltip("The inner shake radius of the explosion. Any shake handlers within this radius will be affected at full strength, falling off to 0 outside this based on the falloff distance.")]
        private float m_ShakeInnerRadius = 10f;

        [SerializeField, Tooltip("The distance beyond the inner radius where the shake effect drops off to 0.")]
        private float m_ShakeFalloffDistance = 10f;

        [SerializeField, Tooltip("The duration of the shake effect.")]
        private float m_ShakeDuration = 0.75f;

        [SerializeField, Tooltip("If the shake handler is attached to a character, do they need to be touching the ground in order to shake.")]
        private bool m_RequiresGrounding = false;

        [SerializeField, Tooltip("Should the shake .")]
        private When m_When = When.Manual;

        private enum When
        {
            Start,
            OnEnable,
            Manual
        }

        void Start()
        {
            if (m_When == When.Start)
                Shake();
        }

        void OnEnable()
        {
            if (m_When == When.OnEnable)
                Shake();
        }

        public void Shake()
        {
            ShakeHandler.Shake(transform.position, m_ShakeInnerRadius, m_ShakeFalloffDistance, m_ShakeStrength, m_ShakeDuration, m_RequiresGrounding);
        }
    }
}