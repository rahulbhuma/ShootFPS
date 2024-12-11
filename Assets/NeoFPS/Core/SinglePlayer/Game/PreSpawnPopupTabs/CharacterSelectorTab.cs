using NeoFPS.Samples;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeoFPS.SinglePlayer
{
    public class CharacterSelectorTab : InstantSwitchTabBase
    {
        [SerializeField, RequiredObjectProperty, Tooltip("A multi-choice UI widget that will be used to select the character. The options will be populated on start")]
        private MultiInputMultiChoice m_CharacterChoice = null;
        [SerializeField, Tooltip("The image widget used to display the character sprite in the popup")]
        private Image m_SpriteImage = null;
        [SerializeField, Tooltip("The text component that will be filled with the selected character's description")]
        private Text m_DescriptionText = null;

        public override string tabName
        {
            get { return "Select Loadout"; }
        }

        public ICharacterSelector characterSelector
        {
            get;
            private set;
        }

        public override bool Initialise(FpsSoloGameCustomisable g)
        {
            base.Initialise(g);

            characterSelector = g as ICharacterSelector;
            if (characterSelector != null && characterSelector.numCharacters > 0)
            {
                // Build options
                List<string> options = new List<string>();
                for (int i = 0; i < characterSelector.numCharacters; ++i)
                    options.Add(characterSelector.GetCharacterInfo(i).displayName);
                m_CharacterChoice.options = options.ToArray();

                // Assign event handler
                m_CharacterChoice.onIndexChanged.AddListener(OnCharacterIndexChanged);

                return true;
            }
            else
                return false;
        }

        private void Start()
        {
            // Set start index
            int startIndex = characterSelector.currentCharacterIndex;
            m_CharacterChoice.index = startIndex;
            ShowDetails(startIndex);
        }

        private void OnCharacterIndexChanged(int index)
        {
            characterSelector.currentCharacterIndex = index;

            ShowDetails(index);
        }

        void ShowDetails(int index)
        {
            var character = characterSelector.GetCharacterInfo(index);

            // Show description
            if (m_DescriptionText != null)
                m_DescriptionText.text = character.description;

            // Show sprite
            if (m_SpriteImage != null)
            {
                if (character.sprite != null)
                {
                    m_SpriteImage.sprite = character.sprite;
                    m_SpriteImage.gameObject.SetActive(true);
                }
                else
                    m_SpriteImage.gameObject.SetActive(false);
            }
        }
    }
}
