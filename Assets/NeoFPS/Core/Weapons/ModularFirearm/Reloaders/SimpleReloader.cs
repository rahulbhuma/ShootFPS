using NeoSaveGames;
using NeoSaveGames.Serialization;
using UnityEngine;
using UnityEngine.Serialization;

namespace NeoFPS.ModularFirearms
{
	[HelpURL("https://docs.neofps.com/manual/weaponsref-mb-simplereloader.html")]
	public class SimpleReloader : BaseReloaderBehaviour
	{
		[SerializeField, Tooltip("The delay type between starting and completing a reload.")]
		private FirearmDelayType m_ReloadDelayType = FirearmDelayType.ElapsedTime;

		[SerializeField, Delayed, Tooltip("The time taken to reload.")]
		private float m_ReloadDuration = 2f;

		[SerializeField, Delayed, FormerlySerializedAs("m_ReloadDuration"), Tooltip("The time taken before you can use the weapon again after the reload starts.")]
		private float m_BlockingDuration = 0f;

		[SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Trigger, true, true), Tooltip("The animator controller trigger key for the reload animation.")]
		private string m_ReloadAnimTrigger = "Reload";

		[SerializeField, Tooltip("The audio clip to play while reloading.")]
        private AudioClip m_ReloadAudio = null;

		[SerializeField, Range(0f, 1f), Tooltip("The volume that reloading sounds are played at.")]
		private float m_Volume = 1f;

		private bool m_Reloaded = true;
		private bool m_WaitingOnExternalTrigger = false;
		private int m_ReloadAnimTriggerHash = 0;
        private float m_ReloadTimeout = 0f;
		private float m_BlockingTimeout = 0f;

		private class WaitForReload : Waitable
		{
			// Store the reloadable owner
			readonly SimpleReloader m_Owner;
			public WaitForReload (SimpleReloader owner)	{ m_Owner = owner; }

			// Check for timeout
			protected override bool CheckComplete () { return !m_Owner.m_WaitingOnExternalTrigger && m_Owner.m_ReloadTimeout == 0f && m_Owner.m_BlockingTimeout == 0f; }
		}

        WaitForReload m_WaitForReload = null;
        private WaitForReload waitForReload
        {
            get
            {
                if (m_WaitForReload == null)
                    m_WaitForReload = new WaitForReload(this);
                return m_WaitForReload;
            }
        }

        public override FirearmDelayType reloadDelayType
		{
			get { return m_ReloadDelayType; }
		}

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (m_ReloadDuration < 0f)
                m_ReloadDuration = 0f;
			if (m_BlockingDuration < m_ReloadDuration)
				m_BlockingDuration = m_ReloadDuration;
        }
