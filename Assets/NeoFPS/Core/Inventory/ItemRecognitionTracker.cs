using NeoSaveGames;
using NeoSaveGames.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inventoryref-mb-itemrecognitiontracker.html")]
    public class ItemRecognitionTracker : MonoBehaviour, INeoSerializableComponent
    {
        [SerializeField, FpsInventoryKey, Tooltip("The inventory items that the player should already be aware of on starting the game.")]
        private int[] m_RecogniseOnStart = { };

        private HashSet<int> m_Recognised = null;

        private void Awake()
        {
            if(m_Recognised == null)
            {
                if (m_RecogniseOnStart != null && m_RecogniseOnStart.Length > 0)
                    m_Recognised = new HashSet<int>(m_RecogniseOnStart);
                else
                    m_Recognised = new HashSet<int>();
            }
        }

        private static readonly NeoSerializationKey k_RecognisedKey = new NeoSerializationKey("recognised");

        public bool IsItemRecognised(int itemID)
        {
            return m_Recognised.Contains(itemID);
        }

        public void RecordItem(int itemID)
        {
            m_Recognised.Add(itemID);
        }

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            if (m_Recognised != null && m_Recognised.Count > 0)
                writer.WriteValues(k_RecognisedKey, m_Recognised.ToArray());
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            if (m_Recognised != null)
                Debug.LogError("Attempting to load recognised items from save, when ItemRecognitionTracker has already been initialised");

            if (reader.TryReadValues(k_RecognisedKey, out int[] idArray, null) && idArray != null)
                m_Recognised = new HashSet<int>(idArray);
        }
    }
}