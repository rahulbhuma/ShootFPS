using NeoFPS.Constants;
using NeoSaveGames;
using NeoSaveGames.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    public abstract class BaseWieldableItem : MonoBehaviour, IWieldable
    {
        [Header ("Select / Deselect")]

        [SerializeField, Tooltip("What animators should be exposed to the weapon components,")]
        private AnimatorLocation m_AnimatorLocation = AnimatorLocation.AttachedOnly;

        [SerializeField, Tooltip("The animator component of the weapon.")]
        private Animator m_Animator = null;

        [SerializeField, AnimatorParameterKey("m_Animator", AnimatorControllerParameterType.Trigger), Tooltip("The key for the AnimatorController trigger property that triggers the draw animation.")]
        private string m_AnimKeyDraw = "Draw";

        [SerializeField, Tooltip("The audio clip when raising the weapon.")]
        private AudioClip m_AudioSelect = null;

        [SerializeField, Tooltip("The audio clip when lowering the weapon.")]
        private AudioClip m_AudioDeselect = null;

        [SerializeField, AnimatorParameterKey("m_Animator", AnimatorControllerParameterType.Trigger), Tooltip("The trigger for the weapon lower animation (blank = no animation).")]
        private string m_AnimKeyLower = string.Empty;

        [SerializeField, Tooltip("The time it takes to raise the weapon.")]
        private float m_DrawDuration = 0.5f;

        [SerializeField, Tooltip("The time taken to lower the item on deselection.")]
        private float m_LowerDuration = 0f;

        private DeselectionWaitable m_DeselectionWaitable = null;
        private int m_AnimHashDraw = -1;
        private int m_AnimHashLower = -1;
        private Coroutine m_BlockingCoroutine = null;
        private float m_DrawTimer = 0f;

        public event UnityAction<ICharacter> onWielderChanged;

        private ICharacter m_Wielder = null;
        public ICharacter wielder
        {
            get { return m_Wielder; }
            private set
            {
                if (m_Wielder != value)
                {
                    m_Wielder = value;
                    if (onWielderChanged != null)
                        onWielderChanged(m_Wielder);
                }
                InitialiseAnimationHandler();
            }
        }

        protected Animator animator
        {
            get { return m_Animator; }
        }
        
        public IWieldableAnimationHandler animationHandler
        {
            get;
            private set;
        }

        protected virtual bool CheckIsBlocked()
        {
            return m_BlockingCoroutine != null || !m_DeselectionWaitable.isComplete;
        }

        public enum DeselectionState
        {
            Selected,
            BusyWaiting,
            Deselecting
        }

        public class DeselectionWaitable : Waitable
        {
            // This is kinda gross. I need to rework with proper busy vs blocking, etc, for 1.2

            private BaseWieldableItem m_Owner = null;
            private float m_StartTime = 0f;

            public DeselectionWaitable(BaseWieldableItem owner)
            {
                m_Owner = owner;
                state = DeselectionState.Deselecting;
            }

            public DeselectionState state
            {
                get; private set;
            }

            public void OnSelected()
            {
                state = DeselectionState.Selected;
            }

            public void OnDeselect()
            {
                state = DeselectionState.BusyWaiting;
            }

            public void StartTimer()
            {
                m_StartTime = Time.time;
                state = DeselectionState.Deselecting;
            }

            protected override bool CheckComplete()
            {
                return state == DeselectionState.Deselecting && (Time.time - m_StartTime) > m_Owner.m_LowerDuration;
            }
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (m_Animator == null)
                m_Animator = GetComponentInChildren<Animator>();
            m_DrawDuration = Mathf.Clamp(m_DrawDuration, 0f, 5f);
            m_LowerDuration = Mathf.Clamp(m_LowerDuration, 0f, 5f);
        }
#endif

        protected virtual void Awake()
        {
            animationHandler = new NullAnimatorHandler();

            if (!string.IsNullOrWhiteSpace(m_AnimKeyDraw))
                m_AnimHashDraw = Animator.StringToHash(m_AnimKeyDraw);
            else
                m_DrawDuration = 0f;

            if (!string.IsNullOrWhiteSpace(m_AnimKeyLower))
            {
                m_AnimHashLower = Animator.StringToHash(m_AnimKeyLower);
                m_DeselectionWaitable = new DeselectionWaitable(this);
            }
            else
                m_LowerDuration = 0f;

            // Set to always animate (since it's first person - some people were getting bugs on raise/lower)
            if (m_Animator != null)
                m_Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Set up pose handler
            m_PoseHandler = new PoseHandler(transform, GetComponent<NeoSerializedGameObject>());
        }

        protected virtual void OnEnable()
        {
            wielder = GetComponentInParent<ICharacter>();
            if (wielder != null)
            {
                if (m_AnimHashDraw != -1)
                    animationHandler.SetTrigger(m_AnimHashDraw);

                if (m_DrawDuration > 0f)
                    m_BlockingCoroutine = StartCoroutine(DrawCoroutine(m_DrawDuration));

                if (m_AudioSelect != null)
                    wielder.audioHandler.PlayClip(m_AudioSelect, FpsCharacterAudioSource.Body);
            }
        }

        protected virtual void OnDisable()
        {
            m_BlockingCoroutine = null;
            // Reset pose
            m_PoseHandler?.OnDisable();
        }

        public void Select()
        {
            // Play lower animation
            if (m_AnimHashDraw != -1)
                animationHandler.SetTrigger(m_AnimHashDraw);

            if (m_DeselectionWaitable != null)
                m_DeselectionWaitable.OnSelected();
        }

        public void DeselectInstant()
        { }

        public Waitable Deselect()
        {
            if (m_DeselectionWaitable != null && m_DeselectionWaitable.state == DeselectionState.Selected)
            {
                m_DeselectionWaitable.OnDeselect();
                StartCoroutine(DeselectCoroutine());
            }

            return m_DeselectionWaitable;
        }

        IEnumerator DeselectCoroutine()
        {
            while (isBusy)
                yield return null;

            // Play lower animation
            if (m_AnimHashLower != 0)
                animationHandler.SetTrigger(m_AnimHashLower);

            // Play the lower audio
            if (m_AudioDeselect != null)
                wielder.audioHandler.PlayClip(m_AudioDeselect, FpsCharacterAudioSource.Body);

            // Start the deselection timer
            m_DeselectionWaitable.StartTimer();
        }

        IEnumerator DrawCoroutine(float timer)
        {
            m_DrawTimer = timer;
            while (m_DrawTimer > 0f)
            {
                yield return null;
                m_DrawTimer -= Time.deltaTime;
            }
            m_BlockingCoroutine = null;
        }

        void InitialiseAnimationHandler()
        {
            switch (m_AnimatorLocation)
            {
                case AnimatorLocation.None:
                    animationHandler = new NullAnimatorHandler();
                    break;
                case AnimatorLocation.AttachedOnly:
                    if (m_Animator != null)
                        animationHandler = new SingleAnimatorHandler(m_Animator);
                    else
                        animationHandler = new NullAnimatorHandler();
                    break;
                case AnimatorLocation.AttachedAndCharacter:
                    {
                        var characterAnimator = wielder?.motionController?.bodyAnimator;
                        if (characterAnimator != null)
                        {
                            if (m_Animator != null)
                                animationHandler = new MultiAnimatorHandler(new Animator[] { m_Animator, characterAnimator });
                            else
                                animationHandler = new SingleAnimatorHandler(characterAnimator);
                        }
                        else
                        {
                            if (m_Animator != null)
                                animationHandler = new SingleAnimatorHandler(m_Animator);
                            else
                                animationHandler = new NullAnimatorHandler();
                        }
                        break;
                    }
                case AnimatorLocation.MultipleAttached:
                    {
                        var animators = GetComponentsInChildren<Animator>(true);
                        if (animators != null && animators.Length > 0)
                            animationHandler = new MultiAnimatorHandler(animators);
                        else
                            animationHandler = new NullAnimatorHandler();
                    }
                    break;
                case AnimatorLocation.MultipleAttachedAndCharacter:
                    {
                        List<Animator> animators = new List<Animator>();
                        GetComponentsInChildren(true, animators);

                        var characterAnimator = wielder?.motionController?.bodyAnimator;
                        if (characterAnimator != null)
                            animators.Add(characterAnimator);

                        switch (animators.Count)
                        {
                            case 0:
                                animationHandler = new NullAnimatorHandler();
                                break;
                            case 1:
                                animationHandler = new SingleAnimatorHandler(animators[0]);
                                break;
                            default:
                                animationHandler = new MultiAnimatorHandler(animators.ToArray());
                                break;
                        }
                    }
                    break;
                case AnimatorLocation.CharacterOnly:
                    {
                        var characterAnimator = wielder?.motionController?.bodyAnimator;
                        if (characterAnimator != null)
                            animationHandler = new SingleAnimatorHandler(characterAnimator);
                        else
                            animationHandler = new NullAnimatorHandler();
                    }
                    break;
            }
        }

        #region BLOCKING

        private List<Object> m_Blockers = new List<Object>();

        public event UnityAction<bool> onBlockedChanged;

        public virtual bool isBusy
        {
            get { return false; }
        }

        public virtual bool isBlocked
        {
            get { return m_Blockers.Count > 0; }
        }

        public void AddBlocker(Object o)
        {
            // Previous state
            int oldCount = m_Blockers.Count;

            // Add blocker
            if (o != null && !m_Blockers.Contains(o))
                m_Blockers.Add(o);

            // Block state changed
            if (m_Blockers.Count != 0 && oldCount == 0)
                OnIsBlockedChanged(true);
        }

        public void RemoveBlocker(Object o)
        {
            // Previous state
            int oldCount = m_Blockers.Count;

            // Remove blocker
            m_Blockers.Remove(o);

            // Block state changed
            if (m_Blockers.Count == 0 && oldCount != 0)
                OnIsBlockedChanged(false);
        }

        protected virtual void OnIsBlockedChanged(bool blocked)
        {
            onBlockedChanged?.Invoke(blocked);
        }

        #endregion

        #region POSE

        private PoseHandler m_PoseHandler = null;

        public void PushPose(PoseInformation pose, MonoBehaviour owner, float blendTime, int priority = 0)
        {
            m_PoseHandler.PushPose(pose, owner, blendTime, priority);
        }

        public void PopPose(MonoBehaviour owner, float blendTime)
        {
            m_PoseHandler.PopPose(owner, blendTime);
        }

        public PoseInformation GetPose(MonoBehaviour owner)
        {
            return m_PoseHandler.GetPose(owner);
        }

        protected void Update()
        {
            if (m_PoseHandler != null)
                m_PoseHandler.UpdatePose();
        }

        #endregion

        #region INeoSerializableComponent IMPLEMENTATION

        private static readonly NeoSerializationKey k_DrawTimerKey = new NeoSerializationKey("drawTimer");

        public virtual void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            if (saveMode == SaveMode.Default)
            {
                // Write coroutine if relevant
                if (m_BlockingCoroutine != null)
                {
                    if (m_DrawTimer > 0f)
                        writer.WriteValue(k_DrawTimerKey, m_DrawTimer);
                }
            }
        }

        public virtual void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            // Read and start coroutines if relevant
            float floatResult = 0f;
            if (reader.TryReadValue(k_DrawTimerKey, out floatResult, 0f))
                m_BlockingCoroutine = StartCoroutine(DrawCoroutine(floatResult));
        }

        #endregion
    }
}