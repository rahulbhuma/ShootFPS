using System.Collections;
using UnityEngine;
using NeoFPS.Constants;
using NeoSaveGames.Serialization;
using NeoSaveGames;

namespace NeoFPS.ModularFirearms
{
    public abstract class OffsetBaseAimer : BaseAimerBehaviour
    {
        [SerializeField, NeoObjectInHierarchyField(true), Tooltip("A transform that represents where the camera should be aligned while aiming down sights.")]
        private Transform m_AimOffset = null;

        [SerializeField, Tooltip("The aim offset relative to the root transform. The gizmo in the scene viewport should align with the weapon sights.")]
        private Vector3 m_AimPosition = Vector3.zero;

        [SerializeField, Tooltip("The aim rotation relative to the root transform. The gizmo in the scene viewport should align with the weapon sights.")]
        private Vector3 m_AimRotation = Vector3.zero;

        [SerializeField, Range(0.1f, 1.5f), Tooltip("A multiplier for the camera FoV for aim zoom.")]
        private float m_FovMultiplier = 0.75f;

        [SerializeField, Range(0.1f, 1.5f), Tooltip("A multiplier for the camera FoV for aim zoom.")]
        private float m_InputMultiplier = 0f;

        [SerializeField, Range(0f, 2f), Tooltip("The time it takes to reach full aim, or return to zero aim.")]
        private float m_AimTime = 0.25f;

        [SerializeField, Range(0f, 1f), Tooltip("A multiplier for weapon recoil position (to reduce kick while aiming).")]
        private float m_PositionSpringMultiplier = 0.25f;

        [SerializeField, Range(0f, 1f), Tooltip("A multiplier for weapon recoil rotation (to reduce kick while aiming).")]
        private float m_RotationSpringMultiplier = 0.5f;

        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Bool, true, true), Tooltip("The animator parameter key for a bool used to control aiming state in animations.")]
        private string m_AimAnimBool = "";

        [SerializeField, Tooltip("If true then the gun cannot fire while transitioning in and out of aim mode. This is used to prevent gunshots interrupting the animation.")]
        private bool m_BlockTrigger = true;

        [SerializeField, Tooltip("The crosshair to use when aiming down sights.")]
        private FpsCrosshair m_CrosshairUp = FpsCrosshair.None;

        [SerializeField, Tooltip("The crosshair to use when not aiming down sights.")]
        private FpsCrosshair m_CrosshairDown = FpsCrosshair.Default;

#if UNITY_EDITOR
        [HideInInspector]
        public bool lockInputToFoV = true;
