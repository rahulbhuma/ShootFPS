using UnityEngine;
using NeoFPS.CharacterMotion.MotionData;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPS.CharacterMotion.States
{
    [MotionGraphElement("Instant/Boost Pad", "BoostPad")]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgs-boostpadstate.html")]
    public class BoostPadState : MotionGraphState
    {
        [SerializeField, Tooltip("The movement velocity to apply.")]
		private VectorParameter m_BoostVector = null;

        [SerializeField, Tooltip("A multiplier for the movement velocity.")]
		private FloatDataReference m_Multiplier = new FloatDataReference(1f);

        [SerializeField, Tooltip("How the boost vector is applied to the character. Options are: " +
            "Absolute sets the character velocity, " +
            "Additive adds the boost to the character velocity, " +
            "MaintainPerpendicular sets the velocity in the direction of the boost, but keeps any velocity along the plane perpendicular to the boost.")]
        private BoostMode m_BoostMode = BoostMode.Absolute;

        [SerializeField, Tooltip("The minimum distance the state should attempt to move before completing. This prevents small jump heights or a very small fixed time step causing the movement to be too small to overcome ground snapping / detection.")]
        private float m_MinimumDistance = 0.05f;

        private Vector3 m_OutVelocity = Vector3.zero;
        private float m_AttemptedDistance = 0f;
        private bool m_Completed = false;

        enum BoostMode
        {
            Absolute,
            Additive,
            MaintainPerpendicular
        }

        public override bool completed
        {
            get { return m_Completed && m_AttemptedDistance >= m_MinimumDistance; }
        }

        public override Vector3 moveVector
        {
            get { return m_OutVelocity * Time.deltaTime; }
        }

        public override bool applyGravity
        {
            get { return false; }
        }

        public override bool applyGroundingForce
        {
            get { return false; }
        }

        public override bool ignorePlatformMove
        {
            get { return false; }
        }

        public override void OnValidate()
        {
            base.OnValidate();
        }

        public override void OnEnter()
        {
            base.OnEnter();
            m_Completed = false;
            m_AttemptedDistance = 0f;
        }

        public override void OnExit()
        {
            base.OnExit();
            m_Completed = false;
            m_OutVelocity = Vector3.zero;
        }

        public override void Update()
        {
            base.Update();

            if (!m_Completed)
            {
                if (m_BoostVector != null)
                {
                    switch (m_BoostMode)
                    {
                        case BoostMode.Absolute:
                            m_OutVelocity = m_BoostVector.value * m_Multiplier.value;
                            break;
                        case BoostMode.Additive:
                            m_OutVelocity = characterController.velocity + m_BoostVector.value * m_Multiplier.value;
                            break;
                        case BoostMode.MaintainPerpendicular:
                            var perpendicular = Vector3.ProjectOnPlane(characterController.velocity, m_BoostVector.value.normalized);
                            m_OutVelocity = m_BoostVector.value * m_Multiplier.value + perpendicular;
                            break;
                    }

                    m_BoostVector.value = Vector3.zero;
                }
                m_Completed = true;
            }
            else
                m_OutVelocity = characterController.velocity;

            // Increment attempted move
            m_AttemptedDistance += m_OutVelocity.magnitude * Time.deltaTime;
        }

        public override void CheckReferences(IMotionGraphMap map)
        {
            m_BoostVector = map.Swap(m_BoostVector);
            m_Multiplier.CheckReference(map);
            base.CheckReferences(map);
        }
    }
}