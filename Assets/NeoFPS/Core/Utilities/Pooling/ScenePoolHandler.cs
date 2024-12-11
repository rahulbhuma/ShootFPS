using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using NeoSaveGames.Serialization;
using NeoSaveGames;
using UnityEngine.Serialization;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/utilitiesref-mb-scenepoolinfo.html")]
	public class ScenePoolHandler : MonoBehaviour, INeoSerializableComponent
    {
        [SerializeField, Tooltip("The collections of pooled objects to set up at initialisation.")]
        private PooledObjectCollection[] m_Collections = { };

        [SerializeField, FormerlySerializedAs("m_StartingPools"), Tooltip("The pools to set up at initialisation.")]
        private PoolInfo[] m_ScenePools = new PoolInfo[0];

        private bool m_Initialised = false;
        private NeoSerializedGameObject m_Nsgo = null;
        private Dictionary<PooledObject, Pool> m_PoolDictionary = new Dictionary<PooledObject, Pool>();
        private List<DelayedReturn> m_DelayedReturns = new List<DelayedReturn>();
        private List<Pool> m_PoolList = new List<Pool>();
        private List<Pool> m_Resizing = new List<Pool>();

        public int numPools
        {
            get { return m_PoolDictionary.Count; }
        }

        private struct DelayedReturn
        {
            public PooledObject pooledObject;
            public float returnTime;
        }

        class Pool
        {
            public PooledObject prototype = null;
            public Transform poolTransform = null;
            public Transform activeTransform = null;
            public NeoSerializedGameObject poolNsgo = null;
            public NeoSerializedGameObject activeNsgo = null;
            public NeoSerializedGameObject prototypeNsgo = null;
            public int targetSize = 0;
            public int currentSize = 0;

            private int m_Counter = 1;
            // Note: With save system refactor, should remove this
            
            public void Grow()
            {
                int start = currentSize;
                int toAdd = Math.Min(targetSize - start, PoolManager.poolIncrement);
                if (prototypeNsgo != null)
                {
                    for (int i = 0; i < toAdd; ++i)
                    {
                        var obj = poolNsgo.InstantiatePrefab<PooledObject>(prototypeNsgo.prefabStrongID, m_Counter++);
                        obj.gameObject.SetActive(false);
                        obj.poolTransform = poolTransform;
                    }
                }
                else
                {
                    for (int i = 0; i < toAdd; ++i)
                    {
                        PooledObject obj = Instantiate(prototype, poolTransform);
                        obj.gameObject.SetActive(false);
                        //obj.transform.SetParent(poolTransform);
                        obj.poolTransform = poolTransform;
                    }
                }
                currentSize += toAdd;
            }

            public void GrowTarget(int target)
            {
                if (target > targetSize)
                    targetSize = target;
                Grow();
            }

            public void GrowBy(int amount)
            {
                if (amount > 0)
                    targetSize += amount;
                Grow();
            }

            public Pool(PooledObject proto, int total, int startCount, Transform pt, Transform at)
            {
                prototype = proto;
                poolTransform = pt;
                //poolTransform.gameObject.SetActive(false);
                activeTransform = at;
                poolNsgo = null;
                activeNsgo = null;
                prototypeNsgo = null;                
                targetSize = total;
                for (int i = 0; i < startCount; ++i)
                {
                    PooledObject obj = Instantiate(prototype);
                    obj.gameObject.SetActive(false);
                    obj.transform.SetParent(poolTransform);
                    obj.poolTransform = poolTransform;
                }
                currentSize = startCount;
            }

            public Pool(PooledObject proto, NeoSerializedGameObject protoNsgo, NeoSerializedGameObject pNsgo, NeoSerializedGameObject aNsgo)
            {
                prototype = proto;
                prototypeNsgo = protoNsgo;
                poolTransform = pNsgo.transform;
                //poolTransform.gameObject.SetActive(false);
                activeTransform = aNsgo.transform;
                poolNsgo = pNsgo;
                activeNsgo = aNsgo;

                // Build hash map of active objects
                int highest = 0;
                HashSet<int> activeObjects = new HashSet<int>();
                for (int i = 0; i < activeTransform.childCount; ++i)
                {
                    int key = activeTransform.GetChild(i).GetComponent<NeoSerializedGameObject>().serializationKey;
                    if (key > highest)
                        highest = key;
                    activeObjects.Add(key);
                }

                // Fill out inactive to count, skipping active
                // Start() will fill out remaining capacity
                for (int i = 1; i < highest; ++i)
                {
                    if (!activeObjects.Contains(i))
                    {
                        var obj = poolNsgo.InstantiatePrefab<PooledObject>(prototypeNsgo.prefabStrongID, i);
                        obj.gameObject.SetActive(false);
                        obj.poolTransform = poolTransform;
                    }
                }

                m_Counter = highest + 1;
                targetSize = highest;

                currentSize = poolTransform.childCount + activeTransform.childCount;
            }

            public Pool(PooledObject proto, int total, int startCount, NeoSerializedGameObject pNsgo, NeoSerializedGameObject aNsgo)
            {
                prototype = proto;
                poolTransform = pNsgo.transform;
                //poolTransform.gameObject.SetActive(false);
                activeTransform = aNsgo.transform;
                targetSize = total;

                prototypeNsgo = proto.GetComponent<NeoSerializedGameObject>();
                if (prototypeNsgo != null)
                {
                    poolNsgo = pNsgo;
                    activeNsgo = aNsgo;
                    
                    for (int i = 0; i < startCount; ++i)
                    {
                        var obj = poolNsgo.InstantiatePrefab<PooledObject>(prototypeNsgo.prefabStrongID, m_Counter++);
                        if (obj != null)
                        {
                            obj.gameObject.SetActive(false);
                            obj.poolTransform = poolTransform;
                        }
                    }
                }
                else
                {
                    poolNsgo = null;
                    activeNsgo = null;

                    for (int i = 0; i < startCount; ++i)
                    {
                        PooledObject obj = Instantiate(prototype);
                        obj.gameObject.SetActive(false);
                        obj.transform.SetParent(poolTransform);
                        obj.poolTransform = poolTransform;
                    }
                }

                currentSize = startCount;
            }

            public void DestroyPool()
            {
                Destroy(poolTransform.gameObject);
                poolTransform = null;
                Destroy(activeTransform.gameObject);
                activeTransform = null;
                poolNsgo = null;
                activeNsgo = null;
                prototype = null;
                prototypeNsgo = null;
            }

            public T GetObject<T>(bool activate) where T : class
            {
                if (poolTransform == null || activeTransform == null)
                    return default(T);

                if (poolTransform.childCount > 0)
                {
                    Transform t = poolTransform.GetChild(poolTransform.childCount - 1);
                    T result = t.GetComponent<T>();

                    if (result != null)
                    {
                        if (prototypeNsgo != null)
                        {
                            var nsgo = t.GetComponent<NeoSerializedGameObject>();
                            nsgo.SetParent(activeNsgo);
                        }
                        else
                        {
                            t.SetParent(activeTransform);
                        }
                    }
                    return result;
                }
                else
                {
                    if (currentSize < targetSize)
                    {
                        Grow();
                        return GetObject<T>(activate);
                    }
                    else
                    {
                        switch (prototype.onOverflow)
                        {
                            case PooledObject.OnOverflow.Grow:
                                {
                                    GrowBy(PoolManager.poolIncrement);
                                    return GetObject<T>(activate);
                                }
                            case PooledObject.OnOverflow.Recycle:
                                {
                                    if (activeTransform.childCount > 0)
                                    {
                                        Transform t = activeTransform.GetChild(0);
                                        T result = t.GetComponent<T>();

                                        if (result != null)
                                        {
                                            t.gameObject.SetActive(false);
                                            t.SetAsLastSibling();
                                        }

                                        return result;
                                    }
                                    else
                                    {
                                        Debug.LogError("Pooling system attempting to recycle an active pooled object, but none found. This shouldn't be possible");
                                        Debug.LogErrorFormat("Current Size = {0}, Target Size = {1}", currentSize, targetSize);
                                        GrowTarget(PoolManager.defaultPoolSize);
                                        return GetObject<T>(activate);
                                    }
                                }
                            default:
                                return null;
                        }
                    }
                }
            }
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            for (int i = 0; i < m_ScenePools.Length; ++i)
            {
                if (m_ScenePools[i].count < 1)
                    m_ScenePools[i].count = 1;
            }
        }
#endif

        void Awake()
        {
            m_Nsgo = GetComponent<NeoSerializedGameObject>();
            PoolManager.SetCurrentScenePoolInfo(this);
        }

        IEnumerator Start ()
        {
            yield return null;
            Initialise();
        }

        void Update()
        {
            int resizing = m_Resizing.Count - 1;
            if (resizing >= 0)
            {
                var pool = m_Resizing[resizing];
                pool.Grow();
                if (pool.currentSize == pool.targetSize)
                    m_Resizing.RemoveAt(resizing);
            }

            CheckDelayedReturns();
        }
        
        public void Initialise()
        {
            if (!m_Initialised)
            {

                // Create the starting pools
                CreatePools(m_ScenePools);
                for (int i = 0; i < m_Collections.Length; ++i)
                {
                    if (m_Collections[i] != null)
                        CreatePools(m_Collections[i].pooledObjects);
                }

                m_Initialised = true;
            }
        }

        public void CreatePools(PoolInfo[] pools)
        {
            for (int i = 0; i < pools.Length; ++i)
            {
                // Get the prototype
                PooledObject prototype = pools[i].prototype;
                if (prototype != null)
                    CreatePool(prototype, pools[i].count);
            }
        }

        public void CreatePool(PooledObject prototype, int count)
        {
            CreatePool(prototype, count, 0);
        }

        public void CreatePool (PooledObject prototype, int total, int startCount)
		{
            // Check invalid pool size
            if (total < 1)
                total = 1;
            if (startCount < 0)
                startCount = 0;

            if (m_PoolDictionary.ContainsKey (prototype))
			{
                var pool = m_PoolDictionary[prototype];
                pool.GrowTarget (total);
                if (pool.currentSize < pool.targetSize)
                    m_Resizing.Add(pool);
			}
			else
            {
                Pool pool;

                var prototypeNsgo = prototype.GetComponent<NeoSerializedGameObject>();
                if (m_Nsgo == null || prototypeNsgo == null || !NeoSerializedObjectFactory.IsPrefabRegistered(prototypeNsgo.prefabStrongID))
                {
                    // Create heirachy
                    Transform poolRoot = new GameObject(prototype.name).transform;
                    poolRoot.SetParent(transform);
                    Transform poolTransform = new GameObject("Pool").transform;
                    poolTransform.SetParent(poolRoot);
                    Transform activeTransform = new GameObject("Active").transform;
                    activeTransform.SetParent(poolRoot);

                    // Create the pool
                    pool = new Pool(prototype, total, startCount, poolTransform, activeTransform);
                }
                else
                {
                    // Create heirachy
                    var nsgo = m_Nsgo;
                    NeoSerializedGameObject poolRoot = nsgo.serializedChildren.CreateChildObject(prototype.name, prototypeNsgo.prefabStrongID);
                    NeoSerializedGameObject activeNsgo = poolRoot.serializedChildren.CreateChildObject("Active", 1);
                    NeoSerializedGameObject poolNsgo = poolRoot.serializedChildren.CreateChildObject("Pool", -1);
                    poolRoot.saveName = true;
                    activeNsgo.saveName = true;
                    poolNsgo.saveName = true;

                    // Set pool object not to serialize children
                    poolNsgo.filterChildObjects = NeoSerializationFilter.Include;

                    // Create the pool
                    pool = new Pool(prototype, total, startCount, poolNsgo, activeNsgo);
                }

                // Add the pool to the handler
                m_PoolDictionary.Add(prototype, pool);
                m_PoolList.Add(pool);

                // Queue up for resizing if required
                if (pool.currentSize < pool.targetSize)
                    m_Resizing.Add(pool);
            }
		}

		public void ReturnObjectToPool (PooledObject obj)
		{
            Pool pool;
			if (m_PoolDictionary.TryGetValue (obj, out pool))
			{
                var nsgo = obj.GetComponent<NeoSerializedGameObject>();
                if (nsgo != null)
                {
                    nsgo.gameObject.SetActive(false);
                    nsgo.SetParent(pool.poolNsgo);
                }
                else
                {
                    obj.gameObject.SetActive(false);
                    obj.transform.SetParent(pool.poolTransform);
                }
			}
			else
				Destroy (obj.gameObject);
        }

        public void ReturnObjectDelayed(PooledObject obj, float delay)
        {
            m_DelayedReturns.Add(new DelayedReturn { pooledObject = obj, returnTime = Time.timeSinceLevelLoad + delay });
        }

        void CheckDelayedReturns()
        {
            float currentTime = Time.timeSinceLevelLoad;
            for (int i = m_DelayedReturns.Count - 1; i >= 0; --i)
            {
                bool remove = false;

                // Check if already back in pool
                if (m_DelayedReturns[i].pooledObject == null || m_DelayedReturns[i].pooledObject.isPooled)
                    remove = true;
                else
                {
                    // Check if time has been reached
                    if (m_DelayedReturns[i].returnTime < currentTime)
                    {
                        m_DelayedReturns[i].pooledObject.ReturnToPool();
                        remove = true;
                    }
                }

                // Shuffle and remove
                if (remove)
                {
                    int last = m_DelayedReturns.Count - 1;
                    m_DelayedReturns[i] = m_DelayedReturns[last];
                    m_DelayedReturns.RemoveAt(last);
                }
            }
        }

        public T GetPooledObject<T> (PooledObject prototype, bool activate = true) where T : class
        {
            Pool pool;
            bool created = false;

            // Get or create the pool
			if (!m_PoolDictionary.TryGetValue (prototype, out pool))
            {
                CreatePool(prototype, PoolManager.defaultPoolSize);
                pool = m_PoolDictionary[prototype];
                created = true;
            }

            // Get and set-up pooled object
			T result = pool.GetObject<T> (activate);
            var comp = result as Component;
			if (comp != null)
			{
				Transform t = comp.transform;
				t.position = Vector3.zero;
				t.rotation = Quaternion.identity;
                t.localScale = Vector3.one;
                if (activate)
                    comp.gameObject.SetActive(true);
            }

            // Queue up for resizing if required
            if (!created && pool.currentSize < pool.targetSize)
                m_Resizing.Add(pool);

            return result;
		}

		public T GetPooledObject<T> (PooledObject prototype, Vector3 position, Quaternion rotation, bool activate = true) where T : class
        {
            Pool pool;
            bool created = false;

            // Get or create the pool
            if (!m_PoolDictionary.TryGetValue(prototype, out pool))
            {
                CreatePool(prototype, PoolManager.defaultPoolSize);
                pool = m_PoolDictionary[prototype];
                created = true;
            }

            // Get and set-up pooled object
            T result = pool.GetObject<T> (activate);
            var comp = result as Component;
            if (comp != null)
            {
				Transform t = comp.transform;
				t.position = position;
				t.rotation = rotation;
                t.localScale = Vector3.one;
                if (activate)
                    comp.gameObject.SetActive(true);
            }

            // Queue up for resizing if required
            if (!created && pool.currentSize < pool.targetSize)
                m_Resizing.Add(pool);

            return result;
		}

        public T GetPooledObject<T>(PooledObject prototype, Vector3 position, Quaternion rotation, Vector3 scale, bool activate = true) where T : class
        {
            Pool pool;
            bool created = false;

            // Get or create the pool
            if (!m_PoolDictionary.TryGetValue(prototype, out pool))
            {
                CreatePool(prototype, PoolManager.defaultPoolSize);
                pool = m_PoolDictionary[prototype];
                created = true;
            }

            // Get and set-up pooled object
            T result = pool.GetObject<T>(activate);
            var comp = result as Component;
            if (comp != null)
            {
                Transform t = comp.transform;
                t.position = position;
                t.rotation = rotation;
                t.localScale = scale;
                if (activate)
                    comp.gameObject.SetActive(true);
            }

            // Queue up for resizing if required
            if (!created && pool.currentSize < pool.targetSize)
                m_Resizing.Add(pool);

            return result;
        }

        private static readonly NeoSerializationKey k_DelayedCountKey = new NeoSerializationKey("delayed");
        private static readonly NeoSerializationKey k_DelayedObjectKey = new NeoSerializationKey("object");
        private static readonly NeoSerializationKey k_DelayedTimeKey = new NeoSerializationKey("time");

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            // Write delayed returns
            if (m_DelayedReturns.Count > 0)
            {
                writer.WriteValue(k_DelayedCountKey, m_DelayedReturns.Count);
                for (int i = 0; i < m_DelayedReturns.Count; ++i)
                {
                    writer.PushContext(SerializationContext.ObjectNeoFormatted, i);
                    writer.WriteComponentReference(k_DelayedObjectKey, m_DelayedReturns[i].pooledObject, nsgo);
                    writer.WriteValue(k_DelayedTimeKey, m_DelayedReturns[i].returnTime - Time.timeSinceLevelLoad);
                    writer.PopContext(SerializationContext.ObjectNeoFormatted);
                }
            }
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            m_Nsgo = nsgo;

            var childObjects = GetComponentsInChildren<NeoSerializedGameObject>();
            for (int i = 0; i < childObjects.Length; ++i)
            {
                var prefab = NeoSerializedObjectFactory.GetPrefab(childObjects[i].serializationKey);
                if (prefab != null)
                {
                    var pooledObject = prefab.GetComponent<PooledObject>();
                    if (pooledObject != null)
                    {
                        var activeNsgo = childObjects[i].serializedChildren.GetChildObject(1);
                        var inactiveNsgo = childObjects[i].serializedChildren.GetChildObject(-1);
                        if (activeNsgo != null && inactiveNsgo != null)
                        {
                            inactiveNsgo.filterChildObjects = NeoSerializationFilter.Include;
                            var pool = new Pool(pooledObject, prefab, inactiveNsgo, activeNsgo);
                            m_PoolDictionary.Add(pooledObject, pool);
                        }
                    }
                }
            }

            // Read delayed returns
            if (reader.TryReadValue(k_DelayedCountKey, out int delayedCount, 0) && delayedCount > 0)
            {
                for(int i = 0; i < delayedCount; ++i)
                {
                    reader.TryReadComponentReference(k_DelayedObjectKey, out PooledObject pooledObject, nsgo);
                    reader.TryReadValue(k_DelayedTimeKey, out float delay, 0f);

                    if (pooledObject != null)
                        m_DelayedReturns.Add(new DelayedReturn { pooledObject = pooledObject, returnTime = Time.timeSinceLevelLoad + delay });
                    else
                        Debug.LogWarning("Failed to set up delay for pooled object return. Pooled object reference is null");
                }
            }
        }
    }
}