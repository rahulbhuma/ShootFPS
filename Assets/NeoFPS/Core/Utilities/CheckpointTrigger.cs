using NeoSaveGames;
using NeoSaveGames.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeoFPS
{
    [RequireComponent(typeof(NeoSerializedGameObject))]
    public class CheckpointTrigger : CharacterTriggerZone, INeoSerializableComponent
    {
        [SerializeField, Tooltip("Should the checkpoint trigger fire multiple times (eg allow back-tracking).")]
        private bool m_OneShot = false;

        [SerializeField, Tooltip("Save the game progress using the auto save feature.")]
        private bool m_AutoSave = true;
        
        [SerializeField, Tooltip("A custom name that will appear in the load menu. If this is empty then the scene's display name will be used.")]
        private string m_CustomSaveName = string.Empty;

        [SerializeField, Tooltip("A list of spawn points to enable at this checkpoint.")]
        private SpawnPoint[] m_SpawnPoints = { };

        [SerializeField, Tooltip("Should previous spawn points be disabled (guaranteeing that the player spawns here).")]
        private bool m_DisableOldSpawns = true;

        private static CheckpointTrigger s_LastCheckpoint = null;
        private bool m_CheckpointActive = true;
        private bool m_Initialised = false;

        protected void OnDestroy()
        {
            if (s_LastCheckpoint == this)
                s_LastCheckpoint = null;
        }

        protected void Start()
        {
            m_Initialised = true;
        }

        protected override void OnCharacterEntered(ICharacter c)
        {
            base.OnCharacterEntered(c);

            if (m_Initialised && m_CheckpointActive && s_LastCheckpoint != this)
            {
                // Record checkpoint (to prevent repeat firing)
                s_LastCheckpoint = this;

                if (m_SpawnPoints.Length > 0)
                {
                    // Disable old spawn points
                    if (m_DisableOldSpawns)
                    {
                        while (SpawnManager.spawnPoints.Count > 0)
                            SpawnManager.spawnPoints[0].gameObject.SetActive(false);
                    }

                    // Enable new spawn points
                    for (int i = 0; i < m_SpawnPoints.Length; ++i)
                    {
                        if (m_SpawnPoints[i] != null)
                            m_SpawnPoints[i].gameObject.SetActive(true);
                    }
                }

                // Deactivate if one-shot
                if (m_OneShot)
                    m_CheckpointActive = false;

                // Save
                if (m_AutoSave)
                {
                    SaveGameManager.AutoSave(m_CustomSaveName);
                }
            }
        }

        private static readonly NeoSerializationKey k_LastCheckpointKey = new NeoSerializationKey("last");
        private static readonly NeoSerializationKey k_CheckpointActiveKey = new NeoSerializationKey("active");

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            // Write active
            writer.WriteValue(k_CheckpointActiveKey, m_CheckpointActive);

            // Write if this was last checkpoint
            if (s_LastCheckpoint == this)
                writer.WriteValue(k_LastCheckpointKey, true);
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            // Read active
            reader.TryReadValue(k_CheckpointActiveKey, out m_CheckpointActive, m_CheckpointActive);

            // Read if this was last checkpoint
            if (reader.TryReadValue(k_LastCheckpointKey, out bool temp, false))
                s_LastCheckpoint = this;

            m_Initialised = true;
        }
    }
}