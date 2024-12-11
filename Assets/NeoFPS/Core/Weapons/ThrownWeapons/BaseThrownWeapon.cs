using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NeoFPS.Constants;
using NeoSaveGames.Serialization;
using NeoSaveGames;

namespace NeoFPS
{
    [RequireComponent(typeof(AudioSource))]
    public abstract class BaseThrownWeapon : MonoBehaviour, IThrownWeapon, IWieldable, IDamageSource, ICrosshairDriver, IPoseHandler, INeoSerializableComponent
    {
        [SerializeField, NeoObjectInHierarchyField(true, required = true), Tooltip("A proxy transform for setting the position and rotation of the spawned projectile (weak throw).")]
        private Transform m_ProjectileSpawnPointWeak = null;

        [SerializeField, NeoObjectInHierarchyField(true, required = true), Tooltip("A proxy transform for setting the position and rotation of the spawned projectile (strong throw).")]
        private Transform m_ProjectileSpawnPointStrong = null;

        [SerializeField, NeoPrefabField(typeof(ThrownWeaponProjectile)), Tooltip("The prefab to throw.")]
        private PooledObject m_SpawnedProjectile = null;

        [SerializeField, NeoObjectInHierarchyField(false), Tooltip("The weapon game object. This is deactivated and swapped with the pooled object during the throw animation.")]
        private GameObject m_HeldObject = null;

        [SerializeField, Range(0f, 1f), Tooltip("How much of the character velocity does the thrown weapon inherit (think Counter Strike).")]
        private float m_InheritVelocity = 0.5f;

        [SerializeField, Tooltip("The point in the animation (seconds) to swap the animated weapon with the pooled physics weapon (weak throw).")]
        private float m_SpawnTimeWeak = 0.5f;

        [SerializeField, Tooltip("The point in the animation (seconds) to swap the animated weapon with the pooled physics weapon (strong throw).")]
        private float m_SpawnTimeStrong = 0.5f;

        [SerializeField, Tooltip("The throw speed of the projectile (weak throw).")]
        private float m_ThrowSpeedWeak = 5f;

        [SerializeField, Tooltip("The throw speed of the projectile (strong throw).")]
        private float m_ThrowSpeedStrong = 7.5f;

        [SerializeField, Tooltip("The full duration of the weak throw animation.")]
        private float m_ThrowDurationWeak = 3f;

        [SerializeField, Tooltip("The full duration of the strong throw animation.")]
        private float m_ThrowDurationStrong = 3f;

        [SerializeField, Tooltip("The crosshair to show when the weapon is drawn.")]
        private FpsCrosshair m_Crosshair = FpsCrosshair.Default;

        [Header("Audio")]

        [SerializeField, Tooltip("The audio clip when raising the weapon.")]
        private AudioClip m_AudioSelect = null;

        [SerializeField, Tooltip("The audio clip for a weak throw.")]
        private AudioClip m_AudioThrowLight = null;

        [SerializeField, Tooltip("The audio clip for a strong throw.")]
        private AudioClip m_AudioThrowHeavy = null;

        [Header("Animation")]

        [SerializeField, Tooltip("What animators should be exposed to the weapon components,")]
        private AnimatorLocation m_AnimatorLocation = AnimatorLocation.AttachedOnly;

        [SerializeField, Tooltip("The animator component of the weapon.")]
        private Animator m_Animator = null;

        [SerializeField, NeoObjectInHierarchyField(true), Tooltip("The transform that should be offset for weapon poses.")]
        private Transform m_PoseTransform = null;

        [SerializeField, AnimatorParameterKey("m_Animator", AnimatorControllerParameterType.Trigger), Tooltip("The key for the AnimatorController trigger property that triggers the draw animation.")]
        private string m_AnimKeyDraw = "Draw";