#endif

        private bool m_AimOffsetsCalculated = false;
        private bool m_AimUp = false;
        private int m_AimAnimBoolHash = -1;

        public override float fovMultiplier
        {
            get { return m_FovMultiplier; }
        }

        protected float inputMultiplier
        {
            get { return m_InputMultiplier; }
        }

        public bool blockTriggerOnTransition
        {
            get { return m_AimAnimBoolHash != -1 && m_BlockTrigger; }
        }

        public override float aimUpDuration
        {
            get { return m_AimTime; }
        }

        public override float aimDownDuration
        {
            get { return m_AimTime; }
        }

        protected Vector3 posePosition
        {
            get;
            private set;
        }

        protected Quaternion poseRotation
        {
            get;
            private set;
        }

        protected override void Awake()
        {
            base.Awake();
            // Set crosshair
            crosshair = m_CrosshairDown;
            // Get animation hash
            if (!string.IsNullOrEmpty(m_AimAnimBool))
                m_AimAnimBoolHash = Animator.StringToHash(m_AimAnimBool);

            if (!m_AimOffsetsCalculated)
            {
                // Build aim offset position and rotation
                bool buildFromOffsets = true;
                if (m_AimOffset != null)
                {
                    var firearm = GetComponentInParent<IModularFirearm>();
                    if (firearm != null)
                    {
                        if (firearm is IAimable aimable && aimable.aimRelativeTransform != null)
                        {
                            // Get aim rotation offset based on relative (gun bone) and firearm
                            Quaternion inverse = Quaternion.Inverse(aimable.aimRelativeTransform.rotation);
                            var relativeRotation = Quaternion.Inverse(inverse * m_AimOffset.rotation);
                            poseRotation = aimable.aimRelativeRotation * relativeRotation;

                            // Get aim position offset based on relative (gun bone) and firearm
                            var relativePosition = -aimable.aimRelativeTransform.InverseTransformPoint(m_AimOffset.position);
                            posePosition = poseRotation * (relativePosition + aimable.aimRelativePosition);
                        }
                        else
                        {
                            var firearmTransform = firearm.transform;

                            // Get the aim pose based on root of weapon
                            Quaternion inverse = Quaternion.Inverse(firearmTransform.rotation);
                            poseRotation = Quaternion.Inverse(inverse * m_AimOffset.rotation);
                            posePosition = poseRotation * -firearmTransform.InverseTransformPoint(m_AimOffset.position);
                        }

                        buildFromOffsets = false;
                    }
                }

                if (buildFromOffsets)
                {
                    // Set the aim pose
                    poseRotation = Quaternion.Inverse(Quaternion.Euler(m_AimRotation));
                    posePosition = poseRotation * m_AimPosition;
                }

                m_AimOffsetsCalculated = true;

                OnOffsetsCalculated();
            }
        }

        protected override void OnEnable()
        {
            if (firearm == null)
                firearm = GetComponentInParent<IModularFirearm>();

            base.OnEnable();

            if (firearm != null)
                firearm.SetRecoilMultiplier(1f, 1f);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (firearm != null)
            {
                firearm.RemoveTriggerBlocker(this);

                if (m_AimUp)
                {
                    // Set crosshair
                    crosshair = m_CrosshairDown;
                    // Reset the recoil multiplier
                    firearm.SetRecoilMultiplier(1f, 1f);
                    // Reset the camera aim
                    if (firearm.wielder != null)
                    {
                        firearm.wielder.fpCamera.ResetOffset(m_AimTime);
                        firearm.wielder.fpCamera.ResetFov(m_AimTime);
                    }
                }

                if (m_CurrentAimCoroutine != null)
                {
                    StopCoroutine(m_CurrentAimCoroutine);
                    m_CurrentAimCoroutine = null;
                }
            }
        }

        protected virtual void OnOffsetsCalculated() { }

        protected override void AimInternal()
        {
            // Stop aim coroutine
            if (m_CurrentAimCoroutine != null)
                StopCoroutine(m_CurrentAimCoroutine);
            // Set recoil multiplier
            firearm.SetRecoilMultiplier(m_PositionSpringMultiplier, m_RotationSpringMultiplier);
            // Start aim up coroutine
            if (gameObject.activeInHierarchy)
                m_CurrentAimCoroutine = StartCoroutine(AimCoroutine(true));
            // Set animator bool parameter
            if (m_AimAnimBoolHash != -1)
                firearm.animationHandler.SetBool(m_AimAnimBoolHash, true);
        }

        protected override void StopAimInternal(bool instant)
        {
            // Set animator bool parameter
            if (m_AimAnimBoolHash != -1)
                firearm.animationHandler.SetBool(m_AimAnimBoolHash, false);
            // Stop aim coroutine
            if (m_CurrentAimCoroutine != null)
            {
                StopCoroutine(m_CurrentAimCoroutine);
                m_CurrentAimCoroutine = null;
            }
            // Reset the recoil multiplier
            firearm.SetRecoilMultiplier(1f, 1f);
            // Insant vs animated
            if (instant)
            {
                // Set to hip fire
                SetAimState(FirearmAimState.HipFire);
                // Set crosshair
                crosshair = m_CrosshairDown;
            }
            else
            {
                // Start aim down coroutine
                if (gameObject.activeInHierarchy)
                    m_CurrentAimCoroutine = StartCoroutine(AimCoroutine(false));
            }
        }

        private Coroutine m_CurrentAimCoroutine = null;
        IEnumerator AimCoroutine(bool up)
        {
            // Block the trigger (prevents shot interrupting raise / lower)
            if (blockTriggerOnTransition)
                firearm.AddTriggerBlocker(this);

            m_AimUp = up;

            if (up)
            {
                SetAimState(FirearmAimState.EnteringAim);
                // Set crosshair
                crosshair = m_CrosshairUp;
            }
            else
                SetAimState(FirearmAimState.ExitingAim);

            float timer = 0f;
            while (timer < m_AimTime)
            {
                yield return null;
                timer += Time.deltaTime;
            }

            if (up)
            {
                SetAimState(FirearmAimState.Aiming);
            }
            else
            {
                SetAimState(FirearmAimState.HipFire);
                // Set crosshair
                crosshair = m_CrosshairDown;
            }

            // Unblock the trigger
            if (blockTriggerOnTransition)
                firearm.RemoveTriggerBlocker(this);

            m_CurrentAimCoroutine = null;
        }

        private static readonly NeoSerializationKey k_AimPositionKey = new NeoSerializationKey("aimPosition");
        private static readonly NeoSerializationKey k_AimRotationKey = new NeoSerializationKey("aimRotation");

        public override void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            base.WriteProperties(writer, nsgo, saveMode);

            // Write aim offsets
            if (m_AimOffsetsCalculated)
            {
                writer.WriteValue(k_AimPositionKey, posePosition);
                writer.WriteValue(k_AimRotationKey, poseRotation);
            }
        }

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);

            if (firearm != null)
            {
                if (isAiming)
                {
                    crosshair = m_CrosshairUp;
                    firearm.SetRecoilMultiplier(m_PositionSpringMultiplier, m_RotationSpringMultiplier);

                    if (firearm.wielder != null)
                        firearm.wielder.fpCamera.SetFov(m_FovMultiplier, 0f);
                }
                else
                {
                    crosshair = m_CrosshairDown;
                    firearm.SetRecoilMultiplier(1f, 1f);
                }

                // Read aim offsets
                if (reader.TryReadValue(k_AimPositionKey, out Vector3 pos, Vector3.zero) &&
                    reader.TryReadValue(k_AimRotationKey, out Quaternion rot, Quaternion.identity))
                {
                    posePosition = pos;
                    poseRotation = rot;
                    m_AimOffsetsCalculated = true;

                    OnOffsetsCalculated();
                }
                else
                    m_AimOffsetsCalculated = false;
            }
        }
    }
}
