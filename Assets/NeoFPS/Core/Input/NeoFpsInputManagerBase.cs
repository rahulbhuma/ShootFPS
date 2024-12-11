using NeoFPS.ScriptGeneration;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    public abstract class NeoFpsInputManagerBase : NeoFpsManager<NeoFpsInputManagerBase>
    {
        private static RuntimeBehaviour s_ProxyBehaviour = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void LoadInputManager()
        {
            GetInstance("FpsManager_Input");
        }

        protected override void Initialise()
        {
            s_ProxyBehaviour = GetBehaviourProxy<RuntimeBehaviour>();
        }

        public override bool IsValid()
        {
            return true;
        }

        #region CURSOR CAPTURE

        private static bool m_CaptureMouseCursor;
        public static bool captureMouseCursor
        {
            get { return m_CaptureMouseCursor; }
            set
            {
                m_CaptureMouseCursor = value;
                if (m_CaptureMouseCursor)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }

        #endregion

        #region GAMEPADS

        public static event UnityAction<bool> onIsGamepadConnectedChanged;

        public static bool isGamepadConnected
        {
            get { return instance.GetIsGamepadConnected(); }
        }

        public static string connectedGamepad
        {
            get { return instance.GetConnectedGamepad(); }
        }

        public static int numGamepadProfiles
        {
            get { return instance.GetNumGamepadProfiles(); }
        }

        public static string GetGamepadProfileName(int index)
        {
            return instance.GetGamepadProfileNameInteral(index);
        }

        protected void OnGamepadConnectedChanged(bool connected)
        {
            onIsGamepadConnectedChanged?.Invoke(connected);
        }

        protected abstract bool GetIsGamepadConnected();

        protected abstract string GetConnectedGamepad();

        protected abstract int GetNumGamepadProfiles();

        protected abstract string GetGamepadProfileNameInteral(int index);

        #endregion

        #region ESCAPE HANDLERS

        private static List<UnityAction> s_EscapeHandlers = new List<UnityAction>(4);

        public static bool wasCancelPressedThisFrame
        {
            get
            {
                if (instance != null)
                    return instance.CheckForCancelInput();
                else
                    return false;
            }
        }

        public static void PushEscapeHandler(UnityAction handler)
        {
            // Check if earlier in the handler stack and move to top if so
            for (int i = 0; i < s_EscapeHandlers.Count - 1; ++i)
            {
                if (s_EscapeHandlers[i] == handler)
                {
                    s_EscapeHandlers.RemoveAt(i);
                    s_EscapeHandlers.Add(handler);
                    return;
                }
            }

            // Add as top handler if empty or not already top
            if (s_EscapeHandlers.Count == 0 || s_EscapeHandlers[s_EscapeHandlers.Count - 1] != handler)
                s_EscapeHandlers.Add(handler);
        }

        public static void PopEscapeHandler(UnityAction handler)
        {
            for (int i = s_EscapeHandlers.Count - 1; i >= 0; --i)
            {
                if (s_EscapeHandlers[i] == handler)
                    s_EscapeHandlers.RemoveAt(i);
            }
        }

        protected void HandleEscape()
        {
            if (s_EscapeHandlers.Count > 0)
            {
                if (s_EscapeHandlers[s_EscapeHandlers.Count - 1] != null)
                    s_EscapeHandlers[s_EscapeHandlers.Count - 1].Invoke();
            }
            else
                captureMouseCursor = !captureMouseCursor;
        }

        protected abstract bool CheckForEscapeInput();
        protected abstract bool CheckForCancelInput();

        #endregion

        #region PROXY BEHAVIOUR

        class RuntimeBehaviour : MonoBehaviour
        {
            protected void Update()
            {
#if AGGRESIVE_CURSOR_LOCK
                if (captureMouseCursor)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
#endif

                // Check for and handle escape
                if (instance.CheckForEscapeInput())
                    instance.HandleEscape();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (s_ProxyBehaviour != null)
            {
                Destroy(s_ProxyBehaviour);
                s_ProxyBehaviour = null;
            }
        }

        #endregion

        #region CODE GENERATION (CONTEXTS)

#if UNITY_EDITOR
#pragma warning disable 0414

        // Input contexts
        [SerializeField] private GeneratorConstantsEntry[] m_InputContextInfo = null;
        [SerializeField] private bool m_InputContextDirty = false;
        [SerializeField] private int m_InputContextError = 1;

        public virtual bool showWarning
        {
            get { return false; }
        }

        public virtual bool showError
        {
            get { return m_InputContextError > 0; }
        }

#pragma warning restore 0414
#endif

        #endregion
    }
}