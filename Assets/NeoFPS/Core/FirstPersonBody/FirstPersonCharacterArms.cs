using UnityEngine;

namespace NeoFPS
{
    [RequireComponent (typeof(Animator))]
    public class FirstPersonCharacterArms : MonoBehaviour
    {
        [SerializeField, Tooltip("An optional arms transform that should be matched to the weapon geometry. Use this to match arm animations to weapon animations after the weapon has been affected by procedural animation effects such as bob or poses.")]
        private Transform m_ArmsRootTransform = null;

        [SerializeField, Tooltip("An offsets asset to help align character hands and fingers to weapon targets. The hand matching uses a standardised set of axes for hands, and you can apply offsets to both the character and weapon arms to match the standard. These will then be merged to get direct rotations from weapon to character when the weapon is equipped.")]
        private HandBoneOffsets m_Offsets = null;

        private Animator m_Animator = null;
        private Vector3 m_RootNeutralPosition = Vector3.zero;
        private Quaternion m_RootNeutralRotation = Quaternion.identity;
        private Quaternion m_HumanoidHandRotationL = Quaternion.identity;
        private Quaternion m_HumanoidHandRotationR = Quaternion.identity;
        private Quaternion m_TargetHandRotationL = Quaternion.identity;
        private Quaternion m_TargetHandRotationR = Quaternion.identity;
        private bool m_HumanoidHack = false;
        private bool m_HumanoidHackPending = false;

        private WieldableItemKinematics m_WieldableKinematics = null;
        public WieldableItemKinematics wieldableKinematics
        {
            get { return m_WieldableKinematics; }
            set
            {
                m_WieldableKinematics = value;

                if (m_ArmsRootTransform != null)
                {
                    if (m_WieldableKinematics == null)
                    {
                        m_ArmsRootTransform.localPosition = m_RootNeutralPosition;
                        m_ArmsRootTransform.localRotation = m_RootNeutralRotation;
                        gameObject.SetActive(false);
                    }
                    else
                        gameObject.SetActive(true);
                }
            }
        }

        protected void Awake()
        {
            if (m_ArmsRootTransform != null)
            {
                m_RootNeutralPosition = m_ArmsRootTransform.localPosition;
                m_RootNeutralRotation = m_ArmsRootTransform.localRotation;
            }

            m_Animator = GetComponent<Animator>();
            if (m_Animator == null)
                enabled = false;

            if (m_ArmsRootTransform != null)
                gameObject.SetActive(m_WieldableKinematics != null);
        }

        protected void OnAnimatorIK(int layerIndex)
        {
            if (wieldableKinematics != null)
            {
                bool matchLeftFingers = false;
                bool matchRightFingers = false;

                // Reposition to match weapon (if required)
                if (m_ArmsRootTransform != null)
                {
                    if (wieldableKinematics.viewModelTransform != null)
                    {
                        m_ArmsRootTransform.position = wieldableKinematics.viewModelTransform.position;
                        m_ArmsRootTransform.rotation = wieldableKinematics.viewModelTransform.rotation;
                    }
                }

                // Queue up the humanoid IK hack (target ik position and rotation doesn't match actual resulting position and rotation)
                m_HumanoidHackPending = !m_HumanoidHack;
                Vector3 targetPosition;

                // Match left hand
                if (wieldableKinematics.GetLeftHandGoals(out targetPosition, out m_TargetHandRotationL))
                {
                    // Apply character offsets
                    if (m_Offsets != null)
                    {
                        m_TargetHandRotationL *= Quaternion.Inverse(m_Offsets.leftHandRotationOffset);
                        targetPosition += m_TargetHandRotationL * m_Offsets.leftHandPositionOffset;
                    }
                    m_Animator.GetBoneTransform(HumanBodyBones.LeftHand).rotation = m_TargetHandRotationL;
                    SetIKGoals(AvatarIKGoal.LeftHand, targetPosition, m_TargetHandRotationL * m_HumanoidHandRotationL);
                    matchLeftFingers = wieldableKinematics.matchFingers;
                }
                else
                {
                    m_HumanoidHackPending = false;
                    ResetIKGoals(AvatarIKGoal.LeftHand);
                    matchLeftFingers = false;
                }

                // Match right hand
                if (wieldableKinematics.GetRightHandGoals(out targetPosition, out m_TargetHandRotationR))
                {
                    // Apply character offsets
                    if (m_Offsets != null)
                    {
                        m_TargetHandRotationR *= Quaternion.Inverse(m_Offsets.rightHandRotationOffset);
                        targetPosition += m_TargetHandRotationR * m_Offsets.rightHandPositionOffset;
                    }

                    m_Animator.GetBoneTransform(HumanBodyBones.RightHand).rotation = m_TargetHandRotationR;
                    SetIKGoals(AvatarIKGoal.RightHand, targetPosition, m_TargetHandRotationR * m_HumanoidHandRotationR);
                    matchRightFingers = wieldableKinematics.matchFingers;
                }
                else
                {
                    m_HumanoidHackPending = false;
                    ResetIKGoals(AvatarIKGoal.RightHand);
                    matchRightFingers = false;
                }

                // Finger matching (left hand)
                if (matchLeftFingers)
                {
                    // 24 = left thumb proximal
                    for (int i = 24; i < 39; ++i)
                        AnimatorMatchFingerRotation((HumanBodyBones)i);
                }

                // Finger matching (right hand)
                if (matchRightFingers)
                {
                    // 39 = right thumb proximal
                    for (int i = 39; i < 54; ++i)
                        AnimatorMatchFingerRotation((HumanBodyBones)i);
                }
            }
        }

