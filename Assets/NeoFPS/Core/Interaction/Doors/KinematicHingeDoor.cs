using System.Collections;
using System.Collections.Generic;
using NeoSaveGames;
using NeoSaveGames.Serialization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/interactionref-mb-kinematichingedoor.html")]
    public class KinematicHingeDoor : DoorBase
    {
		[SerializeField, Range(90f, 180f), Tooltip("The maximum open angle of the door.")]
        private float m_MaxAngle = 120f;

		[SerializeField, Range(1f, 5f), Tooltip("The time it takes to go from fully closed to fully open and vice versa.")]
        private float m_OpenDuration = 1.5f;

		[SerializeField, Tooltip("The interpolation curve for opening and closing the door.")]
        private AnimationCurve m_OpenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

		[SerializeField, Tooltip("The transform point for the positive rotation of the door (defaults to the transform for the object this behaviour is attached to).")]
        private Transform m_PositiveRotationTransform = null;

        [SerializeField, Tooltip("The transform point for the negative rotation of the door. This allows hinges at both edges of a door to prevent overlap. If this is null the door will only open on one side.")]
        private Transform m_NegativeRotationTransform = null;

        [SerializeField, Tooltip("A fixed transform used to check which side of the doorway a character is on (defaults to the transform for the object this behaviour is attached to).")]
        private Transform m_DirectionTransform = null;

        [SerializeField, Tooltip("An optional animated door handle. This will turn and release when the door is opened.")]
        private AnimatedDoorHandle m_Handle = null;

        [Header("Audio")]

        [SerializeField, FormerlySerializedAs("m_AudioOpen"), Tooltip("The audio to play when the door unlatches and starts opening.")]
        private AudioClip m_AudioUnlatch = null;

        [SerializeField, FormerlySerializedAs("m_AudioClose"), Tooltip("The audio to play when the door latches closed.")]
        private AudioClip m_AudioLatch = null;

        [SerializeField, Tooltip("The audio to play when the starts moving.")]
        private AudioClip m_AudioMove = null;

        [SerializeField, Tooltip("The audio to play when attempting to open the door while locked.")]
        private AudioClip m_AudioLocked = null;

        [SerializeField, FormerlySerializedAs("m_AudioUnlock"), Tooltip("The audio to play when unlocking or locking the door.")]
        private AudioClip m_AudioUnlockOrLock = null;

        [Header("Events")]

        [SerializeField, Tooltip("An event fired when the door unlatches and starts opening.")]
        private UnityEvent m_OnOpen = new UnityEvent();

        [SerializeField, Tooltip("An event fired when the door is finished closing and latches.")]
        private UnityEvent m_OnClosed = new UnityEvent();

        [SerializeField, Tooltip("An event fired when attempting to open the door while locked.")]
        private UnityEvent m_OnTryOpenLocked = new UnityEvent();

        private static readonly NeoSerializationKey k_ReverseKey = new NeoSerializationKey("reverse");

#if UNITY_EDITOR
        protected void OnValidate()
        {
            if (m_DirectionTransform == null)
                m_DirectionTransform = transform;
        }
#endif

        private AudioSource m_AudioSource = null;
        private Coroutine m_AnimationCoroutine = null;
        private bool m_Reverse = false;

        public event UnityAction onOpen
        {
            add { m_OnOpen.AddListener(value); }
            remove { m_OnOpen.RemoveListener(value); }
        }

        public event UnityAction onClosed
        {
            add { m_OnClosed.AddListener(value); }
            remove { m_OnClosed.RemoveListener(value); }
        }

        public event UnityAction onTryOpenLocked
        {
            add { m_OnTryOpenLocked.AddListener(value); }
            remove { m_OnTryOpenLocked.RemoveListener(value); }
        }

        public override bool reversible
        {
            get { return true; }
        }

        private float m_NormalisedOpen = 0f;
        public override float normalisedOpen
        {
            get { return m_NormalisedOpen; }
            protected set
            {
                if (reversible)
                    m_NormalisedOpen = Mathf.Clamp(value, -1f, 1f);
                else
                    m_NormalisedOpen = Mathf.Clamp01(value);

                if (Mathf.Approximately(m_NormalisedOpen, 0f))
                {
                    if (m_PositiveRotationTransform != null)
                        m_PositiveRotationTransform.localRotation = Quaternion.identity;
                    if (m_NegativeRotationTransform != null)
                        m_NegativeRotationTransform.localRotation = Quaternion.identity;
                }
                else
                {
                    if (m_NormalisedOpen > 0f)
                    {
                        if (m_NegativeRotationTransform != null)
                            m_NegativeRotationTransform.localRotation = Quaternion.identity;
                        if (m_PositiveRotationTransform != null)
                        {
                            float angle = m_OpenCurve.Evaluate(m_NormalisedOpen) * m_MaxAngle;
                            m_PositiveRotationTransform.localRotation = Quaternion.Euler(0f, angle, 0f);
                        }
                    }
                    else
                    {
                        if (m_PositiveRotationTransform != null)
                            m_PositiveRotationTransform.localRotation = Quaternion.identity;
                        if (m_NegativeRotationTransform != null)
                        {
                            float angle = m_OpenCurve.Evaluate(-m_NormalisedOpen) * -m_MaxAngle;
                            m_NegativeRotationTransform.localRotation = Quaternion.Euler(0f, angle, 0f);
                        }
                    }
                }
            }
        }

        protected void Awake()
        {
            m_AudioSource = GetComponentInChildren<AudioSource>();
        }

        protected override void OnOpen(bool reverse = true)
        {
            if (m_AnimationCoroutine != null)
                StopCoroutine(m_AnimationCoroutine);

            // Get open direction
            float target = 1f;
            if ((reverse && m_NegativeRotationTransform != null) ||
                (!reverse && m_PositiveRotationTransform == null))
            {
                m_Reverse = true;
                target = -1f;
            }

            m_AnimationCoroutine = StartCoroutine(Animation(target, DoorState.Open));

            switch (state)
            {
                case DoorState.Closed:
                    {
                        // Play audio clip
                        if (m_AudioSource != null && m_AudioUnlatch != null)
                            m_AudioSource.PlayOneShot(m_AudioUnlatch);

                        // Turn handle
                        if (m_Handle != null)
                            m_Handle.Twist();

                        // Fire the event
                        m_OnOpen.Invoke();
                    }
                    break;
                case DoorState.Closing:
                    {
                        // Play audio clip
                        if (m_AudioSource != null && m_AudioMove != null)
                            m_AudioSource.PlayOneShot(m_AudioMove);
                        // Turn handle
                        if (m_Handle != null)
                            m_Handle.Twist();
                    }
                    break;
            }

            state = DoorState.Opening;
        }

        protected override void OnClose()
        {
            if (m_AnimationCoroutine != null)
                StopCoroutine(m_AnimationCoroutine);

            m_AnimationCoroutine = StartCoroutine(Animation(0f, DoorState.Closed));

            if (state == DoorState.Open || state == DoorState.Opening)
            {
                // Play audio clip
                if (m_AudioSource != null && m_AudioMove != null)
                    m_AudioSource.PlayOneShot(m_AudioMove);
            }

            state = DoorState.Closing;
        }

        protected override void OnTryOpenLocked()
        {
            // Play audio clip
            if (m_AudioSource != null && m_AudioLocked != null)
                m_AudioSource.PlayOneShot(m_AudioLocked);

            // Turn handle
            if (m_Handle != null)
                m_Handle.Jiggle();

            // Fire the event
            m_OnTryOpenLocked.Invoke();
        }

        protected override void OnLockedStateChanged(bool locked)
        {
            // Play audio clip
            if (m_AudioSource != null && m_AudioUnlockOrLock != null)
                m_AudioSource.PlayOneShot(m_AudioUnlockOrLock);
        }

        public override bool IsTransformInFrontOfDoor(Transform t)
        {
            if (m_DirectionTransform == null)
                return true;

            // Check if transform position is in front of door (+Z)
            Vector3 position = t.position - m_DirectionTransform.position;
            return (Vector3.Dot(position, m_DirectionTransform.forward) > 0f);
        }

        IEnumerator Animation(float target, DoorState endState)
        {
            float inverseDuration = 1f / m_OpenDuration;

            if (normalisedOpen < target)
            {
                while (true)
                {
                    yield return null;
                    float newValue = normalisedOpen + Time.deltaTime * inverseDuration;
                    if (newValue > target)
                    {
                        normalisedOpen = target;
                        break;
                    }
                    else
                        normalisedOpen = newValue;
                }
            }
            else
            {
                while (true)
                {
                    yield return null;
                    float newValue = normalisedOpen - Time.deltaTime * inverseDuration;
                    if (newValue < target)
                    {
                        normalisedOpen = target;
                        break;
                    }
                    else
                        normalisedOpen = newValue;
                }
            }

            // Closed
            if (endState == DoorState.Closed)
            {
                // Play audio clip
                if (m_AudioSource != null && m_AudioLatch != null)
                    m_AudioSource.PlayOneShot(m_AudioLatch);

                // Fire the event
                m_OnClosed.Invoke();
            }

            state = endState;
            m_AnimationCoroutine = null;
        }

        public override void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            base.WriteProperties(writer, nsgo, saveMode);

            writer.WriteValue(k_ReverseKey, m_Reverse);
        }

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);

            reader.TryReadValue(k_ReverseKey, out m_Reverse, m_Reverse);
            switch (state)
            {
                case DoorState.Opening:
                    float target = (m_Reverse) ? -1f : 1f;
                    m_AnimationCoroutine = StartCoroutine(Animation(target, DoorState.Open));
                    break;
                case DoorState.Closing:
                    m_AnimationCoroutine = StartCoroutine(Animation(0f, DoorState.Closed));
                    break;
            }
        }
    }
}