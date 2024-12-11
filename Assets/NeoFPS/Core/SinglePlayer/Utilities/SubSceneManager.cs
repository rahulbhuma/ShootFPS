using NeoSaveGames;
using NeoSaveGames.SceneManagement;
using NeoSaveGames.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeoFPS.SinglePlayer
{
    [RequireComponent(typeof(FpsSoloGameBase))]
    [HelpURL("https://docs.neofps.com/manual/savegamesref-mb-subscenemanager.html")]
    public class SubSceneManager : MonoBehaviour, INeoSerializableComponent
    {
        [SerializeField, Tooltip("The sub-scenes to load/unload while progressing through this level.")]
        private SubSceneCollection m_SubScenes = null;

        [SerializeField, Tooltip("The index (within the sub-scene collection to load by default (if not loading from a save game).")]
        private int m_StartScene = 0;

        private static SubSceneManager s_Instance = null;

        private FpsSoloGameBase m_GameMode = null;
        private List<int> m_LoadedScenes = new List<int>();
        private byte[][] m_SubSceneData = null;
        private bool m_Initialised = false;

        protected void Awake()
        {
            if (s_Instance != null)
            {
                Debug.LogError("Attempting to initialise a SubSceneManager component when another is already active");
                Destroy(gameObject);
            }
            else
            {
                s_Instance = this;
                m_GameMode = GetComponent<FpsSoloGameBase>();
                m_GameMode.spawnOnStart = false;
            }
        }

        protected IEnumerator Start()
        {
            if (!m_Initialised)
            {
                m_Initialised = true;

                // Initialise sub-scene data array
                m_SubSceneData = new byte[m_SubScenes.count][];

                // Check already loaded scenes (eg if testing in the editor)
                bool startSceneLoaded = false;
                for (int i = SceneManager.sceneCount - 1; i >= 0; --i)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    int index = m_SubScenes.IndexOf(scene);
                    if (index != -1)
                    {
                        if (index == m_StartScene)
                        {
                            m_LoadedScenes.Add(index);
                            startSceneLoaded = true;
                        }
                        else
                            SceneManager.UnloadSceneAsync(scene);
                    }
                }

                // Load the start scene
                if (!startSceneLoaded)
                {
                    m_LoadedScenes.Add(m_StartScene);
                    SceneManager.LoadScene(m_SubScenes.subScenes[m_StartScene], LoadSceneMode.Additive);
                }

                yield return null;
                yield return null;

                m_GameMode.Respawn(m_GameMode.player);
            }
        }

        public static void LoadScene(int index)
        {
            if (s_Instance != null)
                s_Instance.LoadSceneInternal(index);
            else
                Debug.Log("Attempting to load sub-scene with no sub-scene manager active. Sub-scene index: " + index);
        }

        public static void UnloadScene(int index)
        {
            if (s_Instance != null)
                s_Instance.UnloadSceneInternal(index);
            else
                Debug.Log("Attempting to unload sub-scene with no sub-scene manager active. Sub-scene index: " + index);
        }

        private void LoadSceneInternal(int index)
        {
            if (!m_LoadedScenes.Contains(index))
            {
                m_LoadedScenes.Add(index);

                SceneManager.sceneLoaded += OnSubSceneLoaded;
                SceneManager.LoadSceneAsync(m_SubScenes.subScenes[index], LoadSceneMode.Additive);
            }
        }

        private void OnSubSceneLoaded(Scene s, LoadSceneMode mode)
        {
            // Data key is scene name
            int index = m_SubScenes.IndexOf(s);
            if (index != -1)
            {
                // Check for sub-scene save data
                if (m_SubSceneData[index] != null)
                {
                    // Get the NeoSerializedScene & deserialize data
                    var nss = NeoSerializedScene.GetByPath(s.path);
                    if (nss != null)
                        SaveGameManager.DeserializeSubsceneData(nss, m_SubSceneData[index]);

                    m_SubSceneData[index] = null;
                }
            }

            // Unsubscribe
            SceneManager.sceneLoaded -= OnSubSceneLoaded;
        }

        public void UnloadSceneInternal(int index)
        {
            if (m_LoadedScenes.Contains(index))
            {
                m_LoadedScenes.Remove(index);

                // Check for sub-scene save data
                if (m_SubSceneData[index] != null)
                {
                    Debug.LogError("Saving sub-scene data when it was already stored. Removing old contents.");
                    m_SubSceneData[index] = null;
                }

                // Save the sub-scene
                var s = SceneManager.GetSceneByName(m_SubScenes.subScenes[index]);
                var nss = NeoSerializedScene.GetByPath(s.path);
                m_SubSceneData[index] = SaveGameManager.SerializeSubsceneData(nss);

                SceneManager.UnloadSceneAsync(m_SubScenes.subScenes[index]);
            }
        }

        private static readonly NeoSerializationKey k_LoadedScenes = new NeoSerializationKey("loadedSubScenes");
        private static readonly NeoSerializationKey k_SceneData = new NeoSerializationKey("sceneData");

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValues(k_LoadedScenes, m_LoadedScenes);
            for (int i = 0; i < m_SubSceneData.Length; ++i)
            {
                if (m_SubSceneData[i] != null)
                {
                    writer.PushContext(SerializationContext.ObjectNeoFormatted, i);
                    writer.WriteValues(k_SceneData, m_SubSceneData[i]);
                    writer.PopContext(SerializationContext.ObjectNeoFormatted);
                }
            }
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            // Get target loaded scenes
            reader.TryReadValues(k_LoadedScenes, m_LoadedScenes);

            // Check already loaded scenes (eg if testing in the editor)
            for (int i = SceneManager.sceneCount - 1; i >= 0 ; --i)
            {
                var scene = SceneManager.GetSceneAt(i);
                int index = m_SubScenes.IndexOf(scene);
                if (index != -1)
                {
                    if (!m_LoadedScenes.Contains(index))
                        SceneManager.UnloadSceneAsync(scene);
                }
            }

            // Load sub-scene data
            m_SubSceneData = new byte[m_SubScenes.count][];
            for (int i = 0; i < m_SubSceneData.Length; ++i)
            {
                if (reader.PushContext(SerializationContext.ObjectNeoFormatted, i))
                {
                    byte[] data;
                    if (reader.TryReadValues(k_SceneData, out data, null))
                        m_SubSceneData[i] = data;
                    reader.PopContext(SerializationContext.ObjectNeoFormatted, i);
                }
            }

            // Load each sub-scene
            foreach (var sceneIndex in m_LoadedScenes)
            {
                if (!m_LoadedScenes.Contains(sceneIndex))
                    SceneManager.LoadScene(m_SubScenes.subScenes[sceneIndex], LoadSceneMode.Additive);
            }

            m_Initialised = true;
        }
    }
}