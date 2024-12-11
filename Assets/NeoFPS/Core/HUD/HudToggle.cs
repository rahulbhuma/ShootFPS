using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/hudref-mb-hudtoggle.html")]
    public class HudToggle : MonoBehaviour
    {
        [SerializeField, Tooltip("The keycode to use when toggling the HUD")]
        private KeyCode m_ToggleKey = KeyCode.Quote;

        [SerializeField, Tooltip("The gameobject containing the HUD UI. Must be a child of this object")]
        private GameObject m_HudObject = null;

        private static bool s_HudVisible = true;

        protected void OnValidate()
        {
            if (m_HudObject != null && (!m_HudObject.transform.IsChildOf(transform) || m_HudObject.transform == transform))
            {
                Debug.Log("Hud Object should be a child of the Hud Toggle");
                m_HudObject = null;
            }
        }

        protected void Start()
        {
            if (m_HudObject != null)
                m_HudObject.SetActive(s_HudVisible);

#if !ENABLE_LEGACY_INPUT_MANAGER
            Debug.LogError("Old input manager component (HudToggle) is active and should be removed. Object: " + gameObject.name, gameObject);
#endif
        }

        protected void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(m_ToggleKey))
            {
                s_HudVisible = !s_HudVisible;
                if (m_HudObject != null)
                    m_HudObject.SetActive(s_HudVisible);
            }
#endif
        }
    }
}