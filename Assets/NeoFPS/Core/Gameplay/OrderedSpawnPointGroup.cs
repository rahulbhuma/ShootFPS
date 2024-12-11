using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/fpcharactersref-mb-orderedspawnpointgroup.html")]
    public class OrderedSpawnPointGroup : MonoBehaviour
    {
        [SerializeField, Tooltip("Should the spawn points be registered immediately on awake?")]
        private bool m_RegisterOnAwake = true;

        [SerializeField, Tooltip("The individual spawn points (the first spawn is at the top of the list)")]
        private SpawnPoint[] m_SpawnPoints = { };

#if UNITY_EDITOR
        public SpawnPoint[] spawnPoints
        {
            get { return m_SpawnPoints; }
        }

        protected void OnValidate()
        {
            int nullSpawns = 0;
            for (int i = 0; i < m_SpawnPoints.Length; ++i)
            {
                if (m_SpawnPoints[i] == null)
                    ++nullSpawns;
            }

            if (nullSpawns > 0)
            {
                var temp = new List<SpawnPoint>(m_SpawnPoints.Length - nullSpawns);
                for (int i = 0; i < m_SpawnPoints.Length; ++i)
                {
                    if (m_SpawnPoints[i] != null)
                        temp.Add(m_SpawnPoints[i]);
                }
                m_SpawnPoints = temp.ToArray();
            }
        }
#endif

        protected void Awake()
        {
            if (m_RegisterOnAwake)
                Register();
        }

        protected void OnEnable()
        {
            if (m_RegisterOnAwake)
                Register();
        }

        protected void OnDisable()
        {
            Unregister();
        }

        public void Register()
        {
            for (int i = 0; i < m_SpawnPoints.Length; ++i)
            {
                if (m_SpawnPoints[i] != null)
                    m_SpawnPoints[i].Register();
            }
        }

        public void Unregister()
        {
            for (int i = 0; i < m_SpawnPoints.Length; ++i)
            {
                if (m_SpawnPoints[i] != null)
                    m_SpawnPoints[i].Unregister();
            }
        }
    }
}