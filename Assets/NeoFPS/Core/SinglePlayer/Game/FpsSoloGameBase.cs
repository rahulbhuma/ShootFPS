using System.Collections;
using System.Collections.Generic;
using NeoSaveGames;
using NeoSaveGames.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeoFPS.SinglePlayer
{
    public abstract class FpsSoloGameBase : FpsGameMode
    {
        [SerializeField, Tooltip("How long after dying does the game react (gives time for visual feedback).")]
        private float m_DeathSequenceDuration = 5f;

        private float m_RespawnTimer = 0f;

        public enum DeathAction
        {
            Respawn,
            ReloadScene,
            MainMenu,
            ContinueFromSave,
            RespawnWithItems
        }

        public abstract bool spawnOnStart
        {
            get;
            set;
        }

        public float deathSequenceDuration
        {
            get { return m_DeathSequenceDuration; }
        }

        private IController m_Player = null;
        public IController player
        {
            get { return m_Player; }
            protected set
            {
                // Unsubscribe from old player events
                if (m_Player != null)
                    m_Player.onCharacterChanged -= OnPlayerCharacterChanged;

                // Set new player
                m_Player = value;

                // Track player for persistence
                var playerComponent = m_Player as Component;
                if (playerComponent != null)
                {
                    var nsgo = playerComponent.GetComponent<NeoSerializedGameObject>();
                    if (nsgo != null && nsgo.wasRuntimeInstantiated)
                        m_PersistentObjects[0] = nsgo;
                    else
                        m_PersistentObjects[0] = null;
                }
                else
                    m_PersistentObjects[0] = null;

                // Subscribe to player events
                if (m_Player != null)
                {
                    m_Player.onCharacterChanged += OnPlayerCharacterChanged;
                    OnPlayerCharacterChanged(m_Player.currentCharacter);
                }
            }
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            m_DeathSequenceDuration = Mathf.Clamp(m_DeathSequenceDuration, 0f, 300f);
        }
#endif

        protected override void OnStart()
        {
            base.OnStart();

            if (player == null)
            {
                // Instantiate player if requried
                if (FpsSoloPlayerController.localPlayer != null)
                    player = FpsSoloPlayerController.localPlayer;
                else
                    player = InstantiatePlayer();
            }

            inGame = true;

            // Spawn player character
            if (player.currentCharacter == null)
            {
                if (spawnOnStart && !PreSpawnStep())
                    Respawn(player);
            }
            else
            {
                var spawn = SpawnManager.GetNextSpawnPoint(false);
                if (spawn != null)
                {
                    var t = player.currentCharacter.transform;
                    t.position = spawn.spawnTransform.position;
                    t.rotation = spawn.spawnTransform.rotation;
                }
            }
        }

        protected override void OnDestroy()
        {
            inGame = false;

            if (m_PlayerCharacter != null)
            {
                m_PlayerCharacter.onIsAliveChanged -= OnPlayerCharacterIsAliveChanged;
                m_PlayerCharacter = null;
            }

            base.OnDestroy();
        }

        #region PLAYER

        protected override IController InstantiatePlayer()
        {
            var playerPrefab = GetPlayerControllerPrototype() as Component;
            var nsgo = GetComponent<NeoSerializedGameObject>();
            if (nsgo != null && nsgo.serializedScene != null)
                return nsgo.serializedScene.InstantiatePrefab(playerPrefab) as IController;
            else
                return Instantiate(playerPrefab) as IController;
        }

        private FpsSoloCharacter m_PlayerCharacter = null;
        public ICharacter playerCharacter
        {
            get { return m_PlayerCharacter; }
        }

        protected virtual void OnPlayerCharacterChanged(ICharacter character)
        {
            // Unsubscribe from old character events
            if (m_PlayerCharacter != null)
            {
                m_PlayerCharacter.onIsAliveChanged -= OnPlayerCharacterIsAliveChanged;
                ProcessOldPlayerCharacter(m_PlayerCharacter);
            }

            // Set new character
            m_PlayerCharacter = character as FpsSoloCharacter;

            // Subscribe to character events
            if (m_PlayerCharacter != null)
            {
                var nsgo = m_PlayerCharacter.GetComponent<NeoSerializedGameObject>();
                if (nsgo != null && nsgo.wasRuntimeInstantiated)
                    m_PersistentObjects[1] = nsgo;
                else
                    m_PersistentObjects[1] = null;

                ProcessNewPlayerCharacter(m_PlayerCharacter);
                m_PlayerCharacter.onIsAliveChanged += OnPlayerCharacterIsAliveChanged;
                OnPlayerCharacterIsAliveChanged(m_PlayerCharacter, m_PlayerCharacter.isAlive);
            }
            else
                m_PersistentObjects[1] = null;
        }

        protected virtual void OnPlayerCharacterIsAliveChanged(ICharacter character, bool alive)
        {
            if (inGame && !alive)
                StartCoroutine(DelayedDeathReactionCoroutine(m_DeathSequenceDuration));
        }

        private IEnumerator DelayedDeathReactionCoroutine(float delay)
        {
            yield return null;

            // Delay timer
            m_RespawnTimer = delay;
            while (m_RespawnTimer > 0f && !SkipDelayedDeathReaction())
            {
                m_RespawnTimer -= Time.deltaTime;
                yield return null;
            }

            if (inGame)
                DelayedDeathAction();
        }

        protected virtual bool SkipDelayedDeathReaction()
        {
            return false;
        }

        protected virtual void DelayedDeathAction()
        {
            if (!PreSpawnStep())
                Respawn(player);
        }

        protected virtual void ProcessOldPlayerCharacter(ICharacter oldCharacter)
        {
            Destroy(oldCharacter.gameObject);
        }

        protected virtual void ProcessNewPlayerCharacter(ICharacter newCharacter)
        {
        }

        public void Spawn()
        {
            if (!PreSpawnStep())
                Respawn(player);
        }

        protected void SpawnPlayerCharacter()
        {
            Respawn(player);
        }

        protected virtual bool PreSpawnStep()
        {
            return false;
        }

        #endregion

        #region PERSISTENCE

        private NeoSerializedGameObject[] m_PersistentObjects = new NeoSerializedGameObject[2];

        protected override NeoSerializedGameObject[] GetPersistentObjects()
        {
            if (m_PersistentObjects[0] != null && m_PersistentObjects[1] != null)
                return m_PersistentObjects;
            else
            {
                Debug.Log("No Persistence Save Objects. Does the scene have a SceneSaveInfo object correctly set up?");
                Debug.Log("m_PersistentObjects[0] != null: " + (m_PersistentObjects[0] != null));
                Debug.Log("m_PersistentObjects[1] != null: " + (m_PersistentObjects[1] != null));
                return null;
            }
        }

        protected override void SetPersistentObjects(NeoSerializedGameObject[] objects)
        {
            var controller = objects[0].GetComponent<IController>();
            if (controller != null)
            {
                player = controller;

                var character = objects[1].GetComponent<ICharacter>();
                if (character != null)
                    player.currentCharacter = character;
            }
        }

        #endregion

        #region SERIALIZATION

        private static readonly NeoSerializationKey k_RespawnTimerKey = new NeoSerializationKey("respawnTimer");

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);

            if (reader.TryReadValue(k_RespawnTimerKey, out m_RespawnTimer, 0f))
                StartCoroutine(DelayedDeathReactionCoroutine(m_RespawnTimer));
        }

        public override void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            base.WriteProperties(writer, nsgo, saveMode);

            if (m_RespawnTimer > 0f)
                writer.WriteValue(k_RespawnTimerKey, m_RespawnTimer);
        }

        #endregion
    }
}