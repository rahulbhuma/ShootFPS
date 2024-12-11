using System.Collections;
using System.Collections.Generic;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using NeoSaveGames;
using NeoSaveGames.Serialization;
using UnityEngine;

namespace NeoFPS
{
    public abstract class ProceduralSprintAnimationHandler : BaseSprintAnimationHandler, IAdditiveTransform, INeoSerializableComponent
    {
        [SerializeField, Tooltip("The neutral weapon / item position in sprint pose before bob is applied.")]
        private Vector3 m_SprintOriginPos = new Vector3(0.05f, -0.05f, 0.05f);
        [SerializeField, Tooltip("The neutral weapon / item rotation in sprint pose before bob is applied.")]
        private Vector3 m_SprintOriginRot = new Vector3(10f, -30f, 15f);
        [SerializeField, Tooltip("The peak position offset of the step cycle on the z and y axes (z does not change).")]
        private Vector2 m_SprintOffset = new Vector2(0.05f, 0.025f);
        [SerializeField, Tooltip("The peak rotation of the step cycle on each axis.")]
        private Vector3 m_SprintRotation = new Vector3(2.5f, 5f, 5f);
        [SerializeField, Tooltip("A position offset range either side of the sprint offset that will be randomised with each step to add variety.")]
        private Vector2 m_OffsetRange = Vector2.zero;
        [SerializeField, Tooltip("A rotation offset range either side of the sprint sprint that will be randomised with each step to add variety.")]
        private Vector3 m_RotationRange = Vector3.zero;
        [SerializeField, Range (-0.5f, 0.5f), Tooltip("The offset in terms of one full step cycle (left and right) for the timing of the rotation. Positive means after the position, Negative means before.")]
        private float m_RotationDesync = 0.1f;
        [SerializeField, Tooltip("The speed at which the full sprint animation strength is reached. This fades out the rotation aspect as the character slows down.")]
        private float m_FullStrengthSpeed = 10f;

        private ICharacterStepTracker m_StepTracker = null;
        private IAdditiveTransformHandler m_AdditiveTransformHandler = null;
        private IPoseHandler m_PoseHandler = null;
        private PoseInformation m_Pose = null;
        private bool m_InSprintPose = false;
        private Vector2 m_RandomisedOffset = Vector2.zero;
        private Vector3 m_RandomisedRotation = Vector3.zero;
        private int m_LastOffsetStep = -1;
        private int m_LastRotationStep = -1;

        public Quaternion rotation
        {
            get;
            private set;
        }

        public Vector3 position
        {
            get;
            private set;
        }

        public bool bypassPositionMultiplier
        {
            get { return true; }
        }

        public bool bypassRotationMultiplier
        {
            get { return true; }
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            m_FullStrengthSpeed = Mathf.Clamp(m_FullStrengthSpeed, 1f, 100f);

            // Clamp animation offsets
            m_SprintOriginPos.x = Mathf.Clamp(m_SprintOriginPos.x, -1f, 1f);
            m_SprintOriginPos.y = Mathf.Clamp(m_SprintOriginPos.y, -1f, 1f);
            m_SprintOriginPos.z = Mathf.Clamp(m_SprintOriginPos.z, -1f, 1f);
            m_SprintOriginRot.x = Mathf.Clamp(m_SprintOriginRot.x, -90f, 90f);
            m_SprintOriginRot.y = Mathf.Clamp(m_SprintOriginRot.y, -90f, 90f);
            m_SprintOriginRot.z = Mathf.Clamp(m_SprintOriginRot.z, -90f, 90f);
            m_SprintOffset.x = Mathf.Clamp(m_SprintOffset.x, -1f, 1f);
            m_SprintOffset.y = Mathf.Clamp(m_SprintOffset.y, -1f, 1f);
            m_SprintRotation.x = Mathf.Clamp(m_SprintRotation.x, -45f, 45f);
            m_SprintRotation.y = Mathf.Clamp(m_SprintRotation.y, -45f, 45f);
            m_SprintRotation.z = Mathf.Clamp(m_SprintRotation.z, -90f, 90f);

            if (m_Pose != null)
            {
                m_Pose.position = m_SprintOriginPos;
                m_Pose.rotation = Quaternion.Euler(m_SprintOriginRot);
            }
        }

        protected override void Awake()
        {
            m_PoseHandler = GetComponent<IPoseHandler>();
            m_AdditiveTransformHandler = GetComponentInChildren<IAdditiveTransformHandler>();

            if (m_Pose == null)
            {
                m_Pose = new PoseInformation(
                    m_SprintOriginPos, Quaternion.Euler(m_SprintOriginRot),
                    PoseTransitions.PositionEaseInOutQuadratic, PoseTransitions.RotationEaseInOutQuadratic
                    );
            }

            // Prevent calculating randomised bob offsets if range is effectively 0
            if ((m_OffsetRange.x * m_OffsetRange.x + m_OffsetRange.y * m_OffsetRange.y) < 0.0000001f)
                m_LastOffsetStep = -2;
            if ((m_RotationRange.x * m_RotationRange.x + m_RotationRange.y * m_RotationRange.y + m_RotationRange.z * m_RotationRange.z) < 0.0000001f)
                m_LastRotationStep = -2;

            base.Awake();
        }

