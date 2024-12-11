using UnityEngine;

namespace NeoSaveGames.SceneManagement
{
    [HelpURL("https://docs.neofps.com/manual/utilitiesref-mb-neomainmenusceneswitcher.html")]
	public class NeoMainMenuSceneSwitcher : MonoBehaviour
	{
        [Header("Loading Scene")]
        [SerializeField, Tooltip("Load and display a custom \"loading screen\" scene or use the SaveGameManager default")]
        private LoadingSceneMode m_LoadingSceneMode = LoadingSceneMode.Default;
        [SerializeField, Tooltip("The loading screen scene to show. Make sure that the scene with this name is added to the project build properties.")]
        private string m_LoadingSceneName = "Loading";
        [SerializeField, Tooltip("The loading screen scene to show. Make sure that this index matches the desired scene in your scene settings.")]
        private int m_LoadingSceneIndex = 1;

        public enum LoadingSceneMode
        {
            Default,
            SceneName,
            SceneIndex
        }

        protected virtual void PreSceneSwitch()
        { }

        public void Switch ()
		{
            PreSceneSwitch();

            switch (m_LoadingSceneMode)
            {
                case LoadingSceneMode.Default:
                    NeoSceneManager.LoadMainMenu();
                    break;
                case LoadingSceneMode.SceneName:
                    NeoSceneManager.LoadMainMenu(m_LoadingSceneName);
                    break;
                case LoadingSceneMode.SceneIndex:
                    NeoSceneManager.LoadMainMenu(m_LoadingSceneIndex);
                    break;
            }
        }
	}
}