using UnityEngine;

namespace NeoFPS.WieldableTools
{
    [HelpURL("https://docs.neofps.com/manual/inputref-mb-animatorbooltoolaction.html")]
    public class AnimatorBoolToolAction : BaseWieldableToolModule
    {
        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Bool, true, false), Tooltip("The animator bool parameter to set")]
        private string m_ParameterKey = string.Empty;
        [SerializeField, Tooltip("The value to set the parameter to while the trigger is held. The value will be reset on release")]
        private bool m_HoldValue = true;
        [SerializeField, Tooltip("If the tool is interrupted (reload) the bool paramater will be reset")]
        private bool m_Interruptable = true;
        
        private int m_ParameterHash = -1;

        public override bool isValid
        {
            get { return !string.IsNullOrWhiteSpace(m_ParameterKey); }
        }

        public override WieldableToolActionTiming timing
        {
            get { return WieldableToolActionTiming.Start | WieldableToolActionTiming.End; }
        }

        public override void Initialise(IWieldableTool t)
        {
            base.Initialise(t);
            
            if (!string.IsNullOrWhiteSpace(m_ParameterKey))
                m_ParameterHash = Animator.StringToHash(m_ParameterKey);

            if (m_ParameterHash == -1)
                enabled = false;
            else
                tool.animationHandler.SetBool(m_ParameterHash, !m_HoldValue);
        }

        public override void FireStart()
        {
            tool.animationHandler.SetBool(m_ParameterHash, m_HoldValue);
        }

        public override void FireEnd(bool success)
        {
            tool.animationHandler.SetBool(m_ParameterHash, !m_HoldValue);
        }

        public override void Interrupt()
        {
            if (m_Interruptable)
                tool.animationHandler.SetBool(m_ParameterHash, !m_HoldValue);
        }

        public override bool TickContinuous()
        {
            return true;
        }
    }
}