using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS.Samples.SinglePlayer
{
    public class WaterZoneMover : MonoBehaviour
    {
        [SerializeField, Tooltip("The pause at point A or B between moving")]
        private float m_PauseDuration = 4f;
        [SerializeField, Tooltip("The time taken to move from A to B and vice versa")]
        private float m_MoveDuration = 5f;
        [SerializeField, Tooltip("The offset from A (the starting position) to B (the target position)")]
        private Vector3 m_MoveOffset = new Vector3(0f, -10f, 0f);

        private Transform m_LocalTransform = null;
        private Vector3 m_StartingPosition = Vector3.zero;
        private Rigidbody m_Rigidbody = null;

        protected void Start()
        {
            m_LocalTransform = transform;
            m_Rigidbody = GetComponent<Rigidbody>();
            m_StartingPosition = m_LocalTransform.localPosition;
        }

        Vector3 CalculatePosition()
        {
            float scale = 1f / m_MoveDuration;
            float pause = m_PauseDuration * scale;
            float lerp = Mathf.PingPong(Time.timeSinceLevelLoad * scale, pause + 1f) - pause * 0.5f;

            return Vector3.Lerp(m_StartingPosition, m_StartingPosition + m_MoveOffset, lerp);
        }

        protected void FixedUpdate()
        {
            if (m_Rigidbody != null)
                m_Rigidbody.MovePosition(CalculatePosition());
        }

        protected void Update()
        {
            if (m_Rigidbody == null)
                m_LocalTransform.localPosition = CalculatePosition();
        }
    }
}