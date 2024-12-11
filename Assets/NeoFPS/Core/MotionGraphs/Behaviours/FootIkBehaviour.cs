using System;
using UnityEngine;

namespace NeoFPS.CharacterMotion.Behaviours
{
    [MotionGraphElement("Character/FootIkBehaviour", "FootIkBehaviour")]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgb-footikbehaviour.html")]
    public class FootIkBehaviour : MotionGraphBehaviour
    {
        [SerializeField, Tooltip("How the character's foot IK should be set on entering the state or sub-graph")]
        private IkState m_OnEnter = IkState.EnableIK;

        [SerializeField]
        private float m_OnEnterTime = 0.5f;

        [SerializeField, Tooltip("How the character's foot IK should be set on exiting the state or sub-graph")]
        private IkState m_OnExit = IkState.DisableIK;

        [SerializeField]
        private float m_OnExitTime = 0.5f;

        public enum IkState
        {
            EnableIK,
            DisableIK,
            NoChange
        }

        private FirstPersonBody m_FirstPersonBody = null;

        public override void Initialise(MotionGraphConnectable o)
        {
            base.Initialise(o);

            m_FirstPersonBody = controller.GetComponentInChildren<FirstPersonBody>();
            if (m_FirstPersonBody == null)
                enabled = false;
        }

        public override void OnEnter()
        {
            switch (m_OnEnter)
            {
                case IkState.EnableIK:
                    m_FirstPersonBody.SetIkState(true, m_OnEnterTime);
                    break;
                case IkState.DisableIK:
                    m_FirstPersonBody.SetIkState(false, m_OnEnterTime);
                    break;
            }
        }

        public override void OnExit()
        {
            switch (m_OnExit)
            {
                case IkState.EnableIK:
                    m_FirstPersonBody.SetIkState(true, m_OnExitTime);
                    break;
                case IkState.DisableIK:
                    m_FirstPersonBody.SetIkState(false, m_OnExitTime);
                    break;
            }
        }
    }
}
