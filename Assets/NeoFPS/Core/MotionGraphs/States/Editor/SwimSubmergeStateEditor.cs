#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion.States;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPSEditor.CharacterMotion.States
{
    [CustomEditor(typeof(SwimSubmergeState))]
    public class SwimSubmergeStateEditor : MotionGraphStateEditor
    {
        protected override void OnInspectorGUIInternal()
        {
            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);
            MotionGraphEditorGUI.ParameterDropdownField<TransformParameter>(container, serializedObject.FindProperty("m_WaterZoneParameter"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Submerge Properties", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SubmergeDistance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Duration"));
        }
    }
}

#endif