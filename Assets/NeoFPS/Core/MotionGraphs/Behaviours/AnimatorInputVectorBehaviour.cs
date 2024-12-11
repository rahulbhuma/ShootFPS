using UnityEngine;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using NeoSaveGames.Serialization;
using System.Collections;

namespace NeoFPS.CharacterMotion
{
    [MotionGraphElement("Animation/AnimatorInputVector", "AnimatorInputVectorBehaviour")]
    public class AnimatorInputVectorBehaviour : MotionGraphBehaviour, IMotionGraphDynamicUpdate
    {
        [SerializeField, Tooltip("The animator parameter name the forward input value should be written to.")]
        private string m_ForwardParamName = "forward";
        [SerializeField, Tooltip("The animator parameter name the strafe input value should be written to. (positive = right)")]
        private string m_StrafeParamName = "strafe";
        [SerializeField, Range(0f, 1f), Tooltip("Damping smooths out the input values.")]
        private float m_Damping = 0.5f;

        [Header ("Directional Multipliers")]

        [SerializeField, Tooltip("A multiplier applied to the forward input before being sent to the animator parameter.")]
        private float m_ForwardMultiplier = 1f;
        [SerializeField, Tooltip("A multiplier applied to the backwards input before being sent to the animator parameter.")]
        private float m_BackwardMultiplier = 1f;
        [SerializeField, Tooltip("A multiplier applied to the strafe input before being sent to the animator parameter.")]
        private float m_StrafeMultiplier = 1f;

        private int m_ForwardParamHash = -1;
        private int m_StrafeParamHash = -1;

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

        public void DynamicUpdate()
        {
            // Get the damped input vector
            Vector2 input = controller.inputMoveDirection * controller.inputMoveScale;

            // Scale forwards/back
            float forwards = Vector2.Dot(input, Vector2.up);
            if (forwards >= 0f)
                forwards *= m_ForwardMultiplier;
            else
                forwards *= m_BackwardMultiplier;

            // Scale strafe
            float strafe = Vector2.Dot(input, Vector2.right) * m_StrafeMultiplier;

            // Apply to animator parameters
            if (m_Damping > 0.001f)
            {
                float dampingLerp = Mathf.Lerp(0.1f, 0.01f, m_Damping);
                controller.bodyAnimator.SetFloat(m_ForwardParamHash, Mathf.Lerp(controller.bodyAnimator.GetFloat(m_ForwardParamHash), forwards, dampingLerp));
                controller.bodyAnimator.SetFloat(m_StrafeParamHash, Mathf.Lerp(controller.bodyAnimator.GetFloat(m_StrafeParamHash), strafe, dampingLerp));
            }
            else
            {
                controller.bodyAnimator.SetFloat(m_ForwardParamHash, forwards);
                controller.bodyAnimator.SetFloat(m_StrafeParamHash, strafe);
            }
        }
    }
}