using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [RequireComponent(typeof(MeshRenderer))]
    public class MaterialGlowEffect : MonoBehaviour
    {
        [SerializeField, Tooltip("The index of the glow material in the attached mesh renderer")]
        private int m_MaterialIndex = 0;
        [SerializeField, Tooltip("The glow amount on start")]
        private float m_StartingGlow = 0f;

        MeshRenderer m_Renderer = null;
        MaterialPropertyBlock m_PropertyBlock = null;
        private int m_NameID = -1;
        private float m_Glow = 0f;

        protected void Awake()
        {
            Initialise(true);
        }

        public float glow
        {
            get { return m_Glow; }
            set
            {
                m_Glow = Mathf.Clamp01(value);
                m_PropertyBlock.SetFloat(m_NameID, m_Glow);
                m_Renderer.SetPropertyBlock(m_PropertyBlock, m_MaterialIndex);
            }
        }

        void Initialise(bool setStart)
        {
            if (m_Renderer == null)
            {
                m_Renderer = GetComponent<MeshRenderer>();
                m_PropertyBlock = new MaterialPropertyBlock();
                m_NameID = Shader.PropertyToID("_Glow");

                // Set the starting glow
                if (setStart)
                    glow = m_StartingGlow;
            }
        }
    }
}