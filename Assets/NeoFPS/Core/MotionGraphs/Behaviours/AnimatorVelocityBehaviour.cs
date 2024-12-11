using UnityEngine;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using NeoSaveGames.Serialization;
using System.Collections;

namespace NeoFPS.CharacterMotion
{
    [MotionGraphElement("Animation/AnimatorVelocity", "AnimatorVelocityBehaviour")]
    public class AnimatorVelocityBehaviour : MotionGraphBehaviour, IMotionGraphDynamicUpdate
    {
        [SerializeField, Tooltip("The animator parameter name the forward input value should be written to.")]
        private string m_ForwardParamName = "forward";
        [SerializeField, Tooltip("The animator parameter name the strafe input value should be written to. (positive = right)")]
        private string m_StrafeParamName = "strafe";
        [SerializeField, Tooltip("The duration used for smooth damping the velocity")]
        private float m_DampingTime = 0.2f;

        private int m_ForwardParamHash = -1;
        private int m_StrafeParamHash = -1;
        private Vector2 m_Acceleration = Vector2.zero;
        private Vector2 m_LastVelocity = Vector2.zero;

        public override void OnValidate()
        {
            m_DampingTime = Mathf.Clamp(m_DampingTime, 0f, 10f);
        }

        public override void Initialise(MotionGraphConnectable o)
        {
            base.Initialise(o);

            if (controller.bodyAnimator != null)
            {
                m_ForwardParamHash = Animator.StringToHash(m_ForwardParamName);
                m_StrafeParamHash = Animator.StringToHash(m_StrafeParamName);
            }
            else
                enabled = false;
        }

        public override void OnEnter()
        {
            m_Acceleration = Vector2.zero;
            m_LastVelocity = GetVelocity();
        }

        Vector2 GetVelocity()
        {
            return new Vector2(
                Vector3.Dot(controller.characterController.velocity, controller.characterController.forward),
                Vector3.Dot(controller.characterController.velocity, controller.characterController.right)
            );
        }

        public void DynamicUpdate()
        {
            // Options for ground slope, etc?
            // Do a vertical speed option for falling?

            // Get the velocity
            Vector2 v = GetVelocity();

            // Damp with previous
            if (m_DampingTime > 0.0001f)
                m_LastVelocity = Vector2.SmoothDamp(m_LastVelocity, v, ref m_Acceleration, m_DampingTime);
            else
                m_LastVelocity = v;

            // Send to animator
            controller.bodyAnimator.SetFloat(m_ForwardParamHash, m_LastVelocity.y);
            controller.bodyAnimator.SetFloat(m_StrafeParamHash, m_LastVelocity.x);
        }
    }
}