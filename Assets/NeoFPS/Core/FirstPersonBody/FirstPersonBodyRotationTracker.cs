using System;
using UnityEngine;

namespace NeoFPS
{
    [RequireComponent (typeof(Animator))]
    [HelpURL("https://docs.neofps.com/manual/fpcharactersref-mb-firstpersonbodyrotationtracker.html")]    
    public class FirstPersonBodyRotationTracker : MonoBehaviour
    {
        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Float), Tooltip("The name of a float parameter in the animator controller to set with the rotation (damped) per frame.")]
        private string m_RotationParameter = "yawRotation";
        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Float), Tooltip("The name of a float parameter in the animator controller to set with the turn rate / angular velocity (damped) per frame.")]
        private string m_TurnRateParameter = "yawTurnRate";

        [SerializeField, Range (0f, 1f), Tooltip("The name of")]
        private float m_Damping = 0.25f;

        private Animator m_Animator = null;
        private AimController m_AimController = null;
        private int m_RotationHash = 0;
        private int m_TurnRateHash = 0;
        private float m_FixedRotation = 0f;
        private float m_DampedRotation = 0f;
        private float m_TurnRate = 0f;

        private void Awake()
        {
            m_Animator = GetComponent<Animator>();
            m_AimController = GetComponentInParent<AimController>();
            if (m_AimController != null)
            {
                if (!string.IsNullOrWhiteSpace(m_RotationParameter))
                    m_RotationHash = Animator.StringToHash(m_RotationParameter);
                if (!string.IsNullOrWhiteSpace(m_TurnRateParameter))
                    m_TurnRateHash = Animator.StringToHash(m_TurnRateParameter);
            }
            else
            {
                Debug.LogError("Attempting to use FirstPersonBodyRotationTracker on an animator object without an aim controller");
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            m_FixedRotation += Mathf.Repeat(m_AimController.yawLocalRotation.eulerAngles.y + 180f, 360f) - 180f;
            if (m_FixedRotation > 1000f || m_FixedRotation < -1000f)
            {
                m_DampedRotation -= m_FixedRotation;
                m_FixedRotation = 0f;
            }
        }

        private void Update()
        {
            // Get the damped rotation
            float dampingTime = Mathf.Lerp(Time.fixedDeltaTime * 1.5f, 1f, m_Damping);
            float damped = Mathf.SmoothDamp(m_DampedRotation, m_FixedRotation, ref m_TurnRate, dampingTime);

            // Get rotation this frame
            float rotation = damped - m_DampedRotation; // Round as well?
            if (m_RotationHash != 0)
                m_Animator.SetFloat(m_RotationHash, rotation);

            // Get the turn rate (prevent crazy decimal places)
            float turnRate = Mathf.Round(m_TurnRate * 100f) * 0.01f;
            if (m_TurnRateHash != 0)
                m_Animator.SetFloat(m_TurnRateHash, turnRate);

            // Store damped rotation
            m_DampedRotation = damped;
        }
    }
}

