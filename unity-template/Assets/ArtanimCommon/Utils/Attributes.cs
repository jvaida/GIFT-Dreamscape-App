using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
    public class ObjectIdAttribute : PropertyAttribute
    {

    }

    public class ReadOnlyPropertyAttribute : PropertyAttribute
    {

    }

    [AttributeUsage(AttributeTargets.Field)]
    public class MinMaxRangeAttribute : PropertyAttribute
    {
        public readonly float Min;
        public readonly float Max;

        public MinMaxRangeAttribute(float min=0f, float max=1f)
        {
            Min = min;
            Max = max;
        }
    }

    public class EnableIfAttribute : PropertyAttribute
    {
        public readonly string BoolPropertyName;

        public EnableIfAttribute(string boolPropertyName)
        {
            BoolPropertyName = boolPropertyName;
        }
    }

}