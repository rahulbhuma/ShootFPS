using UnityEngine;
using NeoSaveGames;
using NeoSaveGames.Serialization;
using System;

namespace NeoFPS.Samples.SinglePlayer.MultiScene
{
    public class ChanceBall : MonoBehaviour, INeoSerializableComponent, IDamageHandler
    {
        public Color regularColour = Color.grey;
        public Color damageColour = Color.red;

        private Renderer m_Renderer = null;
        private Material m_Material = null;

        private float m_Health = 100f;

        static readonly NeoSerializationKey k_HealthKey = new NeoSerializationKey("health");

        public DamageFilter inDamageFilter
        {
            get { return DamageFilter.AllDamageAllTeams; }
            set { }
        }

        public IHealthManager healthManager
        {
            get { return null; }
        }

        void Awake()
        {
            m_Renderer = GetComponent<Renderer>();
            m_Material = m_Renderer.material;
            m_Material.color = regularColour;

            ++BallSpawner.activeBallCount;
        }

        void OnDestroy()
        {
            --BallSpawner.activeBallCount;
        }

        void Update()
        {
            //m_Material.color = Color.red;
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            reader.TryReadValue(k_HealthKey, out m_Health, m_Health);
            m_Material.color = Color.Lerp(damageColour, regularColour, m_Health * 0.01f);
        }

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValue(k_HealthKey, m_Health);
        }

        DamageResult ApplyDamage (float damage)
        {
            m_Health -= damage;
            if (m_Health < 0f)
            {
                Destroy(gameObject);
                return DamageResult.Critical;
            }
            else
            {
                m_Material.color = Color.Lerp(damageColour, regularColour, m_Health * 0.01f);
                return DamageResult.Standard;
            }
        }

        public DamageResult AddDamage(float damage)
        {
            return AddDamage(damage, null);
        }

        public DamageResult AddDamage(float damage, RaycastHit hit)
        {
            return AddDamage(damage, hit, null);
        }

        public DamageResult AddDamage(float damage, IDamageSource source)
        {
            DamageEvents.ReportDamageHandlerHit(this, source, Vector3.zero, ApplyDamage(damage), damage);
            return DamageResult.Standard;
        }

        public DamageResult AddDamage(float damage, RaycastHit hit, IDamageSource source)
        {
            DamageEvents.ReportDamageHandlerHit(this, source, hit.point, ApplyDamage(damage), damage);
            return DamageResult.Standard;
        }
    }
}