using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS.Samples
{
    public class ShakeDemoThumper : MonoBehaviour
    {
        [SerializeField, Tooltip("")]
        private Transform[] m_Pistons = { };

        [SerializeField, Tooltip("")]
        private float m_RaiseHeight = 4f;

        [SerializeField, Tooltip("")]
        private float m_Spacing = 0.5f;

        [SerializeField, Tooltip("")]
        private float m_Interval = 5;

        [SerializeField, Tooltip("")]
        private float m_RaiseDuration = 1.5f;

        [SerializeField, Tooltip("")]
        private float m_FallDuration = 0.5f;

        [SerializeField, Tooltip("")]
        private UnityEvent m_OnThump = null;

        private int m_ThumpCount = 0;

        private void Update()
        {
            float time = Time.timeSinceLevelLoad;

            PositionPistons(Mathf.Repeat(time, m_Interval));

            int targetThumpCount = (int)(time / m_Interval);
            if (targetThumpCount != m_ThumpCount)
            {
                // Fire event
                m_OnThump.Invoke();

                m_ThumpCount = targetThumpCount;
            }
        }

        void PositionPistons(float time)
        {
            if (time < m_Interval - m_FallDuration)
            {
                for (int i = 0; i < m_Pistons.Length; ++i)
                {
                    // Get raise time
                    float raiseTime = time - i * m_Spacing;
                    raiseTime /= m_RaiseDuration;
                    raiseTime = Mathf.Clamp01(raiseTime);

                    var pos = m_Pistons[i].localPosition;
                    pos.y = EasingFunctions.EaseInOutCubic(raiseTime) * m_RaiseHeight;
                    m_Pistons[i].localPosition = pos;
                }
            }
            else
            {
                float fallTime = Mathf.Abs((time - m_Interval) / m_FallDuration);

                for (int i = 0; i < m_Pistons.Length; ++i)
                {
                    var pos = m_Pistons[i].localPosition;
                    pos.y = EasingFunctions.EaseOutCubic(fallTime) * m_RaiseHeight;
                    m_Pistons[i].localPosition = pos;
                }
            }
        }
    }
}