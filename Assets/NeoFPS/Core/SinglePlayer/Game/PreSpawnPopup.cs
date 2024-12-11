using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using NeoFPS.Samples;
using UnityEngine.EventSystems;

namespace NeoFPS.SinglePlayer
{
    public class PreSpawnPopup : PreSpawnPopupBase, IPointerClickHandler
    {
        [SerializeField, RequiredObjectProperty, Tooltip("The multi-choice UI widget that will be used to switch between tabs in the pre-spawn popup")]
        private MultiInputMultiChoice m_TabSelect = null;
        [SerializeField, RequiredObjectProperty, Tooltip("The button that's used to spawn the character")]
        private MultiInputButton m_SpawnButton = null;

        private List<PreSpawnPopupTab> m_Tabs = null;
        private int m_CurrentTab = -1;
        private int m_PendingTab = -1;

        public override Selectable startingSelection
        {
            get { return m_SpawnButton; }
        }
        
        public override void Initialise(FpsSoloGameCustomisable g, UnityAction onComplete)
        {
            base.Initialise(g, onComplete);

            m_Tabs = new List<PreSpawnPopupTab>();
            GetComponentsInChildren(true, m_Tabs);

            // Initialise and remove invalid
            for (int i = m_Tabs.Count - 1; i >= 0; --i)
            {
                var tab = m_Tabs[i];
                if (!tab.Initialise(g))
                    m_Tabs.RemoveAt(i);
                tab.Hide(true);
            }

            // Show the first tab
            if (m_Tabs.Count > 0)
            {
                m_Tabs[0].Show(true);
                for (int i = 1; i < m_Tabs.Count; ++i)
                    m_Tabs[i].Hide(true);
                m_CurrentTab = 0;
                m_PendingTab = 0;

                // Set the tab options
                string[] tabNames = new string[m_Tabs.Count];
                for (int i = 0; i < m_Tabs.Count; ++i)
                    tabNames[i] = m_Tabs[i].tabName;
                m_TabSelect.options = tabNames;

                // Resize
                var rt = transform as RectTransform;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                // Select the first one
                m_TabSelect.onIndexChanged.AddListener(OnTabIndexChanged);
                m_TabSelect.index = 0;

                // Add spawn / complete listener
                m_SpawnButton.onClick.AddListener(Spawn);
            }
            else
            {
                Debug.LogError("Attempting to show pre-spawn popup, but there are no valid tabs based on the game mode");
                Spawn();
            }
        }

        void OnTabIndexChanged(int index)
        {
            m_PendingTab = index;

            if (m_PendingTab != m_CurrentTab)
                m_Tabs[m_CurrentTab].Hide(false);
            else
                m_Tabs[m_CurrentTab].Show(false);
        }

        void Update()
        {
            if (m_PendingTab != m_CurrentTab && m_Tabs[m_CurrentTab].state == PopupTabState.Hidden)
            {
                m_CurrentTab = m_PendingTab;
                m_Tabs[m_CurrentTab].Show(false);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Empty to prevent clicks falling through and cancelling the popup
        }
    }
}