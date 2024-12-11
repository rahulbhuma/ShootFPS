using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NeoFPS.Constants;
using NeoSaveGames.Serialization;
using NeoSaveGames;
using UnityEngine.Serialization;

namespace NeoFPS
{
    [RequireComponent(typeof(AudioSource))]
    public abstract class BaseMeleeWeapon : MonoBehaviour, IMeleeWeapon, IWieldable, IDamageSource, ICrosshairDriver, IPoseHandler, INeoSerializableComponent
    {
        [SerializeField, Tooltip("What animators should be exposed to the weapon components,")]
        private AnimatorLocation m_AnimatorLocation = AnimatorLocation.AttachedOnly;

        [SerializeField, NeoObjectInHierarchyField(true), Tooltip("The animator component of the weapon.")]
        private Animator m_Animator = null;

        [SerializeField, NeoObjectInHierarchyField(true), Tooltip("The transform that should be offset for weapon poses.")]
        private Transform m_PoseTransform = null;

        [SerializeField, Tooltip("The crosshair to show when the weapon is drawn.")]
        private FpsCrosshair m_Crosshair = FpsCrosshair.Default;

        [Header("Base Wieldable")]

        [SerializeField, AnimatorParameterKey("m_Animator", AnimatorControllerParameterType.Trigger), Tooltip("The animation trigger for the raise animation.")]
        private string m_TriggerDraw = "Draw";

        [SerializeField, Tooltip("The time taken to lower the item on deselection.")]
        private float m_RaiseDuration = 0.5f;

        [SerializeField, AnimatorParameterKey("m_Animator", AnimatorControllerParameterType.Trigger), Tooltip("The trigger for the weapon lower animation (blank = no animation).")]
        private string m_TriggerLower = string.Empty;

        [SerializeField, Tooltip("The time taken to lower the item on deselection.")]
        private float m_LowerDuration = 0f;

        [SerializeField, Tooltip("The audio clip when raising the weapon.")]
        private AudioClip m_AudioSelect = null;

        private int m_AnimHashDraw = 0;
        private int m_AnimHashLower = 0;

        private DeselectionWaitable m_DeselectionWaitable = null;
        private ICharacter m_Wielder = null;
        private float m_SelectionTimer;

        public event UnityAction<bool> onAttackingChange;
        public event UnityAction<bool> onBlockStateChange;
        public event UnityAction<ICharacter> onWielderChanged;

        public class DeselectionWaitable : Waitable
        {
            private float m_Duration = 0f;
            private float m_StartTime = 0f;

            public DeselectionWaitable(float duration)
            {
                m_Duration = duration;
            }

            public void ResetTimer()
            {
                m_StartTime = Time.time;
            }

            protected override bool CheckComplete()
            {
                return (Time.time - m_StartTime) > m_Duration;
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

        protected AudioSource audioSource
        {
            get;
            private set;
        }

        protected bool isSelecting
        {
            get { return m_SelectionTimer > 0f; }
        }

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

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (m_Animator == null)
                m_Animator = GetComponentInChildren<Animator>();
            if (m_PoseTransform == null)
                m_PoseTransform = transform;

            m_RaiseDuration = Mathf.Clamp(m_RaiseDuration, 0f, 5f);
            m_LowerDuration = Mathf.Clamp(m_LowerDuration, 0f, 5f);
        }
#endif

        protected virtual void Awake()
        {
            animationHandler = new NullAnimatorHandler();

            m_AnimHashDraw = Animator.StringToHash(m_TriggerDraw);
            m_AnimHashLower = Animator.StringToHash(m_TriggerLower);

            // Get the audio source
            audioSource = GetComponent<AudioSource>();

            // Set up deselection waitable
            if (m_LowerDuration > 0.001f)
                m_DeselectionWaitable = new DeselectionWaitable(m_LowerDuration);

            // Set to always animate (since it's first person - some people were getting bugs on raise/lower)
            if (m_Animator != null)
                m_Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Set up pose handler
            CheckPoseHandler();
        }

        protected virtual void Start()
        {
            if (wielder == null)
                Destroy(gameObject);
        }

        protected virtual void OnEnable()
        {
            wielder = GetComponentInParent<ICharacter>();

            // Play draw audio
            if (m_AudioSelect != null)
                audioSource.PlayOneShot(m_AudioSelect);

            // Trigger draw animation
            if (m_AnimHashDraw != 0)
            {
                animationHandler.SetTrigger(m_AnimHashDraw);

                // Start cooldown to prevent input until raised
                m_SelectionTimer = m_RaiseDuration;
            }
        }

        protected virtual void OnDisable()
        {
            blocking = false;
            attacking = false;
            m_SelectionTimer = 0f;

            // Reset pose
            m_PoseHandler.OnDisable();
        }

        protected virtual void Update()
        {
            // Update pose
            m_PoseHandler.UpdatePose();
        }

        protected virtual void FixedUpdate()
        {
            m_SelectionTimer -= Time.deltaTime;
            if (m_SelectionTimer < 0f)
                m_SelectionTimer = 0f;
        }

        public abstract void PrimaryPress();
        public abstract void PrimaryRelease();
        public abstract void SecondaryPress();
        public abstract void SecondaryRelease();

        public void Select()
        {
            // Play lower animation
            if (m_AnimHashDraw != 0)
                animationHandler.SetTrigger(m_AnimHashDraw);
        }

