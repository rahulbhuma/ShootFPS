#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion.States;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPSEditor.CharacterMotion.States
{
    [CustomEditor(typeof(RootMotionState))]
    [HelpURL("http://docs.neofps.com/manual/motiongraphref-mgs-rootmotionstate.html")]
    public class RootMotionStateEditor : MotionGraphStateEditor
    {
        protected override void OnInspectorGUIInternal()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ForwardParamName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_StrafeParamName"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ForwardMultiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_BackwardMultiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_StrafeMultiplier"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PositionMultiplier"));
            MotionGraphEditorGUI.ParameterDropdownField<FloatParameter>(container, serializedObject.FindProperty("m_RootVelocityMultiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RotationMultiplier"));
            MotionGraphEditorGUI.ParameterDropdownField<FloatParameter>(container, serializedObject.FindProperty("m_RootRotationMultiplier"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_BlendIn"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_InputDamping"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MotionDamping"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ApplyGravity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ApplyGroundingForce"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_IgnorePlatformMove"));
        }
    }
}

#endif