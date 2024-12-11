using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inputref-mb-neofpstouchbutton.html")]
    public class NeoFpsTouchButton : BaseTouchControl
    {
        [SerializeField, Tooltip("The button to emulate.")]
        private FpsInputButton m_Input = FpsInputButton.None;

        [Header("Events")]

        [SerializeField, Tooltip("An event fired when the player first touches this control.")]
        private UnityEvent m_OnTouchStarted = null;
        [SerializeField, Tooltip("An event fired when a touch that started on this control is released.")]
        private UnityEvent m_OnTouchEnded = null;

        public override bool HandleTouch(Touch touch)
        {
            if (m_Input != FpsInputButton.None)
                controller.buttons[m_Input] = true;

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