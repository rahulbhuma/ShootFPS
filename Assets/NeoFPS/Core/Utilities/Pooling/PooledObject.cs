using NeoSaveGames.Serialization;
using System;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/utilitiesref-mb-pooledobject.html")]
	public class PooledObject : MonoBehaviour
	{
        [SerializeField, Tooltip("What should happen if you request an object from the pool when all of its items are in use.")]
        private OnOverflow m_OnOverflow = OnOverflow.Recycle;

        private Transform m_PoolTransform = null;
        private NeoSerializedGameObject m_LocalNsgo = null;
        private NeoSerializedGameObject m_PoolNsgo = null;

        public enum OnOverflow
        {
            Grow,
            Recycle,
            ReturnNull
        }

        public OnOverflow onOverflow
        {
            get { return m_OnOverflow; }
        }

        public bool isPooled
        {
            get { return transform.parent == m_PoolTransform && m_PoolTransform != null; }
        }

        public Transform poolTransform
		{
			get { return m_PoolTransform; }
			set
            {
                if (m_PoolTransform == null)
                {
                    m_PoolTransform = value;
                    m_PoolNsgo = value.GetComponent<NeoSerializedGameObject>();
                }
                else
                    Debug.LogError("Cannot change pool transform for object once it is already set.", gameObject);
            }
		}

        protected void Awake ()
		{
            m_LocalNsgo = GetComponent<NeoSerializedGameObject>();
        }

		public void ReturnToPool ()
		{
			if (poolTransform == null)
				Destroy (gameObject);
			else
			{
                PreReturnToPool();

                gameObject.SetActive (false);
                if (m_LocalNsgo != null && m_PoolNsgo != null)
                    m_LocalNsgo.SetParent(m_PoolNsgo);
                else
                    transform.SetParent(poolTransform);
			}
        }

        public void ReturnToPool(float delay)
        {
            if (delay <= 0.001f)
                ReturnToPool();
            else
                PoolManager.ReturnObjectDelayed(this, delay);
        }

        protected virtual void PreReturnToPool()
        {
        }
    }
}

