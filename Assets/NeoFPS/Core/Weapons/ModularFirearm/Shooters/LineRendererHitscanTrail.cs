using NeoSaveGames;
using NeoSaveGames.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS.ModularFirearms
{
    [RequireComponent(typeof(LineRenderer))]
    [RequireComponent(typeof(PooledObject))]
    public class LineRendererHitscanTrail : MonoBehaviour, IPooledHitscanTrail, IOriginShiftSubscriber, INeoSerializableComponent
    {
        [SerializeField, Tooltip("Randomise the texture's U mapping 0-1. Requires the material to have an \"_OffsetU\" property accessible via property block")]
        private bool m_RandomUOffset = false;

        [SerializeField, Tooltip("The maximum length of the trail.")]
        private float m_MaxLength = 100f;

        private PooledObject m_PooledObject = null;
        private LineRenderer m_LineRenderer = null;
        private float m_Duration = 0f;
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
            m_PooledObject = GetComponent<PooledObject>();
            m_LineRenderer = GetComponent<LineRenderer>();
            m_LineRenderer.positionCount = 2;
            m_LineRenderer.enabled = false;
            m_StartColour = m_LineRenderer.startColor;
            m_EndColour = m_LineRenderer.endColor;

            if (m_RandomUOffset)
            {
                var b = new MaterialPropertyBlock();
                b.SetFloat("_OffsetU", Random.Range(0, 12f));
                m_LineRenderer.SetPropertyBlock(b);
            }

            m_Initialised = true;
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
            if (m_Timer >= m_Duration)
            {
                m_LineRenderer.enabled = false;
                m_PooledObject.ReturnToPool();
            }
            else
            {
                float alpha = Mathf.Clamp01(1f - (m_Timer / m_Duration));

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
            m_Duration = duration;
            m_LineRenderer.SetPosition(0, start);

            // Get the end of the trail
            end -= start;
            end = Vector3.ClampMagnitude(end, m_MaxLength);
            m_LineRenderer.SetPosition(1, start + end);

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

        private static readonly NeoSerializationKey k_StartPointKey = new NeoSerializationKey("p1");
        private static readonly NeoSerializationKey k_EndPointKey = new NeoSerializationKey("p2");
        private static readonly NeoSerializationKey k_SizeKey = new NeoSerializationKey("size");
        private static readonly NeoSerializationKey k_TimeKey = new NeoSerializationKey("time");
        private static readonly NeoSerializationKey k_DurationKey = new NeoSerializationKey("duration");

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            if (gameObject.activeInHierarchy)
            {
                writer.WriteValue(k_TimeKey, m_Timer);
                writer.WriteValue(k_DurationKey, m_Duration);
                writer.WriteValue(k_StartPointKey, m_LineRenderer.GetPosition(0));
                writer.WriteValue(k_EndPointKey, m_LineRenderer.GetPosition(1));
                writer.WriteValue(k_SizeKey, m_LineRenderer.widthMultiplier);
            }
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            if (reader.TryReadValue(k_TimeKey, out m_Timer, m_Timer))
            {
                if (!m_Initialised)
                    Initialise();

                reader.TryReadValue(k_DurationKey, out m_Duration, m_Duration);

                // Get line shape
                if (reader.TryReadValue(k_StartPointKey, out Vector3 p1, Vector3.zero))
                    m_LineRenderer.SetPosition(0, p1);
                if (reader.TryReadValue(k_EndPointKey, out Vector3 p2, Vector3.zero))
                    m_LineRenderer.SetPosition(1, p2);
                if (reader.TryReadValue(k_SizeKey, out float w, 1f))
                    m_LineRenderer.widthMultiplier = w;

                // Calculate colours (fade over time)
                float alpha = Mathf.Clamp01(1f - (m_Timer / m_Duration));

                Color c = m_StartColour;
                c.a *= alpha;
                m_LineRenderer.startColor = c;

                c = m_EndColour;
                c.a *= alpha;
                m_LineRenderer.endColor = c;

                m_LineRenderer.enabled = true;
            }
        }
    }
}