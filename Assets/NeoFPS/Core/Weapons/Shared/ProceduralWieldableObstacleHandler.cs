using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using UnityEngine;

namespace NeoFPS.WieldableTools
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-proceduralwieldableobstaclehandler.html")]
    public class ProceduralWieldableObstacleHandler : BaseWieldableObstacleHandler
    {
        [Header("Procedural Animation")]

        [SerializeField, Tooltip("The target position offset for the obstructed pose.")]
        private Vector3 m_PositionOffset = new Vector3(0.05f, -0.05f, -0.1f);
        [SerializeField, Tooltip("The target rotation offset for the obstructed pose.")]
        private Vector3 m_RotationOffset = new Vector3(10f, -30f, 15f);
        [SerializeField, Tooltip("The time taken to blend in and out of the obstructed pose.")]
        private float m_BlendTime = 0.5f;
        [SerializeField, Tooltip("The time to wait after the weapon becomes unobstructed before it can be used again.")]
        private float m_UnblockDelay = 0.4f;

        // These are a bit of a hack to get around clashes with the procedural sprint handler. Need a better way of prioritising poses in 1.2
        [SerializeField, Tooltip("[OPTIONAL] - A switch parameter on the motion graph which tells the character if it can sprint or not.")]
        private string m_CanSprintParamKey = "canSprint";
        [SerializeField, Tooltip("[OPTIONAL] - A switch parameter on the motion graph which tells the character if it is sprinting or not.")]
        private string m_IsSprintingParamKey = "isSprinting";

        private SwitchParameter m_CanSprintParameter = null;
        private SwitchParameter m_IsSprintingParameter = null;
        private IPoseHandler m_PoseHandler = null;
        private PoseInformation m_PoseInfo = null;

        protected override void Awake()
        {
            base.Awake();

            m_PoseHandler = GetComponent<IPoseHandler>();

            m_PoseInfo = new PoseInformation(m_PositionOffset, Quaternion.Euler(m_RotationOffset),
                PoseTransitions.PositionEaseInOutCubic, PoseTransitions.RotationEaseInOutCubic,
                PoseTransitions.PositionEaseInOutCubic, PoseTransitions.RotationEaseInOutCubic
                );
        }

        protected override int GetBlockingReleaseFrames()
        {
            return Mathf.CeilToInt(m_UnblockDelay / Time.fixedDeltaTime);
        }

        protected override void OnObstructedChanged(bool obstructed)
        {
            base.OnObstructedChanged(obstructed);

            // Set new pose
            if (obstructed)
            {
                if (m_CanSprintParameter != null)
                    m_CanSprintParameter.on = false;
                if (m_IsSprintingParameter != null)
                    m_IsSprintingParameter.on = false;

                m_PoseHandler.PushPose(m_PoseInfo, this, m_BlendTime, PosePriorities.AvoidObstacle);
            }
            else
            {
                if (m_CanSprintParameter != null)
                    m_CanSprintParameter.on = true;

                m_PoseHandler.PopPose(this, m_BlendTime);
            }
        }

        protected override void OnWielderChanged(ICharacter wielder)
        {
            base.OnWielderChanged(wielder);

            m_CanSprintParameter = null;
            m_IsSprintingParameter = null;

            if (wielder != null && !string.IsNullOrEmpty(m_CanSprintParamKey))
            {
                var mc = wielder.GetComponent<MotionController>();
                if (mc != null)
                {
                    var mg = mc.motionGraph;
                    if (mg != null)
                    {
                        m_CanSprintParameter = mg.GetSwitchProperty(m_CanSprintParamKey);
                        m_IsSprintingParameter = mg.GetSwitchProperty(m_IsSprintingParamKey);
                    }
                }
            }
        }
    }
}