using System.Collections;
using UnityEngine;
using NeoFPS.Constants;
using NeoSaveGames.Serialization;

namespace NeoFPS.ModularFirearms
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-cameraconstraintsaimer.html")]
    public class CameraConstraintsAimer : OffsetBaseAimer, IFirstPersonCameraPositionConstraint
    {
        [SerializeField, Tooltip("The transition easing to apply to rotation when entering / exiting ADS.")]
        private RotationTransition m_RotationTransition = RotationTransition.EaseInOut;

        [SerializeField, Tooltip("A priority score for this aimer. The highest scoring active constraint will be the one that controls the camera position")]
        private int m_ConstraintsPriority = 100;

        private Transform m_RootTransform = null;
        private FirstPersonCameraTransformConstraints m_CameraConstraints = null;
        private IPoseHandler m_PoseHandler = null;
        private PoseInformation m_PoseInfo = null;

        public enum RotationTransition
        {
            Lerp,
            EaseIn,
            EaseOut,
            EaseInOut
        }

        public float positionStrength
        {
            get { return 1f; }
        }

        public UnityEngine.Object owner
        {
            get { return this; }
        }

        public bool positionConstraintActive
        {
            get;
            set;
        }

        protected void OnValidate()
        {
            if (m_ConstraintsPriority < 1)
                m_ConstraintsPriority = 1;
        }

        QuaternionInterpolationMethod GetRotationInterpolationRaise()
        {
            // Get the rotation interpolation
            switch (m_RotationTransition)
            {
                case RotationTransition.Lerp:
                    return PoseTransitions.RotationLerp;
                case RotationTransition.EaseIn:
                    return PoseTransitions.RotationEaseInCubic;
                case RotationTransition.EaseOut:
                    return PoseTransitions.RotationEaseOutCubic;
                case RotationTransition.EaseInOut:
                    return PoseTransitions.RotationEaseInOutCubic;
                default:
                    Debug.LogError("Aimer rotation interpolation is weird: " + m_RotationTransition);
                    return null;
            }
        }

        QuaternionInterpolationMethod GetRotationInterpolationLower()
        {
            // Get the rotation interpolation
            switch (m_RotationTransition)
            {
                case RotationTransition.Lerp:
                    return PoseTransitions.RotationLerp;
                case RotationTransition.EaseIn:
                    return PoseTransitions.RotationEaseInCubic;
                case RotationTransition.EaseOut:
                    return PoseTransitions.RotationEaseOutCubic;
                case RotationTransition.EaseInOut:
                    return PoseTransitions.RotationEaseInOutCubic;
                default:
                    Debug.LogError("Aimer rotation interpolation is weird: " + m_RotationTransition);
                    return null;
            }
        }

        void CheckPoseHandler()
        {
            if (m_PoseHandler == null && firearm != null)
                m_PoseHandler = firearm.GetComponent<IPoseHandler>();

            if (m_PoseInfo == null)
            {
                m_PoseInfo = new PoseInformation(
                    Vector3.zero, poseRotation,
                    PoseTransitions.PositionLerp, GetRotationInterpolationRaise(),
                    PoseTransitions.PositionLerp, GetRotationInterpolationLower()
                    );
            }
        }

        protected override void OnOffsetsCalculated()
        {
            m_PoseInfo = new PoseInformation(
                Vector3.zero, poseRotation,
                PoseTransitions.PositionLerp, GetRotationInterpolationRaise(),
                PoseTransitions.PositionLerp, GetRotationInterpolationLower()
                );
        }

        public Vector3 GetConstraintPosition(Transform relativeTo)
        {
            return relativeTo.InverseTransformPoint(m_RootTransform.position + m_RootTransform.rotation * Quaternion.Inverse(poseRotation) * -posePosition);
        }

        void SetConstraints(float blend)
        {
            // Reset the aim constraints
            if (m_CameraConstraints != null)
                m_CameraConstraints.AddPositionConstraint(this, m_ConstraintsPriority, blend);

            // Set the fov
            if (firearm.wielder != null)
                firearm.wielder.fpCamera.SetFov(fovMultiplier, inputMultiplier, blend);
        }

        void ResetConstraints(float blend)
        {
            // Reset the aim constraints
            if (m_CameraConstraints != null)
                m_CameraConstraints.RemovePositionConstraint(this, blend);

            // Reset the fov
            if (firearm.wielder != null)
                firearm.wielder.fpCamera.ResetFov(blend);
        }

        protected override void Awake()
        {
            base.Awake();

            // Get pose handler
            CheckPoseHandler();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // Get pose handler
            CheckPoseHandler();

            if (firearm != null)
            {
                m_RootTransform = firearm.transform;

                // Get the camera constraints
                if (firearm.wielder != null)
                    m_CameraConstraints = firearm.wielder.GetComponentInChildren<FirstPersonCameraTransformConstraints>();
            }
            else
                m_RootTransform = transform;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Reset the aim constraints
            if (isAiming)
                ResetConstraints(aimDownDuration);

            m_CameraConstraints = null;
        }

        protected override void AimInternal()
        {
            base.AimInternal();

            // Set the aim constraints
            SetConstraints(aimUpDuration);

            // Set the aim pose (with transition)
            m_PoseHandler.PushPose(m_PoseInfo, this, aimUpDuration, PosePriorities.Aim);
        }

        protected override void StopAimInternal(bool instant)
        {
            // Reset the aim pose
            m_PoseHandler.PopPose(this, aimDownDuration);

            base.StopAimInternal(instant);

            // Reset the aim constraints
            if (isAiming)
                ResetConstraints(aimDownDuration);
        }

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);

            if (isAiming)
            {
                // Set the camera aim
                SetConstraints(0f);
            }
            else
            {
                // Reset the aim constraints
                ResetConstraints(0f);
            }
        }
    }
}