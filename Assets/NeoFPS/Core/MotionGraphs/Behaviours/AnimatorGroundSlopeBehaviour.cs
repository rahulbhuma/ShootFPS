using UnityEngine;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using NeoSaveGames.Serialization;
using System.Collections;

namespace NeoFPS.CharacterMotion
{
    [MotionGraphElement("Animation/AnimatorGroundSlope", "AnimatorGroundSlopeBehaviour")]
    public class AnimatorGroundSlopeBehaviour : MotionGraphBehaviour
    {
        [SerializeField, Tooltip("The animator parameter name the ground's slope angle value (the angle in degrees from horizontal) should be written to.")]
        private string m_SlopeParamName = "groundSlope";
        [SerializeField, Tooltip("The animator parameter name the direction of the ground slope should be written to. 0 means the character is heading down the slope. 180 means the character is heading up the slope.")]
        private string m_DirectionParamName = "groundDirection";

        private int m_SlopeParamHash = -1;
        private int m_DirectionParamHash = -1;

        public override void Initialise(MotionGraphConnectable o)
        {
            base.Initialise(o);

            bool valid = false;
            if (controller.bodyAnimator != null)
            {
                if (!string.IsNullOrWhiteSpace(m_SlopeParamName))
                {
                    m_SlopeParamHash = Animator.StringToHash(m_SlopeParamName);
                    valid = true;
                }
                if (!string.IsNullOrWhiteSpace(m_DirectionParamName))
                {
                    m_DirectionParamHash = Animator.StringToHash(m_DirectionParamName);
                    valid = true;
                }
            }

            if (!valid)
                enabled = false;
        }

        public override void Update()
        {
            base.Update();

            var groundNormal = controller.characterController.groundSurfaceNormal;
            var up = controller.localTransform.up;
            var slopeAngle = Vector3.Angle(up, groundNormal);

            if (m_SlopeParamHash != -1)
                controller.bodyAnimator.SetFloat(m_SlopeParamHash, slopeAngle);
            if (m_DirectionParamHash != -1)
            {
                if (slopeAngle > 1f)
                {
                    var slopeDirection = Vector3.ProjectOnPlane(groundNormal, up).normalized;
                    controller.bodyAnimator.SetFloat(m_DirectionParamHash, Vector3.SignedAngle(controller.characterController.forward, slopeDirection, up));
                }
                else
                    controller.bodyAnimator.SetFloat(m_DirectionParamHash, 0f);
            }
        }
    }
}