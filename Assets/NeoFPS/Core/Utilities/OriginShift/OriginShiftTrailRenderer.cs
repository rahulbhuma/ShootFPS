using System;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/neofpsref-mb-originshifttrailrenderer.html")]
    [RequireComponent(typeof (TrailRenderer))]
    public class OriginShiftTrailRenderer : OriginShiftSubscriberBase
    {
        private TrailRenderer m_TrailRenderer = null;

        private static Vector3[] s_Positions = null;

		protected override OriginShift.SubscriptionPeriod subscriptionPeriod
		{
			get { return OriginShift.SubscriptionPeriod.EnabledOnly; }
		}

		void Awake()
        {
			m_TrailRenderer = GetComponent<TrailRenderer>();
        }

        public override void ApplyOffset(Vector3 offset)
		{
			int pointCount = m_TrailRenderer.positionCount;
			if (pointCount > 0)
			{
				// Create or extend particle buffer if required
				if (s_Positions == null || pointCount > s_Positions.Length)
				{
					int targetCount = (pointCount / 64) + 1;
					s_Positions = new Vector3[targetCount * 64];
				}

				// Get the particles
				int count = m_TrailRenderer.GetPositions(s_Positions);

				// Offset the particles (skip the last one as that's current position)
				for (int i = 0; i < count - 1; ++i)
				{
					if (s_Positions[i] != Vector3.zero)
						s_Positions[i] += offset;
				}

				// Massive hack. Trail renderers are a special kind of voodoo, so it could have ignored the offset, added or subtracted it
				if (count > 1)
				{
					// Get each version (no offset, add or subtract)
					Vector3 prev = s_Positions[count - 2];
					Vector3 v1 = s_Positions[count - 1];
					Vector3 v2 = v1 - offset;
					Vector3 v3 = v1 + offset;

					// Get (squared) distances from each option to previous point
					float d1 = Vector3.SqrMagnitude(v1 - prev);
					float d2 = Vector3.SqrMagnitude(v2 - prev);
					float d3 = Vector3.SqrMagnitude(v3 - prev);

					// Apply the offset or not, based on the closest to the previous point
					if (d1 > d2 || d1 > d3)
                    {
						if (d2 < d3)
							s_Positions[count - 1] -= offset;
						else
							s_Positions[count - 1] += offset;
                    }
				}

				// Set the particle system contents
				m_TrailRenderer.SetPositions(s_Positions);
			}
		}
    }
}
