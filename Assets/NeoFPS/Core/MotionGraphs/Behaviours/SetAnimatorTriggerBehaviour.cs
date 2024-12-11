using UnityEngine;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using NeoSaveGames.Serialization;
using System.Collections;

namespace NeoFPS.CharacterMotion
{
    [MotionGraphElement("Animation/SetAnimatorTrigger", "SetAnimatorTriggerBehaviour")]
    public class SetAnimatorTriggerBehaviour : MotionGraphBehaviour
    {
        [SerializeField, Tooltip("The name of the animator parameter to write to.")]
        private string m_ParameterName = string.Empty;
        [SerializeField, Tooltip("The action to perform on entering the state / subgraph.")]
        private Action m_OnEnter = Action.Set;
        [SerializeField, Tooltip("The action to perform on exiting the state / subgraph.")]
        private Action m_OnExit = Action.Ignore;

        private int m_ParameterHash = -1;

        public enum Action
        {
            Set,
            Reset,
            Ignore
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
            switch(m_OnEnter)
            {
                case Action.Set:
                    controller.bodyAnimator.SetTrigger(m_ParameterHash);
                    break;
                case Action.Reset:
                    controller.bodyAnimator.ResetTrigger(m_ParameterHash);
                    break;
            }
        }

        public override void OnExit()
        {
            switch (m_OnExit)
            {
                case Action.Set:
                    controller.bodyAnimator.SetTrigger(m_ParameterHash);
                    break;
                case Action.Reset:
                    controller.bodyAnimator.ResetTrigger(m_ParameterHash);
                    break;
            }
        }
    }
}