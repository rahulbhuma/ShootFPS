using NeoSaveGames;
using NeoSaveGames.Serialization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace NeoFPS
{
    [CreateAssetMenu(fileName = "PooledObjectCollection", menuName = "NeoFPS/Pooled Object Collection", order = NeoFpsMenuPriorities.ungrouped_pooledobjectcol)]
    [HelpURL("https://docs.neofps.com/manual/utilitiesref-so-pooledobjectcollection.html")]
    public class PooledObjectCollection : ScriptableObject
    {
        [SerializeField, Tooltip("The pooled objects in this collection.")]
        private PoolInfo[] m_PooledObjects = new PoolInfo[0];

        public PoolInfo[] pooledObjects
        {
            get { return m_PooledObjects; }
        }
    }
}