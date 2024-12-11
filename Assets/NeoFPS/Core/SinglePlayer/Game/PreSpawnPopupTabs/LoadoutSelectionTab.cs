using NeoFPS.Samples;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeoFPS.SinglePlayer
{
    public class LoadoutSelectionTab : InstantSwitchTabBase
    {
        [SerializeField, RequiredObjectProperty, Tooltip("A multi-choice UI widget that will be used to select the loadout. The options will be populated on start")]
        private MultiInputMultiChoice m_LoadoutChoice = null;
        [SerializeField, Tooltip("The image widget used to display the loadout info's sprite in the popup")]
        private Image m_SpriteImage = null;
        [SerializeField, Tooltip("The text component that will be filled with the selected loadout's description")]
        private Text m_DescriptionText = null;

        public override string tabName
        {
            get { return "Select Loadout"; }
        }

        public ILoadoutSelector loadoutSelector
        {
            get;
            private set;
        }

        public override bool Initialise(FpsSoloGameCustomisable g)
        {
            base.Initialise(g);

            loadoutSelector = g as ILoadoutSelector;
            if (loadoutSelector != null && loadoutSelector.numLoadouts > 0)
            {
                // Build options
                List<string> options = new List<string>();
                for (int i = 0; i < loadoutSelector.numLoadouts; ++i)
                    options.Add(loadoutSelector.GetLoadoutInfo(i).displayName);
                m_LoadoutChoice.options = options.ToArray();

                // Assign event handler
                m_LoadoutChoice.onIndexChanged.AddListener(OnLoadoutIndexChanged);


                return true;
            }
            else
                return false;
        }


        private void Start()
        {
            // Set start index
            int startIndex = loadoutSelector.currentLoadoutIndex;
            m_LoadoutChoice.index = startIndex;
            ShowDetails(startIndex);
        }

        private void OnLoadoutIndexChanged(int index)
        {
            loadoutSelector.currentLoadoutIndex = index;

            ShowDetails(index);
        }

        void ShowDetails(int index)
        {
            var loadout = loadoutSelector.GetLoadoutInfo(index);

            // Show description
            if (m_DescriptionText != null)
                m_DescriptionText.text = loadout.description;

            // Show sprite
            if (m_SpriteImage != null)
            {
                if (loadout.sprite != null)
                {
                    m_SpriteImage.sprite = loadout.sprite;
                    m_SpriteImage.gameObject.SetActive(true);
                }
                else
                    m_SpriteImage.gameObject.SetActive(false);
            }
        }
    }
}
