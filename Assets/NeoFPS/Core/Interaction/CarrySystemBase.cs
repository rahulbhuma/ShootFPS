using NeoSaveGames;
using NeoSaveGames.Serialization;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
	[RequireComponent(typeof(ICharacter))]
    public abstract class CarrySystemBase : MonoBehaviour, ICarrySystem, INeoSerializableComponent
	{
		[SerializeField, Min(0.5f), Tooltip("The maximum cast distance from the camera when checking for a carryable item.")]
		private float m_MaxDistance = 3f;

		[SerializeField, Range(1, 60), Tooltip("How frequently (in fixed frames) does the carry system cast forward to check for an object. Smaller numbers mean more responsive but more wasted calculations.")]
		private int m_TickRate = 1;

		[SerializeField, Min(0.1f), Tooltip("The maximum mass of the object you can carry.")]
		private float m_MassLimit = 50.1f;

        [SerializeField, Tooltip("The valid layers for carryable objects. An object can not be picked up if it is not on one of these layers.")]
        private LayerMask m_ValidCarryLayers = PhysicsFilter.Masks.CarryValid;

        [SerializeField, Tooltip("An extra set of layers that can block the raycasts to detect carryable objects. This is used to prevent picking up objects through walls or doors.")]
        private LayerMask m_BlockingLayers = PhysicsFilter.Masks.CarryBlockers;

		[Header ("Manipulation")]

        [SerializeField, Range(1f, 360f), Tooltip("The number of degrees per second to turn the anchor at full analog turn. Be warned that setting this too high might turn the anchor faster than the object can turn, so causing it to bounce back and forth as the closest direction flips.")]
		protected float m_AnalogRotateRate = 60f;

		[SerializeField, Range(0.1f, 25f), Tooltip("A multiplier applied to mouse movement when turning the anchor. Be warned that setting this too high might turn the anchor faster than the object can turn, so causing it to bounce back and forth as the closest direction flips.")]
		protected float m_MouseRotateRate = 10f;

        [SerializeField, Tooltip("A multiplier applied to mouse scroll to determing movement speed when pushing the carried object forwards/backwards.")]
        protected float m_PushScrollMultiplier = 60f;

        [SerializeField, Tooltip("The movement speed when pushing the carried object forwards/backwards.")]
        protected float m_PushDirectionalSpeed = 0.5f;

        [SerializeField, Min(0f), Tooltip("The maximum forward distance the carried object can be pushed from its default starting position")]
        protected float m_PushForwardLimit = 0.5f;

        [SerializeField, Min(0f), Tooltip("The maximum backwards distance the carried object can be pushed from its default starting position")]
        protected float m_PushBackwardLimit = 0.5f;

        [Header("Damage Interrupt")]

		[SerializeField, Tooltip("If the character recieves damage higher than this value in one go, then they will drop the object.")]
		private float m_DropOnDamage = 10f;

		[SerializeField, Tooltip("If the character recieves damage totalling this value since picking up the object, they will drop it.")]
		private float m_MaxTotalDamage = 10f;

		public event UnityAction<CarryState> onCarryStateChanged;

		private IHealthManager m_HealthManager = null;
		private float m_TotalDamage = 0f;
		private int m_TickCounter = 1;
		private RaycastHit m_HitInfo = new RaycastHit();
		private LayerMask m_CombinedMask = 0;

		protected ICharacter character
		{
			get;
			private set;
		}

		private CarryState m_CarryState = CarryState.Inactive;
		public CarryState carryState
		{
			get { return m_CarryState; }
			private set
			{
				if (m_CarryState != value)
				{
					m_CarryState = value;
					onCarryStateChanged?.Invoke(m_CarryState);
				}
			}
		}

		private Rigidbody m_CarryTarget = null;
		public Rigidbody carryTarget
		{
			get { return m_CarryTarget; }
			private set
			{
				if (m_CarryTarget != value)
                {
                    if (m_CarryTarget != null)
						RemoveCarryTarget(m_CarryTarget);

					m_CarryTarget = value;

					if (m_CarryTarget != null)
						AddCarryTarget(m_CarryTarget);
				}
			}
		}

		public float massLimit
		{
			get { return m_MassLimit; }
			protected set { m_MassLimit = value; }
		}

		protected void Awake()
		{
			character = GetComponent<ICharacter>();
			m_HealthManager = GetComponent<IHealthManager>();

        }

		protected void Start()
		{
			m_CombinedMask = m_ValidCarryLayers | m_BlockingLayers;

			if (didLoadFromSave && m_CarryTarget != null)
			{
				AddCarryTarget(m_CarryTarget);

				onCarryStateChanged?.Invoke(m_CarryState);

				if (m_CarryState == CarryState.Carrying)
					ObjectPickedUp();
            }
		}

		protected virtual void AddCarryTarget(Rigidbody target)
		{
			// Enable highlight here
		}

		protected virtual void RemoveCarryTarget(Rigidbody target)
		{
			// Disable highlight here
		}

		protected void FixedUpdate()
		{
			if (carryState == CarryState.Carrying)
			{
				if (carryTarget == null)
				{
					DropObject();
				}
				else
					TickCarryPhysics();
			}
			else
			{
				// Intermittent physics checks
				if (--m_TickCounter <= 0)
				{
					// Raycast and check hit target is valid
					bool didHit = Physics.Raycast(new Ray(character.fpCamera.aimTransform.position, character.fpCamera.aimTransform.forward), out m_HitInfo, m_MaxDistance, m_CombinedMask) // Raycast hit
						&& m_HitInfo.rigidbody != null && m_ValidCarryLayers.ContainsLayer(m_HitInfo.rigidbody.gameObject.layer); // Valid layer

					if (didHit)
					{
						if (m_HitInfo.rigidbody != carryTarget)
						{
							carryTarget = m_HitInfo.rigidbody;

							// Get valid state
							if (CanCarryTarget(m_HitInfo.rigidbody))
							{
								if (m_HitInfo.rigidbody.mass <= m_MassLimit)
									carryState = CarryState.ValidTarget;
								else
									carryState = CarryState.TargetTooHeavy;
							}
							else
								carryState = CarryState.InvalidTarget;
						}
					}
					else
					{
						carryState = CarryState.Inactive;
						carryTarget = null;
					}

					// Reset tick timer
					m_TickCounter = m_TickRate;
				}
			}
		}

		protected abstract bool CanCarryTarget(Rigidbody target);
		protected abstract Quaternion GetStartingOrientation(Quaternion current);
		protected abstract bool CanManipulate();
		protected abstract Vector3 GetOffset();
		protected abstract void OnObjectPickedUp();
		protected abstract void OnObjectDropped();
		protected abstract void AddThrowForceToObject();
		protected abstract void TickCarryPhysics();
		protected abstract void OnManipulateObject(Vector2 mouseDelta, Vector2 analogue);
		protected abstract void OnPushObject(float scroll, int directionInput);

        public void ManipulateObject(Vector2 mouseDelta, Vector2 analogue)
        {
			if (CanManipulate())
				OnManipulateObject(mouseDelta, analogue);
        }

		public void PushObject(float scroll, int directionInput)
        {
			if (CanManipulate())
				OnPushObject(scroll, directionInput);
        }

		public void PickUpObject()
		{
			if (carryState == CarryState.ValidTarget)
				ObjectPickedUp();
		}

		public void DropObject()
		{
			if (carryState == CarryState.Carrying)
			{
				ObjectDropped();
				carryTarget = null;
			}
		}

		public void ThrowObject()
		{
			if (carryState == CarryState.Carrying)
			{
				ObjectDropped();
				AddThrowForceToObject();
				carryTarget = null;
			}
		}

		void ObjectPickedUp()
		{
			// Set carry state
			carryState = CarryState.Carrying;

			// Lower weapon
			if (character.quickSlots != null)
				character.quickSlots.LockSelectionToNothing(this, false);

			// Subscribe to health events
			if (m_HealthManager != null)
			{
				m_HealthManager.onHealthChanged += OnHealthChanged;
				m_HealthManager.onIsAliveChanged += OnIsAliveChanged;
				m_TotalDamage = 0f;
			}

			// Set grab state
			carryState = CarryState.Carrying;

			// Connect
			OnObjectPickedUp();
		}

		void ObjectDropped()
		{
			// Raise weapon
			if (character.quickSlots != null)
				character.quickSlots.UnlockSelection(this);

			// Unsubscribe from health events
			if (m_HealthManager != null)
			{
				m_HealthManager.onHealthChanged -= OnHealthChanged;
				m_HealthManager.onIsAliveChanged -= OnIsAliveChanged;
			}

			// Reset tick timer
			m_TickCounter = m_TickRate;

			// Set carry state
			carryState = CarryState.Inactive;

			// Disconnect
			OnObjectDropped();
		}

		#region EVENT HANDLERS

		void OnHealthChanged(float from, float to, bool critical, IDamageSource source)
		{
			// Get damage and total damage
			float damage = from - to;
			m_TotalDamage += damage;

			// Drop if either pass threshold
			if (damage > m_DropOnDamage || m_TotalDamage > m_MaxTotalDamage)
				DropObject();
		}

		void OnIsAliveChanged(bool alive)
		{
			DropObject();
		}

		#endregion


		#region SAVE GAMES

		static readonly NeoSerializationKey k_CarryTargetKey = new NeoSerializationKey("carryTarget");
		static readonly NeoSerializationKey k_CarryStateKey = new NeoSerializationKey("carryState");
		static readonly NeoSerializationKey k_TotalDamageKey = new NeoSerializationKey("damage");

		protected bool didLoadFromSave
        {
			get;
			private set;
        }

		public virtual void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            if (carryTarget != null)
            {
                var grabbedNsgo = carryTarget.GetComponent<NeoSerializedGameObject>();
				if (grabbedNsgo != null)
				{
					writer.WriteNeoSerializedGameObjectReference(k_CarryTargetKey, grabbedNsgo, nsgo);
					writer.WriteValue(k_CarryStateKey, (int)carryState);
					writer.WriteValue(k_TotalDamageKey, m_TotalDamage);
				}
			}
		}

		public virtual void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
		{
			if (reader.TryReadNeoSerializedGameObjectReference(k_CarryTargetKey, out var grabbed, nsgo) && grabbed != null)
			{
				// Apply the carry target
				m_CarryTarget = grabbed.GetComponent<Rigidbody>();

				// Apply the carry state
				int carryStateValue;
				reader.TryReadValue(k_CarryStateKey, out carryStateValue, 0);
				m_CarryState = (CarryState)carryStateValue;

				// Get total damage
				reader.TryReadValue(k_TotalDamageKey, out m_TotalDamage, m_TotalDamage);

                didLoadFromSave = true;
            }
		}

		#endregion
	}
}
