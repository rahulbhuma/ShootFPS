#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NeoFPS.Samples;

namespace NeoFPSEditor.Samples
{
	[CustomEditor (typeof (MultiInputToggle))]
	[CanEditMultipleObjects]
	public class MultiInputToggleEditor : MultiInputMultiChoiceBaseEditor
	{
        protected override bool showOptions
		{
			get { return serializedObject.FindProperty("m_ToggleType").enumValueIndex == 5; } // Custom
		}

        public override void OnChildInspectorGUI ()
		{
			base.OnChildInspectorGUI ();

			EditorGUILayout.LabelField ("Toggle", EditorStyles.boldLabel);

			EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_ToggleType"));
			EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_StartingValue"));
			EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_OnValueChanged"));
		}
	}
}

#endif