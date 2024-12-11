using NeoSaveGames;
using NeoSaveGames.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS.ModularFirearms
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-transformtargetingsystem.html")]
    public class TransformTargetingSystem : MonoBehaviour, ITargetingSystem, INeoSerializableComponent
    {
        [SerializeField, Tooltip("The amount of time (seconds) the target will be remembered for when there is nothing tracking it.")]
        private float m_UntrackedMemory = 100f;

        private List<ITargetTracker> m_ActiveTrackers = new List<ITargetTracker>();
        private Transform m_TargetTransform = null;
        private Vector3 m_TargetOffset = Vector3.zero;
        private WaitForFixedUpdate m_WaitForFixedUpdate = new WaitForFixedUpdate();
        private float m_LifetimeRemaining = 0f;
        private Coroutine m_TimeoutCoroutine = null;

        public void SetTargetTransform(Transform target)
        {
            SetTargetTransform(target, Vector3.zero);
        }

        public void SetTargetTransform(Transform target, Vector3 relativeOffset)
        {
            m_TargetTransform = target;
            m_TargetOffset = relativeOffset;

            // Apply to active trackers
            for (int i = 0; i < m_ActiveTrackers.Count; ++i)
                m_ActiveTrackers[i].SetTargetTransform(m_TargetTransform, m_TargetOffset, false);

            // Reset timeout
            m_LifetimeRemaining = m_UntrackedMemory;
            if (m_TimeoutCoroutine == null)
                m_TimeoutCoroutine = StartCoroutine(TimeoutCoroutine());
        }

        public void ClearTargetTransform()
        {
            m_TargetTransform = null;
            m_TargetOffset = Vector3.zero;

            // Apply to active trackers
            for (int i = 0; i < m_ActiveTrackers.Count; ++i)
                m_ActiveTrackers[i].ClearTarget();

            // Stop timeout
            m_LifetimeRemaining = 0f;
            if (m_TimeoutCoroutine != null)
            {
                StopCoroutine(m_TimeoutCoroutine);
                m_TimeoutCoroutine = null;
            }
        }

        public void RegisterTracker(ITargetTracker tracker)
        {
            // Record tracker
            m_ActiveTrackers.Add(tracker);
            tracker.onDestroyed += OnTrackerDestroyed;

            // Set target if one exists
            if (m_TargetTransform != null)
                tracker.SetTargetTransform(m_TargetTransform, m_TargetOffset, false);
            else
                tracker.ClearTarget();

            // Reset timeout
            m_LifetimeRemaining = m_UntrackedMemory;
            if (m_TimeoutCoroutine == null)
                m_TimeoutCoroutine = StartCoroutine(TimeoutCoroutine());
        }

        private void OnTrackerDestroyed(ITargetTracker tracker)
        {
            tracker.onDestroyed -= OnTrackerDestroyed;
            m_ActiveTrackers.Remove(tracker);
        }

        IEnumerator TimeoutCoroutine()
        {
            // Countdown lifetime
            while (m_LifetimeRemaining > 0f)
            {
                yield return m_WaitForFixedUpdate;

                // Decrement timer
                if (m_ActiveTrackers.Count == 0)
                    m_LifetimeRemaining -= Time.deltaTime;

                // Check if null or inactive
                if (m_TargetTransform == null || !m_TargetTransform.gameObject.activeInHierarchy)
                    break;
            }

            // reset to zero
            m_LifetimeRemaining = 0f;

            // Clear targets
            for (int i = 0; i < m_ActiveTrackers.Count; ++i)
                m_ActiveTrackers[i].ClearTarget();
            m_TargetTransform = null;
            m_TargetOffset = Vector3.zero;

            // Release coroutine
            m_TimeoutCoroutine = null;
        }

        #region INeoSerializableComponent IMPLEMENTATION

        private static readonly NeoSerializationKey k_ActiveTrackersKey = new NeoSerializationKey("activeTrackers");
        private static readonly NeoSerializationKey k_LifetimeKey = new NeoSerializationKey("lifetime");

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            if (m_ActiveTrackers.Count > 0)
            {
                // Push a context (workaround for no read/write obj reference arrays)
                writer.PushContext(SerializationContext.ObjectNeoSerialized, k_ActiveTrackersKey);

                // Write object references
                for (int i = 0; i < m_ActiveTrackers.Count; ++i)
                    writer.WriteComponentReference(i, m_ActiveTrackers[i], nsgo);

                // Pop context
                writer.PopContext(SerializationContext.ObjectNeoSerialized);
            }

            // Write memory lifetime
            writer.WriteValue(k_LifetimeKey, m_LifetimeRemaining);
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            // Push a context (workaround for no read/write obj reference arrays)
            // If context isn't found, the array was empty
            if (reader.PushContext(SerializationContext.ObjectNeoSerialized, k_ActiveTrackersKey))
            {
                // Read object references
                for (int i = 0; true; ++i)
                {
                    ITargetTracker tracker;
                    if (reader.TryReadComponentReference(i, out tracker, null))
                        m_ActiveTrackers.Add(tracker);
                    else
                        break;
                }

                reader.PopContext(SerializationContext.ObjectNeoSerialized, k_ActiveTrackersKey);
            }

            // Read memory lifetime and start timeout if required
            if (reader.TryReadValue(k_LifetimeKey, out m_LifetimeRemaining, m_LifetimeRemaining))
            {
                if (m_LifetimeRemaining > 0f && m_TimeoutCoroutine == null)
                    m_TimeoutCoroutine = StartCoroutine(TimeoutCoroutine());
            }
        }

        #endregion
    }
}