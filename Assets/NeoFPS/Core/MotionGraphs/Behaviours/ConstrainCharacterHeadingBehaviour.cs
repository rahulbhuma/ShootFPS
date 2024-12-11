using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPS.CharacterMotion.Behaviours
{
    [MotionGraphElement("Camera/ConstrainCharacterHeading", "ConstrainCharacterHeadingBehaviour")]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgb-constraincharacterheadingbehaviour.html")]
    public class ConstrainCharacterHeadingBehaviour : MotionGraphBehaviour
    {
        [SerializeField, Tooltip("What to constrain the heading to.")]
        private ConstraintType m_ConstraintType = ConstraintType.DirectionVector;

        [SerializeField, Tooltip("The vector parameter to use as the constraint direction.")]
        private VectorParameter m_Direction = null;

        [SerializeField, Tooltip("The transform parameter to use as the constraint.")]
        private TransformParameter m_Transform = null;

        [SerializeField, Tooltip("The angle range to constrain to.")]
        private float m_AngleRange = 180f;

        [SerializeField, Tooltip("Flip the direction vector.")]
        private bool m_Flipped = false;

        [SerializeField, Tooltip("Should the constraints be updated each frame (if the vector parameter changes).")]
        private bool m_Continuous = false;

        public enum ConstraintType
        {
            DirectionVector,
            TargetVector,
            TransformForward,
            TransformDirection,
            Velocity
        }

        public override void Initialise(MotionGraphConnectable o)
        {
            base.Initialise(o);

            bool valid = true;

            switch (m_ConstraintType)
            {
                case ConstraintType.DirectionVector:
                    if (m_Direction == null)
                        valid = false;
                    break;
                case ConstraintType.TargetVector:
                    if (m_Direction == null)
                        valid = false;
                    break;
                case ConstraintType.TransformDirection:
                    if (m_Transform == null)
                        valid = false;
                    break;
                case ConstraintType.TransformForward:
                    if (m_Transform == null)
                        valid = false;
                    break;
                case ConstraintType.Velocity:
                    m_Continuous = true;
                    break;
            }

            if (!valid)
            {
                Debug.LogError(string.Format("ConstrainCameraHeadingBehaviour on graph element {0} is set to use a parameter, but the parameter is null.", owner.name));
                enabled = false;
            }
        }

        public override void OnEnter()
        {
            if (!m_Continuous)
                ConstrainHeading();
        }

        public override void OnExit()
        {
            controller.aimController.ResetHeadingConstraints();
        }

        public override void Update()
        {
            if (m_Continuous)
            {
                if (!ConstrainHeading())
                    controller.aimController.ResetHeadingConstraints();
            }
        }

        bool ConstrainHeading()
        {
            switch (m_ConstraintType)
            {
                case ConstraintType.DirectionVector:
                    {
                        float mult = m_Direction.value.magnitude;
                        if (mult > 0.01f)
                        {
                            mult = 1 / mult;
                            if (m_Flipped)
                                mult *= -1f;
                            controller.aimController.SetHeadingConstraints(m_Direction.value * mult, m_AngleRange);
                            return true;
                        }
                        else
                            return false;
                    }
                case ConstraintType.TargetVector:
                    {
                        var delta = m_Direction.value - controller.localTransform.position;
                        if (m_Flipped)
                            delta *= -1f;
                        controller.aimController.SetHeadingConstraints(delta.normalized, m_AngleRange);
                        return true;
                    }
                case ConstraintType.TransformForward:
                    {
                        if (m_Transform.value != null)
                        {
                            var forward = m_Transform.value.forward;
                            if (m_Flipped)
                                forward *= -1f;
                            controller.aimController.SetHeadingConstraints(forward, m_AngleRange);

                            return true;
                        }
                        else
                            return false;
                    }
                case ConstraintType.TransformDirection:
                    {
                        if (m_Transform.value != null)
                        {
                            var delta = m_Transform.value.position - controller.localTransform.position;
                            if (m_Flipped)
                                delta *= -1f;
                            controller.aimController.SetHeadingConstraints(delta.normalized, m_AngleRange);
                            return true;
                        }
                        else
                            return false;
                    }
                case ConstraintType.Velocity:
                    {
                        var velocity = controller.characterController.velocity;
                        if (velocity.sqrMagnitude > 0.0001f)
                        {
                            if (m_Flipped)
                                velocity *= -1f;
                            controller.aimController.SetHeadingConstraints(velocity, m_AngleRange);
                            return true;
                        }
                        else
                            return false;
                    }
                default:
                    return false;
            }
        }

        public override void CheckReferences(IMotionGraphMap map)
        {
            m_Direction = map.Swap(m_Direction);
            m_Transform = map.Swap(m_Transform);
        }
    }
}