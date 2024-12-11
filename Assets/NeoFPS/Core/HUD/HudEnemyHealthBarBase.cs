using UnityEngine;
using UnityEngine.UI;
using NeoFPS;
using System;

namespace NeoFPS.AI
{
    public abstract class HudEnemyHealthBarBase : MonoBehaviour
    {
        [SerializeField, Tooltip("The amount of time to display the health bar for when an enemy is hit")]
        private float m_DisplayTime = 5f;
        [SerializeField, Tooltip("The time it takes to fade from full visibility to invisible after the display time has elapsed")]
        private float m_FadeTime = 1f;

        private IDamageHandler m_CurrentHandler = null;
        public IHealthManager healthManager
        {
            get;
            private set;
        }

        private float m_Timer = 0f;

        protected virtual void OnEnable()
        {
            DamageEvents.onDamageHandlerHit += OnDamageHandlerHit;
        }

        protected virtual void OnDisable()
        {
            DamageEvents.onDamageHandlerHit -= OnDamageHandlerHit;
        }

        private void Start()
        {
            SetAlpha(0f);
        }

        protected abstract void SetDisplayName(string displayName);
        protected abstract void UpdateHealthbar();
        protected abstract void SetAlpha(float alpha);

        private void OnDamageHandlerHit(IDamageHandler handler, IDamageSource source, Vector3 hitPoint, DamageResult result, float damage)
        {
            if (source != null && source.controller != null && source.controller.isLocalPlayer)
            {
                // Get health manager
                bool isValid = true;
                if (m_CurrentHandler != handler)
                {
                    var health = handler.healthManager;
                    if (health != null)
                    {
                        m_CurrentHandler = handler;
                        healthManager = health;

                        var displayName = healthManager.GetComponent<IDisplayName>();
                        if (displayName != null)
                            SetDisplayName(displayName.displayName);
                        else
                            SetDisplayName(healthManager.gameObject.name);
                    }
                    else
                        isValid = false;
                }

                // Reset timer
                if (isValid)
                {
                    SetAlpha(1f);
                    m_Timer = m_FadeTime + m_DisplayTime;

                    UpdateHealthbar();
                }
            }
        }

        private void Update()
        {
            if (m_Timer > 0f)
            {
                m_Timer -= Time.unscaledDeltaTime;
                if (m_Timer <= 0f)
                {
                    m_Timer = 0f;
                    SetAlpha(0f);
                    m_CurrentHandler = null;
                    healthManager = null;
                }
                else
                {
                    // Set alpha
                    float alpha = m_Timer / m_FadeTime;
                    if (alpha < 1f) // Already reset to 1
                        SetAlpha(alpha);
                }

            }
        }
    }
}
