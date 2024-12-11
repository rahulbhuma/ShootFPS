using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    public class FloatRangeLimitAttribute : PropertyAttribute
    {
        public float limitMin { get; private set; }
        public float limitMax { get; private set; }

        public FloatRangeLimitAttribute(float min, float max)
        {
            if (min <= max)
            {
                limitMin = min;
                limitMax = max;
            }
            else
            {
                limitMin = max;
                limitMax = min;
            }
        }
    }
}
