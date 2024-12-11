using UnityEngine;

namespace NeoFPS
{
	[RequireComponent(typeof (Collider))]
    [HelpURL("https://docs.neofps.com/manual/healthref-mb-basicdamagehandler.html")]
	public class BasicDamageHandler : MonoBehaviour, IDamageHandler, IHasHealthManager
    {
		[SerializeField, Tooltip("The value to multiply any incoming damage by. Use to reduce damage to areas like feet, or raise it for areas like the head.")]
		private float m_Multiplier = 1f;

		[SerializeField, Tooltip("Does the damage count as critical. Used to change the feedback for the damage taker and dealer.")]
		private bool m_Critical = false;

		private Collider m_Collider = null;

		public IHealthManager healthManager
		{
			get;
			private set;
		}

        public Transform healthTransform
        {
            get;
            private set;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate ()
		{
			if (m_Multiplier < 0f)
				m_Multiplier = 0f;
		}
#endif

		protected virtual void Awake ()
		{
			healthManager = GetComponentInParent<IHealthManager>();

			if (healthManager == null)
			{
				healthTransform = transform;
                if (m_Multiplier <= 0f)
                    enabled = false;
            }
			else
                healthTransform = healthManager.transform;

            m_Collider = GetComponent<Collider>();
			Debug.Assert(m_Collider != null, "Damage handlers should only be placed on objects with a collider");
		}

		#region IDamageHandler implementation

		private DamageFilter m_InDamageFilter = DamageFilter.AllDamageAllTeams;
		public DamageFilter inDamageFilter 
		{
			get { return m_InDamageFilter; }
			set { m_InDamageFilter = value; }
		}

		// 1.2: Remove raycast hit. It's redundant in health manager imo
		protected DamageResult AddDamageInternal(IDamageSource source, float inDamage, out float outDamage)
		{
			if (source != null && !CheckDamageCollision(source))
			{
				outDamage = 0f;
				return DamageResult.Blocked;
			}
			else
			{
				outDamage = inDamage * m_Multiplier;
				var result = m_Critical ? DamageResult.Critical : DamageResult.Standard;

				if (outDamage > 0f && source != null && source.controller != null)
					source.controller.currentCharacter.ReportTargetHit(m_Critical);

				healthManager.AddDamage(outDamage, m_Critical, source);

				return result;
			}
		}

		protected DamageResult AddDamageInternal(IDamageSource source, RaycastHit hit, float inDamage, out float outDamage)
        {
			if (source != null && !CheckDamageCollision(source))
			{
				outDamage = 0f;
				return DamageResult.Blocked;
			}
			else
			{
				outDamage = inDamage * m_Multiplier;
				var result = m_Critical ? DamageResult.Critical : DamageResult.Standard;

				if (outDamage > 0f && source != null && source.controller != null)
					source.controller.currentCharacter.ReportTargetHit(m_Critical);

				healthManager.AddDamage(outDamage, m_Critical, source, hit);

				return result;
			}
		}

		public virtual DamageResult AddDamage (float damage)
		{
			if (enabled && healthManager != null)
            {
				var result = AddDamageInternal(null, damage, out damage);
				DamageEvents.ReportDamageHandlerHit(this, null, m_Collider.bounds.center, result, damage);
				return result;
			}
			else
				return DamageResult.Ignored;
		}

		public virtual DamageResult AddDamage (float damage, IDamageSource source)
		{
			if (enabled && healthManager != null)
            {
				var result = AddDamageInternal(source, damage, out damage);
				DamageEvents.ReportDamageHandlerHit(this, source, m_Collider.bounds.center, result, damage);
				return result;
			}
			else
				return DamageResult.Ignored;
		}

        public virtual DamageResult AddDamage(float damage, RaycastHit hit)
		{
			if (enabled && healthManager != null)
            {
				var result = AddDamageInternal(null, hit, damage, out damage);
				DamageEvents.ReportDamageHandlerHit(this, null, hit.point, result, damage);
				return result;
			}
			else
				return DamageResult.Ignored;
		}

        public virtual DamageResult AddDamage(float damage, RaycastHit hit, IDamageSource source)
		{
			// Apply damage
			if (enabled && healthManager != null && m_Multiplier > 0f && CheckDamageCollision(source))
			{
				var result = AddDamageInternal(source, hit, damage, out damage);
				DamageEvents.ReportDamageHandlerHit(this, source, hit.point, result, damage);
				return result;
			}
			else
				return DamageResult.Ignored;
		}

		bool CheckDamageCollision(IDamageSource source)
		{
			return !(source != null && !source.outDamageFilter.CollidesWith(inDamageFilter, FpsGameMode.friendlyFire));
		}

        #endregion
    }
}