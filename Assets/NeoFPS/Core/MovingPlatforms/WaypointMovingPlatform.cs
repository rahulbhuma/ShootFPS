using System;
using System.Collections.Generic;
using NeoSaveGames;
using NeoSaveGames.Serialization;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mb-waypointmovingplatform.html")]
    public class WaypointMovingPlatform : BaseMovingPlatform
    {
        [SerializeField]
        private Waypoint[] m_Waypoints = new Waypoint[0];

        [SerializeField]
        private float[] m_JourneyTimes = new float[0];

        [SerializeField, Tooltip("The waypoint the platform starts at (will be repositioned on start).")]
        private int m_StartingWaypoint = 0;

        [SerializeField, Tooltip("An animation curve to apply easing to movement between waypoints.")]
        private AnimationCurve m_SpeedCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 0f, 0f));

        [SerializeField, Tooltip("The delay between waypoints when moving through a sequence. The platform will stop at a waypoint for this duration.")]
        private float m_Delay = 0f;

        [SerializeField, Tooltip("If the waypoints are circular then there is a direct route from the first to last waypoints without going through the others.")]
        private bool m_Circular = true;

        [SerializeField, Tooltip("Defines how the platform behaves when setting the target waypoint while already moving. If interruptable, the platform will immediately switch to the new target waypoint, reversing direction if necessary. If not interruptable, then the platform will move to the next waypoint and then change directions if required.")]
        private bool m_Interruptable = false;

        [SerializeField, Tooltip("What to do on start. If the waypoints are not circular then looping will ping-pong from the first to last waypoints via the intermediates.")]
        private StartingBehaviour m_OnStart = StartingBehaviour.Nothing;

        [SerializeField, Tooltip("An event fired whenever the platform starts moving.")]
        private UnityEvent m_OnStartMoving = null;

        [SerializeField, Tooltip("An event fired whenever the platform starts moving.")]
        private UnityEvent m_OnStopMoving = null;

        [SerializeField, Tooltip("An event fired once the platform has reached its end destination.")]
        private UnityEvent m_OnDestinationReached = null;

        [Serializable]
        public struct Waypoint
        {
            public Vector3 position;
            public Vector3 rotation;
        }

        private struct MoveSegment
        {
            public int index;
            public float duration;
        }

        public enum StartingBehaviour
        {
            Nothing,
            LoopForwards,
            LoopBackwards
        }

        public event Action<int> onReachedWaypoint;

        private int m_SourceIndex = 0;
        private float m_Progress = 0f;
        private float m_Timeout = 0f;
        private float m_DirectionalMultiplier = 1f;
        private MoveSegment[] m_MoveSegments = null;
        private int m_HeadSegment = 0;
        private int m_SegmentCount = 0;
        private int m_LoopDirection = 0;
        private Vector3 m_OriginShift = Vector3.zero;

        public Waypoint[] waypoints
        {
            get { return m_Waypoints; }
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            // Check waypoints have been initialised
            if (m_Waypoints == null || m_Waypoints.Length == 0)
            {
                m_Waypoints = new Waypoint[1];
                m_Waypoints[0].position = transform.position;
                m_Waypoints[0].rotation = transform.rotation.eulerAngles;
            }

            // Clamp the starting waypoint
            m_StartingWaypoint = Mathf.Clamp(m_StartingWaypoint, 0, m_Waypoints.Length - 1);

            // Set correct number of journey times for the number of waypoints
            int numJourneys = m_Waypoints.Length;
            if (!m_Circular)
                --numJourneys;
            if (m_JourneyTimes.Length != numJourneys)
            {
                float[] replacement = new float[numJourneys];
                int i = 0;
                for (; i < numJourneys && i < m_JourneyTimes.Length; ++i)
                    replacement[i] = m_JourneyTimes[i];
                for (; i < numJourneys; ++i)
                    replacement[i] = 5f;
                m_JourneyTimes = replacement;
            }

            // Clamp travel times
            for (int i = 0; i < m_JourneyTimes.Length; ++i)
                m_JourneyTimes[i] = Mathf.Clamp(m_JourneyTimes[i], 1f, 60f);
        }
