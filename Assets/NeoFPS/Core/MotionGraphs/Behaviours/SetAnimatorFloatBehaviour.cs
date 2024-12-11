using UnityEngine;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using NeoSaveGames.Serialization;
using System.Collections;

namespace NeoFPS.CharacterMotion
{
    [MotionGraphElement("Animation/SetAnimatorFloat", "SetAnimatorFloatBehaviour")]
    public class SetAnimatorFloatBehaviour : MotionGraphBehaviour
    {
        [SerializeField, Tooltip("The name of the animator parameter to write to.")]
        private string m_ParameterName = string.Empty;
        [SerializeField, Tooltip("When should the parameter be modified.")]
        private When m_When = When.OnEnter;
        [SerializeField, Tooltip("What to change the animator to on entering the state / subgraph.")]
        private ValueType m_OnEnterType = ValueType.ConstantValue;
        [SerializeField, Tooltip("The value to write to the parameter on entering the state / subgraph.")]
        private float m_OnEnterValue = 0f;
        [SerializeField, Tooltip("The parameter that contains the value to write to the parameter on entering the state / subgraph.")]
        private FloatParameter m_OnEnterParameter = null;
        [SerializeField, Tooltip("What to change the animator to on exiting the state / subgraph.")]
        private ValueType m_OnExitType = ValueType.ConstantValue;
        [SerializeField, Tooltip("The value to write to the parameter on exiting the state / subgraph.")]
        private float m_OnExitValue = 0f;
        [SerializeField, Tooltip("The parameter that contains the value to write to the parameter on exiting the state / subgraph.")]
        private FloatParameter m_OnExitParameter = null;

        private int m_ParameterHash = -1;

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

            if (controller.bodyAnimator != null && !string.IsNullOrWhiteSpace(m_ParameterName))
                m_ParameterHash = Animator.StringToHash(m_ParameterName);
            else
                enabled = false;
        }

        public override void OnEnter()
        {
            if (m_When != When.OnExit)
            {
                if (m_OnEnterType == ValueType.ConstantValue)
                    controller.bodyAnimator.SetFloat(m_ParameterHash, m_OnEnterValue);
                else
                {
                    if (m_OnEnterParameter != null)
                        controller.bodyAnimator.SetFloat(m_ParameterHash, m_OnEnterParameter.value);
                }
            }
        }

        public override void OnExit()
        {
            if (m_When != When.OnEnter)
            {
                if (m_OnExitType == ValueType.ConstantValue)
                    controller.bodyAnimator.SetFloat(m_ParameterHash, m_OnExitValue);
                else
                {
                    if (m_OnExitParameter != null)
                        controller.bodyAnimator.SetFloat(m_ParameterHash, m_OnExitParameter.value);
                }
            }
        }

        public override void CheckReferences(IMotionGraphMap map)
        {
            m_OnEnterParameter = map.Swap(m_OnEnterParameter);
            m_OnExitParameter = map.Swap(m_OnExitParameter);
        }
    }
}