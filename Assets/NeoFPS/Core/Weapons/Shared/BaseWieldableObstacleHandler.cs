using NeoFPS.ModularFirearms;
using UnityEngine;

namespace NeoFPS
{
    public abstract class BaseWieldableObstacleHandler : MonoBehaviour
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

        public bool isBlocking
        {
            get;
            private set;
        }

        public bool isObstructed
        {
            get;
            private set;
        }
        
        protected IWieldable wieldable
        {
            get;
            private set;
        }

        private Transform m_AimTransform = null;
        private Transform m_RootTransform = null;
        private int m_Countdown = 0;
        private int m_BlockingRelease = 0;

        protected virtual void OnValidate()
        {
            m_MinBlockedFrames = Mathf.Clamp(m_MinBlockedFrames, 0, 100);
            m_CastDistance = Mathf.Clamp(m_CastDistance, 0.05f, 2f);
            m_CastRadius = Mathf.Clamp(m_CastRadius, 0.05f, 0.5f);
        }

        protected virtual void Awake()
        {
            wieldable = GetComponent<IWieldable>();
            wieldable.onWielderChanged += OnWielderChanged;
            OnWielderChanged(wieldable.wielder);
        }

        protected virtual void OnDisable()
        {
            m_BlockingRelease = 0;

            if (wieldable is ModularFirearm firearm)
                firearm.RemoveTriggerBlocker(this);
            else
                wieldable.RemoveBlocker(this);
        }

        protected virtual void OnWielderChanged(ICharacter wielder)
        {
            if (wielder != null)
            {
                switch (m_CastSource)
                {
                    case CastSource.CharacterAim:
                        m_AimTransform = wieldable.wielder.fpCamera.aimTransform;
                        break;
                    case CastSource.WieldableParent:
                        m_AimTransform = wieldable.transform.parent;
                        break;
                    case CastSource.Camera:
                        m_AimTransform = wieldable.wielder.fpCamera.cameraTransform;
                        break;
                }

                m_RootTransform = wieldable.wielder.transform;
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
                    SetIsObstructed(PhysicsExtensions.RaycastFiltered(new Ray(m_AimTransform.position, m_AimTransform.forward), m_CastDistance, m_LayerMask, m_RootTransform));
                else
                    SetIsObstructed(PhysicsExtensions.SphereCastFiltered(new Ray(m_AimTransform.position, m_AimTransform.forward), m_CastRadius, m_CastDistance, m_LayerMask, m_RootTransform));
            }

            if (m_BlockingRelease > 0)
            {
                --m_BlockingRelease;
                if (m_BlockingRelease == 0)
                    SetIsBlocking(false);
            }
        }

        void SetIsObstructed(bool obstructed)
        {
            if (isObstructed != obstructed)
            {
                isObstructed = obstructed;

                // Abstract function for handling visuals
                OnObstructedChanged(obstructed);

                // Block trigger
                if (obstructed)
                {
                    SetIsBlocking(true);
                    m_Countdown = m_MinBlockedFrames;
                    m_BlockingRelease = 0;
                }
                else
                {
                    m_BlockingRelease = GetBlockingReleaseFrames();
                    if (m_BlockingRelease <= 0)
                        SetIsBlocking(false);
                }
            }
        }

        void SetIsBlocking(bool blocking)
        {
            if (isBlocking != blocking)
            {
                isBlocking = blocking;
                OnBlockingChanged(blocking);
            }
        }

        protected abstract int GetBlockingReleaseFrames();

        protected virtual void OnObstructedChanged(bool obstructed)
        {
            if (wieldable is ModularFirearm firearm)
            {
                if (obstructed)
                {
                    firearm.AddAimBlocker(this);
                    if (!m_ResumeAim)
                        firearm.aimToggleHold.on = false;
                }
                else
                    firearm.RemoveAimBlocker(this);
            }
        }

        protected virtual void OnBlockingChanged(bool blocking)
        {
            if (wieldable is ModularFirearm firearm)
            {
                if (blocking)
                    firearm.AddTriggerBlocker(this);
                else
                    firearm.RemoveTriggerBlocker(this);
            }
            else
            {
                if (blocking)
                    wieldable.AddBlocker(this);
                else
                    wieldable.RemoveBlocker(this);
            }
        }
    }
}