        [SerializeField, AnimatorParameterKey("m_Animator", AnimatorControllerParameterType.Trigger), Tooltip("The key for the AnimatorController trigger property that triggers the light throw animation.")]
        private string m_AnimKeyLightThrow = "ThrowLight";

        [SerializeField, AnimatorParameterKey("m_Animator", AnimatorControllerParameterType.Trigger), Tooltip("The key for the AnimatorController trigger property that triggers the heavy throw animation.")]
        private string m_AnimKeyHeavyThrow = "ThrowHeavy";

        [SerializeField, AnimatorParameterKey("m_Animator", AnimatorControllerParameterType.Trigger), Tooltip("The trigger for the weapon lower animation (blank = no animation).")]
        private string m_AnimKeyLower = string.Empty;

        [SerializeField, Tooltip("The time it takes to raise the weapon.")]
        private float m_DrawDuration = 0.5f;

        [SerializeField, Tooltip("The time taken to lower the item on deselection.")]
        private float m_LowerDuration = 0f;

        private DeselectionWaitable m_DeselectionWaitable = null;
        private int m_AnimHashDraw = 0;
        private int m_AnimHashLower = 0;
        private int m_AnimHashThrowLight = 0;
        private int m_AnimHashThrowHeavy = 0;
        private AudioSource m_AudioSource = null;
        private Coroutine m_BlockingCoroutine = null;
        private ICharacter m_Wielder = null;
        private float m_DrawTimer = 0f;
        private float m_ThrowDelay = 0f;
        private bool m_Strong = false;

        public event UnityAction onThrowLight;
        public event UnityAction onThrowHeavy;
        public event UnityAction<ICharacter> onWielderChanged;
        
        public IWieldableAnimationHandler animationHandler {
            get;
            private set;
        }

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

        public float durationLight
        {
            get { return m_ThrowDurationWeak; }
        }

        public float durationHeavy
        {
            get { return m_ThrowDurationStrong; }
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            if (m_Animator == null)
                m_Animator = GetComponentInChildren<Animator>();
            if (m_PoseTransform == null)
                m_PoseTransform = transform;

            m_SpawnTimeWeak = Mathf.Clamp(m_SpawnTimeWeak, 0f, 5f);
            m_SpawnTimeStrong = Mathf.Clamp(m_SpawnTimeStrong, 0f, 5f);
            m_ThrowSpeedWeak = Mathf.Clamp(m_ThrowSpeedWeak, 1f, 50f);
            m_ThrowSpeedStrong = Mathf.Clamp(m_ThrowSpeedStrong, 1f, 50f);
            m_ThrowDurationWeak = Mathf.Clamp(m_ThrowDurationWeak, 0.1f, 10f);
            m_ThrowDurationStrong = Mathf.Clamp(m_ThrowDurationStrong, 0.1f, 10f);
            m_DrawDuration = Mathf.Clamp(m_DrawDuration, 0f, 5f);
            m_LowerDuration = Mathf.Clamp(m_LowerDuration, 0f, 5f);
        }
#endif

        protected virtual void Awake()
        {
            m_AnimHashDraw = Animator.StringToHash(m_AnimKeyDraw);
            m_AnimHashLower = Animator.StringToHash(m_AnimKeyLower);
            m_AnimHashThrowLight = Animator.StringToHash(m_AnimKeyLightThrow);
            m_AnimHashThrowHeavy = Animator.StringToHash(m_AnimKeyHeavyThrow);

            // Get the audio source
            m_AudioSource = GetComponent<AudioSource>();

            // Set up deselection waitable
            if (m_LowerDuration > 0.001f)
                m_DeselectionWaitable = new DeselectionWaitable(m_LowerDuration);

            // Set to always animate (since it's first person - some people were getting bugs on raise/lower)
            if (m_Animator != null)
                m_Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Set up pose handler
            InitialisePoseHandler();
        }

        protected virtual void Start()
        {
            if (wielder == null)
                Destroy(gameObject);
        }

