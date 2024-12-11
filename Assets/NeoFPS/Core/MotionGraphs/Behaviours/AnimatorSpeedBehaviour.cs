using UnityEngine;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using NeoSaveGames.Serialization;
using System.Collections;

namespace NeoFPS.CharacterMotion
{
    [MotionGraphElement("Animation/AnimatorSpeed", "AnimatorSpeedBehaviour")]
    public class AnimatorSpeedBehaviour : MotionGraphBehaviour
    {
        [SerializeField, Tooltip("The animator parameter name the speed value should be written to.")]
        private string m_SpeedParamName = "speed";
        [SerializeField, Tooltip("The duration used for smooth damping the velocity")]
        private float m_DampingTime = 0f;

        private int m_SpeedParamHash = -1;
        private float m_Acceleration = 0f;
        private float m_LastSpeed = 0f;

        public override void Initialise(MotionGraphConnectable o)
        {
            base.Initialise(o);

            if (controller.bodyAnimator != null)
                m_SpeedParamHash = Animator.StringToHash(m_SpeedParamName);
            else
                enabled = false;
        }

        public override void OnEnter()
        {
            m_Acceleration = 0f;
            m_LastSpeed = controller.characterController.velocity.magnitude;
        }

        public override void Update()
        {
            base.Update();

            // Damp with previous
            if (m_DampingTime > 0.0001f)
                m_LastSpeed = Mathf.SmoothDamp(m_LastSpeed, controller.characterController.velocity.magnitude, ref m_Acceleration, m_DampingTime);
            else
                m_LastSpeed = controller.characterController.velocity.magnitude;

            controller.bodyAnimator.SetFloat(m_SpeedParamHash, m_LastSpeed);
        }
    }
}