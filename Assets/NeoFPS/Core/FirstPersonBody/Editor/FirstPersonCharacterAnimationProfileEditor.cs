#if UNITY_EDITOR

using NeoFPS;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Animations;
using System;
using System.Collections.Generic;

namespace NeoFPSEditor
{
    [CustomEditor(typeof(FirstPersonCharacterAnimationProfile))]
    public class FirstPersonCharacterAnimationProfileEditor : Editor
    {
        private Dictionary<string, int> m_Descriptions = new Dictionary<string, int>();
        private GUIContent m_ClipLabel = new GUIContent("Clip", "The clip from the selected unity animator controller");
        private GUIContent m_DescriptionLabel = new GUIContent("Description", "A short description that will be exposed to the wieldable item when assigning overrides (for clarity).");
        private ReorderableList m_ClipList = null;

        private void OnEnable()
        {
            m_ClipList = new ReorderableList(serializedObject, serializedObject.FindProperty("m_Clips"));
            m_ClipList.elementHeight = EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing * 3f;
            m_ClipList.onAddDropdownCallback = OnAddNewClipDropdown;
            m_ClipList.onRemoveCallback = OnRemoveClip;
            m_ClipList.onReorderCallback = OnReorderClips;
            m_ClipList.onCanAddCallback = CanAddNewClip;
            m_ClipList.drawElementCallback = DrawClipListElement;
            m_ClipList.drawHeaderCallback = DrawClipListHeader;

            ValidateClips();

            Undo.undoRedoPerformed += RefreshNames;
            RefreshNames();
        }

        private void OnReorderClips(ReorderableList list)
        {
            RefreshNames();
        }

        private void OnRemoveClip(ReorderableList list)
        {
            SerializedArrayUtility.RemoveAt(list.serializedProperty, list.index);
            list.index = -1;
            RefreshNames();
        }

        private void DrawClipListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Animation Clips", EditorStyles.boldLabel);
        }

        private void DrawClipListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            // Get element
            var element = m_ClipList.serializedProperty.GetArrayElementAtIndex(index);

            // Sort out layout rects
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.y += EditorGUIUtility.standardVerticalSpacing;
            var labelRect = rect;
            labelRect.width = EditorGUIUtility.labelWidth;
            rect.x += EditorGUIUtility.labelWidth + 2;
            rect.width -= EditorGUIUtility.labelWidth + 2;

            // Clip label
            EditorGUI.LabelField(labelRect, m_DescriptionLabel);

            // Description properties
            var descriptionProp = element.FindPropertyRelative("description");
            var oldDescription = descriptionProp.stringValue;

            // Check if valid
            bool valid = !m_Descriptions.ContainsKey(oldDescription) || m_Descriptions[oldDescription] == index;

            // Description field
            if (!valid)
                GUI.color = Color.red;
            var newDescription = EditorGUI.DelayedTextField(rect, GUIContent.none, oldDescription);
            if (!valid)
                GUI.color = Color.white;

            // Apply
            if (newDescription != oldDescription)
            {
                descriptionProp.stringValue = newDescription;
                serializedObject.ApplyModifiedProperties();
                RefreshNames();
                throw new ExitGUIException();
            }

            // Next line
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            labelRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Clip label
            EditorGUI.LabelField(labelRect, m_ClipLabel);

            // Clip field
            GUI.enabled = false;
            EditorGUI.ObjectField(rect, element.FindPropertyRelative("clip"), GUIContent.none);
            GUI.enabled = true;
        }

        private bool CanAddNewClip(ReorderableList list)
        {
            // Check controller is set
            var controller = serializedObject.FindProperty("m_Controller").objectReferenceValue as RuntimeAnimatorController;
            if (controller == null)
                return false;

            // Check there's more clips than referenced
            var clips = controller.animationClips;
            var taken = m_ClipList.serializedProperty;
            if (taken.arraySize == clips.Length)
                return false;

            return true;
        }
        
        private void OnAddNewClipDropdown(Rect buttonRect, ReorderableList list)
        {
            var controller = serializedObject.FindProperty("m_Controller").objectReferenceValue as RuntimeAnimatorController;
            if (controller == null)
                return;

            // Gather used clips
            var clipsProperty = m_ClipList.serializedProperty;
            var usedClips = new List<AnimationClip>();
            for (int i = 0; i < clipsProperty.arraySize; ++i)
                usedClips.Add(clipsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("clip").objectReferenceValue as AnimationClip);

            // Filter controller clips against used
            var controllerClips = controller.animationClips;
            var filtered = new List<AnimationClip>();
            for (int i = 0; i < controllerClips.Length; ++i)
            {
                if (!usedClips.Contains(controllerClips[i]))
                    filtered.Add(controllerClips[i]);
            }

            // Show the menu
            var menu = new GenericMenu();
            for (int i = 0; i < filtered.Count; ++i)
                menu.AddItem(new GUIContent(filtered[i].name), false, OnClipSelected, filtered[i]);
            menu.ShowAsContext();
        }

        void OnClipSelected(object o)
        {
            var clip = (AnimationClip)o;

            // If name is valid, add clip info to the list
            var clips = m_ClipList.serializedProperty;
            ++clips.arraySize;
            var newClip = clips.GetArrayElementAtIndex(clips.arraySize - 1);
            newClip.FindPropertyRelative("clip").objectReferenceValue = clip;
            newClip.FindPropertyRelative("description").stringValue = clip.name;

            m_ClipList.serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            NeoFpsEditorGUI.ScriptField(serializedObject);

            // Controller
            var controller = serializedObject.FindProperty("m_Controller");
            EditorGUILayout.PropertyField(controller);

            // Re-check clips (if editing animator controller at the same time)
            if (GUILayout.Button("Check Clips"))
                ValidateClips();

            // Clips
            EditorGUILayout.Space();
            m_ClipList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        void ValidateClips()
        {
            // Properties
            var controller = serializedObject.FindProperty("m_Controller").objectReferenceValue as RuntimeAnimatorController;
            var clips = serializedObject.FindProperty("m_Clips");

            // Check controller is assigned
            if (controller == null)
                clips.arraySize = 0;
            else
            {
                // Check clips are not null and do belong to the controller
                var controllerClips = controller.animationClips;
                for (int i = clips.arraySize - 1; i >= 0; --i)
                {
                    var clip = clips.GetArrayElementAtIndex(i).FindPropertyRelative("clip").objectReferenceValue as AnimationClip;
                    if (clip == null || Array.IndexOf(controllerClips, clip) == -1)
                        SerializedArrayUtility.RemoveAt(clips, i);
                }
            }
        }

        void RefreshNames()
        {
            m_Descriptions.Clear();
            
            // Get each entry's description and store along with index
            var clips = serializedObject.FindProperty("m_Clips");
            for (int i = 0; i < clips.arraySize; ++i)
            {
                string description = clips.GetArrayElementAtIndex(i).FindPropertyRelative("description").stringValue;
                if (!string.IsNullOrEmpty(description) && !m_Descriptions.ContainsKey(description))
                    m_Descriptions.Add(description, i);
            }
        }
    }
}

#endif