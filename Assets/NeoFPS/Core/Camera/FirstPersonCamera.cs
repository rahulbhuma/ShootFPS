using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NeoCC;
using NeoSaveGames.Serialization;
using NeoSaveGames;
using System;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/fpcamref-mb-firstpersoncamera.html")]
	public class FirstPersonCamera : FirstPersonCameraBase
	{
        [Header("Unity Camera Properties")]

		[SerializeField, Tooltip("The main camera for the first person view")]
		private Camera m_Camera = null;

        [SerializeField, Tooltip("What to do with the main camera in the scene? Use this to prevent wasted render cycles and multiple listeners")]
        private CameraAction m_PreviousCameraAction = CameraAction.DeactivateGameObject;

        [SerializeField, Tooltip("The audio listener for the first person view")]
        private AudioListener m_AudioListener = null;

        public override Transform cameraTransform
        {
            get { return m_Camera.transform; }
        }

        public override Camera unityCamera
        {
            get { return m_Camera; }
        }

#if UNITY_EDITOR
        protected override void OnValidate ()
        {
            // Get the Unity camera
            if (m_Camera == null)
                m_Camera = GetComponentInChildren<Camera>(true);

            // Get / disable the audio listener
            if (m_AudioListener == null)
            {
                if (m_Camera != null)
                {
                    m_AudioListener = m_Camera.GetComponent<AudioListener>();
                    if (m_AudioListener != null)
                        m_AudioListener.enabled = false;
                }
            }
            else
                m_AudioListener.enabled = false;

            base.OnValidate();
        }
#endif

		public override void LookThrough (bool value)
		{
			// Set current
            if (value)
            {
                // Deactivate old main camera
                if (m_PreviousCameraAction != CameraAction.Ignore)
                {
                    var main = Camera.main;
                    if (main != null && main != m_Camera)
                    {
                        switch (m_PreviousCameraAction)
                        {
                            case CameraAction.DeactivateGameObject:
                                main.gameObject.SetActive(false);
                                break;
                            case CameraAction.DisableComponent:
                                {
                                    if (main != null)
                                    {
                                        var audio = main.GetComponent<AudioListener>();
                                        if (audio != null)
                                            audio.enabled = false;

                                        main.enabled = false;
                                    }
                                }
                                break;
                            case CameraAction.DestroyGameObject:
                                Destroy(main.gameObject);
                                break;
                        }
                    }
                }
                current = this;
            }
			else
			{
				if (current == this)
					current = null;
			}

            // Activate camera
            m_Camera.gameObject.SetActive (value);
            m_AudioListener.enabled = value;
		}

        protected override void ApplyFoVMultipliers ()
        {
            m_Camera.fieldOfView = baseFoV * fovMultiplier;
            base.ApplyFoVMultipliers();
        }

        #region SAVE GAMES

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);

            LookThrough(m_Camera.isActiveAndEnabled);
        }

        #endregion
    }
}