using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS.Samples
{
    public class ShakeDemoBall : MonoBehaviour
    {
        public float m_Speed = 5f;

        private Rigidbody m_RigidBody = null;

        private void Awake()
        {
            m_RigidBody = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            Vector3 pos = m_RigidBody.position;
            pos.y = 0;

            Vector3 forceDirection = Vector3.Cross(pos.normalized, Vector3.up);

			#if UNITY_6000_0_OR_NEWER
            m_RigidBody.linearVelocity = forceDirection * m_Speed;
			#else
            m_RigidBody.velocity = forceDirection * m_Speed;
			#endif
        }
    }
}