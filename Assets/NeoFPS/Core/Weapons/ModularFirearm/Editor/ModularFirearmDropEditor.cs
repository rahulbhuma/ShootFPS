#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS;
using NeoFPS.ModularFirearms;
using System.Collections.Generic;

namespace NeoFPSEditor.ModularFirearms
{
    [CustomEditor(typeof(ModularFirearmDrop), true)]
    public class ModularFirearmDropEditor : Editor
    {
        // Override this class and the ModularFirearmDrop to swap out the payload settings for your own settings

        private ModularFirearmPayloadSettingsBase m_BackupSettings = null;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var drop = target as ModularFirearmDrop;

            // Payload settings
            var payloadSettings = serializedObject.FindProperty("m_PayloadSettings");
            bool hasPayload = drop.payloadSettings != null;

            bool togglePayload = EditorGUILayout.Toggle("Has Custom Payload Data", hasPayload);
            if (togglePayload != hasPayload)
            {
                if (!togglePayload)
                {
                    m_BackupSettings = drop.payloadSettings;
                    payloadSettings.managedReferenceValue = null;
                }
                else
                {
                    if (m_BackupSettings != null)
                    {
                        payloadSettings.managedReferenceValue = m_BackupSettings;
                        m_BackupSettings = null;
                    }
                    else
                        payloadSettings.managedReferenceValue = CreatePayloadSettings();
                }

                hasPayload = togglePayload;
            }

            if (hasPayload)
            {
                var expanded = payloadSettings.FindPropertyRelative("m_Expanded");
                expanded.boolValue = EditorGUILayout.Foldout(expanded.boolValue, "Custom Payload Data", true);
                if (expanded.boolValue)
                    InspectPayloadSettings(payloadSettings);
            }
            else
            {
                GUI.enabled = false;
                EditorGUILayout.LabelField("Not using custom payload data");
                GUI.enabled = true;
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual ModularFirearmPayloadSettingsBase CreatePayloadSettings()
        {
            return new ModularFirearmPayloadSettings();
        }

        protected virtual void InspectPayloadSettings(SerializedProperty payloadSettings)
        {
            // Ammo count method
            var ammoCountMethod = payloadSettings.FindPropertyRelative("m_AmmoCountMethod");
            EditorGUILayout.PropertyField(ammoCountMethod);

            ++EditorGUI.indentLevel;

            // Ammo count values
            switch (ammoCountMethod.enumValueIndex)
            {
                case 0: // FixedAmount
                    EditorGUILayout.PropertyField(payloadSettings.FindPropertyRelative("m_AmmoCount"));
                    break;
                case 1: // Random
                    EditorGUILayout.PropertyField(payloadSettings.FindPropertyRelative("m_AmmoMin"));
                    EditorGUILayout.PropertyField(payloadSettings.FindPropertyRelative("m_AmmoMax"));
                    break;            
                // Maximum and NotSpecified don't have value settings
            }

            --EditorGUI.indentLevel;

            // Attachments
            EditorGUILayout.PropertyField(payloadSettings.FindPropertyRelative("m_Attachments"), true);            
        }
    }
}

#endif