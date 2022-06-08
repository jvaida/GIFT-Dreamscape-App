using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Artanim
{
    [CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
    public class MinMaxRangePropertyDrawer : PropertyDrawer
    {
        private const float VALUE_FIELDS_WIDTH = 50;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(property.propertyType == SerializedPropertyType.Vector2)
            {
                //Readd tooltip becaues Unity can't...
                label.tooltip = GetToolTip();

                //Draw label
                var fieldsPosition = EditorGUI.PrefixLabel(position, label);

                //Calc positions
                var minRect = new Rect(fieldsPosition.x, position.y, VALUE_FIELDS_WIDTH, position.height);
                var maxRect = new Rect(position.width - VALUE_FIELDS_WIDTH + 12, position.y, VALUE_FIELDS_WIDTH, position.height);
                var rangeRect = new Rect(fieldsPosition.x + VALUE_FIELDS_WIDTH + 5, position.y, maxRect.x - (minRect.x + VALUE_FIELDS_WIDTH + 10), position.height);

                //Input fields
                EditorGUI.PropertyField(minRect, property.FindPropertyRelative("x"), GUIContent.none);
                EditorGUI.PropertyField(maxRect, property.FindPropertyRelative("y"), GUIContent.none);

                //Range slider
                var minMaxRange = attribute as MinMaxRangeAttribute;
                var rangeVector = property.vector2Value;
                EditorGUI.MinMaxSlider(rangeRect, ref rangeVector.x, ref rangeVector.y, minMaxRange.Min, minMaxRange.Max);
                property.vector2Value = rangeVector;
            }
            else
            {
                Debug.LogWarningFormat("MinMaxRange attribute only valid for Vector2 types. Field={0}", property.name);
                EditorGUI.PropertyField(position, property);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private string GetToolTip()
        {
            var tooltips = fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), false);
            if (tooltips != null && tooltips.Length > 0)
                return (tooltips[0] as TooltipAttribute).tooltip;
            return string.Empty;
        }

    }
}