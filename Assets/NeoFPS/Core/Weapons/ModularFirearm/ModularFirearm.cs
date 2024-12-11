using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NeoFPS.Constants;
using NeoSaveGames.Serialization;
using NeoSaveGames;

namespace NeoFPS.ModularFirearms
{
    using Object = UnityEngine.Object;

    [DisallowMultipleComponent]
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-modularfirearm.html")]
    [RequireComponent(typeof(AudioSource))]
    public class ModularFirearm : MonoBehaviour, IModularFirearm, IWieldable, IAimable, IDamageSource, ICrosshairDriver, IPoseHandler, IModularFirearmPayloadReceiver, INeoSerializableComponent
    {
        [SerializeField, Tooltip("Does the firearm need to be in a character's inventory. If so, and no wielding character is found, the firearm will be destroyed.")]
        private bool m_RequiresWielder = true;

        [Header ("Animation")]

        [SerializeField, Tooltip("What animators should be exposed to the weapon components,")]
        private AnimatorLocation m_AnimatorLocation = AnimatorLocation.AttachedOnly;

        [SerializeField, NeoObjectInHierarchyField(true), Tooltip("The weapon geometry animator (optional).")]
		private Animator m_Animator = null;

        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Trigger, true, true), Tooltip("The trigger for the fire animation (blank = no animation).")]
		private string m_FireAnimTrigger = "Fire";

        [SerializeField, NeoObjectInHierarchyField(true), Tooltip("The transform to move when applying pose offsets. Default is the root of the firearm, but you may want to change this for certain effects.")]
        private Transform m_PoseTransform = null;

        [Header("Accuracy")]

        [SerializeField, Tooltip("The speed above which your accuracy hits zero.")]
        private float m_ZeroAccuracySpeed = 15f;

        [SerializeField, Range(0f, 1f), Tooltip("Smoothes changes in accuracy during sudden speed changes.")]
        private float m_MoveAccuracyDamping = 0.5f;

        [SerializeField, Range(0f, 1f), Tooltip("A multiplier for move accuracy when airborne.")]
        private float m_AirAccuracy = 0f;

        [Header ("Raise / Lower")]

		[SerializeField, Tooltip("The delay type for raising the weapon (you can use FirearmAnimEventsHandler to tie delays to animation)")]
		private FirearmDelayType m_RaiseDelayType = FirearmDelayType.ElapsedTime;

		[SerializeField, Tooltip("The duration in seconds for raising the weapon.")]
		private float m_RaiseDuration = 0.5f;

		[SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Trigger, true, true), Tooltip("The trigger for the weapon raise animation (blank = no animation).")]
		private string m_RaiseAnimTrigger = "Draw";

        [SerializeField, Tooltip("The delay type for lowering the weapon (you can use FirearmAnimEventsHandler to tie delays to animation)")]
        private FirearmDelayType m_LowerDelayType = FirearmDelayType.ElapsedTime;        

        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Trigger, true, true), Tooltip("The trigger for the weapon lower animation (blank = no animation).")]
        private string m_LowerAnimTrigger = string.Empty;

        [SerializeField, Tooltip("The time taken to lower the item on deselection.")]
        private float m_DeselectDuration = 0f;

        private Waitable m_DeselectionWaitable = null;
        private int m_FireAnimTriggerHash = -1;
		private int m_RaiseAnimTriggerHash = -1;
        private int m_LowerAnimTriggerHash = -1;
        private float m_RaiseTimer = 0f;
        private bool m_WaitingForExternalTrigger = false;

        public class TimedDeselectionWaitable : Waitable
        {
            private float m_Duration = 0f;
            private float m_StartTime = 0f;

            public TimedDeselectionWaitable(float duration)
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

        public class TriggeredDeselectionWaitable : Waitable
        {
            private bool m_WaitingOnTrigger = false;

            public TriggeredDeselectionWaitable()
            {
                m_WaitingOnTrigger = false;
            }

            public void ResetTrigger()
            {
                m_WaitingOnTrigger = true;
            }

            public void SignalCompleted()
            {
                m_WaitingOnTrigger = false;
            }

            protected override bool CheckComplete()
            {
                return !m_WaitingOnTrigger;
            }
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            if (m_Animator == null)
                m_Animator = GetComponentInChildren<Animator>();

            if (m_PoseTransform == null)
                m_PoseTransform = transform;

            if (m_RaiseDuration < 0f)
                m_RaiseDuration = 0f;
        }
#endif

        protected virtual void Awake ()
		{
            animationHandler = new NullAnimatorHandler();

			// Initialise subsystems
			InitialiseGeometry ();

			// Get animation trigger hashes
			if (m_FireAnimTrigger != string.Empty)
				m_FireAnimTriggerHash = Animator.StringToHash (m_FireAnimTrigger);
			else
				m_FireAnimTriggerHash = -1;

			if (m_RaiseAnimTrigger != string.Empty)
				m_RaiseAnimTriggerHash = Animator.StringToHash (m_RaiseAnimTrigger);
			else
				m_RaiseAnimTriggerHash = -1;

            if (m_LowerAnimTrigger != string.Empty)
                m_LowerAnimTriggerHash = Animator.StringToHash(m_LowerAnimTrigger);
            else
                m_LowerAnimTriggerHash = -1;

            // Get the audio source
            m_AudioSource = GetComponent<AudioSource>();
			
            // Set up deselection waitable
            switch(m_LowerDelayType)
            {
                case FirearmDelayType.ElapsedTime:
                    if (m_DeselectDuration > 0.001f)
                        m_DeselectionWaitable = new TimedDeselectionWaitable(m_DeselectDuration);
                    break;
                case FirearmDelayType.ExternalTrigger:
                    m_DeselectionWaitable = new TriggeredDeselectionWaitable();
                    break;
            }

            // Set to always animate (since it's first person - some people were getting bugs on raise/lower)
            if (m_Animator != null)
                m_Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Set up pose handler
            m_PoseHandler = new PoseHandler(m_PoseTransform, GetComponent<NeoSerializedGameObject>());

            // Calculate aim offsets (if required)
            if (!m_AimOffsetsCalculated)
                CalculateAimRelativeOffsets();
        }

        protected IEnumerator Start ()
        {
            if (wielder == null && m_RequiresWielder)
            {
                Destroy(gameObject);
                yield break;
            }

            ApplyStartupPayloadImmediate();

            OnStart();

            yield return new WaitForEndOfFrame();

            ApplyStartupPayloadDeferred();
        }

        protected virtual void OnStart() { }

		protected virtual void Update ()
		{
            // Aiming
            // aimToggleHold.SetInput (m_AimHeld);
            // m_AimHeld = false;

            // Update pose
            m_PoseHandler.UpdatePose();
        }

        protected void FixedUpdate ()
		{
			// Check movement accuracy
            if (wielder != null && wielder.motionController != null)
            {
                var characterController = wielder.motionController.characterController;

                // Get target move accuracy
                float targetMoveAccuracy = 1f - Mathf.Clamp01(characterController.velocity.magnitude / m_ZeroAccuracySpeed);
                if (!characterController.isGrounded)
                    targetMoveAccuracy *= m_AirAccuracy;

                // Get damped (and snapped) accuracy
                float moveAccuracy = Mathf.Lerp(moveAccuracyModifier, targetMoveAccuracy, Mathf.Lerp(0.75f, 0.05f, m_MoveAccuracyDamping));
                if (moveAccuracy > 0.999f)
                    moveAccuracy = 1f;
                if (moveAccuracy < 0.001f)
                    moveAccuracy = 0f;

                // Apply
                moveAccuracyModifier = moveAccuracy;
            }
            else
            {
                moveAccuracyModifier = 1f;
            }

			// Recover accuracy
			if (currentAccuracy < 1f && recoilHandler != null)
			{
                if (aimToggleHold.on)
                    currentAccuracy += Time.deltaTime * recoilHandler.sightedAccuracyRecover;
				else
					currentAccuracy += Time.deltaTime * recoilHandler.hipAccuracyRecover;
			}
		}

		#region IModularFirearm IMPLEMENTATION

		public ITrigger trigger { get; private set; }
		public IShooter shooter { get; private set; }
		public IAmmo ammo { get; private set; }
		public IReloader reloader { get; private set; }
		public IAimer aimer { get; private set; }
		public IEjector ejector { get; private set; }
		public IMuzzleEffect muzzleEffect { get; private set; }
        public IRecoilHandler recoilHandler { get; private set; }

		public event UnityAction<IModularFirearm, ITrigger> onTriggerChange;
		public event UnityAction<IModularFirearm, IShooter> onShooterChange;
		public event UnityAction<IModularFirearm, IAmmo> onAmmoChange;
		public event UnityAction<IModularFirearm, IReloader> onReloaderChange;
		public event UnityAction<IModularFirearm, IAimer> onAimerChange;
        public event UnityAction<IModularFirearm, string> onModeChange;

        public Animator animator { get { return m_Animator; } }
        public IWieldableAnimationHandler animationHandler { get; private set; }

        public void SetTrigger (ITrigger to)
		{
			if (trigger != to)
			{
                bool pressed = false;

				// Disable previous trigger
				if (trigger as Component != null)
				{
                    // Record and remove block
                    trigger.blocked = false;
                    // Record and remove pressed
                    pressed = trigger.pressed;
                    if (pressed)
                        trigger.Release();
                    // Remove event handlers
                    trigger.onShoot -= Shoot;
                    trigger.onShootContinuousChanged -= ShootContinuous;
					// Disable
					trigger.Disable ();
				}

				// Set new trigger
				trigger = to;

				// Enable new trigger
				if (trigger != null)
				{
					// Add event handlers
					trigger.onShoot += Shoot;
                    trigger.onShootContinuousChanged += ShootContinuous;
                    // Enable
                    trigger.Enable ();
					// Add block if required
					trigger.blocked = (m_TriggerBlockers.Count > 0);
                    // Press if required
                    if (pressed)
                        trigger.Press();
				}

				// Fire event
				if (onTriggerChange != null)
					onTriggerChange (this, trigger);
			}
		}

		public void SetShooter (IShooter to)
		{		
			if (shooter != to)
			{
				// Disable previous shooter
				if (shooter as Component != null)
					shooter.Disable ();

				// Set new shooter
				shooter = to;

				// Enable new shooter
				if (shooter != null)
					shooter.Enable ();

				// Fire event
				if (onShooterChange != null)
					onShooterChange (this, shooter);
			}
		}

		public void SetAmmo (IAmmo to)
		{
			if (ammo != to)
			{
				// Disable previous ammo
				if (ammo as Component != null)
					ammo.Disable ();

				// Set new ammo
				ammo = to;

				// Enable new ammo
				if (ammo != null)
					ammo.Enable ();

				// Fire event
				if (onAmmoChange != null)
					onAmmoChange (this, ammo);
			}
		}

		public void SetReloader (IReloader to)
		{
			if (reloader != to)
			{
                // Record existing magazine count
                bool transfer = m_PersistMagCount;
                int magazine = 0;

                // Disable previous reloader
                if (reloader as Component != null)
                {
                    if (transfer)
                        magazine = reloader.currentMagazine;

                    if (m_DryFireOnceOnly)
                        reloader.onCurrentMagazineChange -= OnCurrentMagazineChange;
                    reloader.Disable();
                }
                else
                    transfer = false;

				// Set new reloader
				reloader = to;

                // Enable new reloader
                if (reloader != null)
                {
                    reloader.Enable();

                    if (transfer)
                    {
                        int excess = magazine - reloader.magazineSize;
                        if (excess > 0)
                        {
                            reloader.currentMagazine = magazine - excess;
                            if (ammo != null)
                                ammo.IncrementAmmo(excess);
                        }
                        else
                            reloader.currentMagazine = magazine;
                    }

                    if (m_DryFireOnceOnly)
                    {
                        reloader.onCurrentMagazineChange += OnCurrentMagazineChange;
                        OnCurrentMagazineChange(this, reloader.currentMagazine);
                    }
                }

				// Fire event
				if (onReloaderChange != null)
					onReloaderChange (this, reloader);
			}
		}

		public void SetAimer (IAimer to)
		{
            if (aimer != to)
            {
                // Disable previous aimer
                if (aimer as Component != null)
                {
                    aimer.StopAimInstant();
                    aimer.Disable();
                    aimer.onCrosshairChange -= OnCrosshairChanged;
                    aimer.onAimStateChanged -= OnAimStateChanged;
                }

                // Set new aimer
                aimer = to;

                // Enable new aimer
                if (aimer != null)
                {
                    aimer.Enable();
                    aimer.onCrosshairChange += OnCrosshairChanged;
                    aimer.onAimStateChanged += OnAimStateChanged;
                    OnCrosshairChanged(aimer.crosshair);
                    if (aimToggleHold.on)
                        aimer.Aim();
                }

                // Fire event
                if (onAimerChange != null)
                    onAimerChange(this, aimer);
            }
		}

		public void SetEjector (IEjector to)
		{
			if (ejector != to)
			{
				// Disable previous ejector
				if (ejector as Component != null)
					ejector.Disable ();

				// Set new ejector
				ejector = to;

				// Enable new ejector
				if (ejector != null)
					ejector.Enable ();
			}
		}

		public void SetMuzzleEffect (IMuzzleEffect to)
		{
			if (muzzleEffect != to)
			{
				// Disable previous muzzle effect
				if (muzzleEffect as Component != null)
					muzzleEffect.Disable ();

				// Set new muzzle effect
				muzzleEffect = to;

				// Enable new muzzle effect
				if (muzzleEffect != null)
					muzzleEffect.Enable ();
			}
		}

        public void SetHandling(IRecoilHandler to)
        {
            if (recoilHandler != to)
            {
                // Disable previous muzzle effect
                if (recoilHandler as Component != null)
                    recoilHandler.Disable();

                // Set new muzzle effect
                recoilHandler = to;

                // Enable new muzzle effect
                if (recoilHandler != null)
                    recoilHandler.Enable();
            }
        }

        public FirearmDelayType raiseDelayType
        {
            get { return m_RaiseDelayType; }
        }

        public FirearmDelayType lowerDelayType
        {
            get { return m_RaiseDelayType; }
        }

        public void ManualWeaponRaised ()
		{
			if (m_RaiseDelayType != FirearmDelayType.ExternalTrigger)
			{
				Debug.LogError ("Attempting to manually signal weapon raised when delay type is not set to external trigger");
				return;
			}
			if (!m_WaitingForExternalTrigger)
			{
				Debug.LogError ("Attempting to manually signal weapon raised, not expected");
				return;
			}
			m_WaitingForExternalTrigger = false;
		}

        public void ManualWeaponLowered()
        {
            if (m_DeselectionWaitable is TriggeredDeselectionWaitable trigger)
                trigger.SignalCompleted();
            else
                Debug.LogError("Attempting to manually signal weapon lowered when delay type is not set to external trigger");
        }

        public void SetRecoilMultiplier(float move, float rotation)
        {
            if (recoilHandler != null)
                recoilHandler.SetRecoilMultiplier(move, rotation);
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

                        switch(animators.Count)
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

                        var characterAnimator = wielder?.bodyAnimator;
                        if (characterAnimator != null)
                            animationHandler = new SingleAnimatorHandler(characterAnimator);
                        else
                            animationHandler = new NullAnimatorHandler();
                    }
                    break;
            }
        }

        #endregion

        #region IAimable IMPLEMENTATION

        [Header("Aiming")]

        [SerializeField, Tooltip("The aim relative transform is the transform of the gun (mesh or bone) itself, which the optics are parented to. This allows a consistent offset to be calculated, even as the weapon is animated by keyframed or procedural spring animations")]
        private Transform m_AimRelativeTransform = null;

        private bool m_AimOffsetsCalculated = false;

        public Transform aimRelativeTransform
        {
            get { return m_AimRelativeTransform; }
        }

        public Vector3 aimRelativePosition
        {
            get;
            private set;
        }

        public Quaternion aimRelativeRotation
        {
            get;
            private set;
        }

        void CalculateAimRelativeOffsets()
        {
            if (m_AimRelativeTransform != null)
            {
                // Get the aim pose based on root of weapon
                Quaternion inverse = Quaternion.Inverse(transform.rotation);
                aimRelativeRotation = Quaternion.Inverse(inverse * m_AimRelativeTransform.rotation);
                aimRelativePosition = aimRelativeRotation * -transform.InverseTransformPoint(m_AimRelativeTransform.position);
            }
            else
            {
                aimRelativePosition = Vector3.zero;
                aimRelativeRotation = Quaternion.identity;
            }
            m_AimOffsetsCalculated = true;
        }

        #endregion

        #region ICrosshairDriver IMPLEMENTATION

        private bool m_HideCrosshair = false;
        private float m_MinAccuracy = 0f;
        private float m_MaxAccuracy = 1f;

        public event UnityAction<FpsCrosshair> onCrosshairChanged;

        public float accuracy
        {
            get { return Mathf.Clamp(m_CurrentAccuracy * m_MoveAccuracyModifier * currentAimerAccuracy, m_MinAccuracy, m_MaxAccuracy); }
        }

        public float minAccuracy
        {
            get { return m_MinAccuracy; }
            set
            { 
                m_MinAccuracy = Mathf.Clamp(value, 0f, m_MaxAccuracy);
                if (onAccuracyChanged != null)
                    onAccuracyChanged(accuracy);
            }
        }

        public float maxAccuracy
        {
            get { return m_MaxAccuracy; }
            set
            {
                m_MaxAccuracy = Mathf.Clamp(value, m_MinAccuracy, 1f);
                if (onAccuracyChanged != null)
                    onAccuracyChanged(accuracy);
            }
        }

        public FpsCrosshair crosshair
        {
            get
            {
                if (m_HideCrosshair)
                    return FpsCrosshair.None;
                else
                {
                    if (aimer != null)
                        return aimer.crosshair;
                    else
                        return FpsCrosshair.Default;
                }
            }
        }

        void OnCrosshairChanged(FpsCrosshair c)
        {
            if (!m_HideCrosshair && onCrosshairChanged != null)
                onCrosshairChanged(c);
        }

        public void HideCrosshair()
        {
            if (!m_HideCrosshair)
            {
                bool triggerEvent = (onCrosshairChanged != null && crosshair != FpsCrosshair.None);

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

        #region IDamageSource IMPLEMENTATION

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

        #region IModularFirearmPayloadReceiver IMPLEMENTATION

        private ModularFirearmPayload m_Payload = null;

        public void SetStartupPayload(ModularFirearmPayload payload)
        {
            m_Payload = payload;
        }

        void ApplyStartupPayloadImmediate()
        {
            if (m_Payload != null)
                m_Payload.ApplyToFirearmImmediate(this);
        }

        void ApplyStartupPayloadDeferred()
        {
            if (m_Payload != null)
                m_Payload.ApplyToFirearmDeferred(this);
        }

        #endregion

        #region SHOOTING

        bool m_ContinuousShooting = false;
        WaitForFixedUpdate m_FixedYield = new WaitForFixedUpdate();

        private void Shoot ()
		{
			if (reloader != null && reloader.empty)
			{
				if (!FpsSettings.gameplay.autoReload || Reload () == false)
                    PlayDryFireSound ();
                return;
            }

            if (reloading && reloader.interruptable)
            {
                reloader.Interrupt();
                return;
            }

			// Shoot
			if (shooter != null)
				shooter.Shoot (accuracy, ammo.effect);

			// Play animation
			if (m_FireAnimTriggerHash != -1 && animationHandler.isValid)
				animationHandler.SetTrigger (m_FireAnimTriggerHash);

			// Handle recoil
            if (recoilHandler != null)
			    recoilHandler.Recoil ();

			// Show the muzzle effect & play firing sound
			if (muzzleEffect != null)
				muzzleEffect.Fire ();

			// Eject shell
			if (ejector != null && ejector.ejectOnFire)
				ejector.Eject ();

            // Decrease the accuracy
            if (recoilHandler != null)
            {
                if (aimToggleHold.on)
                    currentAccuracy -= recoilHandler.sightedAccuracyKick;
                else
                    currentAccuracy -= recoilHandler.hipAccuracyKick;
            }

            // Decrement ammo
            if (reloader != null)
                reloader.DecrementMag(1);
        }

        void ShootContinuous(bool shoot)
        {
            if (shoot)
            {
                if (reloader != null && reloader.empty)
                {
                    if (Reload() == false)
                        PlayDryFireSound();
                    return;
                }

                if (reloading && reloader.interruptable)
                {
                    reloader.Interrupt();
                    return;
                }

                if (!m_ContinuousShooting)
                {
                    m_ContinuousShooting = true;
                    StartCoroutine(ShootCoroutine());
                }
            }
            else
            {
                m_ContinuousShooting = false;
            }
        }

        IEnumerator ShootCoroutine()
        {
            // Show muzzle effect
            if (muzzleEffect != null)
                muzzleEffect.FireContinuous();

            while (m_ContinuousShooting)
            {
                if (reloader != null && reloader.empty)
                    m_ContinuousShooting = false;
                else
                {
                    // Shoot
                    if (shooter != null)
                        shooter.Shoot(accuracy, ammo.effect);

                    // Handle recoil
                    if (recoilHandler != null)
                        recoilHandler.Recoil();

                    // Decrease the accuracy
                    if (recoilHandler != null)
                    {
                        if (aimToggleHold.on)
                            currentAccuracy -= recoilHandler.sightedAccuracyKick;
                        else
                            currentAccuracy -= recoilHandler.hipAccuracyKick;
                    }

                    // Decrement ammo
                    reloader.DecrementMag(1);
                }
                yield return m_FixedYield;
            }

            if (muzzleEffect != null)
                muzzleEffect.StopContinuous();

            // Save/Load required
        }

        #endregion

        #region TRIGGER BLOCKING

        List<Object> m_TriggerBlockers = new List<Object>(2);

        public void AddTriggerBlocker(Object o)
        {
            if (!m_TriggerBlockers.Contains(o) && o != null)
                m_TriggerBlockers.Add(o);

            if (trigger != null)
                trigger.blocked = true;
        }

        public void RemoveTriggerBlocker(Object o)
        {
            int index = m_TriggerBlockers.LastIndexOf(o);
            if (index != -1)
            {
                m_TriggerBlockers.RemoveAt(index);
                if (trigger != null)
                    trigger.blocked = (m_TriggerBlockers.Count > 0);
            }
        }

        #endregion

        #region MODE SWITCHING

        private IModularFirearmModeSwitcher m_ModeSwitcher = null;
                
        public virtual string mode
        {
            get
            {
                if (m_ModeSwitcher != null)
                    return m_ModeSwitcher.currentMode;
                else
                    return string.Empty;
            }
        }

        public IModularFirearmModeSwitcher modeSwitcher
        {
            get { return m_ModeSwitcher; }
            set
            {
                // Unsubscribe from old
                if (m_ModeSwitcher != null)
                {
                    m_ModeSwitcher.onSwitchModes -= OnModeChange;
                }

                // Assign new
                m_ModeSwitcher = value;

                // Subscribe & get starting mode or signal change
                if (m_ModeSwitcher != null)
                {
                    m_ModeSwitcher.onSwitchModes += OnModeChange;
                    m_ModeSwitcher.GetStartingMode();
                }
                else
                    OnModeChange();
            }
        }

        public virtual void SwitchMode()
        {
            if (m_ModeSwitcher != null)
                m_ModeSwitcher.SwitchModes();
        }

        protected void OnModeChange()
        {
            if (onModeChange != null)
                onModeChange(this, mode);
        }

        #endregion

        #region ACCURACY

        public event UnityAction<float> onAccuracyChanged;
        
        private float m_MoveAccuracyModifier = 0f;
		private float moveAccuracyModifier
		{
			get { return m_MoveAccuracyModifier; }
			set 
			{
				float to = Mathf.Clamp01 (value);
				if (m_MoveAccuracyModifier != to)
				{
					m_MoveAccuracyModifier = to;
                    if (onAccuracyChanged != null)
                        onAccuracyChanged(accuracy);
                }
			}
		}

		private float m_CurrentAccuracy = 1f;
		private float currentAccuracy
		{
			get { return m_CurrentAccuracy; }
			set 
			{
				float to = Mathf.Clamp01 (value);
				if (m_CurrentAccuracy != to)
				{
					m_CurrentAccuracy = to;
					if (onAccuracyChanged != null)
                        onAccuracyChanged(accuracy);
				}
			}
		}

        private float currentAimerAccuracy
        {
            get
            {
                if (aimer == null)
                    return 1f;
                else
                {
                    if (aimer.isAiming)
                        return aimer.aimedAccuracyCap;
                    else
                        return aimer.hipAccuracyCap;
                }
            }
        }

        void OnAimStateChanged(IModularFirearm f, FirearmAimState s)
        {
            if (onAccuracyChanged != null)
                onAccuracyChanged(accuracy);
        }

        #endregion

        #region SELECTION

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
                    InitialiseAnimationHandler();
                }
            }
		}

        protected void OnEnable ()
		{
			wielder = GetComponentInParent<ICharacter>();
        }

        protected void OnDisable ()
		{
            // Stop selection coroutine
            if (m_OnSelectCoroutine != null)
            {
                StopCoroutine(m_OnSelectCoroutine);
                m_OnSelectCoroutine = null;
            }
            // Stop aiming
            aimToggleHold.on = false;
            if (aimer != null)
                aimer.StopAimInstant();
            // Stop reloading
            reloading = false;
            RemoveTriggerBlocker(reloader as UnityEngine.Object);
            // Reset pose
            m_PoseHandler.OnDisable();
        }

        protected void OnDestroy()
        {
            // Unset the wielder (may be dropped)
            wielder = null;
        }

        private Coroutine m_OnSelectCoroutine = null;
        private IEnumerator OnSelectCoroutine (float timer)
		{
            ShowGeometry ();

            // Show draw animation
            if (m_RaiseAnimTriggerHash != -1)
                animationHandler.SetTrigger(m_RaiseAnimTriggerHash);

            // Advance one frame
            yield return null;

            // Block the trigger
            AddTriggerBlocker(this);

            // Delay completion
            switch (m_RaiseDelayType)
            {
                case FirearmDelayType.ElapsedTime:
                    m_RaiseTimer = timer;
                    while (m_RaiseTimer > 0f)
                    {
                        yield return null;
                        m_RaiseTimer -= Time.deltaTime;
                    }
                    break;
                case FirearmDelayType.ExternalTrigger:
                    m_WaitingForExternalTrigger = true;
                    while (m_WaitingForExternalTrigger)
                        yield return null;
                    break;
            }

            // Unblock the trigger
            RemoveTriggerBlocker(this);

            m_OnSelectCoroutine = null;
			// NB: For raise animation, set it as the entry state of the weapon geo's animation controller. This means
			// it will be triggered automatically at this point and does not need triggering explicitly
		}

        public void Select()
        {
            if (wielder != null || !m_RequiresWielder)
            {
                PlayWeaponRaiseSound();

                currentAccuracy = 0.75f;

                // Play raise animation
                if (m_RaiseAnimTriggerHash != -1)
                    animationHandler.SetTrigger(m_RaiseAnimTriggerHash);

                m_OnSelectCoroutine = StartCoroutine(OnSelectCoroutine(m_RaiseDuration));
            }
        }

        public void DeselectInstant()
        {
            // Block the trigger
            AddTriggerBlocker(this);
        }

        public Waitable Deselect()
        {
            // Block the trigger
            AddTriggerBlocker(this);

            // Play lower animation
            if (m_LowerAnimTriggerHash != -1)
                animationHandler.SetTrigger(m_LowerAnimTriggerHash);

            // Wait for deselection
            if (m_DeselectionWaitable is TimedDeselectionWaitable timer)
                timer.ResetTimer();
            else
            {
                if (m_DeselectionWaitable is TriggeredDeselectionWaitable triggered)
                    triggered.ResetTrigger();
            }

            return m_DeselectionWaitable;
        }

        #endregion

        #region INPUT

        [Serializable]
        private class ToggleHoldAim : ToggleOrHold
		{
			public ModularFirearm firearm { get; set; }

			protected override void OnActivate ()
			{
                if (firearm.aimer as MonoBehaviour != null)
                    firearm.aimer.Aim();
            }
            protected override void OnDeactivate()
            {
                if (firearm.aimer as MonoBehaviour != null)
                    firearm.aimer.StopAim();
            }

            public ToggleHoldAim(ModularFirearm f) : base(f.IsAimBlocked)
            {
                firearm = f;
            }
        }

        private ToggleHoldAim m_AimToggleHold = null;
        public ToggleOrHold aimToggleHold
        {
            get
            {
                if (m_AimToggleHold == null)
                    m_AimToggleHold = new ToggleHoldAim(this);
                return m_AimToggleHold;
            }
        }

        bool IsAimBlocked()
        {
            // Check if disabled
            if (enabled == false)
                return true;

            // Check if no aimer 
            if (aimer == null)
                return true;

            // Check if weapon is blocked
            if (m_Blockers.Count > 0 || m_OnSelectCoroutine != null)
                return true;

            // Check if no aim blockers
            if (m_AimBlockers.Count > 0)
                return true;

            // Check if currently selecting
            if (m_OnSelectCoroutine != null)
                return true;

            // Check if currently deselecting
            if (m_DeselectionWaitable != null && !m_DeselectionWaitable.isComplete)
                return true;

            //Check if reloading and can't aim during
            if (reloading && !aimer.canAimWhileReloading)
                return true;

            return false;
        }

        List<Object> m_AimBlockers = new List<Object>(2);

        public void AddAimBlocker(Object o)
        {
            if (!m_AimBlockers.Contains(o) && o != null)
                m_AimBlockers.Add(o);
        }

        public void RemoveAimBlocker(Object o)
        {
            int index = m_AimBlockers.LastIndexOf(o);
            if (index != -1)
                m_AimBlockers.RemoveAt(index);
        }

        #endregion

        #region RELOAD

        [Header("Reload")]

        [SerializeField, Tooltip("When switching reloader modules, should the ammo in the magazine be transferred from one to the other?")]
        private bool m_PersistMagCount = false;

        public bool reloading
        {
            get;
            private set;
        }

		public bool Reload ()
        {
            if (reloader != null)
            {
                if (isBlocked)
                    return false;
                if (reloader.isReloading)
					return false;
                if (!reloader.canReload)
					return false;

                StartCoroutine (ReloadCoroutine ());
                return true;
			}
			else
				return false;
		}

		IEnumerator ReloadCoroutine ()
        {
            yield return null;
            // Initiate reload (waitable is an object that can be yielded to)
            Waitable reload = reloader.Reload ();
            var reloadLock = reloader as UnityEngine.Object;
            if (reload != null)
            {
                reloading = !reload.isComplete;

                // Check if reloading (will be false for full mag, or already reloading, etc).
                if (reloading)
                {
                    // Block the trigger
                    AddTriggerBlocker(reloadLock);

                    yield return reload;
                }
                else
                    yield return null;
            }
            else
                yield return null;

            // Complete
            reloading = false;

            // Unblock the aimer
            if (aimer != null && !aimer.canAimWhileReloading)
            {
                // Wait and then unblock the trigger
                if (aimToggleHold.on)
                {
                    float delay = aimer.aimUpDuration;
                    while (delay > 0f)
                    {
                        yield return null;
                        delay -= Time.deltaTime;
                    }
                }
            }

            // Unblock the trigger
            RemoveTriggerBlocker(reloadLock);
        }

		#endregion

		#region GEOMETRY

		private MeshRenderer[] m_MeshRenderers = null;
        private SkinnedMeshRenderer[] m_SkinnedRenderers = null;

        void InitialiseGeometry ()
		{
			m_MeshRenderers = GetComponentsInChildren<MeshRenderer> (true);
			m_SkinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer> (true);
		}

		public void HideGeometry ()
		{
			for (int i = 0; i < m_MeshRenderers.Length; ++i)
				m_MeshRenderers [i].enabled = false;
			for (int i = 0; i < m_SkinnedRenderers.Length; ++i)
				m_SkinnedRenderers [i].enabled = false;
		}

		public void ShowGeometry ()
		{
			for (int i = 0; i < m_MeshRenderers.Length; ++i)
				m_MeshRenderers [i].enabled = true;
			for (int i = 0; i < m_SkinnedRenderers.Length; ++i)
				m_SkinnedRenderers [i].enabled = true;
		}

		#endregion

		#region AUDIO

		[Header ("Audio")]
		[Tooltip("The audio clip to play if attempting to fire while empty.")]
        [SerializeField] private AudioClip m_DryFireSound = null;
        [Tooltip("Should the dry fire only be played once until the weapon is reloaded.")]
        [SerializeField] private bool m_DryFireOnceOnly = false;
        [Tooltip("The audio clip to play when the weapon is drawn.")]
        [SerializeField] private AudioClip m_WeaponRaiseSound = null;
        [Tooltip("The volume of all weapon sounds (includes gunshots and reload - the muzzle effects and reloader modules have their own volume sliders that stack with this).")]
        [SerializeField, Range(0f, 1f)] private float m_WeaponVolume = 1f;

        private AudioSource m_AudioSource = null;
        private bool m_DidDryFire = false;

        public void PlaySound (AudioClip clip, float volume = 1f)
		{
            if (wielder != null && wielder.audioHandler != null)
                wielder.audioHandler.PlayClip(clip, volume);
            else
            {
                if (m_AudioSource != null && m_AudioSource.isActiveAndEnabled)
                    m_AudioSource.PlayOneShot(clip, volume * m_WeaponVolume);
            }
		}
        
        void PlayDryFireSound ()
        {
            if (m_DryFireOnceOnly)
            {
                if (!m_DidDryFire)
                {
                    if (m_DryFireSound != null)
                        PlaySound(m_DryFireSound);
                    m_TriggerBlockers.Add(this);
                    m_DidDryFire = true;
                }
            }
            else
            {
                if (m_DryFireSound != null)
                    PlaySound(m_DryFireSound);
            }
        }

        void PlayWeaponRaiseSound()
        {
            // Play directly as fast switching means you don't want it to continue if the weapon is disabled
            if (m_WeaponRaiseSound != null && m_AudioSource != null && m_AudioSource.isActiveAndEnabled)
                m_AudioSource.PlayOneShot(m_WeaponRaiseSound, m_WeaponVolume);
        }

        void OnCurrentMagazineChange(IModularFirearm firearm, int count)
        {
            if (count > 0 && m_DidDryFire)
            {
                m_TriggerBlockers.Remove(this);
                m_DidDryFire = false;
            }
        }

        #endregion

        #region BLOCKING

        private List<Object> m_Blockers = new List<Object>();

        public event UnityAction<bool> onBlockedChanged;

        public bool isBlocked
        {
            get { return m_Blockers.Count > 0 || m_OnSelectCoroutine != null || (reloader != null && reloader.isReloading); }
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

        PoseHandler GetPoseHandler()
        {
            // Set up pose handler
            if (m_PoseHandler == null)
                m_PoseHandler = new PoseHandler(m_PoseTransform, GetComponent<NeoSerializedGameObject>());
            return m_PoseHandler;
        }

        public void PushPose(PoseInformation pose, MonoBehaviour owner, float blendTime, int priority = 0)
        {
            GetPoseHandler().PushPose(pose, owner, blendTime, priority);
        }

        public void PopPose(MonoBehaviour owner, float blendTime)
        {
            GetPoseHandler().PopPose(owner, blendTime);
        }

        public PoseInformation GetPose(MonoBehaviour owner)
        {
            return GetPoseHandler().GetPose(owner);
        }

        #endregion

        #region INeoSerializableComponent IMPLEMENTATION

        private static readonly NeoSerializationKey k_MoveAccuracyKey = new NeoSerializationKey("moveAccuracy");
        private static readonly NeoSerializationKey k_CurrentAccuracyKey = new NeoSerializationKey("currentAccuracy");
        private static readonly NeoSerializationKey k_ReloadingKey = new NeoSerializationKey("reloading");
        private static readonly NeoSerializationKey k_AimKey = new NeoSerializationKey("aim");
        private static readonly NeoSerializationKey k_RaiseTimerKey = new NeoSerializationKey("raiseTimer");
        private static readonly NeoSerializationKey k_AimPositionKey = new NeoSerializationKey("aimPosition");
        private static readonly NeoSerializationKey k_AimRotationKey = new NeoSerializationKey("aimRotation");

        public virtual void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            if (saveMode == SaveMode.Default)
            {
                // Write accuracy
                writer.WriteValue(k_MoveAccuracyKey, moveAccuracyModifier);
                writer.WriteValue(k_CurrentAccuracyKey, currentAccuracy);

                // Write if reloading
                writer.WriteValue(k_ReloadingKey, reloading);

                // Write draw state
                if (m_OnSelectCoroutine != null)
                    writer.WriteValue(k_RaiseTimerKey, m_RaiseTimer);

                // Write aim state
                aimToggleHold.WriteProperties(writer, k_AimKey, false);

                // Write aim offsets
                if (m_AimOffsetsCalculated)
                {
                    writer.WriteValue(k_AimPositionKey, aimRelativePosition);
                    writer.WriteValue(k_AimRotationKey, aimRelativeRotation);
                }

                // Write pose
                GetPoseHandler().WriteProperties(writer);
            }
        }

        public virtual void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            // Read accuracy
            float floatResult = 0f;
            if (reader.TryReadValue(k_MoveAccuracyKey, out floatResult, 1f))
                moveAccuracyModifier = floatResult;
            if (reader.TryReadValue(k_CurrentAccuracyKey, out floatResult, 1f))
                currentAccuracy = floatResult;
            moveAccuracyModifier = floatResult;

            // Check if reloading
            bool boolResult = false;
            if (reader.TryReadValue(k_ReloadingKey, out boolResult, false) && boolResult)
            {
                StartCoroutine(ReloadCoroutine());
            }

            // Read aim offsets
            if (reader.TryReadValue(k_AimPositionKey, out Vector3 pos, Vector3.zero) &&
                reader.TryReadValue(k_AimRotationKey, out Quaternion rot, Quaternion.identity))
            {
                aimRelativePosition = pos;
                aimRelativeRotation = rot;
                m_AimOffsetsCalculated = true;
            }
            else
                m_AimOffsetsCalculated = false;

            // Read aim state
            aimToggleHold.ReadProperties(reader, k_AimKey);

            // Read draw state
            if (reader.TryReadValue(k_RaiseTimerKey, out floatResult, 0f))
                m_OnSelectCoroutine = StartCoroutine(OnSelectCoroutine(floatResult));

            // Read pose
            GetPoseHandler().ReadProperties(reader);
        }

        #endregion
    }
}