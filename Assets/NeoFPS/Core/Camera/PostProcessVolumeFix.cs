using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/graphicsref-mb-postprocesslayerfix.html")]
    public class PostProcessVolumeFix : MonoBehaviour
    {
#if UNITY_POST_PROCESSING_STACK_V2

        [SerializeField]
        private bool m_IsGlobal = false;
        [SerializeField, Range(0f, 1f)]
        private float m_Weight = 1f;
        [SerializeField]
        private float m_Priority = 0;
        [SerializeField]
        private PostProcessProfile m_Profile = null;

        protected void Awake()
        {
            if (!CheckVolumeExists())
                AddPostProcessVolume();
            else
                RemoveComponent();
        }

        public bool CheckVolumeExists()
        {
            return GetComponent<PostProcessVolume>() != null;
        }

        public void AddPostProcessVolume()
        {
            // Add a post processing volume behaviour
            var ppv = gameObject.AddComponent<PostProcessVolume>();

            // Set the properties based on stored
            ppv.isGlobal = m_IsGlobal;
            ppv.weight = m_Weight;
            ppv.priority = m_Priority;
            ppv.sharedProfile = m_Profile;

            RemoveComponent();
        }

        public void CopySettingsFromExisting()
        {
            var ppv = gameObject.GetComponent<PostProcessVolume>();
            if (ppv != null)
            {
                m_IsGlobal = ppv.isGlobal;
                m_Weight = ppv.weight;
                m_Priority = ppv.priority;
                m_Profile = ppv.sharedProfile;
            }
        }

        void RemoveComponent()
        {
            // Destroy the fix behaviour
            if (Application.isPlaying)
                Destroy(this);
            else
                DestroyImmediate(this);
        }

#else
        protected void Awake()
        {
            Destroy(this);
        }
#endif
    }
}