        private void LateUpdate()
        {
            if (wieldableKinematics != null && m_ArmsRootTransform != null && wieldableKinematics.viewModelTransform != null)
            {
                m_ArmsRootTransform.position = wieldableKinematics.viewModelTransform.position;
                m_ArmsRootTransform.rotation = wieldableKinematics.viewModelTransform.rotation;
            }

            if (m_HumanoidHackPending)
            {
                m_HumanoidHackPending = false;
                m_HumanoidHack = true;

                // Calculate the difference between the tarket IK rotation and the actual IK rotation so that they can match exactly from this point
                m_HumanoidHandRotationL = Quaternion.Inverse(m_Animator.GetBoneTransform(HumanBodyBones.LeftHand).rotation) * m_TargetHandRotationL;
                m_HumanoidHandRotationR = Quaternion.Inverse(m_Animator.GetBoneTransform(HumanBodyBones.RightHand).rotation) * m_TargetHandRotationR;
            }
        }

        void SetIKGoals (AvatarIKGoal goal, Vector3 position, Quaternion rotation)
        {
            m_Animator.SetIKPosition(goal, position);
            m_Animator.SetIKRotation(goal, rotation );
            m_Animator.SetIKPositionWeight(goal, 1f);
            m_Animator.SetIKRotationWeight(goal, 1f);
        }

        void ResetIKGoals(AvatarIKGoal goal)
        {
            m_Animator.SetIKPositionWeight(goal, 0f);
            m_Animator.SetIKRotationWeight(goal, 0f);
        }

        void AnimatorMatchFingerRotation(HumanBodyBones bone)
        {
            if (wieldableKinematics.GetFingerRotationOffset(bone, out Quaternion weapon2Universal))
            {
                var fingerBone = m_Animator.GetBoneTransform(bone);
                var targetBone = wieldableKinematics.GetFingerTransform(bone);

                Quaternion universal2Character = Quaternion.identity;
                if (m_Offsets != null)
                    universal2Character = m_Offsets.GetFingerRotation(bone);

                fingerBone.rotation = targetBone.rotation * weapon2Universal * Quaternion.Inverse(universal2Character);
                m_Animator.SetBoneLocalRotation(bone, fingerBone.localRotation);
            }
        }

#if UNITY_EDITOR

#pragma warning disable CS0414
        [HideInInspector] public bool editOffsets = false;
#pragma warning restore CS0414

        [SerializeField, Tooltip("The thickness of the finger bone gizmos")]
        private float m_FingerGizmoScale = 0.005f;
        [SerializeField, Tooltip("The size of the hand gizmo")]
        private float m_HandGizmoScale = 0.03f;

        protected void OnValidate()
        {
            m_FingerGizmoScale = Mathf.Clamp(m_FingerGizmoScale, 0.001f, 0.1f);
            m_HandGizmoScale = Mathf.Clamp(m_HandGizmoScale, 0.001f, 1f);
        }

        private void OnDrawGizmosSelected()
        {
            if (!editOffsets)
                return;

            HandBoneOffsetsGizmos.DrawHandGizmosHumanoid(GetComponent<Animator>(), m_Offsets, m_FingerGizmoScale, m_HandGizmoScale);
        }

#endif
    }
}