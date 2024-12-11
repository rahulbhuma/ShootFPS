using System.Collections;
using UnityEngine;

namespace NeoFPS.ModularFirearms
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-particlesystemshelleject.html")]
    public class ParticleSystemShellEject : BaseEjectorBehaviour
    {
        [Header("Ejector Settings")]

        [SerializeField, RequiredObjectProperty, Tooltip("The particle systems to play when a shell is ejected")]
        private ParticleSystem[] m_ParticleSystems = { };

        [SerializeField, Tooltip("The delay type between firing and ejecting a shell.")]
        private FirearmDelayType m_DelayType = FirearmDelayType.None;

        [SerializeField, Tooltip("The delay time between firing and ejecting a shell if the delay type is set to elapsed time.")]
        private float m_Delay = 0f;

        [Header("Audio")]

        [SerializeField, Tooltip("The audio clips to play for an empty shell hitting the ground.")]
        private AudioClip[] m_ImpactClips = new AudioClip[0];

        [SerializeField, Tooltip("The time between a shell ejecting, and an impact audio clip being played.")]
        private float m_AudioDelay = 0.5f;

        [SerializeField, Range(0f, 1f), Tooltip("The volume to play the empty shell impact audio.")]
        private float m_ImpactVolume = 1f;

        public override bool ejectOnFire { get { return m_DelayType != FirearmDelayType.ExternalTrigger; } }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            m_Delay = Mathf.Clamp(m_Delay, 0f, 5f);

            if (m_ParticleSystems.Length == 0)
                m_ParticleSystems = new ParticleSystem[1];
        }
#endif

        public override bool isModuleValid
        {
            get
            {
                // Check array
                if (m_ParticleSystems == null || m_ParticleSystems.Length == 0)
                    return false;

                // Check contents
                int found = 0;
                for (int i = 0; i < m_ParticleSystems.Length; ++i)
                {
                    if (m_ParticleSystems[i] != null)
                        ++found;
                }

                return found > 0;
            }
        }

        public override void Eject()
        {
            // Uses a coroutine to check it's on the update frame
            if (isActiveAndEnabled)
                StartCoroutine(EjectCoroutine());
        }

        void DoEject()
        {
            // Play particle systems
            for (int i = 0; i < m_ParticleSystems.Length; ++i)
                if (m_ParticleSystems[i] != null)
                    m_ParticleSystems[i].Play(true);
        }

        IEnumerator EjectCoroutine()
        {
            switch (m_DelayType)
            {
                case FirearmDelayType.None:
                    yield return null;
                    DoEject();
                    break;
                case FirearmDelayType.ElapsedTime:
                    if (m_Delay > 0f)
                        yield return new WaitForSeconds(m_Delay);
                    else
                        yield return null;
                    DoEject();
                    break;
                case FirearmDelayType.ExternalTrigger:
                    yield return null;
                    DoEject();
                    break;
            }

            if (m_ImpactClips.Length > 0)
            {
                yield return new WaitForSeconds(m_AudioDelay);

                var clip = m_ImpactClips[Random.Range(0, m_ImpactClips.Length)];
                if (clip != null)
                    firearm.PlaySound(clip, m_ImpactVolume);
            }
        }
    }
}