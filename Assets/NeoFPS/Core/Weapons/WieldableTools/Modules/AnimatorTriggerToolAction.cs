using UnityEngine;

namespace NeoFPS.WieldableTools
{
    [HelpURL("https://docs.neofps.com/manual/inputref-mb-animatortriggertoolaction.html")]
    public class AnimatorTriggerToolAction : BaseWieldableToolModule
    {
        [SerializeField, FlagsEnum, Tooltip("When should the trigger fire")]
        private WieldableToolOneShotTiming m_Timing = WieldableToolOneShotTiming.Start;
        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Trigger, true, false), Tooltip("The animator trigger parameter to fire")]
        private string m_ParameterKey = string.Empty;
        [SerializeField, Tooltip("The tool will be prevented from re-triggering or deselecting for this duration")]
        private float m_Duration = 0f;

        private int m_ParameterHash = -1;
        private float m_BlockingCountdown = 0f;

        public override bool busy
        {
            get { return m_BlockingCountdown > 0f; }
        }

        public override bool isValid
        {
            get { return !string.IsNullOrWhiteSpace(m_ParameterKey) && m_Timing != 0; }
        }

        public override WieldableToolActionTiming timing
        {
            get { return (WieldableToolActionTiming)m_Timing; }
        }

        public override void Initialise(IWieldableTool t)
        {
            base.Initialise(t);
            
            if (!string.IsNullOrWhiteSpace(m_ParameterKey))
                m_ParameterHash = Animator.StringToHash(m_ParameterKey);

            if (m_ParameterHash == -1 || m_Timing == 0)
                enabled = false;
        }

        public override void FireStart()
        {
            if (m_ParameterHash != -1)
                tool.animationHandler.SetTrigger(m_ParameterHash);

            m_BlockingCountdown = m_Duration;
        }

        public override void FireEnd(bool success)
        {
            if (m_ParameterHash != -1)
                tool.animationHandler.SetTrigger(m_ParameterHash);
        }

        public override bool TickContinuous()
        {
            return true;
        }

        public override void Interrupt()
        {
            base.Interrupt();

            m_BlockingCountdown = 0f;
        }

        private void Update()
        {
            m_BlockingCountdown -= Time.deltaTime;
        }
    }
}