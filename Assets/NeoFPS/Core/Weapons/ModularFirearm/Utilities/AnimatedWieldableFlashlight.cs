using NeoFPS.ModularFirearms;
using NeoSaveGames;
using NeoSaveGames.Serialization;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-animatedwieldableflashlight.html")]
    public class AnimatedWieldableFlashlight : MonoBehaviour, IWieldableFlashlight, INeoSerializableComponent
    {
        [SerializeField, NeoObjectInHierarchyField(false, required = true), Tooltip("A child object with a light component attached.")]
        private GameObject m_LightObject = null;
        [SerializeField, Tooltip("Should the flashlight be enabled on start.")]
        private bool m_StartEnabled = false;

        [Header("Animation Data")]

        [SerializeField, Tooltip("The animation data when switching the flashlight on.")]
        private SwitchAnimationProperties m_ToggleOn = new SwitchAnimationProperties
        {
            animationTrigger = "flashlight",
            toggleDelay = 0.5f,
            blockDuration = 0.85f,
            totalDuration = 1f
        };
        [SerializeField, Tooltip("The animation data when switching the flashlight off.")]
        private SwitchAnimationProperties m_ToggleOff = new SwitchAnimationProperties
        {
            animationTrigger = "flashlight",
            toggleDelay = 0.5f,
            blockDuration = 0.85f,
            totalDuration = 1f
        };

        [Header("Events")]

        [SerializeField, Tooltip("An event fired when the flashlight is switched on")]
        private UnityEvent m_OnToggleOn = null;
        [SerializeField, Tooltip("An event fired when the flashlight is switched off")]
        private UnityEvent m_OnToggleOff = null;

        private Light m_Light = null;
        private float m_FullIntensity = 0f;
        private IWieldable m_Wieldable = null;
        private bool m_CurrentState = false;
        private bool m_TargetState = false;
        private float m_Timer = 0f;
        private Coroutine m_ToggleCoroutine = null;
        private int m_OnTriggerHash = -1;
        private int m_OffTriggerHash = -1;
        private bool m_IsValid = false;

        [Serializable]
        private struct SwitchAnimationProperties
        {
            [Tooltip("The name of the trigger parameter to fire on the weapon's animator when switching the flashlight on or off.")]
            public string animationTrigger;
            [Tooltip("The delay from the start of the animation after which the light should actually be switched on or off.")]
            public float toggleDelay;
            [Tooltip("The amount of time from the start of the animation where it should not be possible to shoot or reload the weapon.")]
            public float blockDuration;
            [Tooltip("The total duration of the animation.")]
            public float totalDuration;
        }

        public bool on
        {
            get { return m_CurrentState; }
            set { SwitchState(value); }
        }

        private float m_Brightness = 1f;
        public float brightness
        {
            get { return m_Brightness; }
            set
            {
                if (m_Light != null)
                {
                    m_Brightness = Mathf.Clamp01(value);
                    m_Light.intensity = m_Brightness * m_FullIntensity;
                }
            }
        }

        protected void OnValidate()
        {
            if (m_LightObject == gameObject)
            {
                Debug.LogError("Light object should be a child of the flashlight");
                m_LightObject = null;
            }
        }

        protected void Start()
        {
            if (m_LightObject != null)
            {
                m_Light = m_LightObject.GetComponent<Light>();
                m_FullIntensity = m_Light.intensity;
                m_Light.intensity = m_FullIntensity * m_Brightness;

                if (!string.IsNullOrWhiteSpace(m_ToggleOn.animationTrigger))
                    m_OnTriggerHash = Animator.StringToHash(m_ToggleOn.animationTrigger);
                if (!string.IsNullOrWhiteSpace(m_ToggleOff.animationTrigger))
                    m_OffTriggerHash = Animator.StringToHash(m_ToggleOff.animationTrigger);

                CheckTimings();

                if (m_ToggleCoroutine == null)
                {
                    if (m_StartEnabled)
                    {
                        m_LightObject.SetActive(true);
                        m_CurrentState = true;
                        m_TargetState = true;
                    }
                    else
                    {
                        m_LightObject.SetActive(false);
                        m_CurrentState = false;
                        m_TargetState = false;
                    }
                }
            }

            CheckIsValid();
        }

        protected void OnEnable()
        {
            m_Wieldable = GetComponentInParent<IWieldable>();
            CheckIsValid();
        }

        protected void OnDisable()
        {
            if (m_ToggleCoroutine != null)
            {
                StopCoroutine(m_ToggleCoroutine);
                m_ToggleCoroutine = null;
                m_Wieldable.RemoveBlocker(this);
                m_Wieldable.RemoveBlocker(m_LightObject);
            }

            m_Wieldable = null;
            on = false;
        }

        void CheckIsValid()
        {
            m_IsValid = m_LightObject != null && m_Light != null && m_Wieldable != null;
        }

        void CheckTimings()
        {
            if (m_ToggleOn.blockDuration > m_ToggleOn.totalDuration)
                m_ToggleOn.blockDuration = m_ToggleOn.totalDuration;
            if (m_ToggleOn.toggleDelay > m_ToggleOn.totalDuration)
                m_ToggleOn.toggleDelay = m_ToggleOn.totalDuration;

            if (m_ToggleOff.blockDuration > m_ToggleOff.totalDuration)
                m_ToggleOff.blockDuration = m_ToggleOff.totalDuration;
            if (m_ToggleOff.toggleDelay > m_ToggleOff.totalDuration)
                m_ToggleOff.toggleDelay = m_ToggleOff.totalDuration;
        }

        bool CheckWieldable()
        {
            if (m_Wieldable as Component == null)
            {
                m_Wieldable = null;

                if (m_ToggleCoroutine != null)
                {
                    StopCoroutine(m_ToggleCoroutine);
                    m_ToggleCoroutine = null;
                }

                return false;
            }
            return true;
        }

        public void Toggle()
        {
            SwitchState(!m_TargetState);
        }

        void SwitchState(bool targetState)
        {
            if (CheckWieldable() && m_IsValid)
            {
                m_TargetState = targetState;

                // Block wieldable (can't shoot / reload, etc while inspecting)
                if (m_TargetState != m_CurrentState)
                {
                    if (m_ToggleCoroutine == null)
                        m_ToggleCoroutine = StartCoroutine(ToggleCoroutine());
                }
                else
                {
                    if (m_ToggleCoroutine != null && m_Timer == 0f)
                    {
                        // TODO: Blocking???
                        StopCoroutine(m_ToggleCoroutine);
                        m_ToggleCoroutine = null;
                    }
                }
            }
        }

        IEnumerator ToggleCoroutine()
        {
            m_Timer = 0f;

            // Wait for current blocker to end
            while (m_CurrentState != m_TargetState && CheckWieldable() && m_Wieldable.isBlocked)
                yield return null;

            // Cancel if target state has been toggled back to current
            if (m_CurrentState == m_TargetState)
            {
                m_ToggleCoroutine = null;
                yield break;
            }

            while (m_CurrentState != m_TargetState)
            {
                var animationData = m_TargetState ? m_ToggleOn : m_ToggleOff;
                int animationHash = m_TargetState ? m_OnTriggerHash : m_OffTriggerHash;

                // Start the animation
                if (animationHash != -1)
                    m_Wieldable.animationHandler.SetTrigger(animationHash);

                // Block the weapon from other actions
                if (animationData.blockDuration > 0.0001f)
                    m_Wieldable.AddBlocker(this);

                while (m_Timer < animationData.totalDuration)
                {
                    yield return null;

                    // Get the new time
                    float newTime = m_Timer + Time.deltaTime;

                    // Check if crossed toggle delay
                    if (m_Timer < animationData.toggleDelay && newTime >= animationData.toggleDelay)
                    {
                        m_CurrentState = m_TargetState;
                        if (m_TargetState)
                        {
                            m_LightObject.SetActive(true);
                            m_OnToggleOn.Invoke();
                        }
                        else
                        {
                            m_LightObject.SetActive(false);
                            m_OnToggleOff.Invoke();
                        }
                    }

                    // Check if crossed blocking delay
                    if (m_Timer < animationData.blockDuration && newTime >= animationData.blockDuration)
                        m_Wieldable.RemoveBlocker(this);

                    m_Timer = newTime;
                }

                m_Timer = 0f;
            }

            m_ToggleCoroutine = null;
        }

        IEnumerator LoadCoroutine()
        {
            var animationData = m_TargetState ? m_ToggleOn : m_ToggleOff;
            int animationHash = m_TargetState ? m_OnTriggerHash : m_OffTriggerHash;

            // Start the animation
            if (animationHash != -1)
                m_Wieldable.animationHandler.SetTrigger(animationHash);

            // Block the weapon from other actions
            if (m_Timer < animationData.blockDuration)
                m_Wieldable.AddBlocker(this);

            while (m_Timer < animationData.totalDuration)
            {
                yield return null;

                // Get the new time
                float newTime = m_Timer + Time.deltaTime;

                // Check if crossed toggle delay
                if (m_Timer < animationData.toggleDelay && newTime >= animationData.toggleDelay)
                {
                    m_CurrentState = m_TargetState;
                    if (m_TargetState)
                    {
                        m_LightObject.SetActive(true);
                        m_OnToggleOn.Invoke();
                    }
                    else
                    {
                        m_LightObject.SetActive(false);
                        m_OnToggleOff.Invoke();
                    }
                }

                // Check if crossed blocking delay
                if (m_Timer < animationData.blockDuration && newTime >= animationData.blockDuration)
                    m_Wieldable.RemoveBlocker(this);

                m_Timer = newTime;
            }

            m_Timer = 0f;

            if (m_CurrentState == m_TargetState)
                m_ToggleCoroutine = null;
            else
                m_ToggleCoroutine = StartCoroutine(ToggleCoroutine());
        }

        #region INeoSerializableComponent IMPLEMENTATION

        static readonly NeoSerializationKey k_OnKey = new NeoSerializationKey("on");
        static readonly NeoSerializationKey k_BrightnessKey = new NeoSerializationKey("brightness");
        static readonly NeoSerializationKey k_TimerKey = new NeoSerializationKey("timer");
        static readonly NeoSerializationKey k_TargetKey = new NeoSerializationKey("target");

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValue(k_OnKey, m_CurrentState);
            writer.WriteValue(k_BrightnessKey, m_Brightness);

            if (m_ToggleCoroutine != null)
            {
                writer.WriteValue(k_TargetKey, m_TargetState);
                writer.WriteValue(k_TimerKey, m_Timer);
            }
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            reader.TryReadValue(k_BrightnessKey, out m_Brightness, m_Brightness);
            reader.TryReadValue(k_OnKey, out m_CurrentState, m_CurrentState);
            m_LightObject.SetActive(m_CurrentState);

            if (reader.TryReadValue(k_TargetKey, out m_TargetState, m_TargetState))
            {
                reader.TryReadValue(k_TimerKey, out m_Timer, m_Timer);
                if (m_Timer > 0f || m_TargetState != m_CurrentState)
                    m_ToggleCoroutine = StartCoroutine(LoadCoroutine());
            }
        }

        #endregion
    }
}