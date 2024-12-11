using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/utilitiesref-mb-temporarypooledobject.html")]
    public class TemporaryPooledObject : PooledObject
    {
        [SerializeField, Tooltip("The duration the object will stay active before returning to the pool")]
        private float m_Lifetime = 5f;

        private float m_Elapsed = 0f;

        protected void OnValidate()
        {
            if (m_Lifetime < 0f)
                m_Lifetime = 0f;
        }

        protected void OnEnable()
        {
            m_Elapsed = 0f;
        }

        protected void Update()
        {
            m_Elapsed += Time.deltaTime;
            if (m_Elapsed > m_Lifetime)
                ReturnToPool();
        }
    }
}
