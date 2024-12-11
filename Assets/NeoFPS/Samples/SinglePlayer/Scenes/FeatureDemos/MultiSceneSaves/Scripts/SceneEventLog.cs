using NeoSaveGames.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NeoFPS.Samples.SinglePlayer.MultiScene
{
    public class SceneEventLog : MonoBehaviour
    {
        [SerializeField, Tooltip("")]
        private float m_VisibleDuration = 5f;
        [SerializeField, Tooltip("")]
        private float m_FadeDuration = 1f;

        private Text m_Output = null;
        private CanvasGroup m_CanvasGroup = null;
        private float m_Timer = 0f;

        private float alpha
        {
            get { return m_CanvasGroup.alpha; }
            set
            {
                if (!Mathf.Approximately(m_CanvasGroup.alpha, value))
                    m_CanvasGroup.alpha = value;
            }
        }

        private void OnValidate()
        {
            if (m_FadeDuration < 0.1f)
                m_FadeDuration = 0.1f;
        }

        private void Start()
        {
            m_Output = GetComponentInChildren<Text>();
            m_CanvasGroup = m_Output.GetComponent<CanvasGroup>();

            alpha = 0f;

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnSceneLoaded(Scene s, LoadSceneMode mode)
        {
            m_Output.text = string.Format("Loaded: {0}", s.name);
            m_Timer = m_VisibleDuration + m_FadeDuration;
            alpha = 1f;
        }

        private void OnSceneUnloaded(Scene s)
        {
            m_Output.text = string.Format("Unloaded: {0}", s.name);
            m_Timer = m_VisibleDuration + m_FadeDuration;
            alpha = 1f;
        }

        private void Update()
        {
            m_Timer -= Time.unscaledDeltaTime;
            alpha = Mathf.Clamp01(m_Timer / m_FadeDuration);
        }
    }
}