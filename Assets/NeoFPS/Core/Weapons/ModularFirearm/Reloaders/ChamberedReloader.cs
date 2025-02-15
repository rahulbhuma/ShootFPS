﻿using NeoSaveGames;
using NeoSaveGames.Serialization;
using UnityEngine;
using UnityEngine.Serialization;

namespace NeoFPS.ModularFirearms
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-chamberedreloader.html")]
    public class ChamberedReloader : BaseReloaderBehaviour
    {
        [SerializeField, Tooltip("The delay type between starting and completing a reload.")]
        private FirearmDelayType m_ReloadDelayType = FirearmDelayType.ElapsedTime;

        [SerializeField, Tooltip("The time taken to reload.")]
        private float m_ReloadDuration = 2f;

        [SerializeField, Delayed, FormerlySerializedAs("m_ReloadDuration"), Tooltip("The time taken before you can use the weapon again after the reload starts.")]
        private float m_BlockingDuration = 0f;

        [SerializeField, Tooltip("The time taken to reload if reloading from empty.")]
        private float m_ReloadDurationEmpty = 3f;

        [SerializeField, Delayed, FormerlySerializedAs("m_ReloadDurationEmpty"), Tooltip("The time taken before you can use the weapon again after the reload from empty starts.")]
        private float m_BlockingDurationEmpty = 0f;

        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Trigger, true, true), Tooltip("The animator controller trigger key for the reload animation.")]
        private string m_ReloadAnimTrigger = "Reload";

        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Bool, true, true), Tooltip("The key to an animator bool parameter to set when the weapon is empty")]
        private string m_EmptyAnimBool = "Empty";

        [SerializeField, Tooltip("The audio clip to play while reloading.")]
        private AudioClip m_ReloadAudio = null;

        [SerializeField, Tooltip("The audio clip to play if reloading from empty.")]
        private AudioClip m_ReloadAudioEmpty = null;

        [SerializeField, Range(0f, 1f), Tooltip("The volume that reloading sounds are played at.")]
        private float m_Volume = 1f;

        private bool m_Reloaded = true;
        private bool m_WaitingOnExternalTrigger = false;
        private int m_ReloadAnimTriggerHash = -1;
        private int m_EmptyAnimBoolHash = -1;
        private float m_ReloadTimeout = 0f;
        private float m_BlockingTimeout = 0f;

        private bool m_Chambered = true;
        protected bool chambered
        {
            get { return m_Chambered; }
            set
            {
                if (m_Chambered != value)
                {
                    m_Chambered = value;
                    // Notify animator if empty
                    if (m_EmptyAnimBoolHash != -1)
                        firearm.animationHandler.SetBool(m_EmptyAnimBoolHash, !m_Chambered);
                }
            }
        }

        private class WaitForReload : Waitable
        {
            // Store the reloadable owner
            readonly ChamberedReloader m_Owner;
            public WaitForReload(ChamberedReloader owner) { m_Owner = owner; }

            // Check for timeout
            protected override bool CheckComplete() { return !m_Owner.m_WaitingOnExternalTrigger && m_Owner.m_ReloadTimeout == 0f && m_Owner.m_BlockingTimeout == 0f; }
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
            if (m_ReloadDurationEmpty < 0f)
                m_ReloadDurationEmpty = 0f;
        }
