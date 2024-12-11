using NeoFPS.CharacterMotion;
using System.Collections;
using UnityEngine;

namespace NeoFPS
{
    [RequireComponent(typeof(Animator))]
    public class FirstPersonBodyRootMotion : MonoBehaviour, IRootMotionHandler
    {
        private Animator m_Animator = null;
        private Transform m_LocalTransform = null;

        protected void Awake()
        {
            m_Animator = GetComponent<Animator>();
            m_LocalTransform = transform;
        }

        protected void OnAnimatorMove()
        {
            m_RootMotionPostionOffset += m_Animator.rootPosition - m_LocalTransform.position;
            m_RootMotionRotationOffset *= Quaternion.Inverse(m_LocalTransform.rotation) * m_Animator.rootRotation;
        }

        private Vector3 m_RootMotionPostionOffset = Vector3.zero;
        private Quaternion m_RootMotionRotationOffset = Quaternion.identity;

        protected void OnEnable()
        {
            StartCoroutine(ResetCoroutine());
        }

        IEnumerator ResetCoroutine()
        {
            var wait = new WaitForFixedUpdate();
            while(true)
            {
                yield return wait;
                m_RootMotionPostionOffset = Vector3.zero;
                m_RootMotionRotationOffset = Quaternion.identity;
            }
        }

        public Vector3 GetRootMotionPositionOffset()
        {
            var result = m_RootMotionPostionOffset;
            return result;
        }

        public Quaternion GetRootMotionRotationOffset()
        {
            var result = m_RootMotionRotationOffset;
            return result;
        }
    }
}
