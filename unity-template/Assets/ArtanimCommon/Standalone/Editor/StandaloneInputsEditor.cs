using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Artanim
{
	[InitializeOnLoad]
	public class TestEditor
	{
		private const string PATH_INPUT_MANAGER = "ProjectSettings/InputManager.asset";
		private const string PROPERTY_AXES = "m_Axes";

		private const string MENU_CREATE_OCULUS_INPUTS = "Artanim/Development Mode/Inputs/Create Oculus Touch Inputs";
		private const string MENU_CREATE_MR_INPUTS = "Artanim/Development Mode/Inputs/Create MR Inputs";
		private const string MENU_REMOVE_INPUTS = "Artanim/Development Mode/Inputs/Remove Inputs";

        #region Input Axis Definitions

        private readonly static List<InputAxis> INPUTS_OCULUS_TOUCH = new List<InputAxis>
		{
            //Navigation
			new InputAxis() { name = DevelopmentMode.AXIS_STANDALONE_MOVE_FORWARD, gravity = 0.0f, dead = 0.19f, sensitivity = 1.0f, invert = true, type = AxisType.JoystickAxis, axis = 2 },
			new InputAxis() { name = DevelopmentMode.AXIS_STANDALONE_MOVE_STRAFING, gravity = 0.0f, dead = 0.19f, sensitivity = 1.0f, type = AxisType.JoystickAxis, axis = 1 },
			new InputAxis() { name = DevelopmentMode.AXIS_STANDALONE_ROTATION, gravity = 0.0f, dead = 0.19f, sensitivity = 1.0f, type = AxisType.JoystickAxis, axis = 4 },

            //Pickup
			new InputAxis() { name = DevelopmentMode.AXIS_STANDALONE_PICKUP_LEFT, gravity = 0.0f, dead = 0.19f, sensitivity = 1.5f, type = AxisType.JoystickAxis, axis = 9 },
			new InputAxis() { name = DevelopmentMode.AXIS_STANDALONE_PICKUP_RIGHT, gravity = 0.0f, dead = 0.19f, sensitivity = 1.5f, type = AxisType.JoystickAxis, axis = 10 },

            //Right Hand HUD
			new InputAxis() { name = DevelopmentMode.AXIS_STANDALONE_POPUP_RIGHT, gravity = 0.0f, dead = 0.19f, sensitivity = 1.5f, type = AxisType.JoystickAxis, axis = 12 },
			new InputAxis() { name = DevelopmentMode.AXIS_STANDALONE_POPUP_RIGHT_VERTICAL, gravity = 0.0f, dead = 0.19f, sensitivity = 1.5f, type = AxisType.JoystickAxis, axis = 5 },
			new InputAxis() { name = DevelopmentMode.BUTTON_STANDALONE_POPUP_RIGHT_SELECT, positiveButton = "joystick button 9", gravity = 1000f, dead = 0.001f, sensitivity = 1000f, type = AxisType.KeyOrMouseButton, axis = 1 },

            //Left Hand HUD
            new InputAxis() { name = DevelopmentMode.AXIS_STANDALONE_POPUP_LEFT, gravity = 0.0f, dead = 0.19f, sensitivity = 1.5f, type = AxisType.JoystickAxis, axis = 11 },
            new InputAxis() { name = DevelopmentMode.AXIS_STANDALONE_POPUP_LEFT_VERTICAL, gravity = 0.0f, dead = 0.19f, sensitivity = 1.0f, invert = true, type = AxisType.JoystickAxis, axis = 2 },
            new InputAxis() { name = DevelopmentMode.BUTTON_STANDALONE_POPUP_LEFT_SELECT, positiveButton = "joystick button 8", gravity = 1000f, dead = 0.001f, sensitivity = 1000f, type = AxisType.KeyOrMouseButton, axis = 1 },

            //Additional actions
            new InputAxis() { name = DevelopmentMode.BUTTON_STANDALONE_RECALIBRATE, positiveButton = "joystick button 0", gravity = 1000.0f, dead = 0.001f, sensitivity = 1000.0f, type = AxisType.KeyOrMouseButton, axis = 1 },
		};

		private readonly static List<InputAxis> INPUTS_MR = new List<InputAxis>
		{
            //Navigation
			new InputAxis() { name = DevelopmentMode.AXIS_STANDALONE_MOVE_FORWARD, gravity = 0.0f, dead = 0.19f, sensitivity = 1.0f, invert = true, type = AxisType.JoystickAxis, axis = 18 },
			new InputAxis() { name = DevelopmentMode.AXIS_STANDALONE_MOVE_STRAFING, gravity = 0.0f, dead = 0.19f, sensitivity = 1.0f, type = AxisType.JoystickAxis, axis = 17 },
			new InputAxis() { name = DevelopmentMode.AXIS_STANDALONE_ROTATION, gravity = 0.0f, dead = 0.19f, sensitivity = 1.0f, type = AxisType.JoystickAxis, axis = 19 },

            //Pickup
			new InputAxis() { name = DevelopmentMode.AXIS_STANDALONE_PICKUP_LEFT, gravity = 0.0f, dead = 0.19f, sensitivity = 1.5f, type = AxisType.JoystickAxis, axis = 9 },
			new InputAxis() { name = DevelopmentMode.AXIS_STANDALONE_PICKUP_RIGHT, gravity = 0.0f, dead = 0.19f, sensitivity = 1.5f, type = AxisType.JoystickAxis, axis = 10 },

            //Right Hand HUD
			new InputAxis() { name = DevelopmentMode.AXIS_STANDALONE_POPUP_RIGHT, gravity = 0.0f, dead = 0.19f, sensitivity = 1.5f, type = AxisType.JoystickAxis, axis = 12 },
			new InputAxis() { name = DevelopmentMode.AXIS_STANDALONE_POPUP_RIGHT_VERTICAL, gravity = 0.0f, dead = 0.19f, sensitivity = 1.5f, type = AxisType.JoystickAxis, axis = 20 },
			new InputAxis() { name = DevelopmentMode.BUTTON_STANDALONE_POPUP_RIGHT_SELECT, positiveButton = "joystick button 9", gravity = 1000f, dead = 0.001f, sensitivity = 1000f, type = AxisType.KeyOrMouseButton, axis = 1 },

            //Left Hand HUD
            new InputAxis() { name = DevelopmentMode.AXIS_STANDALONE_POPUP_LEFT, gravity = 0.0f, dead = 0.19f, sensitivity = 1.5f, type = AxisType.JoystickAxis, axis = 11 },
			new InputAxis() { name = DevelopmentMode.AXIS_STANDALONE_POPUP_LEFT_VERTICAL, gravity = 0.0f, dead = 0.19f, sensitivity = 1.0f, invert = true, type = AxisType.JoystickAxis, axis = 18 },
			new InputAxis() { name = DevelopmentMode.BUTTON_STANDALONE_POPUP_LEFT_SELECT, positiveButton = "joystick button 8", gravity = 1000f, dead = 0.001f, sensitivity = 1000f, type = AxisType.KeyOrMouseButton, axis = 1 },

            //Additional actions
            new InputAxis() { name = DevelopmentMode.BUTTON_STANDALONE_RECALIBRATE, positiveButton = "joystick button 0", gravity = 1000.0f, dead = 0.001f, sensitivity = 1000.0f, type = AxisType.KeyOrMouseButton, axis = 1 },
		};

		#endregion

		#region Editor menu

		[MenuItem(MENU_CREATE_OCULUS_INPUTS, priority = 1)]
		private static void DoCreateOculusTouchInputs()
		{
			//Remove all standalone inputs first
			DoRemoveInputs();

			//Create oculus touch inputs
			foreach (var inputAxis in INPUTS_OCULUS_TOUCH)
			{
				AddInput(inputAxis);
			}
		}

		[MenuItem(MENU_CREATE_MR_INPUTS, priority = 1)]
		private static void DoCreateMRTouchInputs()
		{
			//Remove all standalone inputs first
			DoRemoveInputs();

			//Create oculus touch inputs
			foreach (var inputAxis in INPUTS_MR)
			{
				AddInput(inputAxis);
			}
		}


		[MenuItem(MENU_REMOVE_INPUTS, priority = 2)]
		private static void DoRemoveInputs()
		{
			foreach (var inputAxis in INPUTS_OCULUS_TOUCH)
			{
				RemoveInput(inputAxis.name);
			}
		}

		#endregion

		#region Internals

		private static SerializedObject ReadInputManager()
		{
			var inputManagerSer = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(PATH_INPUT_MANAGER)[0]);
			return inputManagerSer;
		}

		private static SerializedProperty GetAxes()
		{
			var serializedObject = ReadInputManager();
			var axesProperty = serializedObject.FindProperty(PROPERTY_AXES);
			return axesProperty;
		}

		private static SerializedProperty GetChildProperty(SerializedProperty parent, string name)
		{
			SerializedProperty child = parent.Copy();
			child.Next(true);
			do
			{
				if (child.name == name)
					return child;
			}
			while (child.Next(false));
			return null;
		}

		private static bool IsAxisDefined(string axisName)
		{
			SerializedObject serializedObject = ReadInputManager();
			SerializedProperty axesProperty = serializedObject.FindProperty(PROPERTY_AXES);

			axesProperty.Next(true);
			axesProperty.Next(true);
			while (axesProperty.Next(false))
			{
				SerializedProperty axis = axesProperty.Copy();
				axis.Next(true);
				if (axis.stringValue == axisName) return true;
			}
			return false;
		}

		private static int GetIndexOfInput(string name)
		{
			var axesProperty = GetAxes();

			for (var i = 0; i < axesProperty.arraySize; ++i)
			{
				var property = axesProperty.GetArrayElementAtIndex(i);
				var nameProp = GetChildProperty(property, "m_Name");
				if (nameProp != null)
				{
					if (nameProp.stringValue == name)
						return i;
				}
			}

			return -1;
		}

		private static void RemoveInput(string axisName)
		{
			var inputManagerSerialized = ReadInputManager();
			var axesProperty = inputManagerSerialized.FindProperty(PROPERTY_AXES);

			var index = GetIndexOfInput(axisName);
			if (index > -1)
			{
				axesProperty.DeleteArrayElementAtIndex(index);
				inputManagerSerialized.ApplyModifiedProperties();
			}
			return;

		}

		private static void AddInput(InputAxis axis)
		{
			if (IsAxisDefined(axis.name)) return;

			SerializedObject serializedObject = ReadInputManager();
			SerializedProperty axesProperty = serializedObject.FindProperty(PROPERTY_AXES);

			axesProperty.arraySize++;
			serializedObject.ApplyModifiedProperties();

			SerializedProperty axisProperty = axesProperty.GetArrayElementAtIndex(axesProperty.arraySize - 1);

			GetChildProperty(axisProperty, "m_Name").stringValue = axis.name;
			GetChildProperty(axisProperty, "descriptiveName").stringValue = axis.descriptiveName;
			GetChildProperty(axisProperty, "descriptiveNegativeName").stringValue = axis.descriptiveNegativeName;
			GetChildProperty(axisProperty, "negativeButton").stringValue = axis.negativeButton;
			GetChildProperty(axisProperty, "positiveButton").stringValue = axis.positiveButton;
			GetChildProperty(axisProperty, "altNegativeButton").stringValue = axis.altNegativeButton;
			GetChildProperty(axisProperty, "altPositiveButton").stringValue = axis.altPositiveButton;
			GetChildProperty(axisProperty, "gravity").floatValue = axis.gravity;
			GetChildProperty(axisProperty, "dead").floatValue = axis.dead;
			GetChildProperty(axisProperty, "sensitivity").floatValue = axis.sensitivity;
			GetChildProperty(axisProperty, "snap").boolValue = axis.snap;
			GetChildProperty(axisProperty, "invert").boolValue = axis.invert;
			GetChildProperty(axisProperty, "type").intValue = (int)axis.type;
			GetChildProperty(axisProperty, "axis").intValue = axis.axis - 1;
			GetChildProperty(axisProperty, "joyNum").intValue = axis.joyNum;

			serializedObject.ApplyModifiedProperties();
		}

		#endregion

		#region Classes and structures

		public enum AxisType
		{
			KeyOrMouseButton = 0,
			MouseMovement = 1,
			JoystickAxis = 2
		};

		public class InputAxis
		{
			public string name;
			public string descriptiveName;
			public string descriptiveNegativeName;
			public string negativeButton;
			public string positiveButton;
			public string altNegativeButton;
			public string altPositiveButton;

			public float gravity;
			public float dead;
			public float sensitivity;

			public bool snap = false;
			public bool invert = false;

			public AxisType type;

			public int axis;
			public int joyNum;
		}


		#endregion
	}
}