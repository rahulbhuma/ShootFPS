using NeoSaveGames;
using NeoSaveGames.Serialization;
using UnityEngine;

namespace NeoFPS.ModularFirearms
{
    [RequireComponent(typeof(ParticleSystem))]
    [RequireComponent(typeof(PooledObject))]
    public class ParticleSystemHitscanTrail : MonoBehaviour, IPooledHitscanTrail, INeoSerializableComponent
    {
        [SerializeField, Tooltip("The number of particles per meter of trail. Used for the emit count to ensure a predictable density.")]
        private float m_ParticlesPerMeter = 10f;

        private Transform m_LocalTransform = null;
        private PooledObject m_PooledObject = null;
        private ParticleSystem m_ParticleSystem = null;
        private float m_Duration = 0f;
        private float m_Timer = 0f;
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
            m_ParticleSystem = GetComponent<ParticleSystem>();

            var shapeModule = m_ParticleSystem.shape;
            shapeModule.rotation = new Vector3(0f, 90f, 0f);
            m_Duration = m_ParticleSystem.main.duration;
        }

        protected void Update()
        {
            if (m_Timer >= m_Duration)
                m_PooledObject.ReturnToPool();
            else
                m_Timer += Time.deltaTime;
        }

        public void Show(Vector3 start, Vector3 end, float size, float duration)
        {
            m_Timer = 0f;
            //m_Duration = duration;

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
        }

        #region INeoSerializableComponent IMPLEMENTATION

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
                writer.WriteValue(k_DurationKey, m_Duration);

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

                reader.TryReadValue(k_DurationKey, out m_Duration, m_Duration);

                // Read particles
                if (reader.TryReadValues(k_ParticlesKey, out Vector3[] points, null) && points != null && points.Length > 0)
                {
                    if (s_Particles == null)
                        s_Particles = new ParticleSystem.Particle[k_MaxPoints];

                    // Set the particle lifetime
                    var main = m_ParticleSystem.main;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(m_Duration - m_Timer);

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
                        s_Particles[i].remainingLifetime = m_Duration - m_Timer;
                        s_Particles[i].startLifetime = m_Duration;
                    }
                    m_ParticleSystem.SetParticles(s_Particles, pointCount);
                }
            }
        }

        #endregion
    }
}