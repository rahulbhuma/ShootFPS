using UnityEngine;
using NeoFPS.Constants;
using NeoFPS.CharacterMotion;
using UnityEngine.Events;

namespace NeoFPS
{
    [RequireComponent (typeof (MotionController), typeof (ICharacterAudioHandler))]
    [HelpURL("https://docs.neofps.com/manual/audioref-mb-surfacefootstepaudiosystem.html")]
    public class SurfaceFootstepAudioSystem : MonoBehaviour
    {
        [SerializeField, Tooltip("The layers to cast against to detect foot impacts")]
        private LayerMask m_FootstepLayers = PhysicsFilter.Masks.BulletBlockers;

        private MotionController m_MotionController = null;
        private ICharacterAudioHandler m_AudioHandler = null;
        private ISurfaceFootstepAudio m_AudioSetup = null;
        private RaycastHit m_Hit = new RaycastHit();
        private FpsSurfaceMaterial m_LastSurfaceId = FpsSurfaceMaterial.Default;

        public event UnityAction<FpsSurfaceMaterial> onFootStep;

        public enum Direction
        {
            Down,
            InverseGroundNormal,
            LocalVector,
            WorldVector,
            WorldParameter,
            WorldParameterInverse,
            LocalParameter,
            LocalParameterInverse
        }

        public FpsSurfaceMaterial lastStepMaterial
        {
            get;
            private set;
        }

        protected void Awake()
        {
            m_AudioHandler = GetComponent<ICharacterAudioHandler>();
            m_MotionController = GetComponent<MotionController>();

            // Subscribe to step tracking if awake late
            if (m_AudioSetup != null && m_AudioSetup.footstepAudio != null)
                m_MotionController.onStep += OnStep;
        }

        public void SetFootstepAudio(ISurfaceFootstepAudio audio)
        {
            if (m_MotionController != null)
            {
                // Subscribe to step tracking
                if (m_AudioSetup == null && audio != null && audio.footstepAudio != null)
                    m_MotionController.onStep += OnStep;

                // Unsubscribe from step tracking
                if (m_AudioSetup != null && audio == null)
                    m_MotionController.onStep -= OnStep;
            }

            m_AudioSetup = audio;
        }

        void OnStep()
        {
            float sqrSpeed = m_MotionController.characterController.velocity.sqrMagnitude;
            float minSpeed = m_AudioSetup.minimumSpeed;
            if (sqrSpeed > minSpeed * minSpeed)
            {
                // Get the surface
                var surface = GetGroundSurface();

                // Play the audio
                float volume;
                AudioClip clip = m_AudioSetup.footstepAudio.GetAudioClip(surface, out volume);
                if (clip != null)
                    m_AudioHandler.PlayClip(clip, FpsCharacterAudioSource.Feet, volume);

                // Fire footstep event
                if (onFootStep != null)
                    onFootStep(surface);
            }
        }

        FpsSurfaceMaterial GetGroundSurface()
        {
            FpsSurfaceMaterial result = m_LastSurfaceId;

            // Get charactercontroller info
            var cc = m_MotionController.characterController;

            var radius = cc.radius;
            var normalisedHeight = radius / cc.height;

            // Raycast for surface type
            if (cc.RayCast(normalisedHeight, m_AudioSetup.castDirection * (m_AudioSetup.castRange + radius), m_AudioSetup.castSpace, out m_Hit, m_FootstepLayers, QueryTriggerInteraction.Ignore))
            {
                // Get surface ID from ray hit
                Transform t = m_Hit.transform;
                if (t != null)
                {
                    BaseSurface s = t.GetComponent<BaseSurface>();
                    if (s != null)
                        result = s.GetSurface(m_Hit);
                    else
                        result = FpsSurfaceMaterial.Default;
                }

                m_LastSurfaceId = result;
            }

            return result;
        }
    }
}
