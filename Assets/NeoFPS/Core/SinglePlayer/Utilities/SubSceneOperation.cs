using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS.SinglePlayer
{
    [HelpURL("https://docs.neofps.com/manual/savegamesref-mb-subsceneoperation.html")]
    public class SubSceneOperation : MonoBehaviour
    {
        [SerializeField, Tooltip("The index of the scene within the SubSceneCollection to load.")]
        private int m_SubSceneIndex = -1;

        public void LoadSubScene()
        {
            if (m_SubSceneIndex != -1)
                SubSceneManager.LoadScene(m_SubSceneIndex);
        }

        public void UnloadSubScene()
        {
            if (m_SubSceneIndex != -1)
                SubSceneManager.UnloadScene(m_SubSceneIndex);
        }

        public void LoadSubScene(int index)
        {
            if (index != -1)
                SubSceneManager.LoadScene(index);
        }

        public void UnloadSubScene(int index)
        {
            if (index != -1)
                SubSceneManager.UnloadScene(index);
        }
    }
}
