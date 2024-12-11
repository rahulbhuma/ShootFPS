using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace NeoSaveGames.SceneManagement
{
    [CreateAssetMenu(fileName = "FpsManager_NeoSceneManager", menuName = "NeoFPS/Managers/Scene Manager", order = NeoFPS.NeoFpsMenuPriorities.manager_scene)]
    [HelpURL("https://docs.neofps.com/manual/savegamesref-so-scenemanager.html")]
    public class NeoSceneManager : NeoFPS.NeoFpsManager<NeoSceneManager>
    {
        private static RuntimeBehaviour s_ProxyGameObject = null;
        private static bool s_Busy = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void LoadNeoSceneManager()
        {
            GetInstance("FpsManager_NeoSceneManager");
        }

        protected override void Initialise()
        {
            s_ProxyGameObject = GetBehaviourProxy<RuntimeBehaviour>();
			s_Busy = false;
        }

        public override bool IsValid()
        {
            return true;
        }

        [SerializeField, Tooltip("The loading screen scene to load by default if a custom one is not specified.")]
        private int m_DefaultMainMenuIndex = -1;
        [SerializeField, Tooltip("The loading screen scene to load by default if a custom one is not specified.")] 
        private int m_DefaultLoadingScreenIndex = -1;
        [SerializeField, Tooltip("The minimum amount of time the loading screen should be shown for. This prevents the loading screen from appearing for fractions of a second. Useful if you show tips.")]
        private float m_MinLoadScreenTime = 2f;

        [Header("Scene Load Events")]
        [SerializeField] private SceneLoadEvent m_OnSceneLoaded = new SceneLoadEvent();
        [SerializeField] private SceneLoadProgressEvent m_OnSceneLoadProgress = new SceneLoadProgressEvent();
        [SerializeField] private UnityEvent m_PreSceneActivation = new UnityEvent();
        [SerializeField] private UnityEvent m_OnSceneLoadFailed = new UnityEvent();

        enum LoadMode
        {
            Index,
            Name,
            Neither
        }

        [Serializable]
        public class SceneLoadEvent : UnityEvent<int> { }
        [Serializable]
        public class SceneLoadProgressEvent : UnityEvent<float> { }

        public class RuntimeBehaviour : MonoBehaviour { }

        public static int defaultMainMenuSceneIndex
        {
            get
            {
                if (instance != null)
                    return instance.m_DefaultMainMenuIndex;
                else
                {
                    Debug.LogWarning("NeoSceneManager instance not found");
                    return -1;
                }
            }
        }

        public static int defaultLoadingScreenIndex
        {
            get
            {
                if (instance != null)
                    return instance.m_DefaultLoadingScreenIndex;
                else
                {
                    Debug.LogWarning("NeoSceneManager instance not found");
                    return -1;
                }
            }
        }

        public static event UnityAction<int, string> onSceneLoadRequested;

        public static event UnityAction<float> onSceneLoadProgress
        {
            add
            {
                if (instance != null)
                    instance.m_OnSceneLoadProgress.AddListener(value);
            }
            remove
            {
                if (instance != null)
                    instance.m_OnSceneLoadProgress.RemoveListener(value);
            }
        }

        public static event UnityAction<int> onSceneLoaded
        {
            add
            {
                if (instance != null)
                    instance.m_OnSceneLoaded.AddListener(value);
            }
            remove
            {
                if (instance != null)
                    instance.m_OnSceneLoaded.RemoveListener(value);
            }
        }

        public static event UnityAction preSceneActivation
        {
            add
            {
                if (instance != null)
                    instance.m_PreSceneActivation.AddListener(value);
            }
            remove
            {
                if (instance != null)
                    instance.m_PreSceneActivation.RemoveListener(value);
            }
        }

        public static event UnityAction onSceneLoadFailed
        {
            add
            {
                if (instance != null)
                    instance.m_OnSceneLoadFailed.AddListener(value);
            }
            remove
            {
                if (instance != null)
                    instance.m_OnSceneLoadFailed.RemoveListener(value);
            }
        }

        private void OnValidate()
        {
            m_MinLoadScreenTime = Mathf.Clamp(m_MinLoadScreenTime, 0f, 60f);
        }

        public static bool isSceneValid(string sceneName)
        {
            return sceneName != null && Application.CanStreamedLevelBeLoaded(sceneName);
        }

        public static bool isSceneValid(int sceneIndex)
        {
            return Application.CanStreamedLevelBeLoaded(sceneIndex);
        }

        #region LOAD SCENE ALTERNATIVE METHODS

        public static void LoadMainMenu()
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(null, defaultMainMenuSceneIndex, null, defaultLoadingScreenIndex, null, null));
        }

        public static void LoadMainMenu(string loadingSceneName)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(null, defaultMainMenuSceneIndex, loadingSceneName, -1, null, null));
        }

        public static void LoadMainMenu(int loadingIndex)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(null, defaultMainMenuSceneIndex, null, loadingIndex, null, null));
        }

        public static void LoadScene(string sceneName)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(sceneName, -1, null, defaultLoadingScreenIndex, null, null));
        }

        public static void LoadScene(int sceneIndex)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(null, sceneIndex, null, defaultLoadingScreenIndex, null, null));
        }

        public static void LoadScene(string sceneName, string loadingSceneName)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(sceneName, -1, loadingSceneName, -1, null, null));
        }

        public static void LoadScene(int sceneIndex, int loadingIndex)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(null, sceneIndex, null, loadingIndex, null, null));
        }

        public static void LoadScene(string sceneName, int loadingIndex)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(sceneName, -1, null, loadingIndex, null, null));
        }

        public static void LoadScene(int sceneIndex, string loadingSceneName)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(null, sceneIndex, loadingSceneName, -1, null, null));
        }

        public static void LoadScene(string sceneName, Action onComplete)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(sceneName, -1, null, defaultLoadingScreenIndex, null, onComplete));
        }

        public static void LoadScene(int sceneIndex, Action onComplete)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(null, sceneIndex, null, defaultLoadingScreenIndex, null, onComplete));
        }

        public static void LoadScene(string sceneName, string loadingSceneName, Action onComplete)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(sceneName, -1, loadingSceneName, -1, null, onComplete));
        }

        public static void LoadScene(int sceneIndex, int loadingIndex, Action onComplete)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(null, sceneIndex, null, loadingIndex, null, onComplete));
        }

        public static void LoadScene(string sceneName, int loadingIndex, Action onComplete)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(sceneName, -1, null, loadingIndex, null, onComplete));
        }

        public static void LoadScene(int sceneIndex, string loadingSceneName, Action onComplete)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(null, sceneIndex, loadingSceneName, -1, null, onComplete));
        }

        public static void LoadScene(string sceneName, Action<Scene> activationCallback)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(sceneName, -1, null, defaultLoadingScreenIndex, activationCallback, null));
        }

        public static void LoadScene(int sceneIndex, Action<Scene> activationCallback)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(null, sceneIndex, null, defaultLoadingScreenIndex, activationCallback, null));
        }

        public static void LoadScene(string sceneName, string loadingSceneName, Action<Scene> activationCallback)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(sceneName, -1, loadingSceneName, -1, activationCallback, null));
        }

        public static void LoadScene(int sceneIndex, int loadingIndex, Action<Scene> activationCallback)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(null, sceneIndex, null, loadingIndex, activationCallback, null));
        }

        public static void LoadScene(string sceneName, int loadingIndex, Action<Scene> activationCallback)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(sceneName, -1, null, loadingIndex, activationCallback, null));
        }

        public static void LoadScene(int sceneIndex, string loadingSceneName, Action<Scene> activationCallback)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(null, sceneIndex, loadingSceneName, -1, activationCallback, null));
        }

        public static void LoadScene(string sceneName, Action<Scene> activationCallback, Action onComplete)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(sceneName, -1, null, defaultLoadingScreenIndex, activationCallback, onComplete));
        }

        public static void LoadScene(int sceneIndex, Action<Scene> activationCallback, Action onComplete)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(null, sceneIndex, null, defaultLoadingScreenIndex, activationCallback, onComplete));
        }

        public static void LoadScene(string sceneName, string loadingSceneName, Action<Scene> activationCallback, Action onComplete)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(sceneName, -1, loadingSceneName, -1, activationCallback, onComplete));
        }

        public static void LoadScene(int sceneIndex, int loadingIndex, Action<Scene> activationCallback, Action onComplete)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(null, sceneIndex, null, loadingIndex, activationCallback, onComplete));
        }

        public static void LoadScene(string sceneName, int loadingIndex, Action<Scene> activationCallback, Action onComplete)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(sceneName, -1, null, loadingIndex, activationCallback, onComplete));
        }

        public static void LoadScene(int sceneIndex, string loadingSceneName, Action<Scene> activationCallback, Action onComplete)
        {
            if (instance != null && !s_Busy)
                s_ProxyGameObject.StartCoroutine(LoadSceneInternal(null, sceneIndex, loadingSceneName, -1, activationCallback, onComplete));
        }

        #endregion
                
        static IEnumerator LoadSceneInternal(string sceneName, int sceneIndex, string loadingSceneName, int loadingSceneIndex, Action<Scene> activationCallback, Action onComplete)
        {
            s_Busy = true;

            // Check how to load the scene
            LoadMode sceneLoadMode = LoadMode.Neither;
            if (isSceneValid(sceneIndex))
                sceneLoadMode = LoadMode.Index;
            else
            {
                if (isSceneValid(sceneName))
                    sceneLoadMode = LoadMode.Name;
            }

            if (sceneLoadMode == LoadMode.Neither)
            {
                Debug.LogError(string.Format("Attempting to load invalid scene. Check it exists and is added to the build options.", sceneName));
                instance.m_OnSceneLoadFailed.Invoke();
                if (onComplete != null)
                    onComplete();
            }
            else
            {
                // Fire scene load started event
                onSceneLoadRequested?.Invoke(sceneIndex, sceneName);

                // Check how to load the loading overlay scene
                LoadMode overlayLoadMode = LoadMode.Neither;
                if (isSceneValid(loadingSceneIndex))
                    overlayLoadMode = LoadMode.Index;
                else
                {
                    if (isSceneValid(loadingSceneName))
                        overlayLoadMode = LoadMode.Name;
                }

                if (overlayLoadMode == LoadMode.Neither)
                {
                    Debug.LogError(string.Format("\"Loading\" scene is not valid. Reverting to standard SceneManager. Scene index: {1}, Scene name: {0}", loadingSceneIndex, loadingSceneName));

                    AsyncOperation op = null;
                    if (sceneLoadMode == LoadMode.Index)
                        op = SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Single);
                    else
                        op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

                    while (!op.isDone)
                        yield return null;

                    // Signal completion
                    if (onComplete != null)
                        onComplete();
                }
                else
                {
                    AsyncOperation op = null;
                    Scene loadingScene = new Scene();

                    var startTime = Time.realtimeSinceStartup;

                    // Open "loading" scene (immediate)
                    if (overlayLoadMode == LoadMode.Index)
                    {
                        op = SceneManager.LoadSceneAsync(loadingSceneIndex, LoadSceneMode.Single);
                        loadingScene = SceneManager.GetSceneByBuildIndex(loadingSceneIndex);
                    }
                    else
                    {
                        op = SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Single);
                        loadingScene = SceneManager.GetSceneByName(loadingSceneName);
                    }
                    while (!op.isDone)
                        yield return null;

                    // Load the main scene in the background (async)
                    Scene scene = new Scene();
                    if (sceneLoadMode == LoadMode.Index)
                    {
                        op = SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive);
                        scene = SceneManager.GetSceneByBuildIndex(sceneIndex);
                    }
                    else
                    {
                        op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                        //sceneName = sceneName.Substring(7, sceneName.Length - 13);
                        scene = SceneManager.GetSceneByName(sceneName);
                        if (!scene.IsValid())
                            scene = SceneManager.GetSceneByPath(sceneName);
                    }

                    SceneManager.sceneLoaded += OnSceneLoaded;

                    // Prevent activation and wait for pre-activation to complete
                    op.allowSceneActivation = false;
                    while (op.progress < 0.9f)
                    {
                        // Invoke progress event
                        SetSceneLoadProgress(op.progress, Time.realtimeSinceStartup - startTime);

                        yield return null;
                    }

                    // Wait for minimum load time
                    while (Time.realtimeSinceStartup < startTime + instance.m_MinLoadScreenTime)
                    {
                        // Invoke progress event
                        SetSceneLoadProgress(op.progress, Time.realtimeSinceStartup - startTime);

                        yield return null;
                    }

                    // Invoke pre-activation event
                    instance.m_PreSceneActivation.Invoke();

                    // Allow activation and wait for complete
                    op.allowSceneActivation = true;
                    while (!op.isDone)
                    {
                        // Invoke progress event
                        SetSceneLoadProgress(op.progress, 1f);

                        yield return null;
                    }

                    // Invoke progress event
                    SetSceneLoadProgress(1f, 1f);

                    // Set the scene to active and signal activation
                    SceneManager.SetActiveScene(scene);
                    if (activationCallback != null)
                        activationCallback(scene);

