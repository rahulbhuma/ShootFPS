using UnityEngine;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using NeoSaveGames.Serialization;
using System.Collections;

namespace NeoFPS.CharacterMotion
{
    [MotionGraphElement("Animation/SetRootMotionStrength", "SetRootMotionStrengthBehaviour")]
    public class SetRootMotionStrengthBehaviour : MotionGraphBehaviour
    {
        [SerializeField, Tooltip("When should the parameter be modified.")]
        private When m_When = When.OnEnter;
        [SerializeField, Range(0f, 1f), Tooltip("The root motion strength to set on entering this state / subgraph.")]
        private float m_OnEnterValue = 0f;
        [SerializeField, Tooltip("The blend time for the new root motion value on entry.")]
        private float m_OnEnterBlendTime = 0f;
        [SerializeField, Range(0f, 1f), Tooltip("The root motion strength to set on exiting the state / subgraph.")]
        private float m_OnExitValue = 0f;
        [SerializeField, Tooltip("The blend time for the new root motion value on exit.")]
        private float m_OnExitBlendTime = 0f;

        [SerializeField, Range(0f, 1f), Tooltip("Damping smooths out the root motion values that the animator returns.")]
        private float m_RootMotionDamping = 0.25f;
        [SerializeField, Tooltip("A multiplier applied to the root motion position changes.")]
        private float m_PositionMultiplier = 1f;
        [SerializeField, Tooltip("A multiplier applied to the root motion rotation changes.")]
        private float m_RotationMultiplier = 1f;

        public enum When
        {
            OnEnter,
            OnExit,
            Both
        }

        public override void Initialise(MotionGraphConnectable o)
        {
            base.Initialise(o);

            if (controller.bodyAnimator == null || controller.rootMotionHandler == null) 
                enabled = false;
        }

        public override void OnEnter()
        {
            if (m_When != When.OnExit)
            {
                controller.SetRootMotionStrength(m_OnEnterValue, m_OnEnterBlendTime);
                controller.rootMotionDamping = m_RootMotionDamping;
                controller.rootMotionDamping = m_PositionMultiplier;
                controller.rootMotionDamping = m_RotationMultiplier;
            }
        }

        public override void OnExit()
        {
            if (m_When != When.OnEnter)
            {
                controller.SetRootMotionStrength(m_OnExitValue, m_OnExitBlendTime);
                controller.rootMotionDamping = m_RootMotionDamping;
                controller.rootMotionDamping = m_PositionMultiplier;
                controller.rootMotionDamping = m_RotationMultiplier;
            }
        }
    }
}