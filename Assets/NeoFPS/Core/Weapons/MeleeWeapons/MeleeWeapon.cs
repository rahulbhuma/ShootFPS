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
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-meleeweapon.html")]
    [RequireComponent(typeof(AudioSource))]
	public class MeleeWeapon : BaseMeleeWeapon
    {
        [Header("Melee Properties")]

        [SerializeField, Tooltip("The damage the weapon does.")]
		private float m_Damage = 50f;

		[SerializeField, Tooltip("The force to impart on the hit object. Requires either a [Rigidbody][unity-rigidbody] or an impact handler on the hit object.")]
		private float m_ImpactForce = 15f;

        [SerializeField, Tooltip("The layers the attack will collide with.")]
        private LayerMask m_Layers = PhysicsFilter.Masks.BulletBlockers;

        [SerializeField, Tooltip("Should the attack be tested against trigger colliders.")]
        private bool m_QueryTriggerColliders = false;

        [SerializeField, Tooltip("The range that the melee weapon can reach.")]
		private float m_Range = 1f;

		[SerializeField, Tooltip("The delay from starting the attack to checking for an impact. Should be synced with the striking point in the animation.")]
		private float m_Delay = 0.6f;

		[SerializeField, Tooltip("The recovery time after a hit.")]
		private float m_RecoverTime = 1f;

        [Header("Melee Animation")]

        [SerializeField, AnimatorParameterKey("m_Animator", AnimatorControllerParameterType.Trigger), Tooltip("The animation trigger for the attack animation.")]
		private string m_TriggerAttack = "Attack";

		[SerializeField, AnimatorParameterKey("m_Animator", AnimatorControllerParameterType.Trigger), Tooltip("The animation trigger for the attack hit animation.")]
		private string m_TriggerAttackHit = "AttackHit";

        [SerializeField, AnimatorParameterKey("m_Animator", AnimatorControllerParameterType.Bool), Tooltip("The animation bool parameter for the block animation."), FormerlySerializedAs("m_TriggerBlock")]
        private string m_BoolBlock = "Block";

        [Header("Melee Audio")]

        [SerializeField, Tooltip("The audio clip when attacking.")]
        private AudioClip m_AudioAttack = null;

        [SerializeField, Tooltip("The audio clip when bringing the weapon into block position.")]
        private AudioClip m_AudioBlockRaise = null;

        [SerializeField, Tooltip("The audio clip when bringing the weapon out of block position.")]
        private AudioClip m_AudioBlockLower = null;

        private static List<IDamageHandler> s_DamageHandlers = new List<IDamageHandler>(4);

        private int m_AnimHashBlock = 0;
		private int m_AnimHashAttack = 0;
		private int m_AnimHashAttackHit = 0;

        private IMeleeHitExtension[] m_HitExtensions = null;
        private float m_CooldownTimer = 0f;
        private float m_DelayTimer = 0f;

        public enum HitDetectType
		{
			Raycast,
			Spherecast,
			Custom
        }

        public override bool isBlocked
        {
            get { return base.isBlocked || attacking; }
        }

#if UNITY_EDITOR
        protected override void OnValidate ()
		{
            base.OnValidate();
                        
            // Limit values
            if (m_Damage < 0f)
                m_Damage = 0f;
            if (m_ImpactForce < 0f)
                m_ImpactForce = 0f;
            if (m_Delay < 0f)
                m_Delay = 0f;
            if (m_RecoverTime < 0f)
                m_RecoverTime = 0f;
            m_Range = Mathf.Clamp(m_Range, 0.1f, 5f);
        }
		#endif

		protected override void Awake ()
        {
            base.Awake();

            m_AnimHashBlock = Animator.StringToHash (m_BoolBlock);
			m_AnimHashAttack = Animator.StringToHash (m_TriggerAttack);
			m_AnimHashAttackHit = Animator.StringToHash (m_TriggerAttackHit);

            m_HitExtensions = GetComponents<IMeleeHitExtension>();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

			blocking = false;

            // Clear coroutines
            if (m_CooldownCoroutine != null)
            {
                StopCoroutine(m_CooldownCoroutine);
                m_CooldownCoroutine = null;
            }
            if (m_DoRaycastCoroutine != null)
            {
                StopCoroutine(m_DoRaycastCoroutine);
                m_DoRaycastCoroutine = null;
            }
        }

		public override void PrimaryPress ()
		{
			if (!isBlocked && !blocking && m_CooldownCoroutine == null)
			{
                animationHandler.SetTrigger (m_AnimHashAttack);
				m_DoRaycastCoroutine = StartCoroutine (DoRaycast (m_Delay));
				m_CooldownCoroutine = StartCoroutine (Cooldown (m_RecoverTime));

                if (m_AudioAttack != null)
                    audioSource.PlayOneShot(m_AudioAttack);

                attacking = true;
			}
        }

        public override void PrimaryRelease()
        {
            // Current implementation are all one-shots
        }

        public override void SecondaryPress()
        {
            if (!isBlocked)
                blocking = true;
        }

        public override void SecondaryRelease()
        {
            blocking = false;
        }

        Coroutine m_CooldownCoroutine = null;
        IEnumerator Cooldown (float timer)
		{
            m_CooldownTimer = timer;
            while (m_CooldownTimer > 0f)
            {
                yield return null;
             
                m_CooldownTimer -= Time.deltaTime;
            }

            attacking = false;

            m_CooldownCoroutine = null;
		}

		Coroutine m_DoRaycastCoroutine = null;
        IEnumerator DoRaycast (float timer)
        {
            m_DelayTimer = timer;
            while (m_DelayTimer > 0f)
            {
                yield return null;
                m_DelayTimer -= Time.deltaTime;
            }

            Vector3 direction = transform.forward;

            // Get root game object to prevent impacts with body
            Transform ignoreRoot = null;
            if (wielder != null)
                ignoreRoot = wielder.gameObject.transform;

            RaycastHit hit;
			if (PhysicsExtensions.RaycastNonAllocSingle (
				    new Ray (transform.position, direction),
					out hit,
				    m_Range,
                    m_Layers,
                    ignoreRoot,
                    m_QueryTriggerColliders ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore
            ))
			{
                OnMeleeHit(hit, direction);
            }

			m_DoRaycastCoroutine = null;
		}

        protected virtual float GetModifiedDamage(float unmodified)
        {
            return unmodified;
        }

        protected virtual void OnMeleeHit(RaycastHit hit, Vector3 attackDirection)
        {
            // Show effect
            SurfaceManager.ShowBulletHit(hit, attackDirection, 1f, hit.rigidbody != null);

            // Apply damage
            float damage = GetModifiedDamage(m_Damage);
            if (damage > 0f)
            {
                hit.transform.GetComponents(s_DamageHandlers);
                for (int i = 0; i < s_DamageHandlers.Count; ++i)
                {
                    if (s_DamageHandlers[i].enabled)
                        s_DamageHandlers[i].AddDamage(damage, hit, this);
                }
                s_DamageHandlers.Clear();
            }

            // Apply force
            if (hit.rigidbody != null)
                hit.rigidbody.AddForceAtPosition(attackDirection * m_ImpactForce, hit.point, ForceMode.Impulse);
            else
            {
                IImpactHandler impactHandler = hit.transform.GetComponent<IImpactHandler>();
                if (impactHandler != null)
                    impactHandler.HandlePointImpact(hit.point, attackDirection * m_ImpactForce);
            }

            // Trigger hit animation
            if (m_AnimHashAttackHit != 0)
                animationHandler.SetTrigger(m_AnimHashAttackHit);

            // Process hit extensions
            for (int i = 0; i < m_HitExtensions.Length; ++i)
                m_HitExtensions[i].OnMeleeHit(hit);
        }

        protected override void OnBlockStateChange(bool to)
        {
            base.OnBlockStateChange(to);

            // Set animator parameter
            if (m_AnimHashBlock != 0)
                animationHandler.SetBool(m_AnimHashBlock, to);

            // Play audio
            if (to)
            {
                if (m_AudioBlockRaise != null)
                    audioSource.PlayOneShot(m_AudioBlockRaise);
            }
            else
            {
                if (m_AudioBlockLower != null)
                    audioSource.PlayOneShot(m_AudioBlockLower);
            }
        }

        #region INeoSerializableComponent IMPLEMENTATION

        private static readonly NeoSerializationKey k_CooldownKey = new NeoSerializationKey("cooldown");
        private static readonly NeoSerializationKey k_DelayTimerKey = new NeoSerializationKey("delayTimer");

        public override void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            base.WriteProperties(writer, nsgo, saveMode);

            if (saveMode == SaveMode.Default)
            {
                // Write coroutines if relevant
                if (m_CooldownCoroutine != null)
                    writer.WriteValue(k_CooldownKey, m_CooldownTimer);
                if (m_DoRaycastCoroutine != null)
                    writer.WriteValue(k_DelayTimerKey, m_DelayTimer);
            }
        }

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);

            // Read and start coroutines if relevant
            float floatResult = 0f;
            if (reader.TryReadValue(k_DelayTimerKey, out floatResult, 0f))
                m_DoRaycastCoroutine = StartCoroutine(DoRaycast(floatResult));
            if (reader.TryReadValue(k_CooldownKey, out floatResult, 0f))
                m_CooldownCoroutine = StartCoroutine(Cooldown(floatResult));
        }

        #endregion
    }
}