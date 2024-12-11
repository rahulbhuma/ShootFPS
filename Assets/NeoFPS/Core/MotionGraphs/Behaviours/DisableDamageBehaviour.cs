using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.CharacterMotion;

namespace NeoFPS.CharacterMotion.Behaviours
{
    [MotionGraphElement("Character/DisableDamage", "DisableDamageBehaviour")]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgb-disabledamagebehaviour.html")]
    public class DisableDamageBehaviour : MotionGraphBehaviour
    {
        [SerializeField] private DamageSetting m_DamageOnEnter = DamageSetting.Disable;
        [SerializeField] private DamageSetting m_DamageOnExit = DamageSetting.Enable;


        public enum DamageSetting
        {
            Enable,
            Disable,
            Ignore
        }

        private IHealthManager m_HealthManager = null;

        public override void Initialise(MotionGraphConnectable o)
        {
            base.Initialise(o);
            // Get the health manager component
            m_HealthManager = controller.GetComponent<IHealthManager>();
        }

        public override void OnEnter()
        {
            if (m_HealthManager != null)
            {
                switch (m_DamageOnEnter)
                {
                    case DamageSetting.Enable:
                        m_HealthManager.invincible = false;
                        break;
                    case DamageSetting.Disable:
                        m_HealthManager.invincible = true;
                        break;
                }
            }
        }

        public override void OnExit()
        {
            if (m_HealthManager != null)
            {
                switch (m_DamageOnExit)
                {
                    case DamageSetting.Enable:
                        m_HealthManager.invincible = false;
                        break;
                    case DamageSetting.Disable:
                        m_HealthManager.invincible = true;
                        break;
                }
            }
        }
    }
}