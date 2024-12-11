using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inputref-mb-neofpstouchscreencontroller.html")]
    public class NeoFpsTouchScreenController : FpsInput, IVirtualInput
    {
        private const int k_InitialNumTouches = 10;
        private const int k_MaxHandlers = 4;

        protected List<INeoFpsTouchControl> touchControls { get; } = new List<INeoFpsTouchControl>();

        public float[] axes { get; private set; } = new float[FpsInputAxis.count];
        public bool[] buttons { get; private set; } = new bool[FpsInputButton.count];

        private Stack<TouchProcessor> m_TouchPool = new Stack<TouchProcessor>(k_InitialNumTouches);
        private Dictionary<int, TouchProcessor> m_ActiveTouches = new Dictionary<int, TouchProcessor>(k_InitialNumTouches);
        private List<int> m_PendingTouches = new List<int>(k_InitialNumTouches);

        public override FpsInputContext inputContext
        {
            get { return FpsInputContext.Character; }
        }

        protected override void OnAwake()
        {
            // Initialise touch processors
            for (int i = 0; i < k_InitialNumTouches; ++i)
                m_TouchPool.Push(new TouchProcessor());

            // Disable mouse emulation for proper touch handling
            Input.simulateMouseWithTouches = false;
        }

        protected override void UpdateInput()
        {
            // Reset inputs
            for (int i = 0; i < axes.Length; ++i)
                axes[i] = 0f;
            for (int i = 0; i < buttons.Length; ++i)
                buttons[i] = false;

            // Gather existing touch
            foreach (var id in m_ActiveTouches.Keys)
                m_PendingTouches.Add(id);

            // Iterate through touches
            for (int i = 0; i < Input.touchCount; ++i)
            {
                // Get the touch
                var touch = Input.GetTouch(i);

                // Check if touch is valid
                bool valid = touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;

                // Remove handled touch
                if (valid)
                    m_PendingTouches.Remove(touch.fingerId);

                // Check touch processors
                TouchProcessor processor = null;
                if (valid && !m_ActiveTouches.TryGetValue(touch.fingerId, out processor))
                {
                    if (m_TouchPool.Count == 0)
                    {
                        processor = new TouchProcessor();
                        m_ActiveTouches.Add(touch.fingerId, processor);
                    }
                    else
                    {
                        processor = m_TouchPool.Pop();
                        m_ActiveTouches.Add(touch.fingerId, processor);
                    }

                    // Get controls under touch
                    for (int j = touchControls.Count - 1; j >= 0; --j)
                    {
                        var control = touchControls[j];

                        if (RectTransformUtility.RectangleContainsScreenPoint(control.rectTransform, touch.position, null))
                        {
                            control.AddTouch();
                            processor.controls.Add(control);
                        }
                    }
                }

                // Handle the touch input
                if (processor != null)
                    processor.HandleTouch(touch);
            }

            // Clean up touches that weren't handled
            for (int i = 0; i < m_PendingTouches.Count; ++i)
            {
                int id = m_PendingTouches[i];

                // Get the touch processor
                var processor = m_ActiveTouches[id];

                // rEturn to pool
                processor.Reset();
                m_TouchPool.Push(processor);

                // Remove the touch from active
                m_ActiveTouches.Remove(id);
            }
            m_PendingTouches.Clear();
        }

        class TouchProcessor
        {
            public List<INeoFpsTouchControl> controls = new List<INeoFpsTouchControl>(k_MaxHandlers);

            public void Reset()
            {
                for (int i = 0; i < controls.Count; ++i)
                    controls[i].RemoveTouch();
                controls.Clear();
            }

            public void HandleTouch(Touch touch)
            {
                // Iterate through controls until consumed
                for (int i = 0; i < controls.Count; ++i)
                {
                    if (controls[i].HandleTouch(touch))
                        break;
                }
            }
        }

        void ResetAllInputs()
        {
            // Clear active touches if the app is minimised
            foreach (var processor in m_ActiveTouches.Values)
            {
                processor.Reset();
                m_TouchPool.Push(processor);
            }
            m_ActiveTouches.Clear();

            // Reset inputs
            for (int i = 0; i < axes.Length; ++i)
                axes[i] = 0f;
            for (int i = 0; i < buttons.Length; ++i)
                buttons[i] = false;
        }

        void OnApplicationFocus(bool focus)
        {
            ResetAllInputs();
        }

        public void RegisterTouchControl(INeoFpsTouchControl control)
        {
            if (control != null)
            {
                touchControls.Add(control);
                touchControls.Sort((x, y) => { return x.priority - y.priority; });
            }
        }

        public void UnregisterTouchControl(INeoFpsTouchControl control)
        {
            touchControls.Remove(control);
        }

        protected override void OnGainFocus()
        {
            // Set as virtual input handler
            NeoFpsInputManager.virtualInput = this;
        }

        protected override void OnLoseFocus()
        {
            // Detach as virtual input handler
            NeoFpsInputManager.virtualInput = null;

            ResetAllInputs();
        }

        public float GetVirtualAxis(FpsInputAxis axis)
        {
            return axes[axis];
        }

        public bool GetVirtualButton(FpsInputButton button)
        {
            return buttons[button];
        }
    }
}