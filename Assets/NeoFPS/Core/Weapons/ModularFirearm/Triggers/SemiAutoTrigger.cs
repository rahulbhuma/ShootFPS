using UnityEngine;
using NeoSaveGames.Serialization;
using NeoSaveGames;

namespace NeoFPS.ModularFirearms
{
	[HelpURL("https://docs.neofps.com/manual/weaponsref-mb-semiautotrigger.html")]
	public class SemiAutoTrigger : BaseTriggerBehaviour
    {
        [Header("Trigger Settings")]

        [SerializeField, Tooltip("Cooldown between trigger pulls (number of fixed update frames).")]
		private int m_Cooldown = 0;

		[SerializeField, Tooltip("How many fixed update frames before firing again (0 = requires fresh trigger press).")]
		private int m_RepeatDelay = 0;

        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Trigger, true, true), Tooltip("The trigger animator property key to set when the trigger is pressed.")]
        private string m_TriggerPressAnimKey = string.Empty;

        private bool m_Triggered = false;
        private int m_Wait = 0;
        private int m_Repeat = 0;
        private int m_TriggerPressHash = -1;

#if UNITY_EDITOR
        protected void OnValidate()
        {
            if (m_Cooldown < 0)
                m_Cooldown = 0;
            if (m_RepeatDelay < 0)
                m_RepeatDelay = 0;
        }
#endif

        public override bool pressed
		{
			get { return m_Triggered; }
		}

        protected override void Awake()
        {
            base.Awake();

            if (m_TriggerPressAnimKey != string.Empty)
                m_TriggerPressHash = Animator.StringToHash(m_TriggerPressAnimKey);
        }

        public override void Press ()
		{
            base.Press();

            if (m_Wait == 0 || m_RepeatDelay > 0)
			    m_Triggered = true;
		}

		public override void Release ()
		{
            base.Release();

			m_Repeat = 0;
			m_Triggered = false;
        }

        protected override void FixedTriggerUpdate ()
		{
			// Decrement cooldowns
			if (m_Wait > 0)
				--m_Wait;
			if (m_Repeat > 0)
				--m_Repeat;

			// Check triggered and cooldowns
			if (m_Triggered && m_Wait == 0 && m_Repeat == 0)
			{
				// Shoot
				Shoot ();

                // Play the press animation
                if (m_TriggerPressHash != -1 && !blocked)
                    firearm.animationHandler.SetTrigger(m_TriggerPressHash);

                // Set cooldowns
                m_Wait = m_Cooldown;
				m_Repeat = m_RepeatDelay;

				// Disable repeat if required
				if (m_Repeat == 0)
					m_Triggered = false;
			}
        }

        protected override void OnSetBlocked(bool to)
        {
            base.OnSetBlocked(to);
            if (to)
            {
                m_Wait = 0;
                m_Repeat = 0;
            }
        }

        private static readonly NeoSerializationKey k_TriggeredKey = new NeoSerializationKey("triggered");
        private static readonly NeoSerializationKey k_WaitKey = new NeoSerializationKey("wait");
        private static readonly NeoSerializationKey k_RepeatKey = new NeoSerializationKey("repeat");

        public override void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            base.WriteProperties(writer, nsgo, saveMode);

            if (saveMode == SaveMode.Default)
            {
                writer.WriteValue(k_TriggeredKey, m_Triggered);
                writer.WriteValue(k_WaitKey, m_Wait);
                writer.WriteValue(k_RepeatKey, m_Repeat);
            }
        }

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);

            reader.TryReadValue(k_WaitKey, out m_Wait, m_Wait);
            reader.TryReadValue(k_RepeatKey, out m_Repeat, m_Repeat);

            reader.TryReadValue(k_TriggeredKey, out m_Triggered, m_Triggered);
            if (m_Triggered)
                Release();
        }
    }
}