        protected override void AttachToWielder(ICharacter wielder)
        {
            base.AttachToWielder(wielder);
            m_StepTracker = wielder.GetComponent<ICharacterStepTracker>();
        }

        protected override void OnSprintStateChanged(SprintState s)
        {
            switch (s)
            {
                case SprintState.EnteringSprint:
                    if (!m_InSprintPose)
                    {
                        m_PoseHandler.PushPose(m_Pose, this, inTime, PosePriorities.Sprint);
                        m_AdditiveTransformHandler.ApplyAdditiveEffect(this);
                        m_InSprintPose = true;
                    }
                    break;
                case SprintState.ExitingSprint:
                    if (m_InSprintPose)
                    {
                        m_PoseHandler.PopPose(this, outTime);
                        m_InSprintPose = false;
                    }
                    break;
                case SprintState.Sprinting:
                    if (!m_InSprintPose)
                    {
                        m_PoseHandler.PushPose(m_Pose, this, 0f, PosePriorities.Sprint);
                        m_AdditiveTransformHandler.ApplyAdditiveEffect(this);
                        m_InSprintPose = true;
                    }
                    break;
                case SprintState.NotSprinting:
                    if (m_InSprintPose)
                    {
                        m_PoseHandler.PopPose(this, 0f);
                        m_InSprintPose = false;
                    }
                    m_AdditiveTransformHandler.RemoveAdditiveEffect(this);
                    break;
            }
        }

        public void UpdateTransform()
        {
            // Get sprint cycle (from motion graph or internally if not found)
            float stepCycle = 0f;
            if (m_StepTracker != null)
            {
                // Get position in step cycle
                stepCycle = m_StepTracker.stepCounter * 0.5f;

            }

            if (sprintState != SprintState.NotSprinting)
            {
                // Get randomised offset when step cycle flips
                if (m_LastOffsetStep > -2)
                {
                    int step = Mathf.RoundToInt(stepCycle);
                    if (step != m_LastOffsetStep)
                    {
                        m_LastOffsetStep = step;

                        // Change randomised position
                        m_RandomisedOffset = new Vector2(
                            Random.Range(-m_OffsetRange.x, m_OffsetRange.x),
                            Random.Range(-m_OffsetRange.y, m_OffsetRange.y)
                            );
                    }
                }

                float sin = Mathf.Sin(stepCycle * Mathf.PI * 2f);
                position = new Vector3(
                    (m_SprintOffset.x + m_RandomisedOffset.x) * sin,
                    (m_SprintOffset.y + m_RandomisedOffset.y) * Mathf.Abs(sin),
                    0f
                    );

                // Get randomised rotation when step cycle + desync flips
                if (m_LastRotationStep > -2)
                {
                    int step = Mathf.RoundToInt(stepCycle + m_RotationDesync);
                    if (step != m_LastRotationStep)
                    {
                        m_LastRotationStep = step;

                        // Change randomised rotation
                        m_RandomisedRotation = new Vector3(
                            Random.Range(-m_RotationRange.x, m_RotationRange.x),
                            Random.Range(-m_RotationRange.y, m_RotationRange.y),
                            Random.Range(-m_RotationRange.z, m_RotationRange.z)
                            );
                    }
                }

                sin = Mathf.Sin((stepCycle + m_RotationDesync) * Mathf.PI * 2f);
                rotation = Quaternion.Euler(
                    (m_SprintRotation.x + m_RandomisedRotation.x) * sin,
                    (m_SprintRotation.y + m_RandomisedRotation.y) * Mathf.Abs(sin),
                    (m_SprintRotation.z + m_RandomisedRotation.z) * sin
                    );

                if (sprintState == SprintState.EnteringSprint || sprintState == SprintState.ExitingSprint)
                    position *= sprintWeight * sprintWeight;

                float sprintStrength = sprintWeight * sprintWeight * Mathf.Clamp01(sprintSpeed / m_FullStrengthSpeed);
                rotation = Quaternion.Lerp(Quaternion.identity, rotation, sprintStrength);
            }
        }

        private static readonly NeoSerializationKey k_InSprintKey = new NeoSerializationKey("inSprintPose");

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValue(k_InSprintKey, m_InSprintPose);
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            reader.TryReadValue(k_InSprintKey, out m_InSprintPose, false);

            if (m_InSprintPose)
            {
                // Build pose
                var p = m_PoseHandler?.GetPose(this);
                if (p != null)
                {
                    p.interpolatePositionIn = PoseTransitions.PositionEaseInOutQuadratic;
                    p.interpolatePositionOut = PoseTransitions.PositionEaseInOutQuadratic;
                    p.interpolateRotationIn = PoseTransitions.RotationEaseInOutQuadratic;
                    p.interpolateRotationOut = PoseTransitions.RotationEaseInOutQuadratic;
                    m_Pose = p;
                }
            }
        }
    }
}