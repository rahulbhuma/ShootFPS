using UnityEngine;
using UnityEngine.UI;
using NeoFPS;

namespace NeoFPS.AI
{
    [HelpURL("https://docs.neofps.com/manual/hudref-mb-hudhitdamagecounterstandard.html")]
    public class HudHitDamageCounterStandard : HudHitDamageCounterBase
    {
        [SerializeField, Tooltip("The text component used to display the damage amount")]
        private Text m_ValueText = null;
        [SerializeField, Tooltip("The text colour used to represent standard damage")]
        private Color m_StandardColour = Color.cyan;
        [SerializeField, Tooltip("The text colour used to represent critical hits")]
        private Color m_CriticalColour = Color.magenta;
        [SerializeField, Tooltip("The speed (m/s) the counter appears to move upwards after spawning")]
        private float m_Speed = 0.5f;

        protected Vector3 position;

        public override void Initialise(Vector3 worldPos, float damage, bool critical)
        {
            m_ValueText.text = ((int)damage).ToString();
            if (critical)
                m_ValueText.color = m_CriticalColour;
            else
                m_ValueText.color = m_StandardColour;

            position = worldPos;
        }

        public override bool Tick()
        {
            position.y += Time.deltaTime * m_Speed;
            return base.Tick();
        }

        public override Vector3 GetWorldPosition()
        {
            return position;
        }
    }
}