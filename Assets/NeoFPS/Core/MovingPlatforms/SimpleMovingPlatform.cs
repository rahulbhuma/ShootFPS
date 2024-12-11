using NeoSaveGames;
using NeoSaveGames.Serialization;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mb-simplemovingplatform.html")]
    class SimpleMovingPlatform : BaseMovingPlatform
    {
        [SerializeField, Tooltip("The position to move to (offset from current position in world space).")]
        private Vector3 m_OffsetPosition = Vector3.zero;

        [SerializeField, Tooltip("The time it takes to move.")]
        private float m_MovementDuration = 5f;

        [SerializeField, Tooltip("The pause before returning to original position or moving again.")]
        private float m_PauseDuration = 5f;

        [SerializeField, Tooltip("The delay before the first move")]
        private float m_StartPause = 5f;

        [SerializeField, Tooltip("The easing mode for the movement.")]
        private EasingMode m_EasingMode = EasingMode.Linear;

        public enum EasingMode
        {
            Linear,
            Quadratic,
            Cubic,
            Quartic
        }

        private static readonly NeoSerializationKey k_Position1Key = new NeoSerializationKey("p1");
        private static readonly NeoSerializationKey k_Position2Key = new NeoSerializationKey("p2");
        private static readonly NeoSerializationKey k_LerpKey = new NeoSerializationKey("lerp");
        private static readonly NeoSerializationKey k_LerpMultiplier = new NeoSerializationKey("lerpMultiplier");
        private static readonly NeoSerializationKey k_TimerKey = new NeoSerializationKey("timer");

        private float m_Lerp = 0f;
        private float m_LerpIncrement = 0f;
        private float m_LerpMultiplier = 0f;
        private float m_Timer = 0f;
        private float m_TimerIncrement = 0f;
        private Vector3 m_Position1 = Vector3.zero;
        private Vector3 m_Position2 = Vector3.zero;

#if UNITY_EDITOR
        protected void OnValidate()
        {
            m_MovementDuration = Mathf.Clamp(m_MovementDuration, 0.5f, 30f);
            m_PauseDuration = Mathf.Clamp(m_PauseDuration, 0.5f, 30f);
            if (m_StartPause < 0f)
                m_StartPause = 0f;

            if (Application.isPlaying)
            {
                m_LerpIncrement = Time.fixedDeltaTime / m_MovementDuration;
                m_TimerIncrement = Time.fixedDeltaTime / m_PauseDuration;
            }
        }
#endif

        protected override void Start()
        {
            base.Start();

            m_LerpIncrement = Time.fixedDeltaTime / m_MovementDuration;
            m_TimerIncrement = Time.fixedDeltaTime / m_PauseDuration;
            m_LerpMultiplier = 1f;
        }

        protected override void Initialise()
        {
            base.Initialise();

            m_Position1 = localTransform.position;
            m_Position2 = m_Position1 + m_OffsetPosition;

            m_Timer = 1f - (m_StartPause / m_PauseDuration);
        }

        protected override Vector3 GetNextPosition()
        {
            if (m_Timer < 1f)
            {
                // Increment the timer
                m_Timer += m_TimerIncrement;
                if (m_Timer > 1f)
                    m_Timer = 1f;

                // Stay still
                return fixedPosition;
            }
            else
            {
                // Increment the lerp
                m_Lerp += m_LerpIncrement * m_LerpMultiplier;

                // Check if past limits
                if (m_Lerp < 0f || m_Lerp > 1f)
                {
                    m_Lerp = Mathf.Clamp01(m_Lerp);
                    m_Timer = 0f;
                    m_LerpMultiplier *= -1f;
                }

                // Lerp the positions
                switch (m_EasingMode)
                {
                    case EasingMode.Quadratic:
                        return Vector3.Lerp(m_Position1, m_Position2, EasingFunctions.EaseInOutQuadratic(m_Lerp));
                    case EasingMode.Cubic:
                        return Vector3.Lerp(m_Position1, m_Position2, EasingFunctions.EaseInOutCubic(m_Lerp));
                    case EasingMode.Quartic:
                        return Vector3.Lerp(m_Position1, m_Position2, EasingFunctions.EaseInOutQuartic(m_Lerp));
                    default:
                        return Vector3.Lerp(m_Position1, m_Position2, m_Lerp);
                }
            }
        }

        protected override Quaternion GetNextRotation()
        {
            return fixedRotation;
        }

        public override void ApplyOffset(Vector3 offset)
        {
            base.ApplyOffset(offset);
            m_Position1 += offset;
            m_Position2 += offset;
        }

        public override void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            base.WriteProperties(writer, nsgo, saveMode);

            writer.WriteValue(k_Position1Key, m_Position1);
            writer.WriteValue(k_Position2Key, m_Position2);
            writer.WriteValue(k_LerpKey, m_Lerp);
            writer.WriteValue(k_LerpMultiplier, m_LerpMultiplier);
            writer.WriteValue(k_TimerKey, m_Timer);
        }

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);

            reader.TryReadValue(k_Position1Key, out m_Position1, m_Position1);
            reader.TryReadValue(k_Position2Key, out m_Position2, m_Position2);
            reader.TryReadValue(k_LerpKey, out m_Lerp, m_Lerp);
            reader.TryReadValue(k_LerpMultiplier, out m_LerpMultiplier, m_LerpMultiplier);
            reader.TryReadValue(k_TimerKey, out m_Timer, m_Timer);
        }
    }
}
