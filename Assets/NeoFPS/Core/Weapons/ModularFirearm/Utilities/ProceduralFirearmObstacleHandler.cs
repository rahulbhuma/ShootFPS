using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using NeoSaveGames;
using NeoSaveGames.Serialization;
using UnityEngine;

namespace NeoFPS.ModularFirearms
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-proceduralfirearmobstaclehandler.html")]
    public class ProceduralFirearmObstacleHandler : FirearmObstacleHandler, INeoSerializableComponent
    {
        [SerializeField, Tooltip("The target position offset for the obstructed pose.")]
        private Vector3 m_PositionOffset = new Vector3(0.05f, -0.05f, -0.1f);
        [SerializeField, Tooltip("The target rotation offset for the obstructed pose.")]
        private Vector3 m_RotationOffset = new Vector3(10f, -30f, 15f);
        [SerializeField, Tooltip("The time taken to blend in and out of the obstructed pose.")]
        private float m_BlendTime = 0.5f;

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
        
        protected override int GetTriggerBlockFrames()
        {
            return (int)(m_BlendTime / Time.fixedDeltaTime) - 5;
        }

        protected override void OnBlockedChanged(bool blocked)
        {
            // Set new pose
            if (blocked)
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

        private static readonly NeoSerializationKey k_PoseIdKey = new NeoSerializationKey("poseID");

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            //if (m_PoseID != -1)
            //    writer.WriteValue(k_PoseIdKey, m_PoseID);
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            //if (reader.TryReadValue(k_PoseIdKey, out m_PoseID, -1))
            //{
            //    // Build pose
            //    var p = m_PoseHandler.GetPose(m_PoseID);
            //    if (p != null)
            //    {
            //        p.interpolatePositionIn = PoseTransitions.PositionEaseInOutQuadratic;
            //        p.interpolatePositionOut = PoseTransitions.PositionEaseInOutQuadratic;
            //        p.interpolateRotationIn = PoseTransitions.RotationEaseInOutQuadratic;
            //        p.interpolateRotationOut = PoseTransitions.RotationEaseInOutQuadratic;
            //        m_PoseInfo = p;
            //    }
            //}
        }
    }
}