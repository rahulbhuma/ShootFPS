using System.Collections;
using UnityEngine;
using NeoFPS.Constants;
using NeoSaveGames.Serialization;

namespace NeoFPS.ModularFirearms
{
	[HelpURL("https://docs.neofps.com/manual/weaponsref-mb-headmoveaimer.html")]
	public class HeadMoveAimer : OffsetBaseAimer
    {        
        private Transform m_RootTransform = null;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (firearm != null)
                m_RootTransform = firearm.transform;
            else
                m_RootTransform = transform;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (isAiming)
            {
                // Reset the camera aim
                if (firearm.wielder != null)
                {
                    firearm.wielder.fpCamera.ResetOffset(aimDownDuration);
                    firearm.wielder.fpCamera.ResetFov(aimDownDuration);
                }
            }
        }

        protected override void AimInternal ()
		{
            base.AimInternal();

			// Set the camera aim
			if (firearm.wielder != null)
			{
                firearm.wielder.fpCamera.SetOffset(Vector3.Scale(m_RootTransform.lossyScale, posePosition), poseRotation, aimUpDuration);
                firearm.wielder.fpCamera.SetFov(fovMultiplier, inputMultiplier, aimUpDuration);
            }
        }

		protected override void StopAimInternal (bool instant)
        {
            base.StopAimInternal(instant);

            // Reset the camera aim
            if (firearm.wielder != null)
			{
				firearm.wielder.fpCamera.ResetOffset (aimDownDuration);
				firearm.wielder.fpCamera.ResetFov (aimDownDuration);
			}
        }
        
        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);

            if (isAiming)
            {
                if (firearm.wielder != null)
                {
                    firearm.wielder.fpCamera.SetOffset(Vector3.Scale(m_RootTransform.lossyScale, posePosition), poseRotation, 0f);
                    firearm.wielder.fpCamera.SetFov(fovMultiplier, 0f);
                }
            }
            else
            {
                if (firearm.wielder != null)
                {
                    firearm.wielder.fpCamera.ResetOffset(0f);
                    firearm.wielder.fpCamera.ResetFov(0f);
                }
            }
        }
    }
}
