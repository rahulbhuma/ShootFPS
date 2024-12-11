using System;
using UnityEngine;

namespace NeoFPS.CharacterMotion.Behaviours
{
    [MotionGraphElement("Character/AlignWithVelocity", "AlignWithVelocityBehaviour")]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgb-alignwithvelocitybehaviour.html")]
    public class AlignWithVelocityBehaviour : MotionGraphBehaviour
    {
        [SerializeField, Tooltip("The minimum speed the character should be travelling before being aligned with the velocity.")]
        private float m_MinSpeed = 0.01f;

        [SerializeField, Tooltip("The angle range to constrain to.")]
        private float m_AngleRange = 0f;

        public override void OnValidate()
        {
            m_MinSpeed = Mathf.Clamp(m_MinSpeed, 0.001f, 100f);
            m_AngleRange = Mathf.Clamp(m_AngleRange, 0f, 180f);
        }

        public override void Update()
        {
            // Check speed
            var cc = controller.characterController;
            var flat = Vector3.ProjectOnPlane(cc.velocity, controller.localTransform.up);
            if (flat.sqrMagnitude > m_MinSpeed * m_MinSpeed)
            {
                // Constrain
                flat.Normalize();
                controller.aimController.SetHeadingConstraints(flat, m_AngleRange);
            }
            else
            {
                // Remove constraints
                controller.aimController.ResetHeadingConstraints();
            }
        }

        public override void OnExit()
        {
            // Remove constraints
            controller.aimController.ResetHeadingConstraints();
        }
    }
}