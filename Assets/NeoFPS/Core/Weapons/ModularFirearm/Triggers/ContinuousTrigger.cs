using UnityEngine;
using NeoSaveGames.Serialization;
using NeoSaveGames;

namespace NeoFPS.ModularFirearms
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-continuoustrigger.html")]
    public class ContinuousTrigger : BaseTriggerBehaviour
    {
        [Header("Trigger Settings")]

        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Bool, true, true), Tooltip("The bool animator property key to set while the trigger is pressed.")]
        private string m_TriggerHoldAnimKey = string.Empty;

        private bool m_Triggered = false;
        private bool m_IsShooting = false;
        private int m_TriggerHoldHash = -1;

        public override bool pressed
        {
            get { return m_Triggered; }
        }

        protected override void Awake()
        {
            if (m_TriggerHoldAnimKey != string.Empty)
                m_TriggerHoldHash = Animator.StringToHash(m_TriggerHoldAnimKey);
            base.Awake();
        }

        public override void Press()
        {
            base.Press();

            m_Triggered = true;

            // Set the trigger hold animation parameter
            if (m_TriggerHoldHash != -1)
                firearm.animationHandler.SetBool(m_TriggerHoldHash, true);
        }

        public override void Release()
        {
            base.Release();

            StopShootingInternal();
            m_Triggered = false;

            // Set the trigger hold animation parameter
            if (m_TriggerHoldHash != -1)
                firearm.animationHandler.SetBool(m_TriggerHoldHash, false);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            StopShootingInternal();
            m_Triggered = false;

            // Set the trigger hold animation parameter
            if (m_TriggerHoldHash != -1)
                firearm.animationHandler.SetBool(m_TriggerHoldHash, false);
        }

        protected override void OnSetBlocked(bool to)
        {
            base.OnSetBlocked(to);

            if (to)
                StopShootingInternal();
        }

        void StopShootingInternal()
        {
            if (m_IsShooting)
            {
                m_IsShooting = false;
                StopShootContinuous();
            }
        }

        protected override void FixedTriggerUpdate()
        {
            if (m_Triggered && !m_IsShooting && !blocked)
            {
                m_IsShooting = true;
                StartShootContinuous();
            }
        }

        private static readonly NeoSerializationKey k_TriggeredKey = new NeoSerializationKey("triggered");

        public override void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            base.WriteProperties(writer, nsgo, saveMode);

            if (saveMode == SaveMode.Default)
                writer.WriteValue(k_TriggeredKey, m_Triggered);
        }

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);

            reader.TryReadValue(k_TriggeredKey, out m_Triggered, m_Triggered);
            if (m_Triggered)
                Release();
        }
    }
}