using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    public abstract class BaseTouchControl : MonoBehaviour, INeoFpsTouchControl
    {
        [Header ("Touch Control")]

        [SerializeField, Tooltip("")]
        private int m_Priority = 0;
        [SerializeField, Tooltip("")]
        private bool m_Consume = true;

        protected NeoFpsTouchScreenController controller { get; private set; }

        public int priority { get { return m_Priority; } }

        public bool consume { get { return m_Consume; } }

        public RectTransform rectTransform { get; private set; }

        private int m_TouchCount = 0;

        protected virtual void Awake()
        {
            rectTransform = transform as RectTransform;
            controller = GetComponentInParent<NeoFpsTouchScreenController>();
            if (controller == null)
                gameObject.SetActive(false);
        }

        protected void OnEnable()
        {
            controller.RegisterTouchControl(this);
        }

        protected void OnDisable()
        {
            controller.UnregisterTouchControl(this);
        }

        public void AddTouch()
        {
            if (++m_TouchCount == 1)
                OnTouchStarted();

            // Safety check
            Debug.Assert(m_TouchCount <= Input.touchCount);
        }

        public void RemoveTouch()
        {
            if (--m_TouchCount == 0)
                OnTouchEnded();

            // Safety check
            Debug.Assert(m_TouchCount >= 0);
        }

        protected abstract void OnTouchStarted();
        protected abstract void OnTouchEnded();
        public abstract bool HandleTouch(Touch touch);
    }
}
