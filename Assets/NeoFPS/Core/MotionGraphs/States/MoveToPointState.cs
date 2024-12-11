using UnityEngine;
using NeoFPS.CharacterMotion.MotionData;
using NeoFPS.CharacterMotion.Parameters;
using NeoSaveGames.Serialization;

namespace NeoFPS.CharacterMotion.States
{
    [MotionGraphElement("Misc/Move To Point", "Move To Point")]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgs-movetopointstate.html")]
    public class MoveToPointState : MotionGraphState
    {
        [SerializeField, Tooltip("The position to move to")]
        private VectorParameter m_TargetPosition = null;

        [SerializeField, Tooltip("The time required to reach the target")]
        private float m_Duration = 0.25f;

        [SerializeField, Tooltip("The interpolation method from start to end")]
        private Interpolation m_Interpolation = Interpolation.EaseOutCubic;

        [SerializeField, Tooltip("Should collisions be disabled for the duration of the movement")]
        private bool m_DisableCollisions = true;

        private Vector3 m_StartPoint = Vector3.zero;
        private Vector3 m_OutMove = Vector3.zero;
        private float m_Lerp = 0f;
        private bool m_CollisionsEnabled = true;

        public enum Interpolation
        {
            Linear,
            EaseOutQuadratic,
            EaseOutCubic,
            Spring,
            Bounce
        }

        public override bool applyGravity
        {
            get { return false; }
        }

        public override bool applyGroundingForce
        {
            get { return false; }
        }

        public override Vector3 moveVector
        {
            get { return m_OutMove; }
        }

        public override bool completed
        {
            get { return m_Lerp >= 1f; }
        }

        public override void OnValidate()
        {
            base.OnValidate();

            if (m_Duration < 0.1f)
                m_Duration = 0.1f;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            m_StartPoint = controller.localTransform.position;
            m_Lerp = 0f;

            // Disable collisions
            if (m_DisableCollisions)
            {
                m_CollisionsEnabled = characterController.collisionsEnabled;
                characterController.collisionsEnabled = false;
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            m_OutMove = Vector3.zero;
            m_Lerp = 0f;

            // Restore collisions
            if (m_DisableCollisions)
                characterController.collisionsEnabled = m_CollisionsEnabled;
        }

        public override void Update()
        {
            base.Update();

            // Get linear interp
            m_Lerp += Time.deltaTime / m_Duration;
            if (m_Lerp > 1f)
                m_Lerp = 1f;

            // Convert to eased
            float eased = m_Lerp;
            switch (m_Interpolation)
            {
                case Interpolation.EaseOutQuadratic:
                    eased = EasingFunctions.EaseOutQuadratic(eased);
                    break;
                case Interpolation.EaseOutCubic:
                    eased = EasingFunctions.EaseOutCubic(eased);
                    break;
                case Interpolation.Spring:
                    eased = EasingFunctions.EaseInSpring(eased);
                    break;
                case Interpolation.Bounce:
                    eased = EasingFunctions.EaseInBounce(eased);
                    break;
            }

            // Get target position
            Vector3 target = Vector3.Lerp(m_StartPoint, m_TargetPosition.value, eased);

            // Get the offset from current
            m_OutMove = target - controller.localTransform.position;
        }

        public override void CheckReferences(IMotionGraphMap map)
        {
            base.CheckReferences(map);
            m_TargetPosition = map.Swap(m_TargetPosition);
        }

        #region SAVE / LOAD

        private static readonly NeoSerializationKey k_LerpKey = new NeoSerializationKey("lerp");
        private static readonly NeoSerializationKey k_StartKey = new NeoSerializationKey("start");

        public override void WriteProperties(INeoSerializer writer)
        {
            base.WriteProperties(writer);

            writer.WriteValue(k_LerpKey, m_Lerp);
            writer.WriteValue(k_StartKey, m_StartPoint);
        }

        public override void ReadProperties(INeoDeserializer reader)
        {
            base.ReadProperties(reader);

            reader.TryReadValue(k_LerpKey, out m_Lerp, m_Lerp);
            reader.TryReadValue(k_StartKey, out m_StartPoint, m_StartPoint);
        }

        #endregion
    }
}