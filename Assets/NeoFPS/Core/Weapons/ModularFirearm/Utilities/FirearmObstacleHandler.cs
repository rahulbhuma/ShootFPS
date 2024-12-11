using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS.ModularFirearms
{
    [RequireComponent(typeof (ModularFirearm))]
    public abstract class FirearmObstacleHandler : MonoBehaviour
    {
        [SerializeField, Tooltip("The distance in front of the weapon to check for obstacles.")]
        private float m_CastDistance = 0.75f;
        [SerializeField, Tooltip("The cast type to use.")]
        private CastType m_CastType = CastType.Raycast;
        [SerializeField, Tooltip("What to use as the source of the cast.")]
        private CastSource m_CastSource = CastSource.WieldableParent;
        [SerializeField, Tooltip("What layers are counted as obstacles and make the character move the weapon.")]
        private LayerMask m_LayerMask = PhysicsFilter.Masks.CharacterBlockers;
        [SerializeField, Tooltip("The radius of the sphere cast (if cast type is set to Spherecast).")]
        private float m_CastRadius = 0.2f;
        [SerializeField, Tooltip("The minimum number of frames the weapon can be blocked. This prevents the weapon from rapidly switching between blocked and not.")]
        private int m_MinBlockedFrames = 10;
        [SerializeField, Tooltip("Should the aim be resumed when the firearm is no longer blocked (this will always happen if the aim hold button is pressed).")]
        private bool m_ResumeAim = false;

        public enum CastType
        {
            Raycast,
            Spherecast
        }

        public enum CastSource
        {
            WieldableParent,
            CharacterAim,
            Camera
        }

        public bool isBlocked
        {
            get;
            private set;
        }
        
        protected ModularFirearm firearm
        {
            get;
            private set;
        }

        private Transform m_AimTransform = null;
        private Transform m_RootTransform = null;
        private int m_Countdown = 0;
        private int m_TriggerCounter = 0;

        protected virtual int GetTriggerBlockFrames()
        {
            return 0;
        }

        protected virtual void OnValidate()
        {
            m_MinBlockedFrames = Mathf.Clamp(m_MinBlockedFrames, 0, 100);
            m_CastDistance = Mathf.Clamp(m_CastDistance, 0.05f, 2f);
            m_CastRadius = Mathf.Clamp(m_CastRadius, 0.05f, 0.5f);
        }

        protected virtual void Awake()
        {
            firearm = GetComponent<ModularFirearm>();
            firearm.onWielderChanged += OnWielderChanged;
            OnWielderChanged(firearm.wielder);
        }

        private void OnDisable()
        {
            m_TriggerCounter = 0;
            firearm.RemoveTriggerBlocker(this);
        }

        protected virtual void OnWielderChanged(ICharacter wielder)
        {
            if (wielder != null)
            {
                switch (m_CastSource)
                {
                    case CastSource.CharacterAim:
                        m_AimTransform = firearm.wielder.fpCamera.aimTransform;
                        break;
                    case CastSource.WieldableParent:
                        m_AimTransform = firearm.transform.parent;
                        break;
                    case CastSource.Camera:
                        m_AimTransform = firearm.wielder.fpCamera.cameraTransform;
                        break;
                }

                m_RootTransform = firearm.wielder.transform;
            }
            else
            {
                m_AimTransform = null;
                m_RootTransform = null;
            }

            enabled = m_AimTransform != null;
        }

        protected virtual void FixedUpdate()
        {
            if (--m_Countdown < 0)
            {
                if (m_CastType == CastType.Raycast)
                    SetIsBlocked(PhysicsExtensions.RaycastFiltered(new Ray(m_AimTransform.position, m_AimTransform.forward), m_CastDistance, m_LayerMask, m_RootTransform));
                else
                    SetIsBlocked(PhysicsExtensions.SphereCastFiltered(new Ray(m_AimTransform.position, m_AimTransform.forward), m_CastRadius, m_CastDistance, m_LayerMask, m_RootTransform));
            }

            if (m_TriggerCounter > 0)
            {
                --m_TriggerCounter;
                if (m_TriggerCounter == 0)
                    firearm.RemoveTriggerBlocker(this);
            }
        }

        void SetIsBlocked(bool blocked)
        {
            if (isBlocked != blocked)
            {
                isBlocked = blocked;

                // Block trigger
                if (blocked)
                {
                    firearm.AddAimBlocker(this);
                    firearm.AddTriggerBlocker(this);
                    if (!m_ResumeAim)
                        firearm.aimToggleHold.on = false;
                    m_Countdown = m_MinBlockedFrames;
                }
                else
                {
                    firearm.RemoveAimBlocker(this);                    
                    m_TriggerCounter = GetTriggerBlockFrames();
                }

                // Abstract function for handling visuals
                OnBlockedChanged(blocked);
            }
        }

        protected abstract void OnBlockedChanged(bool blocked);
    }
}