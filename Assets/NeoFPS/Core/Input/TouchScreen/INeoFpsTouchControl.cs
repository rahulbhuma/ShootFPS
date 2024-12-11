using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    public interface INeoFpsTouchControl
    {
        int priority { get; }
        RectTransform rectTransform { get; }

        void AddTouch();
        void RemoveTouch();
        bool HandleTouch(Touch touch);
    }
}
