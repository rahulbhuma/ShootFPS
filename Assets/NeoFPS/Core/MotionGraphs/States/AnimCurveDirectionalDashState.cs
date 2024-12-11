#if !NEOFPS_FORCE_QUALITY && (UNITY_ANDROID || UNITY_IOS || UNITY_TIZEN || (UNITY_WSA && NETFX_CORE) || NEOFPS_FORCE_LIGHTWEIGHT)
#define NEOFPS_LIGHTWEIGHT
#endif

using UnityEngine;
using NeoFPS.CharacterMotion.MotionData;
using NeoFPS.CharacterMotion.Parameters;
using NeoSaveGames.Serialization;

namespace NeoFPS.CharacterMotion.States
{
    [MotionGraphElement("Dashes/Anim-Curve Directional Dash", "Directional Dash (Anim Curve)")]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgs-animcurvedirectionaldashstate.html")]
    public class AnimCurveDirectionalDashState : MotionGraphState
    {
        [SerializeField, Tooltip("The target speed for the dash to reach.")]
        private FloatDataReference m_DashSpeed = new FloatDataReference(50f);
        [SerializeField, Tooltip("The direction vector parameter to base the dash off.")]
        private VectorParameter m_DashDirection = null;
        [SerializeField, Tooltip("The space the direction vector exists in.")]
        private Space m_Space = Space.Self;
        [SerializeField, Tooltip("The amount of time it takes to reach the dash speed. At this point, the animation curve kicks in to ease out of the dash. A Dash In Time of 0 is instant.")]
        private float m_DashInTime = 0.05f;
        [SerializeField, Tooltip("The amount of time it takes for the animation curve kicks to ease out of the dash.")]
        private float m_DashOutTime = 0.5f;
        [SerializeField, Tooltip("The ease out curve for the dash velocity. This should start at 1. Dipping below zero will mean the dash is moving backwards.")]
        private AnimationCurve m_DashOutCurve = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0f, 1f), new Keyframe(0.1f, 1f), new Keyframe(0.7f, -0.05f), new Keyframe(0.9f, 0.02f), new Keyframe(1f, 0f)
        });

        private const float k_TinyValue = 0.001f;

        private Vector3 m_DashHeading = Vector3.zero;
        private Vector3 m_OutVelocity = Vector3.zero;
        private Vector3 m_CrossVelocity = Vector3.zero;
        private float m_LerpIn = 0f;
        private float m_LerpOut = 0f;
        private float m_EntrySpeed = 0f;
        private bool m_Completed = false;

        public override bool completed
        {
            get { return m_Completed; }
        }

        public override Vector3 moveVector
        {
            get { return (m_OutVelocity + m_CrossVelocity) * Time.deltaTime; }
        }

        public override bool applyGravity
        {
            get { return false; }
        }

        public override bool applyGroundingForce
        {
            get { return characterController.isGrounded; }
        }

        public override bool ignorePlatformMove
        {
            get { return false; }
        }

        public override void OnValidate()
        {
            base.OnValidate();

            var keys = m_DashOutCurve.keys;
            if (keys.Length < 2)
            {
                var newKeys = new Keyframe[2];
                if (keys.Length == 1)
                    newKeys[0] = keys[0];
                else
                    newKeys[0] = new Keyframe(0f, 1f);
                newKeys[1] = new Keyframe(1f, 0f);
                m_DashOutCurve.keys = newKeys;
            }
            else
            {
                var k = keys[0];
                k.time = 0f;
                m_DashOutCurve.MoveKey(0, k);

                k = keys[keys.Length - 1];
                k.time = 1f;
                m_DashOutCurve.MoveKey(keys.Length - 1, k);
            }

            m_DashInTime = Mathf.Clamp(m_DashInTime, 0f, 10f);
            m_DashOutTime = Mathf.Clamp(m_DashOutTime, 0.05f, 10f);
        }

        public override void OnEnter()
        {
            base.OnEnter();

            m_LerpOut = 0f;
            m_Completed = false;

            m_OutVelocity = characterController.velocity;

            // Reset lerp in (skip if lerp takes less than 1 frame)
            m_LerpIn = 0f;

            // Get heading
            if (m_DashDirection != null)
            {
                if (m_Space == Space.World)
                    m_DashHeading = m_DashDirection.value;
                else
                    m_DashHeading = characterController.transform.TransformDirection(m_DashDirection.value);

                m_DashHeading.Normalize();
            }
            else
            {
                // Fall back on character horizontal if vector parameter isn't assigned
                m_DashHeading = Vector3.ProjectOnPlane(m_OutVelocity, characterController.up).normalized;
            }

            m_EntrySpeed = Vector3.Dot(m_OutVelocity, m_DashHeading);
            m_CrossVelocity = m_OutVelocity - m_DashHeading * m_EntrySpeed;
        }

        public override void OnExit()
        {
            base.OnExit();

            m_Completed = false;
            m_DashHeading = Vector3.zero;
            m_OutVelocity = Vector3.zero;
            m_CrossVelocity = Vector3.zero;
            m_EntrySpeed = 0f;
            m_LerpIn = 0f;
            m_LerpOut = 0f;
        }

        public override void Initialise(IMotionController c)
        {
            base.Initialise(c);
            m_DashOutCurve.preWrapMode = WrapMode.ClampForever;
            m_DashOutCurve.postWrapMode = WrapMode.ClampForever;
        }

        float GetSlopeSpeed()
        {
            // Check if character is moving into a slope, and scale down speed if so
            if (characterController.isGrounded && Vector3.Dot(characterController.groundNormal, m_DashHeading) < 0f)
                return Vector3.Dot(characterController.groundNormal, characterController.up);
            else
                return 1f;
        }

        public override void Update()
        {
            base.Update();

            //Sort dash velocity
            if (m_LerpIn < 1f)
            {
                if (m_DashInTime < Time.fixedDeltaTime)
                    m_LerpIn = 1f;
                else
                {
                    m_LerpIn += Time.deltaTime / m_DashInTime;
                    if (m_LerpIn > 1f)
                        m_LerpIn = 1f;
                }

                m_OutVelocity = Vector3.Lerp(m_DashHeading * m_EntrySpeed, m_DashHeading * GetSlopeSpeed() * m_DashSpeed.value, EasingFunctions.EaseInQuadratic(m_LerpIn));
            }
            else
            {
                m_LerpOut += Time.deltaTime / m_DashOutTime;
                if (m_LerpOut > 1f)
                {
                    m_LerpOut = 1f;
                    m_Completed = true;
                }

                m_OutVelocity = Vector3.Lerp(m_DashHeading * GetSlopeSpeed() * m_DashSpeed.value, m_DashHeading * m_EntrySpeed, EasingFunctions.EaseInQuadratic(m_LerpOut));
            }
        }

        public override void CheckReferences(IMotionGraphMap map)
        {
            m_DashSpeed.CheckReference(map);
            m_DashDirection = map.Swap(m_DashDirection);

            base.CheckReferences(map);
        }

        #region SAVE / LOAD

        private static readonly NeoSerializationKey k_HeadingKey = new NeoSerializationKey("heading");
        private static readonly NeoSerializationKey k_OutVelKey = new NeoSerializationKey("outVel");
        private static readonly NeoSerializationKey k_CrossVelKey = new NeoSerializationKey("crossVel");
        private static readonly NeoSerializationKey k_EntrySpeedKey = new NeoSerializationKey("entry");
        private static readonly NeoSerializationKey k_LerpInKey = new NeoSerializationKey("lerpIn");
        private static readonly NeoSerializationKey k_LerpOutKey = new NeoSerializationKey("lerpOut");

        public override void WriteProperties(INeoSerializer writer)
        {
            base.WriteProperties(writer);
            writer.WriteValue(k_HeadingKey, m_DashHeading);
            writer.WriteValue(k_OutVelKey, m_OutVelocity);
            writer.WriteValue(k_CrossVelKey, m_CrossVelocity);
            writer.WriteValue(k_EntrySpeedKey, m_EntrySpeed);
            writer.WriteValue(k_LerpInKey, m_LerpIn);
            writer.WriteValue(k_LerpOutKey, m_LerpOut);
        }

        public override void ReadProperties(INeoDeserializer reader)
        {
            base.ReadProperties(reader);
            reader.TryReadValue(k_HeadingKey, out m_DashHeading, m_DashHeading);
            reader.TryReadValue(k_OutVelKey, out m_OutVelocity, m_OutVelocity);
            reader.TryReadValue(k_CrossVelKey, out m_CrossVelocity, m_CrossVelocity);
            reader.TryReadValue(k_EntrySpeedKey, out m_EntrySpeed, m_EntrySpeed);
            reader.TryReadValue(k_LerpInKey, out m_LerpIn, m_LerpIn);
            reader.TryReadValue(k_LerpOutKey, out m_LerpOut, m_LerpOut);
        }

        #endregion
    }
}