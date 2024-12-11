#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion.States;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPSEditor.CharacterMotion.States
{
    [CustomEditor(typeof(AnimCurveDirectionalDashState))]
    //[HelpURL("")]
    public class AnimCurveDirectionalDashStateEditor : MotionGraphStateEditor
    {
        protected override void OnInspectorGUIInternal()
        {
            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);
            MotionGraphEditorGUI.ParameterDropdownField<VectorParameter>(container, serializedObject.FindProperty("m_DashDirection"));

            EditorGUILayout.LabelField("Motion Data", EditorStyles.boldLabel);
            MotionGraphEditorGUI.FloatDataReferenceField(container, serializedObject.FindProperty("m_DashSpeed"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dash Properties", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Space"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DashInTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DashOutTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DashOutCurve"));
        }
    }
}

#endif