#endif

        void AppendSegment (int index, float duration)
        {
            // Get new index
            int i = m_HeadSegment + m_SegmentCount;
            if (i >= m_MoveSegments.Length)
                i -= m_MoveSegments.Length;

            // Assign properties
            m_MoveSegments[i].index = index;
            m_MoveSegments[i].duration = duration;

            ++m_SegmentCount;
        }

        void PopHeadSegment ()
        {
            // Get source properties
            m_SourceIndex = m_MoveSegments[m_HeadSegment].index;

            ++m_HeadSegment;
            if (m_HeadSegment >= m_MoveSegments.Length)
                m_HeadSegment -= m_MoveSegments.Length;
            --m_SegmentCount;

            if (m_SegmentCount > 0)
                m_Timeout = m_Delay;
            else
                m_OnDestinationReached.Invoke();
        }

        protected override void Initialise()
        {
            base.Initialise();

            m_MoveSegments = new MoveSegment[m_Waypoints.Length];
            if (m_OnStart != StartingBehaviour.Nothing)
                LoopWaypoints(m_OnStart == StartingBehaviour.LoopForwards);
        }

        protected override Vector3 GetStartingPosition()
        {
            m_SourceIndex = Mathf.Clamp(m_StartingWaypoint, 0, m_Waypoints.Length - 1);
            return m_Waypoints[m_SourceIndex].position + m_OriginShift;
        }

        protected override Quaternion GetStartingRotation()
        {
            m_SourceIndex = Mathf.Clamp(m_StartingWaypoint, 0, m_Waypoints.Length - 1);
            return Quaternion.Euler(m_Waypoints[m_SourceIndex].rotation);
        }

        protected override Vector3 GetNextPosition()
        {
            // Handle direction change
            if (m_DirectionalMultiplier < 1f)
            {
                m_DirectionalMultiplier += Time.deltaTime;
                if (m_DirectionalMultiplier > 1f)
                    m_DirectionalMultiplier = 1f;
            }

            if (m_SegmentCount == 0)
                return fixedPosition;

            // Timeout if required
            if (m_Timeout > 0f)
            {
                m_Timeout -= Time.deltaTime;
                if (m_Timeout < 0f)
                {
                    m_Timeout = 0f;

                    if (m_MoveSegments.Length > 0)
                        m_OnStartMoving.Invoke();
                }
                else
                    return fixedPosition;
            }

            // Increment progress
            m_Progress += m_DirectionalMultiplier * Time.deltaTime / m_MoveSegments[m_HeadSegment].duration;
            if (m_Progress >= 1f)
            {
                m_Progress = 1f;
                return m_Waypoints[m_MoveSegments[m_HeadSegment].index].position + m_OriginShift;
            }

            return Vector3.LerpUnclamped(
                m_Waypoints[m_SourceIndex].position,
                m_Waypoints[m_MoveSegments[m_HeadSegment].index].position,
                m_SpeedCurve.Evaluate(m_Progress)
                ) + m_OriginShift;
        }

        protected override Quaternion GetNextRotation()
        {
            if (m_SegmentCount == 0)
                return fixedRotation;

            if (m_Timeout > 0f)
                return fixedRotation;

            if (m_Progress == 1f)
            {
                // Reset progress
                m_Progress = 0f;

                // Pop head segment & return position
                int index = m_MoveSegments[m_HeadSegment].index;
                PopHeadSegment();

                // React to reaching waypoint
                OnReachedWaypoint(index);

                return Quaternion.Euler(m_Waypoints[index].rotation);
            }

            // Slerp between source and destination
            return Quaternion.SlerpUnclamped(
                Quaternion.Euler(m_Waypoints[m_SourceIndex].rotation),
                Quaternion.Euler(m_Waypoints[m_MoveSegments[m_HeadSegment].index].rotation),
                m_SpeedCurve.Evaluate(m_Progress)
                );
        }

        protected virtual void OnReachedWaypoint(int wp)
        {
            // Loop if set
            if (m_LoopDirection != 0)
            {
                m_Timeout = m_Delay;
                GetNextLoopWaypoint();

                if (m_Delay > 0f)
                    m_OnStopMoving.Invoke();
            }

            // Fire waypoint reached event
            if (onReachedWaypoint != null)
                onReachedWaypoint(wp);
        }

        public void GoToStart()
        {
            GoToWaypoint(0, false);
        }

        public void GoToEnd()
        {
            GoToWaypoint(m_Waypoints.Length - 1, false);
        }

        public void GoToOppositeEnd()
        {
            // Get the current index (different if moving)
            int current = m_SourceIndex;
            if (m_SegmentCount > 0)
                current = m_MoveSegments[m_HeadSegment + m_SegmentCount - 1].index;

            if (current > (m_Waypoints.Length - 1) / 2f)
                GoToWaypoint(0, false);
            else
                GoToWaypoint(m_Waypoints.Length - 1, false);
        }

        public void GoToWaypoint(int index, bool direct)
        {
            // Check if valid
            if (index < 0 || index >= m_Waypoints.Length)
                return;
            
            int source = m_SourceIndex;

            // Stop looping
            m_LoopDirection = 0;

            // Prevent delay
            if (m_Timeout > 0f)
            {
                m_Timeout = 0f;
                // Clear pending moves
                m_SegmentCount = 0;

                // If source is target then complete
                if (source == index)
                    return;
            }
            else
            {
                // Trim all but current move if one is in progress
                if (m_SegmentCount > 0)
                {
                    // Source is current move (when complete)
                    m_SegmentCount = 1;
                    source = m_MoveSegments[m_HeadSegment].index;
                }
                else
                {
                    // If source is target then complete
                    if (source == index)
                        return;
                }
            }

            bool forwards;
            if (m_Circular)
            {
                int diff = index - m_SourceIndex;
                int halfLength = m_Waypoints.Length / 2;
                forwards = (diff > 0 && diff <= halfLength) || (diff < 0 && diff < -halfLength);
            }
            else
            {
                forwards = index > m_SourceIndex;
            }

            // Check if potentially interrupting the current move
            if (m_SegmentCount == 1 && m_Interruptable)
            {
                // Check old and new move directions
                bool interrupt;
                if (forwards)
                    interrupt = m_SourceIndex > m_MoveSegments[m_HeadSegment].index;
                else
                    interrupt = m_SourceIndex < m_MoveSegments[m_HeadSegment].index;

                // Interrupt (reverse current movement)
                if (interrupt)
                {
                    int oldSource = m_SourceIndex;
                    m_SourceIndex = m_MoveSegments[m_HeadSegment].index;
                    m_MoveSegments[m_HeadSegment].index = oldSource;
                    source = oldSource;
                    m_Progress = 1f - m_Progress;
                }

                // If source is target then complete
                if (source == index)
                    return;
            }


            int itr = source;
            float duration = 0f;
            if (forwards)
            {
                // Walk forwards through waypoints
                while (itr != index)
                {
                    // Record source waypoint
                    int original = itr;

                    // Iterate through waypoints
                    ++itr;
                    if (itr >= m_Waypoints.Length)
                        itr -= m_Waypoints.Length;

                    if (direct)
                    {
                        // Accumulate duration
                        duration += m_JourneyTimes[original];
                        // Set position at the end
                        if (itr == index)
                            AppendSegment(itr, duration);
                    }
                    else
                    {
                        if (original == m_JourneyTimes.Length)
                            AppendSegment(itr, m_JourneyTimes[0]);
                        else
                            AppendSegment(itr, m_JourneyTimes[original]);
                    }
                }
            }
            else
            {
                // Walk backwards through waypoints
                while (itr != index)
                {
                    // Iterate through waypoints
                    --itr;
                    if (itr < 0)
                        itr += m_Waypoints.Length;

                    if (direct)
                    {
                        // Accumulate duration
                        duration += m_JourneyTimes[itr];
                        // Set position at the end
                        if (itr == index)
                            AppendSegment(itr, duration);
                    }
                    else
                    {
                        if (itr == m_JourneyTimes.Length)
                            AppendSegment(itr, m_JourneyTimes[0]);
                        else
                            AppendSegment(itr, m_JourneyTimes[itr]);
                    }
                }
            }

            if (m_MoveSegments.Length > 0)
                m_OnStartMoving.Invoke();
        }

        public void GoToWaypoint(int index)
        {
            GoToWaypoint(index, false);
        }

        public void LoopWaypoints (bool forward)
        {
            if (m_Waypoints.Length == 1)
                return;

            m_LoopDirection = (forward) ? 1 : -1;

            // If waiting, stop timer and clear moves
            if (m_Timeout > 0f)
            {
                m_Timeout = 0f;
                m_SegmentCount = 0;
            }
            else
            {
                // If move in progress, drop subsequent moves
                if (m_SegmentCount > 1)
                {
                    m_SegmentCount = 1;
                    return; // Looping handled on reached waypoint
                }
            }

            GetNextLoopWaypoint();
        }

        void GetNextLoopWaypoint ()
        {
            // If waypoints aren't circular, ping pong
            if (!m_Circular)
            {
                if (m_SourceIndex == 0)
                    m_LoopDirection = 1;
                if (m_SourceIndex == m_Waypoints.Length - 1)
                    m_LoopDirection = -1;
            }

            // Get wrapped index
            int index = m_SourceIndex + m_LoopDirection;
            if (index < 0)
                index += m_Waypoints.Length;
            if (index >= m_Waypoints.Length)
                index -= m_Waypoints.Length;

            // Append segment
            if (m_LoopDirection > 0)
                AppendSegment(index, m_JourneyTimes[m_SourceIndex]);
            else
                AppendSegment(index, m_JourneyTimes[index]);
        }

        public void Stop()
        {
            m_LoopDirection = 0;

            // If waiting, stop timer and clear moves
            if (m_Timeout > 0f)
            {
                m_Timeout = 0f;
                m_SegmentCount = 0;
            }
            else
            {
                // If move in progress, drop subsequent moves
                if (m_SegmentCount > 1)
                    m_SegmentCount = 1;
            }
        }

        public override void ApplyOffset(Vector3 offset)
        {
            base.ApplyOffset(offset);
            m_OriginShift += offset;
        }

        #region INeoSerializableComponent IMPLEMENTATION

        private static readonly NeoSerializationKey k_MoveIndicesKey = new NeoSerializationKey("moveIndices");
        private static readonly NeoSerializationKey k_MoveDurationsKey = new NeoSerializationKey("moveDurations");
        private static readonly NeoSerializationKey k_SourceIndexKey = new NeoSerializationKey("sourceIndex");
        private static readonly NeoSerializationKey k_DirectionMultiplierKey = new NeoSerializationKey("direction");
        private static readonly NeoSerializationKey k_ProgressKey = new NeoSerializationKey("progress");
        private static readonly NeoSerializationKey k_TimeoutKey = new NeoSerializationKey("timeout");
        private static readonly NeoSerializationKey k_LoopDirKey = new NeoSerializationKey("loopDir");
        private static readonly NeoSerializationKey k_OriginShiftKey = new NeoSerializationKey("originShift");

        public override void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            base.WriteProperties(writer, nsgo, saveMode);

            if (m_SegmentCount > 0)
            {
                // Gather move segments
                int[] indices = new int[m_SegmentCount];
                float[] durations = new float[m_SegmentCount];
                for (int i = 0; i < m_SegmentCount; ++i)
                {
                    int index = m_HeadSegment + i;
                    if (index > m_MoveSegments.Length)
                        index -= m_MoveSegments.Length;

                    indices[i] = m_MoveSegments[index].index;
                    durations[i] = m_MoveSegments[index].duration;
                }

                writer.WriteValues(k_MoveIndicesKey, indices);
                writer.WriteValues(k_MoveDurationsKey, durations);
            }

            writer.WriteValue(k_SourceIndexKey, m_SourceIndex);
            writer.WriteValue(k_ProgressKey, m_Progress);
            writer.WriteValue(k_TimeoutKey, m_Timeout);
            writer.WriteValue(k_LoopDirKey, m_LoopDirection);
            writer.WriteValue(k_DirectionMultiplierKey, m_DirectionalMultiplier);
            writer.WriteValue(k_OriginShiftKey, m_OriginShift);
        }

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);

            reader.TryReadValue(k_SourceIndexKey, out m_SourceIndex, m_SourceIndex);
            reader.TryReadValue(k_ProgressKey, out m_Progress, m_Progress);
            reader.TryReadValue(k_TimeoutKey, out m_Timeout, m_Timeout);
            reader.TryReadValue(k_LoopDirKey, out m_LoopDirection, m_LoopDirection);
            reader.TryReadValue(k_DirectionMultiplierKey, out m_DirectionalMultiplier, m_DirectionalMultiplier);
            reader.TryReadValue(k_OriginShiftKey, out m_OriginShift, m_OriginShift);

            reader.TryReadValues(k_MoveIndicesKey, out int[] indices, null);
            reader.TryReadValues(k_MoveDurationsKey, out float[] durations, null);

            if (m_MoveSegments == null)
                m_MoveSegments = new MoveSegment[m_Waypoints.Length];

            if (indices != null)
            {
                for (int i = 0; i < indices.Length; ++i)
                {
                    m_MoveSegments[i].index = indices[i];
                    m_MoveSegments[i].duration = durations[i];
                }
                m_SegmentCount = indices.Length;
            }
            else
                m_SegmentCount = 0;
        }

        #endregion
    }
}