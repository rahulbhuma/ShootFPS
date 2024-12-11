using UnityEngine;
using NeoSaveGames;
using NeoSaveGames.Serialization;

namespace NeoFPS.Samples.SinglePlayer.MultiScene
{
    public class BallSpawner : MonoBehaviour, INeoSerializableComponent
    {
        public Rigidbody ballPrefab = null;
        public Transform spawnPoint = null;
        public float interval = 2f;
        public int maxBalls = 25;

        static readonly NeoSerializationKey k_TimerKey = new NeoSerializationKey("timer");

        private NeoSerializedScene m_NeoScene = null;
        private float m_Timer = 0.5f;

        public static int activeBallCount
        {
            get;
            set;
        }

        private void Awake()
        {
            m_NeoScene = NeoSerializedScene.GetByPath(gameObject.scene.path);
        }

        private void Update()
        {
            m_Timer -= Time.deltaTime;
            if (m_Timer < 0f && activeBallCount < maxBalls)
            {
                m_Timer = interval;
                SpawnBall();
            }
        }

        public void SpawnBall()
        {
            var ball = m_NeoScene.InstantiatePrefab(ballPrefab, spawnPoint.position, Quaternion.identity);
            ball.angularVelocity = Random.insideUnitSphere * 50f;
        }

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValue(k_TimerKey, m_Timer);
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            reader.TryReadValue(k_TimerKey, out m_Timer, m_Timer);
        }
    }
}