#endif

        protected override void Start()
        {
            if (m_BlockingDurationEmpty < m_ReloadDurationEmpty)
                m_BlockingDurationEmpty = m_ReloadDurationEmpty;
            if (m_BlockingDuration < m_ReloadDuration)
                m_BlockingDuration = m_ReloadDuration;
            m_WaitForReload = new WaitForReload(this);
            if (m_ReloadAnimTrigger != string.Empty)
                m_ReloadAnimTriggerHash = Animator.StringToHash(m_ReloadAnimTrigger);
            if (m_EmptyAnimBool != string.Empty)
                m_EmptyAnimBoolHash = Animator.StringToHash(m_EmptyAnimBool);
            base.Start();
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

        protected override void OnEnable()
        {
            base.OnEnable();

            // Notify animator if empty
            if (m_EmptyAnimBoolHash != -1)
                firearm.animationHandler.SetBool(m_EmptyAnimBoolHash, !m_Chambered);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            m_Reloaded = true;
            m_ReloadTimeout = 0f;
            m_BlockingTimeout = 0f;
            m_WaitingOnExternalTrigger = false;
        }

        void ReloadInternal()
        {
            // Record the old magazine count (to check against)
            int oldMagazineSize = currentMagazine;
            // Get the new magazine count (clamped in property)
            currentMagazine += firearm.ammo.currentAmmo;
            // Decrement the ammo
            firearm.ammo.DecrementAmmo(currentMagazine - oldMagazineSize);
            // Fire completed event
            SendReloadCompletedEvent();
        }

        protected override void OnCurrentMagazineChange(int from, int to)
        {
            base.OnCurrentMagazineChange(from, to);
            chambered = (to > 0);
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

        public override void ManualReloadComplete()
        {
            if (reloadDelayType != FirearmDelayType.ExternalTrigger)
            {
                Debug.LogError("Attempting to manually signal weapon reloaded when delay type is not set to custom");
                return;
            }
            if (!m_WaitingOnExternalTrigger)
            {
                Debug.LogError("Attempting to manually signal weapon reloaded, not expected");
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

        public override Waitable Reload()
        {
            if (!full && firearm.ammo.available)
            {
                switch (reloadDelayType)
                {
                    case FirearmDelayType.None:
                        SendReloadStartedEvent();
                        ReloadInternal();
                        break;
                    case FirearmDelayType.ElapsedTime:
                        if (m_BlockingTimeout == 0f)
                        {
                            // Fire started event
                            SendReloadStartedEvent();
                            // Set timers
                            float multiplier = GetReloadDurationMultiplier();
                            if (chambered)
                            {
                                m_ReloadTimeout = m_ReloadDuration * multiplier;
                                m_BlockingTimeout = m_BlockingDuration * multiplier;
                            }
                            else
                            {
                                m_ReloadTimeout = m_ReloadDurationEmpty * multiplier;
                                m_BlockingTimeout = m_BlockingDurationEmpty * multiplier;
                            }
                            // Trigger animation
                            if (m_ReloadAnimTriggerHash != -1)
                                firearm.animationHandler.SetTrigger(m_ReloadAnimTriggerHash);
                        }
                        break;
                    case FirearmDelayType.ExternalTrigger:
                        if (!m_WaitingOnExternalTrigger)
                        {
                            // Fire started event
                            SendReloadStartedEvent();
                            // Set trigger
                            m_WaitingOnExternalTrigger = true;
                            // Set as not reloaded
                            m_Reloaded = false;
                            // Trigger animation
                            if (m_ReloadAnimTriggerHash != -1)
                                firearm.animationHandler.SetTrigger(m_ReloadAnimTriggerHash);
                        }
                        break;
                }
                // Play audio
                if (chambered)
                {
                    if (m_ReloadAudio != null)
                        firearm.PlaySound(m_ReloadAudio, m_Volume);
                }
                else
                {
                    if (m_ReloadAudioEmpty != null)
                        firearm.PlaySound(m_ReloadAudioEmpty, m_Volume);
                }
            }
            return waitForReload;
        }

        private static readonly NeoSerializationKey k_TimeoutKey = new NeoSerializationKey("timeout");
        private static readonly NeoSerializationKey k_ChamberedKey = new NeoSerializationKey("chambered");
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

            writer.WriteValue(k_ChamberedKey, m_Chambered);

            base.WriteProperties(writer, nsgo, saveMode);
        }

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            reader.TryReadValue(k_TimeoutKey, out m_ReloadTimeout, m_ReloadTimeout);
            reader.TryReadValue(k_BlockingKey, out m_BlockingTimeout, m_BlockingTimeout);
            reader.TryReadValue(k_ReloadedKey, out m_Reloaded, m_Reloaded);
            reader.TryReadValue(k_ChamberedKey, out m_Chambered, m_Chambered);

            base.ReadProperties(reader, nsgo);
        }

        #endregion
    }
}