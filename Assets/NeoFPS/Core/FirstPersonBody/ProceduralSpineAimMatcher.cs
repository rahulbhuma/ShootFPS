using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/fpcharactersref-mb-proceduralspineaimmatcher.html")]
    public class ProceduralSpineAimMatcher : MonoBehaviour
    {
        [Header("Transforms")]

        [SerializeField, Tooltip("The transform to use for the body heading")]
        private Transform m_YawTransform = null;
        [SerializeField, Tooltip("The transform to use for the aim heading")]
        private Transform m_AimTransform = null;

        [Header("Aim Matching")]

        [SerializeField, FormerlySerializedAs("m_UpperChestStrength"), Range(0f, 1f), Tooltip("The strength of the rotation to apply to the upper chest (usually this should be the strongest and all bones should add up to 1 or less).")]
        private float m_AimUpperChest = 0.5f;
        [SerializeField, FormerlySerializedAs("m_ChestStrength"), Range(0f, 1f), Tooltip("The strength of the rotation to apply to the middle chest bone (usually this should be medium strength, and all bones should add up to 1 or less).")]
        private float m_AimChest = 0.3f;
        [SerializeField, FormerlySerializedAs("m_SpineStrength"), Range(0f, 1f), Tooltip("The strength of the rotation to apply to the lower spine bone (usually this should be the weakest since it is anchored to the hips, and all bones should add up to 1 or less).")]
        private float m_AimSpine = 0.2f;

        [Header("Leaning")]

        [SerializeField, Tooltip("A target angle for the leaning bone rotation (right). The results will not be exact as the rotation is blended across the bones of the spine.")]
        private float m_MaxRightLean = 30f;
        [SerializeField, Tooltip("A target angle for the leaning bone rotation (left). The results will not be exact as the rotation is blended across the bones of the spine.")]
        private float m_MaxLeftLean = 40f;
        [SerializeField, Range(0f, 1f), Tooltip("The strength of the lean rotation to apply to the upper chest (usually this should be the strongest).")]
        private float m_LeanUpperChest = 0.7f;
        [SerializeField, Range(0f, 1f), Tooltip("The strength of the lean rotation to apply to the middle chest bone (usually this should be medium strength).")]
        private float m_LeanChest = 0.4f;
        [SerializeField, Range(0f, 1f), Tooltip("The strength of the lean rotation to apply to the lower spine bone (usually this should be the weakest since it is anchored to the hips).")]
        private float m_LeanSpine = 0.3f;
        [SerializeField, Range(0f, 1f), Tooltip("The strength of the lean counter rotation to apply to the head bone (usually this should be medium strength).")]
        private float m_CounterLeanHead = 0.6f;
        [SerializeField, Range(0f, 1f), Tooltip("The strength of the lean counter rotation to apply to the neck bone (usually this should be low strength).")]
        private float m_CounterLeanNeck = 0.6f;

        private Animator m_Animator = null;
        private float m_AimStrength = 1f;
        private float m_TargetAimStrength = 0f;
        private float m_StrengthIncrement = 100000f;

        public float leanAmount
        {
            get;
            set;
        }

        private void OnValidate()
        {
            m_MaxRightLean = Mathf.Clamp(m_MaxRightLean, 0f, 90f);
            m_MaxLeftLean = Mathf.Clamp(m_MaxLeftLean, 0f, 90f);
        }

        private void Awake()
        {
            m_Animator = GetComponent<Animator>();
        }
        
        private void OnAnimatorIK(int layerIndex)
        {
            if (m_AimStrength != m_TargetAimStrength)
            {
                m_AimStrength += Time.deltaTime * m_StrengthIncrement;

                if (m_AimStrength > 1f)
                {
                    m_AimStrength = 1f;
                    m_StrengthIncrement = 0f;
                }
                if (m_AimStrength < 0f)
                {
                    m_AimStrength = 0f;
                    m_StrengthIncrement = 0f;
                }
            }

            bool doLean = !Mathf.Approximately(leanAmount, 0f);

            if (doLean)
            {
                var aimRotation = m_AimTransform.rotation * Quaternion.Inverse(m_YawTransform.rotation);

                float angle = 0f;
                if (leanAmount < 0f)
                    angle = -leanAmount * m_MaxLeftLean;
                else
                    angle = -leanAmount * m_MaxRightLean;

                var leanRotation = Quaternion.AngleAxis(angle, m_YawTransform.forward);
                var inverseLeanRotation = Quaternion.Inverse(leanRotation);

                // Rotate spine bones based on aim and lean
                SetBoneRotation(HumanBodyBones.UpperChest, Quaternion.Lerp(Quaternion.identity, aimRotation, m_AimUpperChest * m_AimStrength) * Quaternion.Lerp(Quaternion.identity, leanRotation, m_LeanUpperChest));
                SetBoneRotation(HumanBodyBones.Chest, Quaternion.Lerp(Quaternion.identity, aimRotation, m_AimChest * m_AimStrength) * Quaternion.Lerp(Quaternion.identity, leanRotation, m_LeanChest));
                SetBoneRotation(HumanBodyBones.Spine, Quaternion.Lerp(Quaternion.identity, aimRotation, m_AimSpine * m_AimStrength) * Quaternion.Lerp(Quaternion.identity, leanRotation, m_LeanSpine));

                // Counter rotate head and neck for lean
                SetBoneRotation(HumanBodyBones.Neck, Quaternion.Lerp(Quaternion.identity, inverseLeanRotation, m_CounterLeanNeck));
                SetBoneRotation(HumanBodyBones.Head, Quaternion.Lerp(Quaternion.identity, inverseLeanRotation, m_CounterLeanHead));
            }
            else
            {
                if (m_AimStrength > 0f)
                {
                    var aimRotation = m_AimTransform.rotation * Quaternion.Inverse(m_YawTransform.rotation);
                    SetBoneRotation(HumanBodyBones.UpperChest, Quaternion.Lerp(Quaternion.identity, aimRotation, m_AimUpperChest * m_AimStrength));
                    SetBoneRotation(HumanBodyBones.Chest, Quaternion.Lerp(Quaternion.identity, aimRotation, m_AimChest * m_AimStrength));
                    SetBoneRotation(HumanBodyBones.Spine, Quaternion.Lerp(Quaternion.identity, aimRotation, m_AimSpine * m_AimStrength));
                }
            }                 
        }

        void SetBoneRotation (HumanBodyBones bone, Quaternion rotation)
        {
            var boneTransform = m_Animator.GetBoneTransform(bone);
            if (boneTransform != null)
            {
                boneTransform.rotation = rotation * boneTransform.rotation;
                m_Animator.SetBoneLocalRotation(bone, boneTransform.localRotation);
            }
        }

        public void EnableAimMatching(float blendTime)
        {
            m_TargetAimStrength = 1f;
            if (m_AimStrength != 1f)
            {
                if (blendTime > 0f)
                    m_StrengthIncrement = 1f / blendTime;
                else
                    m_StrengthIncrement = 100000f;
            }
        }

        public void DisableAimAdapting(float blendTime)
        {
            m_TargetAimStrength = 0f;
            if (m_AimStrength != 0f)
            {
                if (blendTime > 0f)
                    m_StrengthIncrement = -1f / blendTime;
                else
                    m_StrengthIncrement = -100000f;
            }
        }
    }
}