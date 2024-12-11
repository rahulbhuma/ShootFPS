using UnityEngine;
using NeoFPS.CharacterMotion.MotionData;
using NeoFPS.CharacterMotion.Parameters;
using NeoSaveGames.Serialization;

namespace NeoFPS.CharacterMotion.States
{
    [MotionGraphElement("Misc/Root Motion", "RootMotion")]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgs-rootmotionstate.html")]
    public class RootMotionState : MotionGraphState
    {
        [Header("Animator Parameters")]

        [SerializeField, Tooltip("The animator parameter name the forward input value should be written to.")]
        private string m_ForwardParamName = "forward";
        [SerializeField, Tooltip("The animator parameter name the strafe input value should be written to. (positive = right)")]
        private string m_StrafeParamName = "strafe";

        [Header("Input Multipliers")]

        [SerializeField, Tooltip("A multiplier applied to the forward input before being sent to the animator parameter.")]
        private float m_ForwardMultiplier = 1f;
        [SerializeField, Tooltip("A multiplier applied to the backwards input before being sent to the animator parameter.")]
        private float m_BackwardMultiplier = 1f;
        [SerializeField, Tooltip("A multiplier applied to the strafe input before being sent to the animator parameter.")]
        private float m_StrafeMultiplier = 1f;

        [Header("Root Motion Multipliers")]

        [SerializeField, Tooltip("A multiplier applied to the root motion position changes.")]
        private float m_PositionMultiplier = 1f;
        [SerializeField, Tooltip("An optional parameter that will multiply the root motion movement velocity. This will be applied alongside the value above.")]
        private FloatParameter m_RootVelocityMultiplier = null;
        [SerializeField, Tooltip("A multiplier applied to the root motion rotation changes.")]
        private float m_RotationMultiplier = 1f;
        [SerializeField, Tooltip("An optional parameter that will multiply the root motion rotation rate. This will be applied alongside the value above.")]
        private FloatParameter m_RootRotationMultiplier = null;

        [Header("Misc")]

        [SerializeField, Tooltip("The amount of time it should take to blend into the root motion movement.")]
        private float m_BlendIn = 0.5f;
        [SerializeField, Range(0f, 1f), Tooltip("Damping smooths out the input values sent to the animator.")]
        private float m_InputDamping = 0.25f;
        [SerializeField, Range(0f, 1f), Tooltip("Damping smooths out the root motion values that the animator returns.")]
        private float m_MotionDamping = 0.25f;
        [SerializeField, Tooltip("Should gravity be applied while in the state.")]
        private bool m_ApplyGravity = true;
        [SerializeField, Tooltip("Should ground snapping be applied while in the state.")]
        private bool m_ApplyGroundingForce = true;
        [SerializeField, Tooltip("Should the character be affected by moving platforms while in the state.")]
        private bool m_IgnorePlatformMove = false;

        private Vector3 m_OutVelocity = Vector3.zero;
        private int m_ForwardParamHash = -1;
        private int m_StrafeParamHash = -1;
        private Vector2 m_Input = Vector2.zero;

#if UNITY_EDITOR
        public override void OnValidate()
        {
            base.OnValidate();

            m_BlendIn = Mathf.Clamp(m_BlendIn, 0f, 1f);

            m_ForwardMultiplier = Mathf.Clamp(m_ForwardMultiplier, 0f, 100f);
            m_BackwardMultiplier = Mathf.Clamp(m_BackwardMultiplier, 0f, 100f);
            m_StrafeMultiplier = Mathf.Clamp(m_StrafeMultiplier, 0f, 100f);
            m_PositionMultiplier = Mathf.Clamp(m_PositionMultiplier, -100f, 100f);
            m_RotationMultiplier = Mathf.Clamp(m_RotationMultiplier, -100f, 100f);
        }
#endif

        public override bool CheckCanEnter()
        {
            return controller.bodyAnimator != null && controller.rootMotionHandler != null;
        }

        public override Vector3 moveVector
        {
            get { return m_OutVelocity * Time.deltaTime; }
        }

        public override bool applyGravity
        {
            get { return m_ApplyGravity; }
        }

        public override bool applyGroundingForce
        {
            get { return m_ApplyGroundingForce; }
        }

        public override bool ignorePlatformMove
        {
            get { return m_IgnorePlatformMove; }
        }

        public override void Initialise(IMotionController c)
        {
            base.Initialise(c);

            if (controller.bodyAnimator != null)
            {
                m_ForwardParamHash = Animator.StringToHash(m_ForwardParamName);
                m_StrafeParamHash = Animator.StringToHash(m_StrafeParamName);
            }
            else
                Debug.LogError("Motion graph is attempting to use RootMotionState, but the MotionController has no body animator set.");
        }

        public override void OnEnter()
        {
            base.OnEnter();
            m_Input = controller.inputMoveDirection * controller.inputMoveScale;

            controller.SetRootMotionStrength(1f, m_BlendIn);
            controller.rootMotionDamping = m_MotionDamping;

            if (m_RootVelocityMultiplier != null)
                controller.rootMotionPositionMultiplier = m_PositionMultiplier * m_RootVelocityMultiplier.value;
            else
                controller.rootMotionPositionMultiplier = m_PositionMultiplier;

            if (m_RootRotationMultiplier != null)
                controller.rootMotionRotationMultiplier = m_RotationMultiplier * m_RootRotationMultiplier.value;
            else
                controller.rootMotionRotationMultiplier = m_RotationMultiplier;
        }

        public override void OnExit()
        {
            base.OnExit();
            controller.SetRootMotionStrength(0f, 0f);
        }

        public override void Update()
        {
            base.Update();

            // Use current velocity for smooth blending (will be overruled by root motion when not blending)
            m_OutVelocity = controller.characterController.velocity;

            // Get the damped input vector
            if (m_InputDamping > 0.001f)
            {
                float dampingLerp = Mathf.Lerp(0.25f, 0.01f, m_InputDamping);
                m_Input = Vector2.Lerp(m_Input, controller.inputMoveDirection * controller.inputMoveScale, dampingLerp);
            }
            else
                m_Input = controller.inputMoveDirection * controller.inputMoveScale;

            // Scale forwards/back
            float forwards = m_Input.y;
            if (forwards >= 0f)
                forwards *= m_ForwardMultiplier;
            else
                forwards *= m_BackwardMultiplier;

            // Scale strafe
            float strafe = m_Input.x * m_StrafeMultiplier;

            // Apply to animator parameters
            controller.bodyAnimator.SetFloat(m_ForwardParamHash, forwards);
            controller.bodyAnimator.SetFloat(m_StrafeParamHash, strafe);
        }

        public override void CheckReferences(IMotionGraphMap map)
        {
            m_RootVelocityMultiplier = map.Swap(m_RootVelocityMultiplier);
            m_RootRotationMultiplier = map.Swap(m_RootRotationMultiplier);
            base.CheckReferences(map);
        }
    }
}