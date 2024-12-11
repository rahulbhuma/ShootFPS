using NeoSaveGames;
using NeoSaveGames.Serialization;
using UnityEngine;

namespace NeoFPS.ModularFirearms
{
    [RequireComponent(typeof(LineRenderer))]
    [RequireComponent(typeof(ParticleSystem))]
    [RequireComponent(typeof(PooledObject))]
    public class LineAndParticleHitscanTrail : MonoBehaviour, IPooledHitscanTrail, IOriginShiftSubscriber, INeoSerializableComponent
    {
        [SerializeField, Tooltip("The number of particles per meter of trail. Used for the emit count to ensure a predictable density.")]
        private float m_ParticlesPerMeter = 10f;
        [SerializeField, Tooltip("Randomise the line texture's U mapping 0-1. Requires the material to have an \"_OffsetU\" property accessible via property block")]
        private bool m_RandomUOffset = false;

        private Transform m_LocalTransform = null;
        private PooledObject m_PooledObject = null;
        private LineRenderer m_LineRenderer = null;
        private ParticleSystem m_ParticleSystem = null;
        private float m_TotalDuration = 0f;
        private float m_LineDuration = 0f;
        private float m_Timer = 0f;
        private Color m_StartColour = Color.white;
        private Color m_EndColour = Color.white;
        private bool m_Initialised = false;

        protected void Awake()
        {
            if (!m_Initialised)
                Initialise();
        }

        private void Initialise()
        {
            m_LocalTransform = transform;
            m_PooledObject = GetComponent<PooledObject>();

            m_LineRenderer = GetComponent<LineRenderer>();
            m_LineRenderer.positionCount = 2;
            m_LineRenderer.enabled = false;
            m_StartColour = m_LineRenderer.startColor;
            m_EndColour = m_LineRenderer.endColor;

            m_ParticleSystem = GetComponent<ParticleSystem>();
            var shapeModule = m_ParticleSystem.shape;
            shapeModule.rotation = new Vector3(0f, 90f, 0f);
            m_TotalDuration = m_ParticleSystem.main.duration;

            if (m_RandomUOffset)
            {
                var b = new MaterialPropertyBlock();
                b.SetFloat("_OffsetU", Random.Range(0, 12f));
                m_LineRenderer.SetPropertyBlock(b);
            }
        }

        protected void OnEnable()
        {
            if (OriginShift.system != null)
                OriginShift.system.AddSubscriber(this);
        }

        protected void OnDisable()
        {
            if (OriginShift.system != null)
                OriginShift.system.RemoveSubscriber(this);
        }

        protected void Update()
        {
            // Disable Line Renderer
            if (m_Timer >= m_LineDuration && m_LineRenderer.enabled)
                m_LineRenderer.enabled = false;

            if (m_Timer >= m_TotalDuration)
                m_PooledObject.ReturnToPool();
            {
                float alpha = Mathf.Clamp01(1f - (m_Timer / m_LineDuration));

                Color c = m_StartColour;
                c.a *= alpha;
                m_LineRenderer.startColor = c;

                c = m_EndColour;
                c.a *= alpha;
                m_LineRenderer.endColor = c;

                m_Timer += Time.deltaTime;
            }
        }

        public void Show(Vector3 start, Vector3 end, float size, float duration)
        {
            m_Timer = 0f;
            m_LineDuration = duration;

            // Get the required length
            float length = (end - start).magnitude;

            // Position and rotate the object
            m_LocalTransform.position = (start + end) * 0.5f;
            m_LocalTransform.localRotation = Quaternion.FromToRotation(Vector3.forward, end - start);

            // Set the particle system shape length
            var shape = m_ParticleSystem.shape;
            shape.radius = length * 0.5f;

            // Emit based on particles per meter
            m_ParticleSystem.Emit((int)(length * m_ParticlesPerMeter));

            // Setup line renderer
            m_LineRenderer.SetPosition(0, start);
            m_LineRenderer.SetPosition(1, end);
            m_LineRenderer.widthMultiplier = size;
            m_LineRenderer.startColor = Color.white;
            m_LineRenderer.endColor = Color.white;
            m_LineRenderer.enabled = true;
        }

