using NeoSaveGames.Serialization;
using UnityEngine;

namespace NeoSaveGames
{
    [CreateAssetMenu(fileName = "SaveGamePrefabCollection", menuName = "NeoFPS/Save-game Prefab Collection", order = NeoFPS.NeoFpsMenuPriorities.ungrouped_savegameprefabcol)]
    [HelpURL("https://docs.neofps.com/manual/savegamesref-so-savegameprefabcollection.html")]
    public class SaveGamePrefabCollection : ScriptableObject
    {
        [SerializeField, Tooltip("Prefabs available for serialization in this collection.")]
        private NeoSerializedGameObject[] m_Prefabs = new NeoSerializedGameObject[0];

        [SerializeField, Tooltip("Assets available for serialization in this collection.")]
        private ScriptableObject[] m_Assets = new ScriptableObject[0];

        public NeoSerializedGameObject[] prefabs
        {
            get { return m_Prefabs; }
        }

        public ScriptableObject[] assets
        {
            get { return m_Assets; }
        }
    }
}