using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeoFPS.SinglePlayer
{
    public class SpawnZoneSelectionTab : InstantSwitchTabBase
    {
        [SerializeField, RequiredObjectProperty, Tooltip("The UI image widget that will be used to diplay the current scene's map")]
        private Image m_MapImage = null;
        [SerializeField, RequiredObjectProperty, Tooltip("A button widget that will be cloned and posiioned for each of the available spawn zones")]
        private Button m_SpawnSelectPrototype = null;
        [SerializeField, Tooltip("The colour that should be set for the selected spawn zone widget")]
        private Color m_SelectedColour = new Color(0, 1f, 0.63f, 0.4f);
        [SerializeField, Tooltip("The colour that should be set for all unselected spawn zone widgets")]
        private Color m_UnselectedColour = Color.white;

        private double m_FirstCharacter = 0;
        private Vector2 m_MapSize = Vector2.zero;
        private List<SelectorInfo> m_Selectors = new List<SelectorInfo>();

        private struct SelectorInfo
        {
            public Button button;
            public Graphic[] graphics;

            public SelectorInfo(Button b)
            {
                button = b;
                graphics = b.GetComponentsInChildren<Graphic>(true);
            }
        }

        public override string tabName
        {
            get { return "Select Spawn Zone"; }
        }

        public ISpawnZoneSelector spawnZoneSelector
        {
            get;
            private set;
        }

        public override bool Initialise(FpsSoloGameCustomisable g)
        {
            base.Initialise(g);

            if (m_SpawnSelectPrototype == null)
                return false;

            // Get numeric value for A
            m_FirstCharacter = char.GetNumericValue('A');

            spawnZoneSelector = g as ISpawnZoneSelector;
            if (spawnZoneSelector != null && spawnZoneSelector.numSpawnZones > 0)
            {
                // Assign map image
                if (m_MapImage != null)
                {
                    m_MapImage.sprite = spawnZoneSelector.mapSprite;
                    m_MapSize = m_MapImage.rectTransform.rect.size;
                }

                // Set up selection buttons
                for (int i = 0; i < spawnZoneSelector.numSpawnZones; ++i)
                {
                    var spawnZone = spawnZoneSelector.GetSpawnZoneInfo(i);

                    // Instantiate the selection button
                    var selector = Instantiate(m_SpawnSelectPrototype, m_SpawnSelectPrototype.transform.parent);
                    var selectorTransform = selector.transform as RectTransform;
                    selectorTransform.anchoredPosition = spawnZone.mapPosition * m_MapSize;
                    m_Selectors.Add(new SelectorInfo(selector));

                    // Set the button name
                    var text = selector.GetComponentInChildren<Text>(true);
                    if (text != null)
                        text.text = GetSpawnZoneID(i);

                    // Set the button event handler
                    int index = i;
                    selector.onClick.AddListener(() => {
                        spawnZoneSelector.currentSpawnZoneIndex = index;
                        OnSpawnIndexChanged(index);
                    });

                }

                // Set the initial value
                Invoke("ResetSpawnIndex", 0.02f);

                // Disable the prototype
                m_SpawnSelectPrototype.gameObject.SetActive(false);

                return true;
            }
            else
                return false;
        }

        public void ResetSpawnIndex()
        {
            OnSpawnIndexChanged(spawnZoneSelector.currentSpawnZoneIndex);
        }

        void OnSpawnIndexChanged(int index)
        {
            for (int i = 0; i < m_Selectors.Count; ++i)
            {
                foreach (var g in m_Selectors[i].graphics)
                    g.color = (i == index) ? m_SelectedColour : m_UnselectedColour;
            }
        }

        string GetSpawnZoneID(int index)
        {
            return ((char)('A' + index)).ToString();
        }
    }
}
