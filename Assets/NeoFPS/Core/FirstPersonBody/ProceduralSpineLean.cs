using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/fpcamref-mb-proceduralspinelean.html")]
    public class ProceduralSpineLean : BodyLeanBase
    {
        [SerializeField, Tooltip("The spine aim matcher component that actually controls the spine rotation")]
        ProceduralSpineAimMatcher m_SpineAimMatcher = null;

        protected new void OnValidate()
        {
            base.OnValidate();

            if (m_SpineAimMatcher == null)
                transform.root.GetComponentInChildren<ProceduralSpineAimMatcher>();
        }

        protected override void SetLeanZero()
        {
            base.SetLeanZero();

            if (m_SpineAimMatcher != null)
                m_SpineAimMatcher.leanAmount = 0f;
        }

        protected override void ApplyLean()
        {
            base.ApplyLean();

            if (m_SpineAimMatcher != null)
                m_SpineAimMatcher.leanAmount = currentLean;
        }

        protected new void OnDisable()
        {
            base.OnDisable();

            if (m_SpineAimMatcher != null)
                m_SpineAimMatcher.leanAmount = 0f;
        }
    }
}
