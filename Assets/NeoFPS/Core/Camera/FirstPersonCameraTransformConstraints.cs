using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace NeoFPS
{
    public class FirstPersonCameraTransformConstraints : MonoBehaviour
    {
        [SerializeField, Tooltip("The default target will act as a position constraint from start. Use this for full body to match to the head bone. This only constrains position, not rotation.")]
        private Transform m_DefaultTarget = null;
        [SerializeField, Range(0f, 1f), FormerlySerializedAs("m_Damping"), Tooltip("A damping factor applied to the default target position constraint to prevent excessive jerkiness during animation blends.")]
        private float m_PositionDamping = 0.25f;

        private Transform m_LocalParent = null;
        private Transform m_LocalTransform = null;
        private List<PositionConstraintInfo> m_PositionConstraints = new List<PositionConstraintInfo>();
        private List<RotationConstraintInfo> m_RotationConstraints = new List<RotationConstraintInfo>();
        private Vector3 m_StartPosition = Vector3.zero;
        private Vector3 m_TargetPosition = Vector3.zero;
        private Quaternion m_StartRotation = Quaternion.identity;
        private Quaternion m_TargetRotation = Quaternion.identity;
        private float m_InverseFrameTime = 1f;
        private float m_CurrentFrameTime = 0f;

        private struct PositionConstraintInfo
        {
            public IFirstPersonCameraPositionConstraint constraint;
            public float blend;
            public float blendRate;
            public int priority;
        }

        private struct RotationConstraintInfo
        {
            public IFirstPersonCameraRotationConstraint constraint;
            public float blend;
            public float blendRate;
            public int priority;
        }

        private class StartingConstraints : IFirstPersonCameraPositionConstraint
        {
            private FirstPersonCameraTransformConstraints m_Owner = null;

            public float positionStrength { get { return 1f; } }

            public UnityEngine.Object owner { get { return m_Owner; } }

            private bool m_PositionConstraintActive = false;
            public bool positionConstraintActive
            {
                get { return m_PositionConstraintActive; }
                set
                {
                    if (m_PositionConstraintActive != value)
                    {
                        m_PositionConstraintActive = value;
                        if (value)
                            m_DampedPosition = m_Owner.m_LocalParent.InverseTransformPoint(m_Owner.m_DefaultTarget.position);
                    }
                }
            }

            private float dampingMultiplier
            {
#if UNITY_EDITOR
                get { return GetDampingMultiplier(); }
#else
            get;
            set;
#endif
            }

            private Vector3 m_DampedPosition = Vector3.zero;

            public StartingConstraints(FirstPersonCameraTransformConstraints owner)
            {
                m_Owner = owner;

#if !UNITY_EDITOR
                dampingMultiplier = GetDampingMultiplier();
#endif
            }

            float GetDampingMultiplier()
            {
                return Mathf.Lerp(0.1f / Time.fixedDeltaTime, 0.025f / Time.fixedDeltaTime, m_Owner.m_PositionDamping);
            }

            public Vector3 GetConstraintPosition(Transform relativeTo)
            {
                m_DampedPosition = Vector3.Lerp(m_DampedPosition, relativeTo.InverseTransformPoint(m_Owner.m_DefaultTarget.position), dampingMultiplier * Time.deltaTime);
                return m_DampedPosition;
            }
        }

        protected void Awake()
        {
            m_LocalTransform = transform;
            m_LocalParent = m_LocalTransform.parent;
            m_InverseFrameTime = 1f / Time.fixedDeltaTime;

            //if (m_DefaultTarget == null)
            //{
            //    var tempCameraTarget = new GameObject("TempCameraTarget").transform;
            //    tempCameraTarget.SetParent(transform.parent);
            //    tempCameraTarget.position = transform.position;
            //    tempCameraTarget.rotation = transform.rotation;
            //    m_DefaultTarget = tempCameraTarget;
            //}

            if (m_DefaultTarget != null)
            {
                var startingConstraints = new StartingConstraints(this);
                AddPositionConstraint(startingConstraints, 0, 0f);
            }
        }

        protected void Start()
        {
            FixedUpdate();
            m_CurrentFrameTime = 1f;
        }

        protected void FixedUpdate()
        {
            m_CurrentFrameTime = 0f;

            // Update position constraints
            if (m_PositionConstraints.Count > 0)
                UpdatePositionConstraints();
            else
            {
                m_TargetPosition = Vector3.zero;
                m_LocalTransform.localPosition = Vector3.zero;
            }

            // Update rotation constraints
            if (m_RotationConstraints.Count > 0)
                UpdateRotationConstraints();
            else
            {
                m_TargetRotation = Quaternion.identity;
                m_LocalTransform.localRotation = Quaternion.identity;
            }
        }

        protected void Update()
        {
            m_CurrentFrameTime += Time.deltaTime * m_InverseFrameTime;
            if (m_PositionConstraints.Count > 0)
                m_LocalTransform.localPosition = Vector3.Lerp(m_StartPosition, m_TargetPosition, m_CurrentFrameTime);
            if (m_RotationConstraints.Count > 0)
                m_LocalTransform.localRotation = Quaternion.Lerp(m_StartRotation, m_TargetRotation, m_CurrentFrameTime);
        }

        void UpdatePositionConstraints()
        {
            // Reset start position
            m_StartPosition = m_TargetPosition;
            m_LocalTransform.localPosition = m_StartPosition;

            // Iterate through and sort blends (removing zeros)
            for (int i = m_PositionConstraints.Count -1; i >= 0; --i)
            {
                // Safety check (destruction)
                if (m_PositionConstraints[i].constraint.owner == null)
                {
                    m_PositionConstraints.RemoveAt(i);
                }
                else
                {
                    // Blend the constraint
                    if (m_PositionConstraints[i].blendRate != 0f)
                    {
                        var c = m_PositionConstraints[i];
                        c.blend += Time.deltaTime * c.blendRate;

                        // Check if blended out
                        if (c.blend <= 0f)
                            m_PositionConstraints.RemoveAt(i);
                        else
                        {
                            // Check if fully blended in
                            if (c.blend > 1f)
                            {
                                c.blend = 1f;
                                c.blendRate = 0f;
                            }

                            // Reapply
                            m_PositionConstraints[i] = c;
                        }
                    }
                }
            }

            // Get earliest
            int itr = m_PositionConstraints.Count - 1;
            while (itr > 0)
            {
                float strength = m_PositionConstraints[itr].blend * m_PositionConstraints[itr].constraint.positionStrength;
                if (strength == 1f)
                    break;
                else
                    --itr;
            }

            // Set active / inactive
            for (int i = 0; i < m_PositionConstraints.Count; ++i)
            {
                if (i < itr)
                    m_PositionConstraints[i].constraint.positionConstraintActive = false;
                else
                    m_PositionConstraints[i].constraint.positionConstraintActive = true;
            }

            // Work forwards applying lerps, etc
            Vector3 constrainedPosition = Vector3.zero;
            while (itr >= 0 && itr < m_PositionConstraints.Count)
            {
                // Get the target position
                Vector3 targetPosition = m_PositionConstraints[itr].constraint.GetConstraintPosition(m_LocalParent);

                // Lerp based on strength and blend
                constrainedPosition = Vector3.Lerp(constrainedPosition, targetPosition, m_PositionConstraints[itr].constraint.positionStrength * EasingFunctions.EaseInOutQuadratic(m_PositionConstraints[itr].blend));

                // Next
                ++itr;
            }

            // Apply to position
            m_TargetPosition = constrainedPosition;
        }

        void UpdateRotationConstraints()
        {
            // Reset start rotation
            m_StartRotation = m_TargetRotation;
            m_LocalTransform.localRotation = m_StartRotation;

            // Iterate through and sort blends (removing zeros)
            for (int i = m_RotationConstraints.Count - 1; i >= 0; --i)
            {
                // Safety check (destruction)
                if (m_RotationConstraints[i].constraint.owner == null)
                {
                    m_RotationConstraints.RemoveAt(i);
                }
                else
                {
                    // Blend the constraint
                    if (m_RotationConstraints[i].blendRate != 0f)
                    {
                        var c = m_RotationConstraints[i];
                        c.blend += Time.deltaTime * c.blendRate;

                        // Check if blended out
                        if (c.blend <= 0f)
                            m_RotationConstraints.RemoveAt(i);
                        else
                        {
                            // Check if fully blended in
                            if (c.blend > 1f)
                            {
                                c.blend = 1f;
                                c.blendRate = 0f;
                            }

                            // Reapply
                            m_RotationConstraints[i] = c;
                        }
                    }
                }
            }

            // Get earliest
            int itr = m_RotationConstraints.Count - 1;
            while (itr > 0)
            {
                float strength = m_RotationConstraints[itr].blend * m_RotationConstraints[itr].constraint.rotationStrength;
                if (strength == 1f)
                    break;
                else
                    --itr;
            }

            // Set active / inactive
            for (int i = 0; i < m_RotationConstraints.Count; ++i)
            {
                if (i < itr)
                    m_RotationConstraints[i].constraint.rotationConstraintActive = false;
                else
                    m_RotationConstraints[i].constraint.rotationConstraintActive = true;
            }

            // Work forwards applying lerps, etc
            Quaternion constrainedRotation = Quaternion.identity;
            while (itr >= 0 && itr < m_RotationConstraints.Count)
            {
                // Get the target position
                Quaternion targetRotation = m_RotationConstraints[itr].constraint.GetConstraintRotation(m_LocalParent);

                // Lerp based on strength and blend
                constrainedRotation = Quaternion.Lerp(constrainedRotation, targetRotation, m_RotationConstraints[itr].constraint.rotationStrength * EasingFunctions.EaseInOutQuadratic(m_RotationConstraints[itr].blend));

                // Next
                ++itr;
            }

            // Apply to position
            m_TargetRotation = constrainedRotation;
        }

        public void AddPositionConstraint(IFirstPersonCameraPositionConstraint constraint, int priority, float blendDuration)
        {
            // Create the constraint info
            var constraintInfo = new PositionConstraintInfo();
            constraintInfo.constraint = constraint;
            constraintInfo.priority = priority;

            if (blendDuration > 0.001f)
            {
                constraintInfo.blend = 0f;
                constraintInfo.blendRate = 1f / blendDuration;
            }
            else
            {
                constraintInfo.blend = 1f;
                constraintInfo.blendRate = 0f;
            }

            // Add the constraint
            m_PositionConstraints.Add(constraintInfo);

            // Sort the active constraints by priority
            m_PositionConstraints.Sort((lhs, rhs) => { return lhs.priority - rhs.priority; });
        }

        public void AddRotationConstraint(IFirstPersonCameraRotationConstraint constraint, int priority, float blendDuration)
        {
            // Create the constraint info
            var constraintInfo = new RotationConstraintInfo();
            constraintInfo.constraint = constraint;
            constraintInfo.priority = priority;

            if (blendDuration > 0.001f)
            {
                constraintInfo.blend = 0f;
                constraintInfo.blendRate = 1f / blendDuration;
            }
            else
            {
                constraintInfo.blend = 1f;
                constraintInfo.blendRate = 0f;
            }

            // Add the constraint
            m_RotationConstraints.Add(constraintInfo);

            // Sort the active constraints by priority
            m_RotationConstraints.Sort((lhs, rhs) => { return lhs.priority - rhs.priority; });
        }

        public void RemovePositionConstraint(IFirstPersonCameraPositionConstraint constraint, float blendDuration)
        {
            for (int i = m_PositionConstraints.Count - 1; i >= 0; --i)
            {
                // Find the relevant constraint info
                if (m_PositionConstraints[i].constraint == constraint)
                {
                    // Set to blend out if has duration
                    if (blendDuration > 0.001f)
                    {
                        var c = m_PositionConstraints[i];
                        c.blendRate = -1f / blendDuration;
                        m_PositionConstraints[i] = c;
                    }
                    else
                    {
                        // Remove if not
                        m_PositionConstraints.RemoveAt(i);
                    }

                    break;
                }
            }
        }

        public void RemoveRotationConstraint(IFirstPersonCameraRotationConstraint constraint, float blendDuration)
        {
            for (int i = m_RotationConstraints.Count - 1; i >= 0; --i)
            {
                // Find the relevant constraint info
                if (m_RotationConstraints[i].constraint == constraint)
                {
                    // Set to blend out if has duration
                    if (blendDuration > 0.001f)
                    {
                        var c = m_RotationConstraints[i];
                        c.blendRate = -1f / blendDuration;
                        m_RotationConstraints[i] = c;
                    }
                    else
                    {
                        // Remove if not
                        m_RotationConstraints.RemoveAt(i);
                    }

                    break;
                }
            }
        }
    }

    public interface IFirstPersonCameraPositionConstraint
    {
        Vector3 GetConstraintPosition(Transform relativeTo);

        float positionStrength { get; }

        bool positionConstraintActive { get; set; }

        UnityEngine.Object owner { get; }
    }

    public interface IFirstPersonCameraRotationConstraint
    {
        Quaternion GetConstraintRotation(Transform relativeTo);

        float rotationStrength { get; }

        bool rotationConstraintActive { get; set; }

        UnityEngine.Object owner { get; }
    }
}