using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inputref-mb-neofpstouchvirtualtrackball.html")]
    public class NeoFpsTouchVirtualTrackball : BaseTouchControl
    {
        [Header("Axes")]

        [SerializeField, Tooltip("The axis to apply the horizontal touch delta to.")]
        private FpsInputAxis m_HorizontalAxis = FpsInputAxis.MouseX;
        [SerializeField, Tooltip("The axis to apply the vertical touch delta to.")]
        private FpsInputAxis m_VerticalAxis = FpsInputAxis.MouseY;
        [SerializeField, Tooltip("A multiplier applied to the touch delta.")]
        private float m_Multiplier = 0.12f;

        [Header("Events")]

        [SerializeField, Tooltip("An event fired when the player first touches this control.")]
        private UnityEvent m_OnTouchStarted = null;
        [SerializeField, Tooltip("An event fired when a touch that started on this control is released.")]
        private UnityEvent m_OnTouchEnded = null;

        public override bool HandleTouch(Touch touch)
        {
            controller.axes[m_HorizontalAxis] += touch.deltaPosition.x * m_Multiplier;
            controller.axes[m_VerticalAxis] += touch.deltaPosition.y * m_Multiplier;

            return consume;
        }

        protected override void OnTouchStarted()
        {
            m_OnTouchStarted.Invoke();
        }

        protected override void OnTouchEnded()
        {
            m_OnTouchEnded.Invoke();
        }
    }
}