using UnityEngine;
using NeoSaveGames;
using NeoSaveGames.Serialization;
using System;
using UnityEngine.UI;
using NeoFPS.SinglePlayer;
using NeoFPS.ModularFirearms;

namespace NeoFPS.Samples.SinglePlayer.MultiScene
{
    public class TargetHitCounter : SoloCharacterTriggerZone, INeoSerializableComponent
    {
        public Text hitCounter = null;
        public Text missCounter = null;

        static readonly NeoSerializationKey k_HitCountKey = new NeoSerializationKey("hitCount");
        static readonly NeoSerializationKey k_MissCountKey = new NeoSerializationKey("missCount");

        private static TargetHitCounter s_Instance = null;

        private ModularFirearm m_Firearm = null;
        private IShooter m_Shooter = null;
        private bool m_Hit = false;
        private bool m_Shot = false;
        private int m_HitCount = 0;
        private int m_MissCount = 0;

        void Awake()
        {
            if (s_Instance != null)
                Destroy(gameObject);
            else
                s_Instance = this;
        }

        void Start()
        {
            hitCounter.text = m_HitCount.ToString();
            missCounter.text = m_MissCount.ToString();
        }

        void OnDestroy()
        {
            if (s_Instance == this)
                s_Instance = null;
        }

        protected override void OnCharacterEntered(FpsSoloCharacter c)
        {
            base.OnCharacterEntered(c);

            var qs = c.GetComponent<IQuickSlots>();
            if (qs != null)
            {
                qs.onSelectionChanged += OnInventorySelectionChanged;
                if (qs.selected != null)
                    OnInventorySelectionChanged(qs.selected.quickSlot, qs.selected);
                else
                    OnInventorySelectionChanged(-1, null);
            }
        }

        protected override void OnCharacterExited(FpsSoloCharacter c)
        {
            base.OnCharacterExited(c);

            var qs = c.GetComponent<IQuickSlots>();
            if (qs != null)
            {
                qs.onSelectionChanged -= OnInventorySelectionChanged;
                OnInventorySelectionChanged(-1, null);
            }
        }

        private void OnInventorySelectionChanged(int slot, IQuickSlotItem item)
        {
            if (m_Firearm != null)
                m_Firearm.onShooterChange -= OnShooterChanged;

            if (item != null)
                m_Firearm = item.GetComponent<ModularFirearm>();
            else
                m_Firearm = null;

            if (m_Firearm != null)
            {
                m_Firearm.onShooterChange += OnShooterChanged;
                OnShooterChanged(m_Firearm, m_Firearm.shooter);
            }
            else
                OnShooterChanged(null, null);
        }

        private void OnShooterChanged(IModularFirearm firearm, IShooter shooter)
        {
            if (m_Shooter != null)
                m_Shooter.onShoot -= OnShotFired;

            m_Shooter = shooter;

            if (m_Shooter != null)
                m_Shooter.onShoot += OnShotFired;
        }

        private void OnShotFired(IModularFirearm firearm)
        {
            m_Shot = true;
        }

        public static void ReportHit()
        {
            if (s_Instance != null)
                s_Instance.ReportHitInternal();
        }

        void ReportHitInternal()
        {
            m_Hit = true;
        }

        void Update()
        {
            if (m_Shot)
            {
                if (m_Hit)
                {
                    ++m_HitCount;
                    hitCounter.text = m_HitCount.ToString();
                }
                else
                {
                    ++m_MissCount;
                    missCounter.text = m_MissCount.ToString();
                }
            }

            m_Shot = false;
            m_Hit = false;
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            reader.TryReadValue(k_HitCountKey, out m_HitCount, m_HitCount);
            reader.TryReadValue(k_MissCountKey, out m_MissCount, m_MissCount);
            hitCounter.text = m_HitCount.ToString();
            missCounter.text = m_MissCount.ToString();
        }

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValue(k_HitCountKey, m_HitCount);
            writer.WriteValue(k_MissCountKey, m_MissCount);
        }
    }
}