        protected void OnEnable()
        {
            wielder = GetComponentInParent<ICharacter>();
         
            // Can't inherit velocity without being able to get velocity
            if (wielder == null || wielder.motionController == null)
                m_InheritVelocity = 0f;

            // Trigger draw animation
            animationHandler.SetTrigger(m_AnimHashDraw);
            if (m_DrawDuration > 0f)
                m_BlockingCoroutine = StartCoroutine(DrawCoroutine(m_DrawDuration));

            // Play selection audio
            if (m_AudioSelect != null)
                m_AudioSource.PlayOneShot(m_AudioSelect);
        }

        protected void OnDisable()
        {
            m_BlockingCoroutine = null;
            // Reset pose
            m_PoseHandler.OnDisable();
        }

        protected virtual void Update()
        {
            // Update pose
            m_PoseHandler.UpdatePose();
        }

        public void Select()
        {
            // Play lower animation
            if (m_AnimHashDraw != -1)
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

        protected virtual float GetModifiedThowSpeedLight(float unmodified)
        {
            return unmodified;
        }

        public void ThrowLight()
        {
            if (!isBlocked && m_BlockingCoroutine == null)
            {
                m_Strong = false;

                animationHandler.SetTrigger(m_AnimHashThrowLight);
                m_BlockingCoroutine = StartCoroutine(ThrowCoroutine(m_ThrowDurationWeak, m_SpawnTimeWeak, GetModifiedThowSpeedLight(m_ThrowSpeedWeak), m_ProjectileSpawnPointWeak));

                if (m_AudioThrowLight != null)
                    m_AudioSource.PlayOneShot(m_AudioThrowLight, FpsCharacterAudioSource.Head);

                if (onThrowLight != null)
                    onThrowLight();
            }
        }

        protected virtual float GetModifiedThowSpeedHeavy(float unmodified)
        {
            return unmodified;
        }

        public void ThrowHeavy()
        {
            if (!isBlocked && m_BlockingCoroutine == null)
            {
                m_Strong = true;

                animationHandler.SetTrigger(m_AnimHashThrowHeavy);
                m_BlockingCoroutine = StartCoroutine(ThrowCoroutine(m_ThrowDurationStrong, m_SpawnTimeStrong, GetModifiedThowSpeedHeavy(m_ThrowSpeedStrong), m_ProjectileSpawnPointStrong));

                if (m_AudioThrowHeavy != null)
                    m_AudioSource.PlayOneShot(m_AudioThrowHeavy, FpsCharacterAudioSource.Head);

                if (onThrowHeavy != null)
                    onThrowHeavy();
            }
        }

        protected abstract void DecrementQuantity();

        IEnumerator DrawCoroutine(float timer)
        {
            m_DrawTimer = timer;

            while (m_DrawTimer > 0f)
            {
                yield return null;
                m_DrawTimer -= Time.deltaTime;
            }

            m_DrawTimer = 0f;
            m_BlockingCoroutine = null;
        }

        IEnumerator ThrowCoroutine(float duration, float delay, float speed, Transform spawnPoint)
        {
            m_DrawTimer = 0f;
            if (delay > 0f)
            {
                m_ThrowDelay = delay;
                while (m_ThrowDelay > 0f)
                {
                    yield return null;
                    m_ThrowDelay -= Time.deltaTime;
                }
            }
            m_HeldObject.SetActive(false);

            ThrownWeaponProjectile projectile = PoolManager.GetPooledObject<ThrownWeaponProjectile>(m_SpawnedProjectile, spawnPoint.position, spawnPoint.rotation);

            Vector3 velocity = spawnPoint.forward * speed;

            if (m_InheritVelocity > 0f)
            {
                Vector3 inherited = wielder.motionController.characterController.velocity;
                velocity += inherited * m_InheritVelocity;
            }

            projectile.Throw(velocity, this);

            DecrementQuantity();

            if (duration > delay)
            {
                m_DrawTimer = m_DrawDuration + duration - delay;
                while (m_DrawTimer > m_DrawDuration)
                {
                    yield return null;
                    m_DrawTimer -= Time.deltaTime;
                }
            }
            m_HeldObject.SetActive(true);

            m_BlockingCoroutine = StartCoroutine(DrawCoroutine(m_DrawTimer));
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

        public bool isBlocked
        {
            get { return m_Blockers.Count > 0 || m_BlockingCoroutine != null; }
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

        void InitialisePoseHandler()
        {
            if (m_PoseHandler == null)
                m_PoseHandler = new PoseHandler(m_PoseTransform == null ? transform : m_PoseTransform, GetComponent<NeoSerializedGameObject>());
        }

        public void PushPose(PoseInformation pose, MonoBehaviour owner, float blendTime, int priority = 0)
        {
            InitialisePoseHandler();
            m_PoseHandler.PushPose(pose, owner, blendTime, priority);
        }

        public void PopPose(MonoBehaviour owner, float blendTime)
        {
            InitialisePoseHandler();
            m_PoseHandler.PopPose(owner, blendTime);
        }

        public PoseInformation GetPose(MonoBehaviour owner)
        {
            InitialisePoseHandler();
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
            //private set
            //{
            //    m_Crosshair = value;
            //    if (onCrosshairChanged != null)
            //        onCrosshairChanged(m_Crosshair);
            //}
        }

        private float m_Accuracy = 1f;
        public float accuracy
        {
            get { return m_Accuracy; }
            private set
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

        private static readonly NeoSerializationKey k_DrawTimerKey = new NeoSerializationKey("drawTimer");
        private static readonly NeoSerializationKey k_ThrowDelayKey = new NeoSerializationKey("throwDelay");
        private static readonly NeoSerializationKey k_AccuracyKey = new NeoSerializationKey("accuracy");
        private static readonly NeoSerializationKey k_StrongKey = new NeoSerializationKey("strong");

        public virtual void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            if (saveMode == SaveMode.Default)
            {
                // Write coroutine if relevant
                if (m_BlockingCoroutine != null)
                {
                    if (m_DrawTimer > 0f)
                        writer.WriteValue(k_DrawTimerKey, m_DrawTimer);
                    if (m_ThrowDelay > 0f)
                        writer.WriteValue(k_ThrowDelayKey, m_ThrowDelay);
                }

                // Write properties
                writer.WriteValue(k_AccuracyKey, accuracy);
                writer.WriteValue(k_StrongKey, m_Strong);
            }
        }

        public virtual void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            // Read properties
            float floatResult = 0f;
            if (reader.TryReadValue(k_AccuracyKey, out floatResult, 1f))
                accuracy = floatResult;

            // Read and start coroutines if relevant
            if (reader.TryReadValue(k_DrawTimerKey, out floatResult, 0f))
                m_BlockingCoroutine = StartCoroutine(DrawCoroutine(floatResult));
            if (reader.TryReadValue(k_ThrowDelayKey, out floatResult, 0f))
            {
                reader.TryReadValue(k_StrongKey, out m_Strong, false);
                if (m_Strong)
                {
                    // Calculate duration based on how far along it is
                    float duration = m_ThrowDurationStrong - m_SpawnTimeStrong + floatResult;
                    // Strong throw with new delay
                    m_BlockingCoroutine = StartCoroutine(ThrowCoroutine(duration, floatResult, m_ThrowSpeedStrong, m_ProjectileSpawnPointStrong));
                }
                else
                {
                    // Calculate duration based on how far along it is
                    float duration = m_ThrowDurationWeak - m_SpawnTimeWeak + floatResult;
                    // Weak throw with new delay
                    m_BlockingCoroutine = StartCoroutine(ThrowCoroutine(duration, floatResult, m_ThrowSpeedWeak, m_ProjectileSpawnPointWeak));
                }
            }
        }

        #endregion
    }
}