        public void ApplyOffset(Vector3 offset)
        {
            m_LineRenderer.SetPosition(0, m_LineRenderer.GetPosition(0) + offset);
            m_LineRenderer.SetPosition(1, m_LineRenderer.GetPosition(1) + offset);
        }

        #region INeoSerializableComponent IMPLEMENTATION

        private static readonly NeoSerializationKey k_StartPointKey = new NeoSerializationKey("p1");
        private static readonly NeoSerializationKey k_EndPointKey = new NeoSerializationKey("p2");
        private static readonly NeoSerializationKey k_SizeKey = new NeoSerializationKey("size");
        private static readonly NeoSerializationKey k_ParticlesKey = new NeoSerializationKey("particles");
        private static readonly NeoSerializationKey k_TimeKey = new NeoSerializationKey("time");
        private static readonly NeoSerializationKey k_DurationKey = new NeoSerializationKey("duration");

        private const int k_MaxPoints = 2048;

        private static ParticleSystem.Particle[] s_Particles = null;

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            if (gameObject.activeInHierarchy)
            {
                if (s_Particles == null)
                    s_Particles = new ParticleSystem.Particle[k_MaxPoints];

                writer.WriteValue(k_TimeKey, m_Timer);
                writer.WriteValue(k_DurationKey, m_LineDuration);
                writer.WriteValue(k_SizeKey, m_LineRenderer.widthMultiplier);

                // Write particles
                int numPoints = m_ParticleSystem.GetParticles(s_Particles);
                Vector3[] points = new Vector3[numPoints];
                for (int i = 0; i < numPoints; ++i)
                    points[i] = s_Particles[i].position;
                writer.WriteValues(k_ParticlesKey, points);
            }
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            if (reader.TryReadValue(k_TimeKey, out m_Timer, m_Timer))
            {
                if (!m_Initialised)
                    Initialise();

                reader.TryReadValue(k_DurationKey, out m_LineDuration, m_LineDuration);

                // Get line shape
                if (reader.TryReadValue(k_StartPointKey, out Vector3 p1, Vector3.zero))
                    m_LineRenderer.SetPosition(0, p1);
                if (reader.TryReadValue(k_EndPointKey, out Vector3 p2, Vector3.zero))
                    m_LineRenderer.SetPosition(1, p2);
                if (reader.TryReadValue(k_SizeKey, out float w, 1f))
                    m_LineRenderer.widthMultiplier = w;

                // Calculate colours (fade over time)
                float alpha = Mathf.Clamp01(1f - (m_Timer / m_LineDuration));

                Color c = m_StartColour;
                c.a *= alpha;
                m_LineRenderer.startColor = c;

                c = m_EndColour;
                c.a *= alpha;
                m_LineRenderer.endColor = c;

                m_LineRenderer.enabled = true;

                // Read particles
                if (reader.TryReadValues(k_ParticlesKey, out Vector3[] points, null) && points != null && points.Length > 0)
                {
                    if (s_Particles == null)
                        s_Particles = new ParticleSystem.Particle[k_MaxPoints];

                    // Set the particle system shape length
                    var shape = m_ParticleSystem.shape;
                    shape.radius = 50f;

                    // Emit the required particles
                    m_ParticleSystem.Emit(points.Length);

                    // Reposition based on save data
                    var pointCount = m_ParticleSystem.GetParticles(s_Particles, points.Length);
                    for (int i = 0; i < points.Length; ++i)
                    {
                        s_Particles[i].position = points[i];
                        s_Particles[i].remainingLifetime = m_TotalDuration - m_Timer;
                        s_Particles[i].startLifetime = m_TotalDuration;
                    }
                    m_ParticleSystem.SetParticles(s_Particles, pointCount);
                }
            }
        }

        #endregion
    }
}