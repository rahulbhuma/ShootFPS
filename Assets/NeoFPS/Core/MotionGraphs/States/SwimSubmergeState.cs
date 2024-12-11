#if !NEOFPS_FORCE_QUALITY && (UNITY_ANDROID || UNITY_IOS || UNITY_TIZEN || (UNITY_WSA && NETFX_CORE) || NEOFPS_FORCE_LIGHTWEIGHT)
#define NEOFPS_LIGHTWEIGHT
#endif

using UnityEngine;
using NeoFPS.CharacterMotion.MotionData;
using NeoFPS.CharacterMotion.Parameters;
using NeoSaveGames.Serialization;

namespace NeoFPS.CharacterMotion.States
{
    [MotionGraphElement("Swimming/Swim Submerge", "Submerge")]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgs-swimsubmergestate.html")]
    public class SwimSubmergeState : MotionGraphState
    {
        [SerializeField, Tooltip("The transform parameter which contains the transform of the water zone object")]
        private TransformParameter m_WaterZoneParameter = null;
        [SerializeField, Tooltip("The distance below the surface of the water to submerge")]
        private float m_SubmergeDistance = 0.5f;
        [SerializeField, Tooltip("The time to take while submerging (will be instant if already below submerge distance)")]
        private float m_Duration = 1f;

        private Vector3 m_OutMoveVector = Vector3.zero;
        private Transform m_WaterZoneTransform = null;
        private IWaterZone m_WaterZone = null;
        private Vector3 m_EntryVelocity = Vector3.zero;
        private float m_EntryHeight = 0f;
        private float m_Lerp = 0f;

        public override bool CheckCanEnter()
        {
            return m_WaterZoneParameter != null;
        }

        public override Vector3 moveVector
        {
            get { return m_OutMoveVector; }
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

        public override bool completed
        {
            get { return m_Lerp >= 1f; }
        }

        public override void OnValidate()
        {
            base.OnValidate();

            m_SubmergeDistance = Mathf.Clamp(m_SubmergeDistance, 0.1f, 10f);
            m_Duration = Mathf.Clamp(m_Duration, 0.1f, 10f);
        }

        public override void OnEnter()
        {
            base.OnEnter();

            CheckWaterZone();

            // Get the water surface from the top sphere of the character
            var highest = WaterZoneHelpers.GetHighestSphereCenter(controller);
            highest.y += characterController.radius;
            var surface = m_WaterZone.SurfaceInfoAtPosition(highest);

            // Get the height relative to the surface
            m_EntryHeight = highest.y - surface.height;
            if (m_EntryHeight < -m_SubmergeDistance)
                m_Lerp = 1f;
            else
            {
                // Reset the lerp
                m_Lerp = 0f;

                // Get the entry velocity
                m_EntryVelocity = characterController.velocity;
                m_EntryVelocity.y = 0f;
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            m_Lerp = 0f;
            m_OutMoveVector = Vector3.zero;
        }

        void CheckWaterZone()
        {
            // Get the water zone
            if (m_WaterZoneParameter != null)
            {
                if (m_WaterZoneTransform != m_WaterZoneParameter.value)
                {
                    m_WaterZoneTransform = m_WaterZoneParameter.value;
                    if (m_WaterZoneTransform != null)
                        m_WaterZone = m_WaterZoneTransform.GetComponent<IWaterZone>();
                }
            }
        }

        public override void Update()
        {
            base.Update();

            CheckWaterZone();

            // Update lerp
            m_Lerp += Time.deltaTime / m_Duration;
            if (m_Lerp > 1f)
                m_Lerp = 1f;

            // Decelerate horizontal velocity
            var horizontalV = m_EntryVelocity;// * (1f - m_Lerp * m_Lerp);
            m_OutMoveVector = horizontalV * Time.deltaTime;

            // Get the water surface from the top sphere of the character
            var highest = WaterZoneHelpers.GetHighestSphereCenter(controller);
            highest.y += characterController.radius;
            var surface = m_WaterZone.SurfaceInfoAtPosition(highest);

            // Get the target height relative to the surface
            var targetHeight = Mathf.Lerp(m_EntryHeight, -0.1f, EasingFunctions.EaseInOutQuadratic(m_Lerp));
            var actualHeight = surface.height + targetHeight;

            // Add lerped height to move vector
            m_OutMoveVector.y = actualHeight - highest.y;

            // Flow?
        }

        public override void CheckReferences(IMotionGraphMap map)
        {
            m_WaterZoneParameter = map.Swap(m_WaterZoneParameter);
            base.CheckReferences(map);
        }


        #region SAVE / LOAD

        private static readonly NeoSerializationKey k_EntryVelocityKey = new NeoSerializationKey("entryVelocity");
        private static readonly NeoSerializationKey k_EntryHeightKey = new NeoSerializationKey("entryHeight");
        private static readonly NeoSerializationKey k_LerpKey = new NeoSerializationKey("lerp");

        public override void WriteProperties(INeoSerializer writer)
        {
            base.WriteProperties(writer);
            writer.WriteValue(k_EntryVelocityKey, m_EntryVelocity);
            writer.WriteValue(k_EntryHeightKey, m_EntryHeight);
            writer.WriteValue(k_LerpKey, m_Lerp);
        }

        public override void ReadProperties(INeoDeserializer reader)
        {
            base.ReadProperties(reader);
            reader.TryReadValue(k_EntryVelocityKey, out m_EntryVelocity, m_EntryVelocity);
            reader.TryReadValue(k_EntryHeightKey, out m_EntryHeight, m_EntryHeight);
            reader.TryReadValue(k_LerpKey, out m_Lerp, m_Lerp);
        }

        #endregion
    }
}