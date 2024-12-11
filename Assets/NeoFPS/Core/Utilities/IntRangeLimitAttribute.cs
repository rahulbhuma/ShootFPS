using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    public class IntRangeLimitAttribute : PropertyAttribute
    {
        public int limitMin { get; private set; }
        public int limitMax { get; private set; }

        public IntRangeLimitAttribute(int min, int max)
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
