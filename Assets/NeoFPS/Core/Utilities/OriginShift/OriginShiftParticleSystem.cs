using System;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/neofpsref-mb-originshiftparticlesystem.html")]
	[RequireComponent(typeof(ParticleSystem))]
	public class OriginShiftParticleSystem : OriginShiftSubscriberBase
	{
		private ParticleSystem m_ParticleSystem = null;
		private Vector3 m_Offset = Vector3.zero;

		public static ParticleSystem.Particle[] s_ParticleBuffer = null;

		protected override OriginShift.SubscriptionPeriod subscriptionPeriod
		{
			get { return OriginShift.SubscriptionPeriod.EnabledOnly; }
		}

		void Awake()
		{
			m_ParticleSystem = GetComponent<ParticleSystem>();
			if (m_ParticleSystem.main.simulationSpace != ParticleSystemSimulationSpace.World)
			{
				Debug.LogWarning("Added OriginShiftParticleSystem to a local space particle system. Use the OriginShiftTransform instead.");
				enabled = false;
			}
		}

		public override void ApplyOffset(Vector3 offset)
		{
			int particleCount = m_ParticleSystem.particleCount;
			if (particleCount > 0)
			{
				// Create or extend particle buffer if required
				if (s_ParticleBuffer == null || particleCount > s_ParticleBuffer.Length)
				{
					int targetCount = (particleCount / 512) + 1;
					s_ParticleBuffer = new ParticleSystem.Particle[targetCount * 512];
				}

				// Get the particles
				int count = m_ParticleSystem.GetParticles(s_ParticleBuffer);

				// Offset the particles
				for (int i = 0; i < count; ++i)
					s_ParticleBuffer[i].position += offset;

				// Set the particle system contents
				m_ParticleSystem.SetParticles(s_ParticleBuffer, count);
			}
		}
	}
}
