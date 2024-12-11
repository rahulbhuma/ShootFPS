#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS;

namespace NeoFPSEditor
{
    [CustomEditor(typeof(InputDualWield), true)]
    public class InputDualWieldEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            NeoFpsEditorGUI.ScriptField(serializedObject);

            NeoFpsEditorGUI.Header("Left");
            InspectWeaponInfo(serializedObject.FindProperty("m_Left"));
            NeoFpsEditorGUI.Header("Right");
            InspectWeaponInfo(serializedObject.FindProperty("m_Right"));

            var styleProperty = serializedObject.FindProperty("m_Style");
            EditorGUILayout.PropertyField(styleProperty);
            if (styleProperty.enumValueIndex == 2) // Together with aim
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AimingKey"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_StrictBlocking"));

            serializedObject.ApplyModifiedProperties();
        }

        void InspectWeaponInfo(SerializedProperty prop)
        {
            var typeProperty = prop.FindPropertyRelative("type");
            EditorGUILayout.PropertyField(typeProperty);

            switch (typeProperty.enumValueIndex)
            {
                case 0: // Firearm
                    EditorGUILayout.PropertyField(prop.FindPropertyRelative("firearm"));
                    break;
                case 1: // WieldableTool
                    EditorGUILayout.PropertyField(prop.FindPropertyRelative("wieldableTool"));
                    break;
                case 2: // Melee
                    EditorGUILayout.PropertyField(prop.FindPropertyRelative("melee"));
                    break;
                case 3: // Thrown
                    EditorGUILayout.PropertyField(prop.FindPropertyRelative("thrown"));
                    break;
            }
        }
    }
}

#endif