using UnityEngine;

namespace NeoFPS.ModularFirearms
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-animatedfirearmobstaclehandler.html")]
    public class AnimatedFirearmObstacleHandler : FirearmObstacleHandler
    {
        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Bool, true), Tooltip("The name of the animator parameter to set when the weapon's obstructed state changes.")]
        private string m_ObstructedKey = "Obstructed";

        private Animator m_Animator = null;
        private int m_ObstructedHash = -1;

        protected override void Awake()
        {
            base.Awake();

            m_Animator = GetComponentInChildren<Animator>();
            if (m_Animator != null)
            {
                if (!string.IsNullOrWhiteSpace(m_ObstructedKey))
                    m_ObstructedHash = Animator.StringToHash(m_ObstructedKey);
                else
                    enabled = false;
            }
            else
                enabled = false;
        }

        protected override void OnBlockedChanged(bool blocked)
        {
            m_Animator.SetBool(m_ObstructedHash, blocked);
        }
    }
}