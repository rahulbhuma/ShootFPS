using NeoSaveGames;
using NeoSaveGames.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/neofpsref-mb-originshift.html")]
    public class OriginShift : MonoBehaviour, INeoSerializableComponent
    {
        [SerializeField, Tooltip("The distance from 0 on each axis before the origin shift system repositions object back towards the center.")]
        private float m_Threshold = 250f;

        [SerializeField, Tooltip("Is the threshold only applied on the xz plane, or all axes.")]
        private bool m_2D = false;

        [SerializeField, Tooltip("The transform to track the position of. This can be set at runtime via the API (such as when a character is spawned.")]
        private Transform m_Focus = null;

        public static OriginShift system
        {
            get;
            private set;
        }

        public static event UnityAction<OriginShift> onOriginShiftSystemChanged;

        private HashSet<IOriginShiftSubscriber> m_Subscribers = new HashSet<IOriginShiftSubscriber>();
        private Vector3 m_CurrentOffset = Vector3.zero;
        private Coroutine m_DeferredCheckRoutine = null;

        public enum SubscriptionPeriod
        {
            ObjectLifecycle,
            EnabledOnly
        }

        public static bool originShiftActive
        {
            get { return system != null && system.m_Focus != null; }
        }

        public float threshold
        {
            get { return m_Threshold; }
            set
            {
                m_Threshold = value;

                if (m_Focus != null)
                    CheckFocusPosition();
            }
        }

        public Transform currentFocus
        {
            get { return m_Focus; }
            set
            {
                m_Focus = value;

                if (m_Focus != null && m_DeferredCheckRoutine == null)
                    m_DeferredCheckRoutine = StartCoroutine(CheckFocusPositionDeferred());
            }
        }

        public Vector3 currentOffset
        {
            get { return m_CurrentOffset; }
        }

        public void AddSubscriber(IOriginShiftSubscriber subscriber)
        {
            m_Subscribers.Add(subscriber);
        }

        public void RemoveSubscriber(IOriginShiftSubscriber subscriber)
        {
            m_Subscribers.Remove(subscriber);
        }

        public static void SetFocus (Transform focus)
        {
            if (system != null)
                system.currentFocus = focus;
        }

        public static void SetThreshold(float threshold)
        {
            if (system != null)
                system.threshold = threshold;
        }

        void ApplyOffset(Vector3 offset)
        {
            // Disable auto sync transforms temporarily
            bool autoSync = Physics.autoSyncTransforms;
            Physics.autoSyncTransforms = false;

            // Add the offset
            m_CurrentOffset += offset;

            // Apply to subscribers
            foreach(var sub in m_Subscribers)
                sub.ApplyOffset(offset);

            // Sync transforms
            Physics.SyncTransforms();

            // Re-apply auto-sync setting
            Physics.autoSyncTransforms = autoSync;
        }

        private void Awake()
        {
            if (system != null)
            {
                Debug.LogError("Attempting to use multiple OriginShift components at the same time. You should have either 1 or 0 at any time.");
                Destroy(gameObject);
            }
            else
            {
                system = this;
                onOriginShiftSystemChanged?.Invoke(this);
            }
        }

        private void Start()
        {
            if (m_Focus != null && system == this && m_DeferredCheckRoutine == null)
                m_DeferredCheckRoutine = StartCoroutine(CheckFocusPositionDeferred());
        }

        private void OnDestroy()
        {
            if (system == this)
            {
                system = null;
                onOriginShiftSystemChanged?.Invoke(null);
            }
        }

        private void FixedUpdate()
        {
            if (m_Focus != null && m_DeferredCheckRoutine == null)
                CheckFocusPosition();
        }

        IEnumerator CheckFocusPositionDeferred()
        {
            yield return null;
            yield return new WaitForFixedUpdate();

            if (m_Focus != null)
                CheckFocusPosition();

            m_DeferredCheckRoutine = null;
        }

        void CheckFocusPosition()
        {
            // Get the focus position
            Vector3 pos = m_Focus.position;

            // Scale down to threshold increments
            pos *= 1f / threshold;

            // Get number of steps on each axis (conversion to int truncates denominator)
            Vector3Int steps = new Vector3Int((int)pos.x, m_2D ? 0 : (int)pos.y, (int)pos.z);

            // Apply offset if non-zero
            if (steps != Vector3Int.zero)
            {
                Vector3 offset = steps;
                offset *= -threshold;
                ApplyOffset(offset);
            }
        }

        private static readonly NeoSerializationKey k_OffsetKey = new NeoSerializationKey("offset");

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValue(k_OffsetKey, m_CurrentOffset);
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            reader.TryReadValue(k_OffsetKey, out m_CurrentOffset, m_CurrentOffset);
        }
    }
}
