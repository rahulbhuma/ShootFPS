using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    public class MaxValueAttribute : PropertyAttribute
    {
        public string maxValueFieldName
        {
            get;
            private set;
        }

        public MaxValueAttribute(string maxValueFieldName)
        {
            this.maxValueFieldName = maxValueFieldName;
        }
    }
}