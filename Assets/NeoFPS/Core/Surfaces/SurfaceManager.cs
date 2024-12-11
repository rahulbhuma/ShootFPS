using NeoFPS.Constants;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeoFPS
{
    [CreateAssetMenu(fileName = "FpsManager_Surfaces", menuName = "NeoFPS/Managers/Surface Manager", order = NeoFpsMenuPriorities.manager_surfaces)]
    [HelpURL("https://docs.neofps.com/manual/surfacesref-so-poolmanager.html")]
    public class SurfaceManager : NeoFpsManager<SurfaceManager>
    {
        protected static RuntimeBehaviour s_ProxyBehaviour = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void LoadSurfaceManager()
        {
            GetInstance("FpsManager_Surfaces");
        }

        protected override void Initialise()
        {
            s_ProxyBehaviour = GetBehaviourProxy<RuntimeBehaviour>();
        }

        [Header("VFX")]

        [SerializeField, RequiredObjectProperty, Tooltip("The impact special effects for things like bullet hits.")]
        private SurfaceHitFxData m_ImpactEffects = null;

        [SerializeField, Tooltip("Which physics layers will show decals.")]
        private LayerMask m_DecalLayers = PhysicsFilter.LayerFilter.EnvironmentDetail;

        [Header("Audio")]

        [SerializeField, RequiredObjectProperty, Tooltip("The audio library for impact audio, eg. bullet hits.")]
        private SurfaceAudioData m_ImpactAudio = null;

        [SerializeField, Tooltip("Should impact audio be louder for heavier hits.")]
        private bool m_ScaleVolumeToHitSize = false;

        [Tooltip("The maximum distance from the player character to play impact audio.")]
        [SerializeField] private float m_MaxImpactAudioDistance = 20f;

        private static Camera s_CurrentMainCamera = null;
        private float m_MaxAudioDistanceSqrd = 0f;
        private static QueuedAudio[] s_SurfaceAudioQueue = null;

        private struct QueuedAudio
        {
            public AudioClip clip;
            public float volume;
            public float distanceSqr;
            public Vector3 position;

            public static readonly QueuedAudio Empty = new QueuedAudio
            {
                clip = null,
                volume = 0f, 
                distanceSqr = float.MaxValue,
                position = Vector3.zero
            };
        }

        public static SurfaceFxOverrides currentOverrides
        {
            get;
            private set;
        }

        public static LayerMask decalLayers
        {
            get
            {
                if (instance != null)
                    return instance.m_DecalLayers;
                else
                    return PhysicsFilter.LayerFilter.EnvironmentDetail;
            }
        }

        protected void OnValidate()
        {
            if (m_MaxImpactAudioDistance < 10f)
                m_MaxImpactAudioDistance = 10f;
        }

        public override bool IsValid()
        {
            return m_ImpactEffects != null && m_ImpactAudio != null;
        }

        protected void Awake()
        {
            m_MaxAudioDistanceSqrd = m_MaxImpactAudioDistance * m_MaxImpactAudioDistance;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (s_ProxyBehaviour != null)
            {
                Destroy(s_ProxyBehaviour);
                s_ProxyBehaviour = null;
            }
        }

        protected class RuntimeBehaviour : MonoBehaviour, IOriginShiftSubscriber
        {
            public BaseHitFxBehaviour[] impactEffectInstances
            {
                get;
                private set;
            }

            protected Transform m_EffectsParent = null;

            protected void Awake()
            {
                SetSurfaceHitFx(instance.m_ImpactEffects);
                SceneManager.activeSceneChanged += OnActiveSceneChanged;
            }

            protected void OnEnable()
            {
                if (s_SurfaceAudioQueue == null)
                {
                    s_SurfaceAudioQueue = new QueuedAudio[FpsSurfaceMaterial.count];
                    for (int i = 0; i < FpsSurfaceMaterial.count; i++)
                        s_SurfaceAudioQueue[i] = QueuedAudio.Empty;
                }
                StartCoroutine(DelayedAudioPlay());
            }

            protected void OnDestroy()
            {
                SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            }

            void OnActiveSceneChanged(Scene s1, Scene s2)
            {
                if (OriginShift.system != null)
                    OriginShift.system.AddSubscriber(this);

                if (impactEffectInstances != null)
                {
                    for (int i = 0; i < impactEffectInstances.Length; ++i)
                    {
                        if (impactEffectInstances[i] != null)
                            impactEffectInstances[i].OnActiveSceneChange();
                    }
                }
            }

            public virtual void ApplyOffset(Vector3 offset)
            {
                for (int i = 0; i < impactEffectInstances.Length; ++i)
                {
                    if (impactEffectInstances[i] != null)
                        impactEffectInstances[i].ApplyFloatingOriginOffset(offset);
                }
            }

            protected virtual IEnumerator DelayedInitEffects()
            {
                yield return null;

                for (int i = 0; i < impactEffectInstances.Length; ++i)
                {
                    if (impactEffectInstances[i] != null && impactEffectInstances[i].forceInitialise)
                        impactEffectInstances[i].Hit(null, new Vector3(0f, -10000f, 0f), Vector3.up);
                }
            }

            public virtual void SetSurfaceHitFx(SurfaceHitFxData data)
            {
                if (data == null)
                {
                    Debug.LogError("No SurfaceHitFxData assigned to Impact Effects in the surface manager");
                    return;
                }

                if (m_EffectsParent == null)
                {
                    // Create effects parent object
                    var go = new GameObject("SurfaceManagerEffects");
                    m_EffectsParent = go.transform;
                    m_EffectsParent.SetParent(transform);
                }

                // Allocate array
                if (impactEffectInstances == null)
                    impactEffectInstances = new BaseHitFxBehaviour[FpsSurfaceMaterial.count];

                // Instantiate surface effects
                for (int i = 0; i < impactEffectInstances.Length; ++i)
                {
                    var effect = data.GetImpactEffect((FpsSurfaceMaterial)i);
                    if (effect != null)
                    {
                        // Destroy existing effect
                        if (impactEffectInstances[i] != null)
                            Destroy(impactEffectInstances[i].gameObject);
                        // Instantiate new effect
                        impactEffectInstances[i] = Instantiate(effect, m_EffectsParent);
                    }
                }

                // Initialise effects on the next frame (not compulsory but prevents frame hitch when first used)
                StartCoroutine(DelayedInitEffects());
            }

            IEnumerator DelayedAudioPlay()
            {
                var wait = new WaitForEndOfFrame();
                while (true)
                {
                    yield return wait;

                    for (int i = 0; i < FpsSurfaceMaterial.count; ++i)
                    {
                        var audio = s_SurfaceAudioQueue[i];
                        if (audio.clip != null)
                        {
                            // Play the audio
                            NeoFpsAudioManager.PlayEffectAudioAtPosition(audio.clip, audio.position, audio.volume);

                            // Reset the queued audio element
                            s_SurfaceAudioQueue[i] = QueuedAudio.Empty;
                        }
                    }
                }
            }
        }

        public static void ApplyOverrides(SurfaceFxOverrides overrides)
        {
            if (instance != null)
                instance.ApplyOverridesInternal(overrides);
        }

        public static void RemoveOverrides(SurfaceFxOverrides overrides)
        {
            if (instance != null)
                instance.RemoveOverridesInternal(overrides);
        }

        protected virtual void ApplyOverridesInternal(SurfaceFxOverrides overrides)
        {
            currentOverrides = overrides;

            if (s_ProxyBehaviour != null)
                s_ProxyBehaviour.SetSurfaceHitFx(overrides.impactEffects);
        }

        protected virtual void RemoveOverridesInternal(SurfaceFxOverrides overrides)
        {
            if (currentOverrides == overrides)
            {
                currentOverrides = null;

                if (s_ProxyBehaviour != null)
                    s_ProxyBehaviour.SetSurfaceHitFx(instance.m_ImpactEffects);
            }
        }

        public static void ShowBulletHit(RaycastHit hit, Vector3 rayDirection, float size, bool rigidBody = false)
        {
            if (instance == null)
                return;

            if (hit.transform == null)
            {
                Debug.LogError("SurfaceManager.ShowBulletHit() called with invalid RaycastHit data. Are you checking the raycast actually hit before sending?");
                return;
            }

            instance.ShowBulletHitInternal(hit, rayDirection, size, rigidBody);
        }
        
        public static void ShowImpactFX(FpsSurfaceMaterial surfaceMaterial, Vector3 position, Vector3 normal, bool decal = false, float size = 1f)
        {
            if (instance != null)
                instance.ShowImpactFXInternal(surfaceMaterial, position, normal, size, decal);
        }

        public static void ShowImpactFX(FpsSurfaceMaterial surfaceMaterial, Vector3 position, Vector3 normal, int layerIndex, float size = 1f)
        {
            if (instance != null)
                instance.ShowImpactFXInternal(surfaceMaterial, position, normal, size, (instance.m_DecalLayers & (1 << layerIndex)) != 0);
        }

        public static void PlayImpactNoiseAtPosition(FpsSurfaceMaterial surfaceMaterial, Vector3 position, float volume)
        {
            if (instance != null && instance.m_ImpactAudio != null)
                instance.PlayImpactNoiseAtPositionInternal(surfaceMaterial, position, volume);
        }

        protected virtual void ShowBulletHitInternal(RaycastHit hit, Vector3 rayDirection, float size, bool rigidBody)
        {
            FpsSurfaceMaterial surfaceMaterial = FpsSurfaceMaterial.Default;

            // Get the main camera (Camera.main sucks, but I can't assume all users will add a camera tracking component to any relevant camera)
            if (s_CurrentMainCamera == null || !s_CurrentMainCamera.isActiveAndEnabled)
                s_CurrentMainCamera = Camera.main;

            float distanceSquared = s_CurrentMainCamera != null ? (s_CurrentMainCamera.transform.position - hit.point).sqrMagnitude : 0f;

            // Get the surface material using BaseSurface behaviour (if present - default if not / invalid)
            BaseSurface surface = hit.collider.GetComponentInParent<BaseSurface>();
            if (surface != null)
                surfaceMaterial = surface.GetSurface(hit);

            // Show bullet hit
            var hitBehaviour = s_ProxyBehaviour.impactEffectInstances[surfaceMaterial];
            if (hitBehaviour != null && distanceSquared < (hitBehaviour.maxRange * hitBehaviour.maxRange))
            {
                bool decal = !rigidBody && (m_DecalLayers & (1 << hit.collider.gameObject.layer)) != 0;
                hitBehaviour.Hit(hit.transform.gameObject, hit.point, hit.normal, rayDirection, size, decal);
            }

            // Play bullet hit sound effect if close enough to camera
            if (m_ImpactAudio != null && distanceSquared <= m_MaxAudioDistanceSqrd)
            {
                float v = 1f;
                AudioClip clip = null;

                // Get audio clip & volume
                if (currentOverrides != null)
                {
                    clip = currentOverrides.impactAudio.GetAudioClip(surfaceMaterial, out v);
                    if (clip == null)
                        clip = instance.m_ImpactAudio.GetAudioClip(surfaceMaterial, out v);
                }
                else
                    clip = instance.m_ImpactAudio.GetAudioClip(surfaceMaterial, out v);

                if (m_ScaleVolumeToHitSize)
                    v *= Mathf.LerpUnclamped(0.5f, 1f, size);

                // Enqueue the audio if closest
                if (s_SurfaceAudioQueue[surfaceMaterial].distanceSqr > distanceSquared)
                {
                    s_SurfaceAudioQueue[surfaceMaterial] = new QueuedAudio
                    {
                        clip = clip,
                        distanceSqr = distanceSquared,
                        volume = v,
                        position = hit.point
                    };
                }
            }
        }

        protected virtual void ShowImpactFXInternal(FpsSurfaceMaterial surfaceMaterial, Vector3 position, Vector3 normal, float size, bool decal)
        {
            // Get the main camera (Camera.main sucks, but I can't assume all users will add a camera tracking component to any relevant camera)
            if (s_CurrentMainCamera == null || !s_CurrentMainCamera.isActiveAndEnabled)
                s_CurrentMainCamera = Camera.main;

            float distanceSquared = s_CurrentMainCamera != null ? (s_CurrentMainCamera.transform.position - position).sqrMagnitude : 0f;

            // Show bullet hit
            var hitBehaviour = s_ProxyBehaviour.impactEffectInstances[surfaceMaterial];
            if (hitBehaviour != null && distanceSquared < (hitBehaviour.maxRange * hitBehaviour.maxRange))
                hitBehaviour.Hit(null, position, normal, Vector3.zero, size, decal);

            // Play bullet hit sound effect if close enough to camera
            if (m_ImpactAudio != null && distanceSquared <= m_MaxAudioDistanceSqrd)
            {
                float volume = 1f;
                AudioClip clip = null;

                // Get audio clip & volume
                if (currentOverrides != null)
                {
                    clip = currentOverrides.impactAudio.GetAudioClip(surfaceMaterial, out volume);
                    if (clip == null)
                        clip = instance.m_ImpactAudio.GetAudioClip(surfaceMaterial, out volume);
                }
                else
                    clip = instance.m_ImpactAudio.GetAudioClip(surfaceMaterial, out volume);

                if (m_ScaleVolumeToHitSize)
                    volume *= size;

                // Play the audio
                NeoFpsAudioManager.PlayEffectAudioAtPosition(clip, position, volume);
            }
        }

        protected virtual void PlayImpactNoiseAtPositionInternal(FpsSurfaceMaterial surfaceMaterial, Vector3 position, float volume)
        {
            // Get audio clip
            float clipVolume = 1f;
            AudioClip clip = instance.m_ImpactAudio.GetAudioClip(surfaceMaterial, out clipVolume);
            volume *= clipVolume;

            // Play the audio
            NeoFpsAudioManager.PlayEffectAudioAtPosition(clip, position, volume);
        }
    }
}