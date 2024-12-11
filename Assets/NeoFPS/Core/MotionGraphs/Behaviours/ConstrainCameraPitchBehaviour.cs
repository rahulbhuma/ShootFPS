using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using System;

namespace NeoFPS.CharacterMotion.Behaviours
{
    [MotionGraphElement("Camera/ConstrainCameraPitch", "ConstrainCameraPitchBehaviour")]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgb-constraincamerapitchbehaviour.html")]
    public class ConstrainCameraPitchBehaviour : MotionGraphBehaviour
    {
        [SerializeField, Tooltip("What is the minimum pitch based on. Value is a specific angle from the horizontal plane. Parameter is an angle set in a float parameter. GroundSlope is an angle from the ground plane in the current direction.")]
        private Constraint m_MinPitchConstraint = Constraint.Value;
        [SerializeField, Delayed, Tooltip("The minimum angle the camera can look down.")]
        private float m_MinimumPitch = -89f;
        [SerializeField, Tooltip("A float parameter representing the minimum angle the camera can look down.")]
        private FloatParameter m_MinPitchParameter = null;
        [SerializeField, Delayed, Tooltip("The angle from the ground plane (based on the current ground contact) that will be the minimum angle the camera can look down.")]
        private float m_MinGroundOffset = -30f;

        [SerializeField, Tooltip("What is the maximum pitch based on. Value is a specific angle from the horizontal plane. Parameter is an angle set in a float parameter. GroundSlope is an angle from the ground plane in the current direction.")]
        private Constraint m_MaxPitchConstraint = Constraint.Value;
        [SerializeField, Delayed, Tooltip("The maximum angle the camera can look up.")]
        private float m_MaximumPitch = 89f;
        [SerializeField, Tooltip("A float parameter representing the maximum angle the camera can look down.")]
        private FloatParameter m_MaxPitchParameter = null;
        [SerializeField, Delayed, Tooltip("The angle from the ground plane (based on the current ground contact) that will be the maximum angle the camera can look down.")]
        private float m_MaxHorizonOffset = -30f;

        public enum Constraint
        {
            Value,
            Parameter,
            GroundSlope
        }

        private bool m_Continuous = false;

        public override void Initialise(MotionGraphConnectable o)
        {
            base.Initialise(o);

            // Check if constraint needs to be applied continuously
            m_Continuous = m_MinPitchConstraint == Constraint.GroundSlope || m_MaxPitchConstraint == Constraint.GroundSlope;

            // Check parameters are set up if required
            bool parametersValid = true;
            if (m_MinPitchConstraint == Constraint.Parameter && m_MinPitchParameter == null)
                parametersValid = false;
            if (m_MaxPitchConstraint == Constraint.Parameter && m_MaxPitchParameter == null)
                parametersValid = false;

            if (!parametersValid)
            {
                Debug.LogError(string.Format("ConstrainCameraPitchBehaviour on graph element {0} is set to use a parameter, but the parameter is null.", owner.name));
                enabled = false;
            }
        }

        public override void OnEnter()
        {
            // Apply pitch constraints if not done in update
            if (!m_Continuous)
                ApplyPitchConstraints();

            // Add event handlers to parameters
            if (m_MinPitchConstraint == Constraint.Parameter)
                m_MinPitchParameter.onValueChanged += OnPitchConstraintChanged;
            if (m_MaxPitchConstraint == Constraint.Parameter)
                m_MaxPitchParameter.onValueChanged += OnPitchConstraintChanged;
        }

        public override void OnExit()
        {
            // Reset pitch constraints
            controller.aimController.ResetPitchConstraints();

            // Remove event handlers from parameters
            if (m_MinPitchConstraint == Constraint.Parameter)
                m_MinPitchParameter.onValueChanged -= OnPitchConstraintChanged;
            if (m_MaxPitchConstraint == Constraint.Parameter)
                m_MaxPitchParameter.onValueChanged -= OnPitchConstraintChanged;
        }

        public override void Update()
        {
            if (m_Continuous)
                ApplyPitchConstraints();
        }

        private void OnPitchConstraintChanged(float arg0)
        {
            ApplyPitchConstraints();
        }

        void ApplyPitchConstraints()
        {
            // Get the ground pitch if required (don't want to do it twice)
            float groundPitch = 0f;
            if (m_MinPitchConstraint == Constraint.GroundSlope || m_MaxPitchConstraint == Constraint.GroundSlope)
            {
                var aimForward = controller.aimController.aimHeading;
                var groundForward = Vector3.ProjectOnPlane(aimForward, controller.characterController.groundNormal);
                groundPitch = Vector3.Angle(aimForward, groundForward);

                // Check if above or below horizon line
                if (Vector3.Dot(groundForward, controller.localTransform.up) < 0f)
                    groundPitch = -groundPitch;
            }

            // Get minimum pitch
            float min = m_MinimumPitch;
            switch (m_MinPitchConstraint)
            {
                case Constraint.Parameter:
                    min = Mathf.Clamp(m_MinPitchParameter.value, -89f, 89f);
                    break;
                case Constraint.GroundSlope:
                    min = Mathf.Max(groundPitch + m_MinGroundOffset, -89f);
                    break;
            }

            // Get maximum pitch
            float max = m_MaximumPitch;
            switch (m_MaxPitchConstraint)
            {
                case Constraint.Parameter:
                    max = Mathf.Clamp(m_MaxPitchParameter.value, min, 89f);
                    break;
                case Constraint.GroundSlope:
                    max = Mathf.Clamp(groundPitch + m_MaxHorizonOffset, min, 89f);
                    break;
            }

            // Apply pitch constraints
            controller.aimController.SetPitchConstraints(min, max);
        }

        public override void CheckReferences(IMotionGraphMap map)
        {
            m_MinPitchParameter = map.Swap(m_MinPitchParameter);
            m_MaxPitchParameter = map.Swap(m_MaxPitchParameter);
        }
    }
}