        public void DeselectInstant()
        { }

        public Waitable Deselect()
        {
            // Play lower animation
            if (m_AnimHashLower != 0)
                animationHandler.SetTrigger(m_AnimHashLower);

            // Wait for deselection
            if (m_DeselectionWaitable != null)
                m_DeselectionWaitable.ResetTimer();

            return m_DeselectionWaitable;
        }

        private bool m_Attacking = false;
        public bool attacking
        {
            get { return m_Attacking; }
            protected set
            {
                if (m_Attacking != value)
                {
                    m_Attacking = value; 
                    OnAttackingChange(m_Attacking);
                }
            }
        }

        private bool m_Blocking = false;
        public bool blocking
        {
            get { return m_Blocking; }
            protected set
            {
                if (m_Blocking != value)
                {
                    m_Blocking = value;
                    OnBlockStateChange(m_Blocking);
                }
            }
        }

        protected virtual void OnAttackingChange(bool to)
        {
            if (onAttackingChange != null)
                onAttackingChange(m_Attacking);
        }

        protected virtual void OnBlockStateChange(bool to)
        {
            if (onBlockStateChange != null)
                onBlockStateChange(m_Blocking);
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

        #region BLOCKING (PREVENTING USE)

        private List<Object> m_Blockers = new List<Object>();

        public event UnityAction<bool> onBlockedChanged;

        public virtual bool isBlocked
        {
            get { return m_Blockers.Count > 0 || m_SelectionTimer > 0f; }
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

        PoseHandler CheckPoseHandler()
        {
            if (m_PoseHandler == null)
                m_PoseHandler = new PoseHandler(m_PoseTransform == null ? transform : m_PoseTransform, GetComponent<NeoSerializedGameObject>());
            return m_PoseHandler;
        }

        public void PushPose(PoseInformation pose, MonoBehaviour owner, float blendTime, int priority = 0)
        {
            CheckPoseHandler().PushPose(pose, owner, blendTime, priority);
        }

        public void PopPose(MonoBehaviour owner, float blendTime)
        {
            CheckPoseHandler().PopPose(owner, blendTime);
        }

        public PoseInformation GetPose(MonoBehaviour owner)
        {
            return m_PoseHandler.GetPose(owner);
        }

        #endregion

        #region IDamageSource implementation

        private DamageFilter m_OutDamageFilter = DamageFilter.AllDamageAllTeams;
        public DamageFilter outDamageFilter
        {
            get
            {
                return m_OutDamageFilter;
            }
            set
            {
                m_OutDamageFilter = value;
            }
        }

        public IController controller
        {
            get
            {
                if (wielder != null)
                    return wielder.controller;
                else
                    return null;
            }
        }

        public Transform damageSourceTransform
        {
            get
            {
                return transform;
            }
        }

        public string description
        {
            get
            {
                return name;
            }
        }

        #endregion

        #region ICrosshairDriver IMPLEMENTATION

        private bool m_HideCrosshair = false;

        public FpsCrosshair crosshair
        {
            get { return m_Crosshair; }
            protected set
            {
                m_Crosshair = value;
                if (onCrosshairChanged != null)
                    onCrosshairChanged(m_Crosshair);
            }
        }

        private float m_Accuracy = 1f;
        public float accuracy
        {
            get { return m_Accuracy; }
            protected set
            {
                m_Accuracy = value;
                if (onAccuracyChanged != null)
                    onAccuracyChanged(m_Accuracy);
            }
        }

        public event UnityAction<FpsCrosshair> onCrosshairChanged;
        public event UnityAction<float> onAccuracyChanged;

        public void HideCrosshair()
        {
            if (!m_HideCrosshair)
            {
                bool triggerEvent = (onCrosshairChanged != null && crosshair == FpsCrosshair.None);

                m_HideCrosshair = true;

                if (triggerEvent)
                    onCrosshairChanged(FpsCrosshair.None);
            }
        }

        public void ShowCrosshair()
        {
            if (m_HideCrosshair)
            {
                // Reset
                m_HideCrosshair = false;

                // Fire event
                if (onCrosshairChanged != null && crosshair != FpsCrosshair.None)
                    onCrosshairChanged(crosshair);
            }
        }

        #endregion

        #region INeoSerializableComponent IMPLEMENTATION

        private static readonly NeoSerializationKey k_BlockingKey = new NeoSerializationKey("blocking");
        private static readonly NeoSerializationKey k_AttackingKey = new NeoSerializationKey("attacking");
        private static readonly NeoSerializationKey k_AccuracyKey = new NeoSerializationKey("accuracy");

        public virtual void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            // Write properties if relevant
            if (saveMode == SaveMode.Default)
            {
                writer.WriteValue(k_BlockingKey, blocking);
                writer.WriteValue(k_AttackingKey, blocking);
                writer.WriteValue(k_AccuracyKey, accuracy);
            }
        }

        public virtual void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            // Read properties
            float floatResult;
            if (reader.TryReadValue(k_AccuracyKey, out floatResult, 1f))
                accuracy = floatResult;

            bool boolResult;
            if (reader.TryReadValue(k_BlockingKey, out boolResult, false))
                blocking = boolResult;
            if (reader.TryReadValue(k_AttackingKey, out boolResult, false))
                attacking = boolResult;
        }

        #endregion
    }
}