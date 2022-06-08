using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Artanim
{
    [CustomPropertyDrawer(typeof(EnableIfAttribute))]
    public class EnableIfPropertyDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var enableIfAttribute = attribute as EnableIfAttribute;
            var enabled = true;

            var boolPropperty = property.serializedObject.FindProperty(enableIfAttribute.BoolPropertyName);
            if (boolPropperty != null)
                enabled = boolPropperty.boolValue;


            GUI.enabled = enabled;
            EditorGUI.PropertyField(position, property, true);
            GUI.enabled = true;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

    }
}