#endif

        protected override void Start ()
		{
			m_WaitForReload = new WaitForReload(this);

			// Get the reload animation hash
			if (m_ReloadAnimTrigger == string.Empty)
				m_ReloadAnimTriggerHash = -1;
			else
				m_ReloadAnimTriggerHash = Animator.StringToHash (m_ReloadAnimTrigger);

			// Check blocking duration
			if (m_BlockingDuration < m_ReloadDuration)
				m_BlockingDuration = m_ReloadDuration;

			base.Start ();
		}

		protected override void Update()
		{
			// Decrement reload timer
			if (m_ReloadTimeout > 0f)
			{
				m_ReloadTimeout -= Time.deltaTime;
				if (m_ReloadTimeout <= 0f)
				{
					ReloadInternal();
					m_ReloadTimeout = 0f;
				}
			}

			// Decrement blocking timer
			if (m_BlockingTimeout > 0f)
			{
				m_BlockingTimeout -= Time.deltaTime;
				if (m_BlockingTimeout <= 0f)
					m_BlockingTimeout = 0f;
			}
		}

        protected override void OnDisable()
        {
            base.OnDisable();
			m_Reloaded = true;
            m_ReloadTimeout = 0f;
			m_BlockingTimeout = 0f;
            m_WaitingOnExternalTrigger = false;
        }

        void ReloadInternal ()
		{
			// Record the old magazine count (to check against)
			int oldMagazineSize = currentMagazine;
			// Get the new magazine count (clamped in property)
			currentMagazine += firearm.ammo.currentAmmo;
			// Decrement the ammo
			firearm.ammo.DecrementAmmo (currentMagazine - oldMagazineSize);
			// Fire completed event
			SendReloadCompletedEvent ();
		}

        public override void ManualReloadPartial()
		{
			if (reloadDelayType != FirearmDelayType.ExternalTrigger)
			{
				//Debug.LogError("Attempting to manually signal weapon reloaded when delay type is not set to custom");
				return;
			}
			if (!m_WaitingOnExternalTrigger)
			{
				Debug.LogError("Attempting to manually signal weapon reloaded when not waiting for reload");
				return;
			}
			// Reload and reset trigger
			if (!m_Reloaded)
				ReloadInternal();
			m_Reloaded = true;
		}

        public override void ManualReloadComplete ()
		{		
			if (reloadDelayType != FirearmDelayType.ExternalTrigger)
			{
				Debug.LogError ("Attempting to manually signal weapon reloaded when delay type is not set to custom");
				return;
			}
			if (!m_WaitingOnExternalTrigger)
			{
				Debug.LogError ("Attempting to manually signal weapon reloaded when not waiting for reload");
				return;
			}
			// Reload and reset trigger
			if (!m_Reloaded)
			{
				ReloadInternal();
				m_Reloaded = true;
			}
			m_WaitingOnExternalTrigger = false;
		}

		#region BaseReloaderBehaviour implementation

		public override bool isReloading
		{
			get { return !waitForReload.isComplete; }
		}

		protected float GetReloadDurationMultiplier()
        {
			return 1f;
        }

		public override Waitable Reload ()
		{
			if (waitForReload.isComplete && !full && firearm.ammo.available)
			{
				switch (reloadDelayType)
				{
					case FirearmDelayType.None:
						SendReloadStartedEvent ();
						ReloadInternal ();
						break;
					case FirearmDelayType.ElapsedTime:
						if (m_BlockingTimeout == 0f)
						{
							// Fire started event
							SendReloadStartedEvent ();
							// Set timers
							float multiplier = GetReloadDurationMultiplier();
							m_ReloadTimeout = m_ReloadDuration * multiplier;
							m_BlockingTimeout = m_BlockingDuration * multiplier;
							// Trigger animation
							if (m_ReloadAnimTriggerHash != -1)
								firearm.animationHandler.SetTrigger (m_ReloadAnimTriggerHash);
						}
						break;
					case FirearmDelayType.ExternalTrigger:
						if (!m_WaitingOnExternalTrigger)
						{
							// Fire started event
							SendReloadStartedEvent ();
							// Set trigger
							m_WaitingOnExternalTrigger = true;
							// Set as not reloaded
							m_Reloaded = false;
							// Trigger animation
							if (m_ReloadAnimTriggerHash != -1)
								firearm.animationHandler.SetTrigger (m_ReloadAnimTriggerHash);
						}
						break;
                }
                // Play audio
                if (m_ReloadAudio != null)
                    firearm.PlaySound(m_ReloadAudio, m_Volume);
			}
			return waitForReload;
		}

        private static readonly NeoSerializationKey k_TimeoutKey = new NeoSerializationKey("timeout");
		private static readonly NeoSerializationKey k_BlockingKey = new NeoSerializationKey("blocking");
		private static readonly NeoSerializationKey k_ReloadedKey = new NeoSerializationKey("reloaded");

		public override void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
			if (saveMode == SaveMode.Default)
			{
				writer.WriteValue(k_TimeoutKey, m_ReloadTimeout);
				writer.WriteValue(k_BlockingKey, m_BlockingTimeout);
				writer.WriteValue(k_ReloadedKey, m_Reloaded);
			}
            base.WriteProperties(writer, nsgo, saveMode);
        }

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            reader.TryReadValue(k_TimeoutKey, out m_ReloadTimeout, m_ReloadTimeout);
			reader.TryReadValue(k_BlockingKey, out m_BlockingTimeout, m_BlockingTimeout);
			reader.TryReadValue(k_ReloadedKey, out m_Reloaded, m_Reloaded);

			base.ReadProperties(reader, nsgo);
        }

        #endregion
    }
}