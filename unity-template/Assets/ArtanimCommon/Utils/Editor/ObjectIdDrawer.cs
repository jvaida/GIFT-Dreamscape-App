using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Artanim
{

    [CustomPropertyDrawer(typeof(ObjectIdAttribute))]
    public class ObjectIdDrawer : PropertyDrawer
    {
        private enum EObjectIdType { Manual, Guid, GameObjectName}

        private int TypeIndex;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            {
                var verticalSpace = EditorGUIUtility.standardVerticalSpacing;
                var headerPosition = new Rect(position.x, position.y + verticalSpace, position.width, EditorGUIUtility.singleLineHeight);
                var popupPosition = new Rect(position.x, headerPosition.y + headerPosition .height + verticalSpace, position.width, EditorGUIUtility.singleLineHeight);
                var inputPosition = new Rect(position.x, popupPosition.y + popupPosition.height + verticalSpace, position.width, EditorGUIUtility.singleLineHeight);
                var helpBoxPosition = new Rect(
                    position.x + EditorGUIUtility.labelWidth, 
                    inputPosition.y + inputPosition.height + verticalSpace, 
                    position.width - EditorGUIUtility.labelWidth, 60f - EditorGUIUtility.singleLineHeight);

                //Get current id type
                TypeIndex = (int)IdentifyType(property);

                //Header
                var origFontStyle = EditorStyles.label.fontStyle;
                EditorStyles.label.fontStyle = FontStyle.Bold;
                EditorGUI.LabelField(headerPosition, label);
                EditorStyles.label.fontStyle = origFontStyle;

                //Selection and input field
                EditorGUI.indentLevel = 1;
                var prevIndex = TypeIndex;
                TypeIndex = EditorGUI.Popup(popupPosition, "Type", TypeIndex, Enum.GetNames(typeof(EObjectIdType)));

                //Property field
                if((EObjectIdType)TypeIndex != EObjectIdType.Guid)
                {
                    EditorGUI.PropertyField(inputPosition, property, new GUIContent("Id", label.tooltip), false);
                }
                else
                {
                    //Draw with regenerate
                    inputPosition.width -= 65f;
                    EditorGUI.PropertyField(inputPosition, property, new GUIContent("Id", label.tooltip), false);
                    inputPosition.x = inputPosition.width + 18f; //???
                    inputPosition.width = 60f;
                    if(GUI.Button(inputPosition, "Renew"))
                    {
                        property.stringValue = null;
                    }
                }

                //Info box
                var numDuplicates = GetNumDuplicateIds(property);
                if(numDuplicates == 0)
                {
                    EditorGUI.HelpBox(helpBoxPosition,
                        string.Format("Found no other {0} with the same ObjectId.", property.serializedObject.targetObject.GetType().Name), MessageType.Info);
                }
                else
                {
                    EditorGUI.HelpBox(helpBoxPosition,
                        string.Format("Found {0} other {1} with the same ObjectId.", numDuplicates, property.serializedObject.targetObject.GetType().Name), MessageType.Warning);
                }
                

                //Identify new type in case user typed something
                if (TypeIndex != (int)EObjectIdType.Manual && prevIndex == TypeIndex)
                    TypeIndex = (int)IdentifyType(property);

                //Update value if needed
                UpdateObjectId(property, (EObjectIdType)TypeIndex);
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 3 + 60f;
        }

        private int GetNumDuplicateIds(SerializedProperty property)
        {
            var numDuplicates = 0;
            foreach (var obj in UnityEngine.Object.FindObjectsOfType(property.serializedObject.targetObject.GetType()))
            {
                if (obj != property.serializedObject.targetObject)
                {
                    var otherObj = new SerializedObject(obj);
                    var otherProperty = otherObj.FindProperty(property.name);

                    if (property.stringValue == otherProperty.stringValue)
                    {
                        numDuplicates++;
                    }
                }
            }
            return numDuplicates;
        }

        private EObjectIdType IdentifyType(SerializedProperty property)
        {
            //Guid?
            try
            {
                new Guid(property.stringValue);
                return EObjectIdType.Guid;
            }
            catch { }

            //Game Object Name?
            if (property.stringValue == property.serializedObject.targetObject.name)
                return EObjectIdType.GameObjectName;

            return EObjectIdType.Manual;
        }

        private void UpdateObjectId(SerializedProperty property, EObjectIdType type)
        {
            //Generate based on type
            switch (type)
            {
                case EObjectIdType.Guid:

                    //Already a Guid?
                    var guid = Guid.Empty;
                    try { guid = new Guid(property.stringValue); } catch { guid = Guid.Empty; }
                    if (guid == Guid.Empty)
                    {
                        //Nope, generate new one
                        property.stringValue = Guid.NewGuid().ToString();
                    }
                    break;

                case EObjectIdType.GameObjectName:
                    property.stringValue = property.serializedObject.targetObject.name;
                    break;

                case EObjectIdType.Manual:
                    break;
                default:
                    break;
            }

            //Check for default value
            if(string.IsNullOrEmpty(property.stringValue))
            {
                //Default GUID
                property.stringValue = Guid.NewGuid().ToString();
            }
        }
    }
}