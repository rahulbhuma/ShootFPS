using System.Collections;
using UnityEngine;
using NeoFPS.Constants;
using NeoSaveGames.Serialization;
using UnityEngine.Serialization;
using NeoSaveGames;

namespace NeoFPS.ModularFirearms
{
	[HelpURL("https://docs.neofps.com/manual/weaponsref-mb-weaponmoveaimer.html")]
	public class WeaponMoveAimer : OffsetBaseAimer
    {
        [SerializeField, FormerlySerializedAs("m_Transition"), Tooltip("The transitions easing to apply. Note: custom transition requires inheriting a new class and overriding the custom transition methods.")]
        private PositionTransition m_PositionTransition = PositionTransition.EaseInOut;

        [SerializeField, Tooltip("The transitions easing to apply. Note: custom transition requires inheriting a new class and overriding the custom transition methods.")]
        private RotationTransition m_RotationTransition = RotationTransition.EaseInOut;
        
        private IPoseHandler m_PoseHandler = null;
        private PoseInformation m_PoseInfo = null;

        public enum PositionTransition
        {
            Lerp,
            SwingUp,
            SwingAcross,
            EaseInOut,
            Overshoot,
            OvershootIn,
            Spring,
            SpringIn,
            Bounce,
            BounceIn,
            Custom,
            EaseIn,
            EaseOut
        }

        public enum RotationTransition
        {
            Lerp,
            EaseIn,
            EaseOut,
            EaseInOut,
            Overshoot,
            OvershootIn,
            Spring,
            SpringIn,
            Bounce,
            BounceIn,
            Custom
        }

        void CheckPoseHandler()
        {
            if (m_PoseHandler == null && firearm != null)
                m_PoseHandler = firearm.GetComponent<IPoseHandler>();

            if (m_PoseInfo == null)
            {
                m_PoseInfo = new PoseInformation(
                    posePosition, poseRotation,
                    GetPositionInterpolationRaise(), GetRotationInterpolationRaise(),
                    GetPositionInterpolationLower(), GetRotationInterpolationLower()
                    );
            }
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
        }

        protected override void OnOffsetsCalculated()
        {
            m_PoseInfo = new PoseInformation(
                posePosition, poseRotation,
                GetPositionInterpolationRaise(), GetRotationInterpolationRaise(),
                GetPositionInterpolationLower(), GetRotationInterpolationLower()
                );
        }

        protected override void AimInternal()
        {
            base.AimInternal();

            // Set the camera fov
            if (firearm.wielder != null)
                firearm.wielder.fpCamera.SetFov(fovMultiplier, inputMultiplier, aimUpDuration);

            // Set the aim pose (with transition)
            m_PoseHandler.PushPose(m_PoseInfo, this, aimUpDuration, PosePriorities.Aim);
        }

        protected override void StopAimInternal(bool instant)
        {
            // Reset the aim pose
            m_PoseHandler.PopPose(this, aimDownDuration);

            base.StopAimInternal(instant);

            // Reset the camera fov
            if (firearm.wielder != null)
                firearm.wielder.fpCamera.ResetFov(aimDownDuration);
        }
        
        VectorInterpolationMethod GetPositionInterpolationRaise()
        {
            // Get the position interpolation
            switch (m_PositionTransition)
            {
                case PositionTransition.Lerp:
                    return PoseTransitions.PositionLerp;
                case PositionTransition.SwingUp:
                    return PoseTransitions.PositionSwingUp;
                case PositionTransition.SwingAcross:
                    return PoseTransitions.PositionSwingAcross;
                case PositionTransition.Overshoot:
                    return PoseTransitions.PositionOvershootIn;
                case PositionTransition.OvershootIn:
                    return PoseTransitions.PositionOvershootIn;
                case PositionTransition.Spring:
                    return PoseTransitions.PositionSpringIn;
                case PositionTransition.SpringIn:
                    return PoseTransitions.PositionSpringIn;
                case PositionTransition.Bounce:
                    return PoseTransitions.PositionBounceIn;
                case PositionTransition.BounceIn:
                    return PoseTransitions.PositionBounceIn;
                case PositionTransition.Custom:
                    return CustomTransitionRaise;
                case PositionTransition.EaseIn:
                    return PoseTransitions.PositionEaseInCubic;
                case PositionTransition.EaseOut:
                    return PoseTransitions.PositionEaseOutCubic;
                case PositionTransition.EaseInOut:
                    return PoseTransitions.PositionEaseInOutCubic;
                default:
                    Debug.LogError("Aimer position interpolation is weird: " + m_PositionTransition);
                    return null;
            }
        }

