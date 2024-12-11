using NeoSaveGames;
using NeoSaveGames.Serialization;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mb-simplerotatingplatform.html")]
    class SimpleRotatingPlatform : BaseMovingPlatform
    {
        [SerializeField, Tooltip("The total rotation for each rotation phase (relative to the starting rotation of the phase, in world space).")]
        private Vector3 m_Rotation = Vector3.zero;

        [SerializeField, Tooltip("The time it takes to move.")]
        private float m_MovementDuration = 5f;

        [SerializeField, Tooltip("The pause before returning to original position or moving again.")]
        private float m_PauseDuration = 5f;

        [SerializeField, Tooltip("The delay before the first rotation.")]
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

        private float m_TimeOffset = 0f;
        private float m_CurrentTime = 0f;

#if UNITY_EDITOR
        protected void OnValidate()
        {
            m_MovementDuration = Mathf.Clamp(m_MovementDuration, 0.5f, 30f);
            m_PauseDuration = Mathf.Clamp(m_PauseDuration, 0.5f, 30f);
        }
#endif

        protected override void Initialise()
        {
            base.Initialise();

            m_TimeOffset = m_PauseDuration - m_StartPause;
        }

        protected override Vector3 GetNextPosition()
        {
            return fixedPosition;
        }

        protected override Quaternion GetNextRotation()
        {
            float fullPhaseTime = m_MovementDuration + m_PauseDuration;

            m_CurrentTime = Time.timeSinceLevelLoad + m_TimeOffset;

            int step = Mathf.FloorToInt(m_CurrentTime / fullPhaseTime);
            if (step < 0)
                return Quaternion.identity;
            else
            {
                float timeWithinStep = m_CurrentTime - step * fullPhaseTime;
                if (timeWithinStep <= m_PauseDuration)
                    return Quaternion.Euler(m_Rotation * step);
                else
                {
                    Vector3 fromRotation = m_Rotation * step;
                    Vector3 toRotation = m_Rotation * (step + 1);
                    float lerp = (timeWithinStep - m_PauseDuration) / m_MovementDuration;

                    // Lerp the positions
                    switch (m_EasingMode)
                    {
                        case EasingMode.Quadratic:
                            return Quaternion.Euler(Vector3.Lerp(fromRotation, toRotation, EasingFunctions.EaseInOutQuadratic(lerp)));
                        case EasingMode.Cubic:
                            return Quaternion.Euler(Vector3.Lerp(fromRotation, toRotation, EasingFunctions.EaseInOutCubic(lerp)));
                        case EasingMode.Quartic:
                            return Quaternion.Euler(Vector3.Lerp(fromRotation, toRotation, EasingFunctions.EaseInOutQuartic(lerp)));
                        default:
                            return Quaternion.Euler(Vector3.Lerp(fromRotation, toRotation, lerp));
                    }
                }
            }
        }

        private static readonly NeoSerializationKey k_TimeOffsetKey = new NeoSerializationKey("timeOffset");

        public override void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            base.WriteProperties(writer, nsgo, saveMode);

            writer.WriteValue(k_TimeOffsetKey, m_CurrentTime);
        }

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);

            reader.TryReadValue(k_TimeOffsetKey, out m_TimeOffset, m_TimeOffset);
            m_TimeOffset -= Time.timeSinceLevelLoad;
        }
    }
}
