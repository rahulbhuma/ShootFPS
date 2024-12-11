using NeoSaveGames;
using NeoSaveGames.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-animatedweaponinspect.html")]
    public class AnimatedWeaponInspect : MonoBehaviour, INeoSerializableComponent
    { 
        [Header("Inspect")]

        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Bool, true, false), Tooltip("The name of the bool parameter to set on the weapon's animator while inspecting.")]
        private string m_InspectKey = "Inspect";
        [SerializeField, Tooltip("How long after releasing the inspect key, should the weapon be able to function again.")]
        private float m_ReleaseDelay = 0.25f;

        [Header ("Poses")]

        [SerializeField, Min(1), Tooltip("How many inspect poses does the weapon have.")]
        private int m_NumPoses = 1;
        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Int, true, false), Tooltip("The name of the integer parameter to set on the weapon's animator that controls the animation pose.")]
        private string m_PoseKey = "PoseIndex";
        [SerializeField, Tooltip("Should the pose index be reset when inspecting state changes.")]
        private bool m_ResetPose = true;

        private IWieldableAnimationHandler m_AnimationHandler = null;
        private int m_InspectHash = 0;
        private int m_PoseHash = 0;
        private int m_PoseIndex = 0;
        private float m_ReleaseTimer = 0f;
        private bool m_Inspecting = false;
        private bool m_InspectInput = false;
        private Coroutine m_InspectCoroutine = null;

        public IWieldable wieldable
        {
            get;
            private set;
        }

        public bool inspecting
        {
            get { return m_Inspecting; }
            set
            {
                if (m_InspectInput != value && wieldable != null)
                {
                    m_InspectInput = value;

                    // Block wieldable (can't shoot / reload, etc while inspecting)
                    if (m_InspectInput)
                    {
                        if (m_InspectCoroutine == null)
                            m_InspectCoroutine = StartCoroutine(InspectCoroutine());
                    }
                    else
                    {
                        if (!m_Inspecting && m_InspectCoroutine != null)
                        {
                            StopCoroutine(m_InspectCoroutine);
                            m_InspectCoroutine = null;
                        }
                    }
                }
            }
        }

        public virtual bool toggle
        {
            get { return false; }
        }

        public int pose
        {
            get { return m_PoseIndex; }
            set
            {
                // Clamp index
                int clamped = Mathf.Clamp(value, 0, m_NumPoses - 1);
                if (m_PoseIndex != clamped)
                {
                    // Set pose parameter
                    m_PoseIndex = clamped;
                    m_AnimationHandler.SetInteger(m_PoseHash, m_PoseIndex);
                }
            }
        }

        public int numPoses
        {
            get { return m_NumPoses; }
        }

        protected virtual void Start()
        {
            Initialise();
        }

        void Initialise()
        {
            if (wieldable == null)
            {
                wieldable = GetComponentInParent<IWieldable>();
                m_AnimationHandler = wieldable.animationHandler;
                if (!string.IsNullOrWhiteSpace(m_InspectKey))
                    m_InspectHash = Animator.StringToHash(m_InspectKey);
                if (m_NumPoses > 1 && !string.IsNullOrWhiteSpace(m_PoseKey))
                    m_PoseHash = Animator.StringToHash(m_PoseKey);
                inspecting = false;
            }
        }
        
        public void CyclePose()
        {
            if (m_PoseHash != 0)
            {
                // Cycle index
                if (++m_PoseIndex == m_NumPoses)
                    m_PoseIndex = 0;

                // Set pose parameter
                m_AnimationHandler.SetInteger(m_PoseHash, m_PoseIndex);
            }
        }

        private void OnDisable()
        {
            if (m_InspectCoroutine != null)
            {
                StopCoroutine(m_InspectCoroutine);
                m_InspectCoroutine = null;
                wieldable.RemoveBlocker(this);
            }
        }

        protected virtual void OnStartInspecting() { }
        protected virtual void OnStopInspecting() { }

        IEnumerator InspectCoroutine()
        {
            // Wait for current blocker to end
            while (m_InspectInput && wieldable.isBlocked)
                yield return null;

            // Cancel if no longer wanting to inspect (this shouldn't happen)
            if (!m_InspectInput)
            {
                m_InspectCoroutine = null;
                yield break;
            }

            // Start inspecting
            m_Inspecting = true;
            wieldable.AddBlocker(this);
            OnStartInspecting();

            // Reset release timer
            m_ReleaseTimer = m_ReleaseDelay;

            // Reset pose parameter
            if (m_ResetPose && m_PoseHash != 0)
            {
                m_PoseIndex = 0;
                m_AnimationHandler.SetInteger(m_PoseHash, m_PoseIndex);
            }

            // Set inspect parameter
            if (m_InspectHash != 0)
                m_AnimationHandler.SetBool(m_InspectHash, true);

            // Wait for inspect input to stop
            while (m_InspectInput)
                yield return null;
            OnStopInspecting();

            // Set inspect parameter
            if (m_InspectHash != 0)
                m_AnimationHandler.SetBool(m_InspectHash, false);

            // Wait for release delay
            while (m_ReleaseTimer > 0f)
            {
                yield return null;
                m_ReleaseTimer -= Time.deltaTime;
            }

            // Stop inspecting
            m_Inspecting = false;
            wieldable.RemoveBlocker(this);

            m_InspectCoroutine = null;
        }

        IEnumerator LoadCoroutine(bool input)
        {
            m_Inspecting = true;
            wieldable.AddBlocker(this);

            if (input)
            {
                m_InspectInput = true;
                yield return null;
                m_InspectInput = false;

                OnStopInspecting();

                // Set inspect parameter
                if (m_InspectHash != 0)
                    m_AnimationHandler.SetBool(m_InspectHash, false);
            }

            // Wait for release delay
            while (m_ReleaseTimer > 0f)
            {
                yield return null;
                m_ReleaseTimer -= Time.deltaTime;
            }

            // Stop inspecting
            m_Inspecting = false;
            wieldable.RemoveBlocker(this);

            m_InspectCoroutine = null;
        }

        #region INeoSerializableComponent IMPLEMENTATION

        private static readonly NeoSerializationKey k_InspectingKey = new NeoSerializationKey("inspecting");
        private static readonly NeoSerializationKey k_ReleaseKey = new NeoSerializationKey("releaseTimer");

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            if (m_InspectCoroutine != null && m_ReleaseTimer > 0f)
            {
                writer.WriteValue(k_InspectingKey, m_InspectInput);
                writer.WriteValue(k_ReleaseKey, m_ReleaseTimer);
            }
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            if (reader.TryReadValue(k_ReleaseKey, out m_ReleaseTimer, m_ReleaseTimer) && m_ReleaseTimer > 0.0001f)
            {
                reader.TryReadValue(k_InspectingKey, out bool input, false);

                Initialise();

                m_InspectCoroutine = StartCoroutine(LoadCoroutine(input));
            }
        }

        #endregion
    }
}