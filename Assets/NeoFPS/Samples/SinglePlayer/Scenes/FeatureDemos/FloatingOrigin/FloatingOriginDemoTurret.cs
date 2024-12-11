using NeoFPS.ModularFirearms;
using System.Collections;
using UnityEngine;

namespace NeoFPS.Samples
{
    public class FloatingOriginDemoTurret : MonoBehaviour
    {
        [SerializeField, Tooltip("The spacing between trigger release and the next trigger press.")]
        private float m_FiringInterval = 8f;

        [SerializeField, Tooltip("The amount of time to hold the trigger down for.")]
        private float m_FiringDuration = 2f;

        [SerializeField, Tooltip("The modular firearm component for the turret.")]
        private ModularFirearm m_TurretFirearm = null;

        private IEnumerator Start()
        {
            var interval = new WaitForSeconds(m_FiringInterval);
            var duration = new WaitForSeconds(m_FiringDuration);
            while (true)
            {
                yield return interval;
                m_TurretFirearm.trigger.Press();
                yield return duration;
                m_TurretFirearm.trigger.Release();
            }
        }
    }
}