using System;
using UnityEngine;

namespace NeoFPS.SinglePlayer
{
    [Serializable]
    public class CharacterSelectorData
    {
        [Tooltip("The various characters available to select")]
        public CharacterInfo[] characters = { };

        public int currentIndex = -1;
    }

    [Serializable]
    public class CharacterInfo : ICharacterInfo
    {
        [SerializeField, NeoPrefabField, Tooltip("The player character for this option")]
        private FpsSoloCharacter m_Character = null;

        [SerializeField, Tooltip("The name that should be used to identify the character in the selection popup")]
        private string m_DisplayName = string.Empty;

        [SerializeField, Tooltip("A description for the character which will be displayed in the selection popup")]
        private string m_Description = string.Empty;

        [SerializeField, Tooltip("An image for the character which will be displayed in the selection popup")]
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

        public FpsSoloCharacter character
        {
            get { return m_Character; }
        }
    }
}
