using System;
using UnityEngine;

namespace NeoFPS.SinglePlayer
{
    [Serializable]
    public class SpawnZoneSelectorData
    {
        [Tooltip("The spawn areas (groups of spawn points) available on this map.")]
        public Sprite mapSprite = null;

        [Tooltip("The spawn areas (groups of spawn points) available on this map.")]
        public SpawnZoneInfo[] spawnZones = { };

        private int m_CurrentIndex = -1;
        public int currentIndex
        {
            get { return m_CurrentIndex; }
            set
            {
                m_CurrentIndex = value;

                // Disable other zones' spawn points
                for (int i = 0; i < spawnZones.Length; ++i)
                {
                    // Skip the current index (in case of overlap)
                    if (i == m_CurrentIndex)
                        continue;
                    else
                    {
                        var zone = spawnZones[i];
                        for (int j = 0; j < zone.spawnPoints.Length; ++j)
                            zone.spawnPoints[j].gameObject.SetActive(false);
                    }
                }

                // Enable current zone's spawn points
                var currentZone = spawnZones[m_CurrentIndex];
                for (int i = 0; i < currentZone.spawnPoints.Length; ++i)
                    currentZone.spawnPoints[i].gameObject.SetActive(true);
            }
        }
    }

    [Serializable]
    public class SpawnZoneInfo : ISpawnZoneInfo
    {
        [SerializeField, Tooltip("The name to show in a spawn point selection UI.")]
        private string m_DisplayName = string.Empty;

        [SerializeField, Tooltip("The position of this spawn zone on the map (0 to 1 on each axis).")]
        private Vector2 m_MapPosition = new Vector2(0.5f, 0.5f);

        [SerializeField, Tooltip("The spawn point objects assigned to this spawn area.")]
        private SpawnPoint[] m_SpawnPoints = { };

        public string displayName
        {
            get { return m_DisplayName; }
        }

        public Vector2 mapPosition
        {
            get { return m_MapPosition; }
        }

        public SpawnPoint[] spawnPoints
        {
            get { return m_SpawnPoints; }
        }
    }
}
