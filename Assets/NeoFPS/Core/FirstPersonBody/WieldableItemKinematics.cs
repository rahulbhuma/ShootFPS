using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inventoryref-mb-wieldableitemkinematics.html")]
    public class WieldableItemKinematics : MonoBehaviour
    {
        [SerializeField, Tooltip("The wieldable's animator component.")]
        private Animator m_Animator = null;

        [SerializeField, Tooltip("Which hand positions and rotations should be matched (using IK).")]
        private HandMatching m_HandMatching = HandMatching.Both;
        [SerializeField, Tooltip("Should finger rotations be matched.")]
        private bool m_FingerMatching = true;

        [SerializeField, Tooltip("What type of animation rig does the weapon use. A generic rig requires you to specify which objects represent the hand and finger bones.")]
        private RigType m_RigType = RigType.Generic;

        // Generic rig elements
        [SerializeField, RequiredObjectProperty, Tooltip("The target transform the character's left hand should align to.")]
        private Transform m_LeftHandIkTarget = null;
        [SerializeField, RequiredObjectProperty, Tooltip("The target transform the character's right hand should align to.")]
        private Transform m_RightHandIkTarget = null;
        [SerializeField]
        private Transform[] m_FingerBones = new Transform[30];

        [Header("Offsets")]

        [SerializeField, Tooltip("An offsets asset to help align character hands and fingers to weapon targets. Offsets can be edited below when in play mode and changes will persist to edit mode.")]
        private HandBoneOffsets m_Offsets = null;

        private IWieldable m_Wieldable = null;
        private FirstPersonCharacterArms m_CharacterArms = null;

        enum RigType
        {
            Generic,
            Humanoid
        }

        enum HandMatching
        {
            None,
            Left,
            Right,
            Both
        }

        // Accessors
        public Transform viewModelTransform
        {
            get;
            private set;
        }

        public bool matchLeftHand
        {
            get { return m_HandMatching == HandMatching.Left || m_HandMatching == HandMatching.Both; }
        }

        public bool matchRightHand
        {
            get { return m_HandMatching == HandMatching.Right || m_HandMatching == HandMatching.Both; }
        }

        public bool matchFingers
        {
            get { return m_HandMatching != HandMatching.None && m_FingerMatching; }
        }

        public Quaternion leftHandRotationOffset
        {
            get
            {
                if (m_Offsets != null)
                    return m_Offsets.leftHandRotationOffset;
                else
                    return Quaternion.identity;
            }
        }

        public Quaternion rightHandRotationOffset
        {
            get
            {
                if (m_Offsets != null)
                    return m_Offsets.rightHandRotationOffset;
                else
                    return Quaternion.identity;
            }
        }

        protected void Awake()
        {
            // Get the view model transform
            if (m_Animator != null)
                viewModelTransform = m_Animator.transform;

            // Attach to the wieldable
            m_Wieldable = GetComponentInParent<IWieldable>();
            if (m_Wieldable != null)
            {
                m_Wieldable.onWielderChanged += OnWielderChanged;
                OnWielderChanged(m_Wieldable.wielder);
            }
            else
                OnWielderChanged(null);

            // Clear finger bones (save space)
            if (m_RigType != RigType.Generic)
                m_FingerBones = null;
        }

        void OnWielderChanged(ICharacter character)
        {
            // Get arms
            if (character != null)
                m_CharacterArms = character.GetComponentInChildren<FirstPersonCharacterArms>(true);
            else
                m_CharacterArms = null;

            if (m_CharacterArms == null)
                enabled = false;
            else
            {
                enabled = true;
                if (gameObject.activeInHierarchy)
                    m_CharacterArms.wieldableKinematics = this;
            }
        }

        public Transform GetLeftHandTransform()
        {
            if (matchLeftHand)
            {
                if (m_RigType == RigType.Humanoid)
                    return m_Animator.GetBoneTransform(HumanBodyBones.LeftHand);
                else
                    return m_LeftHandIkTarget;
            }
            else
                return null;
        }

        public Transform GetRightHandTransform()
        {
            if (matchRightHand)
            {
                if (m_RigType == RigType.Humanoid)
                    return m_Animator.GetBoneTransform(HumanBodyBones.RightHand);
                else
                    return m_RightHandIkTarget;
            }
            else
                return null;
        }

        public Transform GetFingerTransform(HumanBodyBones bone)
        {
            if (matchFingers)
            {
                switch (m_RigType)
                {
                    case RigType.Humanoid:
                        {
                            return m_Animator.GetBoneTransform(bone);
                        }
                    case RigType.Generic:
                        {
                            int index = (int)bone - 24; // 24 is first joint index
                            if (index >= 0 && index < m_FingerBones.Length) // 30 = 10 fingers * 3 joints
                                return m_FingerBones[index];
                            else
                                return null;
                        }
                    default:
                        return null;
                }
            }
            else
                return null;
        }

        public bool GetFingerRotationOffset(HumanBodyBones bone, out Quaternion rotation)
        {
            if (m_Offsets != null)
            {
                if ((bone < HumanBodyBones.RightThumbProximal && m_Offsets.offsetLeftFingers) ||
                    (bone >= HumanBodyBones.RightThumbProximal && m_Offsets.offsetRightFingers))
                {
                    rotation = m_Offsets.GetFingerRotation(bone);
                    return true;
                }
            }

            // Don't match rotation
            rotation = Quaternion.identity;
            return false;
        }

        public bool GetLeftHandGoals(out Vector3 position, out Quaternion rotation)
        {
            var target = GetLeftHandTransform();
            if (target != null)
            {
                // Get targets
                var targetPosition = target.position;
                var targetRotation = target.rotation;

                // Apply offsets
                if (m_Offsets != null)
                {
                    // Apply rotation offset
                    targetRotation *= m_Offsets.leftHandRotationOffset;

                    // Add position offset
                    targetPosition += targetRotation * m_Offsets.leftHandPositionOffset;
                }

                // Return
                position = targetPosition;
                rotation = targetRotation;
                return true;
            }
            else
            {
                // No bone
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return false;
            }
        }

        public bool GetRightHandGoals(out Vector3 position, out Quaternion rotation)
        {
            var target = GetRightHandTransform();
            if (target != null)
            {
                // Get targets
                var targetPosition = target.position;
                var targetRotation = target.rotation;

                // Apply offsets
                if (m_Offsets != null)
                {
                    // Apply rotation offset
                    targetRotation *= m_Offsets.rightHandRotationOffset;

                    // Add position offset
                    targetPosition += targetRotation * m_Offsets.rightHandPositionOffset;
                }

                // Return
                position = targetPosition;
                rotation = targetRotation;
                return true;
            }
            else
            {
                // No bone
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return false;
            }
        }

        protected void OnEnable()
        {
            if (m_CharacterArms != null)
                m_CharacterArms.wieldableKinematics = this;
        }

        protected void OnDisable()
        {
            if (m_CharacterArms != null && m_CharacterArms.wieldableKinematics == this)
                m_CharacterArms.wieldableKinematics = null;
        }

#if UNITY_EDITOR

        [SerializeField, Tooltip("The thickness of the finger bone gizmos")]
        private float m_FingerGizmoScale = 0.005f;
        [SerializeField, Tooltip("The size of the hand gizmo")]
        private float m_HandGizmoScale = 0.03f;

#pragma warning disable CS0414
        [HideInInspector] public bool editOffsets = false;
        [HideInInspector] public bool expandLeftFingers = false;
        [HideInInspector] public bool expandRightFingers = false;
#pragma warning restore CS0414

        protected void OnValidate()
        {
            m_FingerGizmoScale = Mathf.Clamp(m_FingerGizmoScale, 0.001f, 0.1f);
            m_HandGizmoScale = Mathf.Clamp(m_HandGizmoScale, 0.001f, 1f);

            if (m_RigType == RigType.Generic)
            {
                if (m_FingerBones.Length != 30)
                {
                    var temp = new Transform[30];
                    for (int i = 0; i < m_FingerBones.Length && i < temp.Length; ++i)
                        temp[i] = m_FingerBones[i];
                    m_FingerBones = temp;
                }
            }

            if (m_Animator == null)
                m_Animator = GetComponentInChildren<Animator>();
        }

        private void OnDrawGizmosSelected()
        {
            if (!editOffsets)
                return;

            if (m_RigType == RigType.Humanoid)
                HandBoneOffsetsGizmos.DrawHandGizmosHumanoid(m_Animator, m_Offsets, m_FingerGizmoScale, m_HandGizmoScale);
            else
                HandBoneOffsetsGizmos.DrawHandGizmosGeneric(m_FingerBones, m_LeftHandIkTarget, m_RightHandIkTarget, m_Offsets, m_FingerGizmoScale, m_HandGizmoScale);
        }

#endif
    }
}