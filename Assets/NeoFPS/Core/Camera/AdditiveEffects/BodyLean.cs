using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using NeoSaveGames;
using NeoSaveGames.Serialization;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/fpcamref-mb-bodylean.html")]
    public class BodyLean : BodyLeanBase
    {
        [SerializeField, Tooltip("The vertical offset of the pivot")]
        private float m_PivotOffset = -1f;

        private Vector3 m_LeanPosition = Vector3.zero;

        public override Vector3 position
        {
            get { return m_LeanPosition; }
        }

        protected new void OnValidate()
        {
            base.OnValidate();

            m_PivotOffset = Mathf.Clamp(m_PivotOffset, -2f, 0f);
        }

        protected override void SetLeanZero()
        {
            base.SetLeanZero();

            m_LeanPosition = Vector3.zero;
        }

        protected override void ApplyLean()
        {
            base.ApplyLean();

            Vector3 leanPivot = new Vector3(0f, m_PivotOffset, 0f);
            m_LeanPosition = (bodyPivotRotation * -leanPivot) + leanPivot;
        }
    }
}