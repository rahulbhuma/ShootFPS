using UnityEngine;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPS
{
    [MotionGraphElement("Camera/CameraShake (OneShot)", "CameraShakeOneShotBehaviour")]
    public class CameraShakeOneShotBehaviour : MotionGraphBehaviour
    {
        [SerializeField, Tooltip("An optional float parameter to multiply the shake value by. This allows for increasing shake while falling, etc.")]
        private FloatParameter m_ShakeMultiplier = null;

        [SerializeField, Range(0f, 1f), Tooltip("The strength of the shake effect (how this translates to camera movement is set in the CameraShake component on the character).")]
        private float m_ShakeStrength = 0.25f;

        [SerializeField, Tooltip("How long should the shake last.")]
        private float m_ShakeDuration = 1f;

        [SerializeField, Tooltip("When should the camera shake.")]
        private When m_When = When.OnEnter;

        private ShakeHandler m_Shake = null;

        private enum When
        {
            OnEnter,
            OnExit,
            Both
        }

        public override void OnValidate()
        {
            m_ShakeDuration = Mathf.Clamp(m_ShakeDuration, 0.1f, 10f);
        }

        public override void Initialise(MotionGraphConnectable o)
        {
            base.Initialise(o);

            var character = controller.GetComponent<ICharacter>();
            if (character != null)
                m_Shake = character.headTransformHandler.GetComponent<ShakeHandler>();
            if (m_Shake == null)
                enabled = false;
        }

        void Shake()
        {
            if (m_ShakeMultiplier != null)
                m_Shake.Shake(m_ShakeStrength * m_ShakeMultiplier.value, m_ShakeDuration, false);
            else
                m_Shake.Shake(m_ShakeStrength, m_ShakeDuration, false);
        }

        public override void OnEnter()
        {
            if (m_When != When.OnExit)
                Shake();
        }

        public override void OnExit()
        {
            if (m_When != When.OnEnter)
                Shake();
        }
    }
}