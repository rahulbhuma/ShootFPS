using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/audioref-mb-criticalhitaudioplayer.html")]
    [RequireComponent(typeof(AudioSource))]
    public class CriticalHitAudioPlayer : MonoBehaviour
    {
        [SerializeField, Tooltip("The minimum damage required to trigger the audio (still requires a critical hit)")]
        private float m_MinDamage = 0f;
        [SerializeField, Tooltip("The minimum time between critical hit sounds.")]
        private float m_MinDelay = 0.1f;
        [SerializeField, Tooltip("The audio clips to choose from on a critical hit being detected")]
        private AudioClip[] m_Clips = new AudioClip[0];

        private AudioSource m_AudioSource = null;
		private float m_Cooldown = 0f;

#if UNITY_EDITOR
        protected void OnValidate()
        {
            if (m_MinDamage < 0f)
                m_MinDamage = 0f;
            if (m_MinDelay < 0f)
                m_MinDelay = 0f;
        }
#endif

        protected void Awake()
        {
            m_AudioSource = GetComponent<AudioSource>();
        }

        protected void OnEnable()
        {
            DamageEvents.onDamageHandlerHit += OnDamageHandlerHit;
        }

        protected void OnDisable()
        {
            DamageEvents.onDamageHandlerHit -= OnDamageHandlerHit;
        }
		
		protected void Update()
		{
			if (m_Cooldown > 0f)
			{
				m_Cooldown -= Time.deltaTime;
				if (m_Cooldown < 0f)
					m_Cooldown = 0f;
			}
		}

        protected virtual void OnDamageHandlerHit(IDamageHandler handler, IDamageSource source, Vector3 hitPoint, DamageResult result, float damage)
        {
            if (m_Cooldown <= 0f && result == DamageResult.Critical && damage > m_MinDamage)
            {
                m_Cooldown = m_MinDelay;
                switch (m_Clips.Length)
                {
                    case 0:
                        return;
                    case 1:
                        if (m_Clips[0] != null)
                            m_AudioSource.PlayOneShot(m_Clips[0]);
                        return;
                    default:
                        int index = Random.Range(0, m_Clips.Length);
                        if (m_Clips[index] != null)
                            m_AudioSource.PlayOneShot(m_Clips[index]);
                        return;
                }
            }
        }
    }
}