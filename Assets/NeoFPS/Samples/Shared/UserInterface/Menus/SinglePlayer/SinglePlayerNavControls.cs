using System.Collections;
using UnityEngine;
using NeoSaveGames;
using NeoSaveGames.SceneManagement;
using UnityEngine.EventSystems;

namespace NeoFPS.Samples
{
	public class SinglePlayerNavControls : MenuNavControls
    {
        [SerializeField, Tooltip("The new game button (disable if the new game scene is not valid)")]
        private MultiInputButton m_NewGameButton = null;
        [SerializeField, Tooltip("The starting scene for a new game.")]
        private string m_NewGameScene = string.Empty;
        [SerializeField, Tooltip("The continue game button (disable if no auto saves exist)")]
        private MultiInputButton m_ContinueButton = null;
        [SerializeField, Tooltip("The load game button (disable if no saves exist)")]
        private MultiInputButton m_LoadButton = null;
        [SerializeField, Tooltip("The button to show the level select menu")]
        private MultiInputButton m_SelectLevelButton = null;

        private bool m_CanContinue = false;
        bool canContinue
        {
            get { return m_CanContinue; }
            set
            {
                if (m_CanContinue != value)
                {
                    m_CanContinue = value;
                    m_ContinueButton.interactable = value;
                    m_ContinueButton.RefreshInteractable();
                }
            }
        }

        public override void Show()
        {
            base.Show();

            bool selectedSet = false;

            // Check if new game scene is valid
            if (m_NewGameButton != null)
            {
                if (!string.IsNullOrWhiteSpace(m_NewGameScene) && NeoSceneManager.isSceneValid(m_NewGameScene))
                {
                    m_NewGameButton.onClick.AddListener(OnClickNewGame);
                    m_NewGameButton.gameObject.SetActive(true);
                    EventSystem.current.SetSelectedGameObject(m_NewGameButton.gameObject);
                    selectedSet = true;
                }
                else
                    m_NewGameButton.gameObject.SetActive(false);
            }

            // Check for available saves
            if (m_LoadButton != null)
            {
                m_LoadButton.interactable = SaveGameManager.hasAvailableSaves;
                m_LoadButton.RefreshInteractable();
            }

            if (m_ContinueButton != null)
            {
                // Check if can continue (this can block so do it intermittently)
                canContinue = SaveGameManager.canContinue;
                if (!canContinue)
                {
                    m_ContinueButton.interactable = false;
                    StartCoroutine(UpdateContinueButton());
                }
                else
                {
                    if (!selectedSet)
                    {
                        EventSystem.current.SetSelectedGameObject(m_ContinueButton.gameObject);
                        selectedSet = true;
                    }
                }

                m_ContinueButton.onClick.AddListener(OnClickContinue);
            }

            // Reset navigation (in case buttons disabled)
            widgetList.ResetWidgetNavigation();
            
            // Fall back to level select as starting selection
            if (!selectedSet)
                EventSystem.current.SetSelectedGameObject(m_SelectLevelButton?.gameObject);
        }

        public override void Hide()
        {
            // Remove continue event listener
            if (m_ContinueButton != null)
                m_ContinueButton.onClick.RemoveListener(OnClickContinue);

            base.Hide();
        }

        IEnumerator UpdateContinueButton()
        {
            float t = 0f;
            while (t < 0.25f)
            {
                yield return null;
                t += Time.unscaledDeltaTime;
            }

            while (!canContinue)
            {
                t = 0f;
                while (t < 0.5f)
                {
                    yield return null;
                    t += Time.unscaledDeltaTime;
                }
                canContinue = SaveGameManager.canContinue;
            }
        }

        public void OnClickNewGame()
        {
            NeoSceneManager.LoadScene(m_NewGameScene);
        }

        public void OnClickContinue()
        {
            SaveGameManager.Continue();
            canContinue = false;
        }
    }
}