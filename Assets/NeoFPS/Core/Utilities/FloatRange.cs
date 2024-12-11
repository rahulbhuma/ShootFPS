using System;
using UnityEngine;

namespace NeoFPS
{ 
    [Serializable]
    public struct FloatRange
    {
        public float min;
        public float max;

        public FloatRange(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public static readonly FloatRange Null = new FloatRange(0f, 0f);
    }
}
