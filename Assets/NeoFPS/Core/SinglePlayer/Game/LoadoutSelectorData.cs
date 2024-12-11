using System;
using UnityEngine;

namespace NeoFPS.SinglePlayer
{
    [Serializable]
    public class LoadoutSelectorData
    {
        [Tooltip("The spawn areas (groups of spawn points) available on this map.")]
        public LoadoutInfo[] loadouts = { };

        public int currentIndex = -1;
    }

    [Serializable]
    public class LoadoutInfo : ILoadoutInfo
    {
        [SerializeField, Tooltip("The loadout asset to use for this selection")]
        private FpsInventoryLoadout m_Loadout = null;

        [SerializeField, Tooltip("A name for the loadout that will be displayed in the loadout selection popup")]
        private string m_DisplayName = string.Empty;

        [SerializeField, Tooltip("A description for the loadout that will be displayed in the loadout selection popup")]
        private string m_Description = string.Empty;

        [SerializeField, Tooltip("An image for the loadout that will be displayed in the loadout selection popup")]
        private Sprite m_Sprite = null;

        public string displayName
        {
            get { return m_DisplayName; }
        }

        public string description
        {
            get { return m_Description; }
        }

        public Sprite sprite
        {
            get { return m_Sprite; }
        }

        public FpsInventoryLoadout loadout
        {
            get { return m_Loadout; }
        }
    }
}
