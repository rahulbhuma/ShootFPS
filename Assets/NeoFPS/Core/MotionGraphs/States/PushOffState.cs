#if !NEOFPS_FORCE_QUALITY && (UNITY_ANDROID || UNITY_IOS || UNITY_TIZEN || (UNITY_WSA && NETFX_CORE) || NEOFPS_FORCE_LIGHTWEIGHT)
#define NEOFPS_LIGHTWEIGHT
#endif

using UnityEngine;
using NeoFPS.CharacterMotion.MotionData;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPS.CharacterMotion.States
{
    [MotionGraphElement("Instant/Push Off", "Push Off")]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgs-pushoffstate.html")]
    public class PushOffState : MotionGraphState
    {
        [SerializeField, Tooltip("The world direction to push in (you can fill this parameter using enhanced cast conditions)")]
        private VectorParameter m_PushDirection = null;

        [SerializeField, Tooltip("An additional upward rotation applied to the push direction. Resulting direction won't rotate past up/down.")]
        private FloatDataReference m_PushUpAngle = new FloatDataReference(30f);

        [SerializeField, Tooltip("The speed to along the rotated push direction.")]
        private FloatDataReference m_PushSpeed = new FloatDataReference(5f);

        [SerializeField, Tooltip("Should the resulting velocity be additive to the original character velocity or ignore it.")]
        private bool m_Additive = true;

        [SerializeField, Tooltip("The minimum distance the state should attempt to move before completing. This prevents small jump heights or a very small fixed time step causing the movement to be too small to overcome ground snapping / detection.")]
        private float m_MinimumDistance = 0.05f;

        private Vector3 m_OutVelocity = Vector3.zero;
        private float m_AttemptedDistance = 0f;
        private bool m_Completed = false;

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
                if (m_PushDirection != null)
                {
                    Vector3 dir = m_PushDirection.value.normalized;
                    
                    // Rotate the push direction upwards
                    if (Mathf.Abs(m_PushUpAngle.value) > 0.1f)
                        dir = Vector3.RotateTowards(dir, characterController.up, m_PushUpAngle.value * Mathf.Deg2Rad, 0f);

                    // Apply the push velocity
                    if (m_Additive)
                        m_OutVelocity = characterController.velocity + (dir * m_PushSpeed.value);
                    else
                        m_OutVelocity = (dir * m_PushSpeed.value);
                }
                else
                    m_OutVelocity = characterController.velocity;

                m_Completed = true;
            }

            // Increment attempted move
            m_AttemptedDistance += m_OutVelocity.magnitude * Time.deltaTime;
        }

        public override void CheckReferences(IMotionGraphMap map)
        {
            m_PushDirection = map.Swap(m_PushDirection);
            m_PushUpAngle.CheckReference(map);
            m_PushSpeed.CheckReference(map);
            base.CheckReferences(map);
        }
    }
}