        VectorInterpolationMethod GetPositionInterpolationLower()
        {
            // Get the position interpolation
            switch (m_PositionTransition)
            {
                case PositionTransition.Lerp:
                    return PoseTransitions.PositionLerp;
                case PositionTransition.SwingUp:
                    return PoseTransitions.PositionSwingAcross;
                case PositionTransition.SwingAcross:
                    return PoseTransitions.PositionSwingUp;
                case PositionTransition.Overshoot:
                    return PoseTransitions.PositionOvershootIn;
                case PositionTransition.OvershootIn:
                    return PoseTransitions.PositionOvershootIn;
                case PositionTransition.Spring:
                    return PoseTransitions.PositionSpringIn;
                case PositionTransition.SpringIn:
                    return PoseTransitions.PositionOvershootIn;
                case PositionTransition.Bounce:
                    return PoseTransitions.PositionBounceIn;
                case PositionTransition.BounceIn:
                    return PoseTransitions.PositionOvershootIn;
                case PositionTransition.Custom:
                    return CustomTransitionLower;
                case PositionTransition.EaseIn:
                    return PoseTransitions.PositionEaseInCubic;
                case PositionTransition.EaseOut:
                    return PoseTransitions.PositionEaseOutCubic;
                case PositionTransition.EaseInOut:
                    return PoseTransitions.PositionEaseInOutCubic;
                default:
                    Debug.LogError("Aimer position interpolation is weird: " + m_PositionTransition);
                    return null;
            }
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
                case RotationTransition.Overshoot:
                    return PoseTransitions.RotationOvershootIn;
                case RotationTransition.OvershootIn:
                    return PoseTransitions.RotationOvershootIn;
                case RotationTransition.Spring:
                    return PoseTransitions.RotationSpringIn;
                case RotationTransition.SpringIn:
                    return PoseTransitions.RotationSpringIn;
                case RotationTransition.Bounce:
                    return PoseTransitions.RotationBounceIn;
                case RotationTransition.BounceIn:
                    return PoseTransitions.RotationBounceIn;
                case RotationTransition.Custom:
                    return CustomTransitionRotateIn;
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
                case RotationTransition.Overshoot:
                    return PoseTransitions.RotationOvershootIn;
                case RotationTransition.OvershootIn:
                    return PoseTransitions.RotationOvershootIn;
                case RotationTransition.Spring:
                    return PoseTransitions.RotationSpringIn;
                case RotationTransition.SpringIn:
                    return PoseTransitions.RotationOvershootIn;
                case RotationTransition.Bounce:
                    return PoseTransitions.RotationBounceIn;
                case RotationTransition.BounceIn:
                    return PoseTransitions.RotationOvershootIn;
                case RotationTransition.Custom:
                    return CustomTransitionRotateIn;
                default:
                    Debug.LogError("Aimer rotation interpolation is weird: " + m_RotationTransition);
                    return null;
            }
        }

        protected virtual Vector3 CustomTransitionRaise(Vector3 source, Vector3 target, float lerp)
        {
            return Vector3.Lerp(source, target, lerp);
		}

        protected virtual Vector3 CustomTransitionLower(Vector3 source, Vector3 target, float lerp)
        {
            return Vector3.Lerp(source, target, lerp);
        }

        protected virtual Quaternion CustomTransitionRotateIn(Quaternion source, Quaternion target, float lerp)
        {
            return Quaternion.Lerp(source, target, lerp);
        }

        protected virtual Quaternion CustomTransitionRotateOut(Quaternion source, Quaternion target, float lerp)
        {
            return Quaternion.Lerp(source, target, lerp);
        }
    }
}