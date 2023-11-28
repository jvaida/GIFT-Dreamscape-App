using UnityEditor;
using UnityEngine;

namespace Dreamscape
{

	public static class DMX_editorList
	{
		public static void Show(SerializedProperty list)
		{
			EditorGUILayout.PropertyField(list);

			EditorGUI.indentLevel += 1;
			if (list.isExpanded)
			{
				for (int i = 0; i < list.arraySize; i++)
				{
					DMX_editorChannel.Show(list.GetArrayElementAtIndex(i));
				}
			}
			EditorGUI.indentLevel -= 1;
		}
	}

}