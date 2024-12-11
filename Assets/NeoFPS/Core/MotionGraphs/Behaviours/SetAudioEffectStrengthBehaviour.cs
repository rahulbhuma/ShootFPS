using UnityEngine;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using NeoSaveGames.Serialization;
using System.Collections;

namespace NeoFPS.CharacterMotion
{
    [MotionGraphElement("Audio/SetAudioEffectStrength", "SetAudioEffectStrengthBehaviour")]
    public class SetAudioEffectStrengthBehaviour : MotionGraphBehaviour
    {
        [SerializeField, Tooltip("The name of the audio effect on the character camera's FpsCharacterAudioEffects component.")]
        private string m_EffectName = string.Empty;
        [SerializeField, Tooltip("When should the parameter be modified.")]
        private When m_When = When.OnEnter;
        [SerializeField, Tooltip("The strength the effect should be set to on entering the state / subgraph.")]
        private float m_OnEnterValue = 1f;
        [SerializeField, Tooltip("The strength the effect should be set to on exiting the state / subgraph.")]
        private float m_OnExitValue = 0f;
        [SerializeField, Tooltip("The time taken to blend from the current strength to the target strength.")]
        private float m_BlendDuration = 0.5f;

        private FpsCharacterAudioEffects m_Effects = null;

        public enum When
        {
            OnEnter,
            OnExit,
            Both
        }

        public enum ValueType
        {
            ConstantValue,
            MotionGraphParameter
        }

        public override void Initialise(MotionGraphConnectable o)
        {
            base.Initialise(o);

            var fpCamera = controller.GetComponentInChildren<FirstPersonCameraBase>();
            m_Effects = fpCamera.unityCamera.GetComponent<FpsCharacterAudioEffects>();
            if (m_Effects == null)
                enabled = false;
        }

        public override void OnEnter()
        {
            if (m_When != When.OnExit)
                m_Effects.SetEffectStrength(m_EffectName, m_OnEnterValue, m_BlendDuration);
        }

        public override void OnExit()
        {
            if (m_When != When.OnEnter)
                m_Effects.SetEffectStrength(m_EffectName, m_OnExitValue, m_BlendDuration);
        }
    }
}