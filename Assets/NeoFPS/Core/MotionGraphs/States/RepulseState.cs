using UnityEngine;
using NeoFPS.CharacterMotion.MotionData;
using NeoFPS.CharacterMotion.Parameters;
using NeoSaveGames.Serialization;

namespace NeoFPS.CharacterMotion.States
{
    [MotionGraphElement("Instant/Repulse", "Repulse")]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgs-repulsestate.html")]
    public class RepulseState : MotionGraphState
    {
        [SerializeField, Tooltip("The transform that the character is repulsed by.")]
        private TransformParameter m_RepulsorTransform = null;

        [SerializeField, Tooltip("Should the transform be nullified after use?")]
        private bool m_NullifyTransform = true;

        [SerializeField, Tooltip("The vector away from the repulsor transform to repulse (will be rotated to match).")]
        private Vector3 m_RepulsionVector = Vector3.zero;

        [SerializeField, Tooltip("An optional multiplier applied to the repulse vector.")]
        private FloatDataReference m_RepulseMultiplier = new FloatDataReference(1f);

        [SerializeField, Tooltip("The minimum distance the state should attempt to move before completing. This prevents small jump heights or a very small fixed time step causing the movement to be too small to overcome ground snapping / detection.")]
        private float m_MinimumDistance = 0.05f;

        private static readonly NeoSerializationKey k_CompletedKey = new NeoSerializationKey("completed");

        private bool m_Completed = false;
        private float m_AttemptedDistance = 0f;
        private Vector3 m_OutVelocity = Vector3.zero;

#if UNITY_EDITOR
        public override void OnValidate()
        {
            base.OnValidate();

            m_RepulseMultiplier.ClampValue(0f, 2f);

            // Set a (high) cap on speed
            float magnitude = m_RepulsionVector.magnitude;
            if (magnitude > 100f)
                m_RepulsionVector *= 100f / magnitude;
        }
#endif

        public override bool completed
        {
            get { return m_Completed && m_AttemptedDistance >= m_MinimumDistance; }
        }

        public override Vector3 moveVector
        {
            get { return m_OutVelocity * Time.deltaTime; }
        }

        public override bool applyGravity
        {
            get { return false; }
        }

        public override bool applyGroundingForce
        {
            get { return false; }
        }

        public override bool ignorePlatformMove
        {
            get { return true; }
        }
        
        public override void OnEnter()
        {
            base.OnEnter();
            m_Completed = false;
            m_AttemptedDistance = 0f;
        }

        public override void OnExit()
        {
            base.OnExit();
            m_Completed = false;
            m_OutVelocity = Vector3.zero;
        }

        public override void Update()
        {
            base.Update();

            if (!m_Completed)
            {
                if (m_RepulsorTransform != null && m_RepulsorTransform.value != null)
                {
                    m_OutVelocity = m_RepulsorTransform.value.rotation * m_RepulsionVector * m_RepulseMultiplier.value;
                    if (m_NullifyTransform)
                        m_RepulsorTransform.value = null;
                }
                else
                    m_OutVelocity = characterController.velocity;

                m_Completed = true;
            }

            // Increment attempted move
            m_AttemptedDistance += m_OutVelocity.magnitude * Time.deltaTime;
        }

        public override void CheckReferences(IMotionGraphMap map)
        {
            base.CheckReferences(map);
            m_RepulsorTransform = map.Swap(m_RepulsorTransform);
            m_RepulseMultiplier.CheckReference(map);
        }

        public override void WriteProperties(INeoSerializer writer)
        {
            base.WriteProperties(writer);
            writer.WriteValue(k_CompletedKey, m_Completed);
        }

        public override void ReadProperties(INeoDeserializer reader)
        {
            base.ReadProperties(reader);
            reader.TryReadValue(k_CompletedKey, out m_Completed, m_Completed);
        }
    }
}