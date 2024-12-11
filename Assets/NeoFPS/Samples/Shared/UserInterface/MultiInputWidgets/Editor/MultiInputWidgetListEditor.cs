#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using NeoFPS.Samples;

namespace NeoFPSEditor.Samples
{
	[CustomEditor(typeof(MultiInputWidgetList), true)]
	[CanEditMultipleObjects]
	public class MultiInputWidgetListEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Layout"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_NavigateLeft"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_NavigateRight"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_NavigateUp"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_NavigateDown"));

			serializedObject.ApplyModifiedProperties();
		}
	}
}

#endif