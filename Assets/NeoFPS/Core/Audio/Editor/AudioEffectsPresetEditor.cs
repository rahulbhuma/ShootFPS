#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using NeoFPS.Constants;
using NeoFPS;

namespace NeoFPSEditor
{
    [CustomEditor(typeof(AudioEffectsPreset))]
    public class AudioEffectsPresetEditor : Editor
    {
        private static readonly GUIContent k_ReverbPresetsLabel = new GUIContent("Apply Preset", "Set the values to match a preset reverb effect");
        private static readonly GUIContent k_ReverbResetLabel = new GUIContent("Reset", "Set the values so that no reverb effect can be heard");

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_EffectName"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_HighpassCutOff"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_HighpassResonance"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_LowpassCutOff"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_LowpassResonance"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Distortion"));

            var reverbEnabled = serializedObject.FindProperty("m_ReverbEnabled");

            EditorGUILayout.PropertyField(reverbEnabled);

            if (reverbEnabled.boolValue)
            {
                if (EditorGUILayout.DropdownButton(k_ReverbPresetsLabel, FocusType.Keyboard))
                {
                    var menu = new GenericMenu();
                    for (int i = 0; i < (int)AudioReverbPreset.User; ++i)
                        menu.AddItem(new GUIContent(((AudioReverbPreset)i).ToString()), false, OnReverbPresetSelected, i);
                    menu.ShowAsContext();
                }

                if (GUILayout.Button(k_ReverbResetLabel))
                {
                    serializedObject.FindProperty("m_ReverbDryLevel").floatValue = ReverbDefaults.dryLevel;
                    serializedObject.FindProperty("m_ReverbRoom").floatValue = ReverbDefaults.room;
                    serializedObject.FindProperty("m_ReverbRoomHF").floatValue = ReverbDefaults.roomHF;
                    serializedObject.FindProperty("m_ReverbRoomLF").floatValue = ReverbDefaults.roomLF;
                    serializedObject.FindProperty("m_ReverbDecayTime").floatValue = ReverbDefaults.decayTime;
                    serializedObject.FindProperty("m_ReverbDecayHFRatio").floatValue = ReverbDefaults.decayHFRatio;
                    serializedObject.FindProperty("m_ReverbReflectionsLevel").floatValue = ReverbDefaults.reflectionsLevel;
                    serializedObject.FindProperty("m_ReverbReflectionsDelay").floatValue = ReverbDefaults.reflectionsDelay;
                    serializedObject.FindProperty("m_ReverbLevel").floatValue = ReverbDefaults.level;
                    serializedObject.FindProperty("m_ReverbDelay").floatValue = ReverbDefaults.delay;
                    serializedObject.FindProperty("m_ReverbHFReference").floatValue = ReverbDefaults.hfReference;
                    serializedObject.FindProperty("m_ReverbLFReference").floatValue = ReverbDefaults.lfReference;
                    serializedObject.FindProperty("m_ReverbDiffusion").floatValue = ReverbDefaults.diffusion;
                    serializedObject.FindProperty("m_ReverbDensity").floatValue = ReverbDefaults.density;
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ReverbDryLevel"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ReverbRoom"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ReverbRoomHF"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ReverbRoomLF"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ReverbDecayTime"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ReverbDecayHFRatio"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ReverbReflectionsLevel"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ReverbReflectionsDelay"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ReverbLevel"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ReverbDelay"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ReverbHFReference"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ReverbLFReference"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ReverbDiffusion"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ReverbDensity"));
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void OnReverbPresetSelected(object userData)
        {
            var preset = (AudioReverbPreset)userData;

            var go = new GameObject("Reverb Preset");
            go.AddComponent<AudioSource>();
            var reverb = go.AddComponent<AudioReverbFilter>();
            reverb.reverbPreset = preset;

            serializedObject.FindProperty("m_ReverbDryLevel").floatValue = reverb.dryLevel;
            serializedObject.FindProperty("m_ReverbRoom").floatValue = reverb.room;
            serializedObject.FindProperty("m_ReverbRoomHF").floatValue = reverb.roomHF;
            serializedObject.FindProperty("m_ReverbRoomLF").floatValue = reverb.roomLF;
            serializedObject.FindProperty("m_ReverbDecayTime").floatValue = reverb.decayTime;
            serializedObject.FindProperty("m_ReverbDecayHFRatio").floatValue = reverb.decayHFRatio;
            serializedObject.FindProperty("m_ReverbReflectionsLevel").floatValue = reverb.reflectionsLevel;
            serializedObject.FindProperty("m_ReverbReflectionsDelay").floatValue = reverb.reflectionsDelay;
            serializedObject.FindProperty("m_ReverbLevel").floatValue = reverb.reverbLevel;
            serializedObject.FindProperty("m_ReverbDelay").floatValue = reverb.reverbDelay;
            serializedObject.FindProperty("m_ReverbHFReference").floatValue = reverb.hfReference;
            serializedObject.FindProperty("m_ReverbLFReference").floatValue = reverb.lfReference;
            serializedObject.FindProperty("m_ReverbDiffusion").floatValue = reverb.diffusion;
            serializedObject.FindProperty("m_ReverbDensity").floatValue = reverb.density;

            DestroyImmediate(go);

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif