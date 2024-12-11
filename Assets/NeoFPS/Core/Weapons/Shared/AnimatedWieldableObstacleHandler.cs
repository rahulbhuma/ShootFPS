using UnityEngine;

namespace NeoFPS.WieldableTools
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-animatedwieldableobstaclehandler.html")]
    public class AnimatedWieldableObstacleHandler : BaseWieldableObstacleHandler
    {
        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Bool, true), Tooltip("The name of the animator parameter to set when the weapon's obstructed state changes.")]
        private string m_ObstructedKey = "Obstructed";
        [SerializeField, Tooltip("The time to wait after setting the obstructed bool key in the animator controller to false before enabling the trigger.")]
        private float m_UnblockDelay = 0f;

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

        protected override int GetBlockingReleaseFrames()
        {
            return Mathf.CeilToInt(m_UnblockDelay / Time.fixedDeltaTime);
        }

        protected override void OnObstructedChanged(bool obstructed)
        {
            base.OnObstructedChanged(obstructed);
            m_Animator.SetBool(m_ObstructedHash, obstructed);
        }
    }
}