using System;
using UnityEngine;

namespace NeoFPS.CharacterMotion.Behaviours
{
    [MotionGraphElement("Character/SpineAimBehaviour", "SpineAimBehaviour")]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgb-spineaimbehaviour.html")]
    public class SpineAimBehaviour : MotionGraphBehaviour
    {
        [SerializeField, Tooltip("How the spine aim matching should be set on entering the state or sub-graph")]
        private SpineAimState m_OnEnter = SpineAimState.AdaptToAim;

        [SerializeField]
        private float m_OnEnterTime = 0.5f;

        [SerializeField, Tooltip("How the spine aim matching should be set on exiting the state or sub-graph")]
        private SpineAimState m_OnExit = SpineAimState.Disable;

        [SerializeField]
        private float m_OnExitTime = 0.5f;

        public enum SpineAimState
        {
            AdaptToAim,
            Disable,
            NoChange
        }
        
        private ProceduralSpineAimMatcher m_SpineAimMatcher = null;

        public override void Initialise(MotionGraphConnectable o)
        {
            base.Initialise(o);

            m_SpineAimMatcher = controller.bodyAnimator.GetComponent<ProceduralSpineAimMatcher>();
            if (m_SpineAimMatcher == null)
                enabled = false;
        }

        public override void OnEnter()
        {
            switch (m_OnEnter)
            {
                case SpineAimState.AdaptToAim:
                    m_SpineAimMatcher.EnableAimMatching(m_OnEnterTime);
                    break;
                case SpineAimState.Disable:
                    m_SpineAimMatcher.DisableAimAdapting(m_OnEnterTime);
                    break;
            }
        }

        public override void OnExit()
        {
            switch (m_OnExit)
            {
                case SpineAimState.AdaptToAim:
                    m_SpineAimMatcher.EnableAimMatching(m_OnExitTime);
                    break;
                case SpineAimState.Disable:
                    m_SpineAimMatcher.DisableAimAdapting(m_OnExitTime);
                    break;
            }
        }
    }
}
