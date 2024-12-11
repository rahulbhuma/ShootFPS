using UnityEngine;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPS
{
    [MotionGraphElement("Camera/CameraShake (Continuous)", "CameraShakeBehaviour")]
    public class CameraShakeBehaviour : MotionGraphBehaviour
    {
        [SerializeField, Tooltip("An optional float parameter to multiply the shake value by. This allows for increasing shake while falling, etc.")]
        private FloatParameter m_ShakeMultiplier = null;

        [SerializeField, Range(0f, 1f), Tooltip("The strength of the shake effect (how this translates to camera movement is set in the CameraShake component on the character).")]
        private float m_ShakeStrength = 0.25f;

        private ShakeHandler m_Shake = null;

        public override void Initialise(MotionGraphConnectable o)
        {
            base.Initialise(o);

            var character = controller.GetComponent<ICharacter>();
            if (character != null)
                m_Shake = character.headTransformHandler.GetComponent<ShakeHandler>();
        }

        public override void OnEnter()
        {
            if (m_Shake != null)
            {
                if (m_ShakeMultiplier != null)
                    m_Shake.continuousShake = m_ShakeStrength * m_ShakeMultiplier.value;
                else
                    m_Shake.continuousShake = m_ShakeStrength;
            }
        }

        public override void Update()
        {
            if (m_ShakeMultiplier != null && m_Shake != null)
                m_Shake.continuousShake = m_ShakeStrength * m_ShakeMultiplier.value;
        }

        public override void OnExit()
        {
            if (m_Shake != null)
                m_Shake.continuousShake = 0f;
        }
    }
}