#if UNITY_2019_3_OR_NEWER
                    // Rebuild light-probes
                    LightProbes.Tetrahedralize();
#endif

                    // Unload the "loading" scene
                    op = SceneManager.UnloadSceneAsync(loadingScene.buildIndex);
                    if (op == null)
                    {
                        Debug.LogError("Can't unload \"Loading\" scene, because the main scene failed to load.");

                        // Invoke failed event
                        instance.m_OnSceneLoadFailed.Invoke();
                    }
                    else
                    {
                        while (!op.isDone)
                            yield return null;

                        // Invoke succeeded event
                        instance.m_OnSceneLoaded.Invoke(scene.buildIndex);
                    }

                    // Signal completion
                    if (onComplete != null)
                        onComplete();
                }
            }

            s_Busy = false;
        }

        static float s_Progress = 0f;
        static void SetSceneLoadProgress(float opProgress, float timerProgress)
        {
            float progress = Mathf.Clamp01(Mathf.Min(opProgress, timerProgress));
            if (!Mathf.Approximately(s_Progress, progress))
            {
                s_Progress = progress;
                instance.m_OnSceneLoadProgress.Invoke(progress);
            }
        }

        static void OnSceneLoaded(Scene s, LoadSceneMode mode)
        {
            SceneManager.SetActiveScene(s);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}