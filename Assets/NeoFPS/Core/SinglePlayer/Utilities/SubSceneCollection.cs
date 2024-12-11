using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeoFPS.SinglePlayer
{
    [HelpURL("https://docs.neofps.com/manual/savegamesref-so-subscenecollection.html")]
    [CreateAssetMenu(fileName = "SubSceneCollection", menuName = "NeoFPS/Sub-Scene Collection", order = NeoFpsMenuPriorities.ungrouped_subscenecollection)]
    public class SubSceneCollection : ScriptableObject
    {
        [SerializeField, Tooltip("The sub-scenes to load/unload in the background while progressing through the level.")]
        private string[] m_SubScenes = { };

        public string[] subScenes
        {
            get { return m_SubScenes; }
        }

        public int count
        {
            get { return m_SubScenes.Length; }
        }

        public bool Contains(Scene scene)
        {
            return Array.IndexOf(m_SubScenes, scene.name) != -1;
        }

        public int IndexOf(Scene scene)
        {
            return Array.IndexOf(m_SubScenes, scene.name);
        }
    }
}