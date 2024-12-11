using UnityEngine;
using NeoFPS.CharacterMotion.MotionData;
using NeoFPS.CharacterMotion.Parameters;
using NeoSaveGames.Serialization;

namespace NeoFPS.CharacterMotion.States
{
    [MotionGraphElement("Misc/Match Transform", "Match Transform")]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgs-matchtransformstate.html")]
    public class MatchTransformState : MotionGraphState
    {
        [SerializeField, Tooltip("The transform to match to")]
        private TransformParameter m_TargetTransform = null;

        [SerializeField, Tooltip("What transform components to match")]
        private MatchingMode m_MatchingMode = MatchingMode.Position;

        [SerializeField, Tooltip("The time taken to blend from current position and velocity to match the transform")]
        private float m_BlendInTime = 0.5f;

        [SerializeField, Tooltip("Should collisions be disabled for the duration of the movement")]
        private bool m_DisableCollisions = true;

        public enum MatchingMode
        {
            Position,
            PositionAndUp,
            PositionAndDirection,
            All
        }

        private Vector3 m_OutMove = Vector3.zero;
        private Vector3 m_PreviousPosition = Vector3.zero;
        private float m_BlendIn = 0f;
        private bool m_CollisionsEnabled = true;

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
            get { return m_TargetTransform.value == null; }
        }

        public override void OnValidate()
        {
            base.OnValidate();

            m_BlendInTime = Mathf.Clamp(m_BlendInTime, 0f, 10f);
        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (m_TargetTransform.value != null)
                m_PreviousPosition = m_TargetTransform.value.position;
            m_BlendIn = 0f;

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
            m_BlendIn = 0f;

            // Restore collisions
            if (m_DisableCollisions)
                characterController.collisionsEnabled = m_CollisionsEnabled;
        }

        public override void Update()
        {
            base.Update();

            // Get linear interp
            m_BlendIn += Time.deltaTime / m_BlendInTime;
            if (m_BlendIn > 1f)
                m_BlendIn = 1f;

            // Get target position
            Transform targetTransform = m_TargetTransform.value;
            if (targetTransform != null)
            {
                Vector3 target = targetTransform.position;

                // Sort direction and/or up vector
                switch (m_MatchingMode)
                {
                    case MatchingMode.PositionAndDirection:
                        characterController.AddYawOffset(Vector3.SignedAngle(characterController.forward, targetTransform.forward, characterController.up));
                        break;
                    case MatchingMode.PositionAndUp:
                        characterController.characterGravity.up = targetTransform.up;
                        break;
                    case MatchingMode.All:
                        characterController.AddYawOffset(Vector3.SignedAngle(characterController.forward, targetTransform.forward, characterController.up));
                        characterController.characterGravity.up = targetTransform.up;
                        break;
                }

                // Get the offset from current
                m_OutMove = target - controller.localTransform.position;
            }
            else
                m_OutMove = Vector3.zero;
        }

        public override void CheckReferences(IMotionGraphMap map)
        {
            base.CheckReferences(map);
            m_TargetTransform = map.Swap(m_TargetTransform);
        }

        #region SAVE / LOAD

        private static readonly NeoSerializationKey k_LerpKey = new NeoSerializationKey("lerp");

        public override void WriteProperties(INeoSerializer writer)
        {
            base.WriteProperties(writer);

            if (m_BlendIn < 1f)
                writer.WriteValue(k_LerpKey, m_BlendIn);
        }

        public override void ReadProperties(INeoDeserializer reader)
        {
            base.ReadProperties(reader);

            reader.TryReadValue(k_LerpKey, out m_BlendIn, m_BlendIn);
        }

        #endregion
    }
}