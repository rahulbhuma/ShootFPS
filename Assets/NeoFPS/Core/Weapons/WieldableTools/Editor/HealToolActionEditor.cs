#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using NeoFPS;
using NeoFPS.WieldableTools;
using System;
using System.Collections.Generic;

namespace NeoFPSEditor.WieldableTools
{
    [CustomEditor(typeof(HealToolAction))]
    public class HealToolActionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            var timingProp = serializedObject.FindProperty("m_Timing");
            EditorGUILayout.PropertyField(timingProp);

            var subjectProp = serializedObject.FindProperty("m_Subject");
            EditorGUILayout.PropertyField(subjectProp);
            if (subjectProp.enumValueIndex == 1)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_TargetLayers"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MaxRange"));
            }

            WieldableToolActionTiming timing = (WieldableToolActionTiming)timingProp.intValue;
            if ((timing & WieldableToolActionTiming.Start) == WieldableToolActionTiming.Start)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_StartHeal"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_StartDelay"));
            }

            if ((timing & WieldableToolActionTiming.Continuous) == WieldableToolActionTiming.Continuous)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ContinuousHeal"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_HealInterval"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Instant"));
            }

            if ((timing & WieldableToolActionTiming.End) == WieldableToolActionTiming.End)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_EndHeal"));

            var inventoryProp = serializedObject.FindProperty("m_InventoryConsume");
            EditorGUILayout.PropertyField(inventoryProp);
            if (inventoryProp.enumValueIndex != 0)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ItemKey"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif