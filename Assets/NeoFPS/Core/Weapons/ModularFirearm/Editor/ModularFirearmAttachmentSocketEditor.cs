#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using NeoFPS.ModularFirearms;
using System;

namespace NeoFPSEditor.ModularFirearms
{
    [CustomEditor(typeof(ModularFirearmAttachmentSocket))]
    public class ModularFirearmAttachmentSocketEditor : Editor
    {
        private ReorderableList m_AttachmentsList = null;
        private GUIContent m_CurrentDefaultAttachment = null;
        private GUIContent m_DefaultAttachmentPrefixLabel = null;
        private bool m_Valid = false;

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            NeoFpsEditorGUI.ScriptField(serializedObject);

            // Check attachments list (property required for default attachment)
            var attachmentsList = CheckAttachmentsList();

            // Socket Name
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SocketName"));

            // Default Attachment
            var defaultSource = serializedObject.FindProperty("m_DefaultAttachmentSource");
            var defaultAttachment = serializedObject.FindProperty("m_DefaultAttachment");
            var attachmentGroup = serializedObject.FindProperty("m_AttachmentGroup");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(defaultSource);
            if (EditorGUI.EndChangeCheck())
            {
                defaultAttachment.intValue = -1;
                m_CurrentDefaultAttachment = null;
            }

            if (defaultSource.enumValueIndex == 1 || defaultSource.enumValueIndex == 2)
                ShowDefaultAttachmentDropdown(defaultSource, defaultAttachment, attachmentGroup);

            // Attachment Group
            EditorGUILayout.PropertyField(attachmentGroup);

            // Local Attachments
            attachmentsList.DoLayoutList();

            // Contents Change
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SocketFilledObject"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SocketEmptyObject"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_NullAttachmentName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnAttachmentChanged"));

            serializedObject.ApplyModifiedProperties();
        }

        void ShowDefaultAttachmentDropdown(SerializedProperty sourceProp, SerializedProperty attachmentProp, SerializedProperty attachmentGroup)
        {
            if (m_CurrentDefaultAttachment == null)
            {
                m_Valid = false;
                if (sourceProp.enumValueIndex == 1)
                {
                    int count = m_AttachmentsList.serializedProperty.arraySize;
                    if (count == 0)
                    {
                        m_CurrentDefaultAttachment = new GUIContent("<Attachment list is empty>");
                    }
                    else
                    {
                        // Bounds check
                        int index = attachmentProp.intValue;
                        if (index < -1 || index >= count)
                        {
                            index = -1;
                            attachmentProp.intValue = -1;
                        }

                        if (index == -1)
                        {
                            m_CurrentDefaultAttachment = new GUIContent(string.Format("<Not Set>", index));
                        }
                        else
                        { 
                            var element = m_AttachmentsList.serializedProperty.GetArrayElementAtIndex(index);
                            var attachmentPrefab = element.FindPropertyRelative("attachment").objectReferenceValue as ModularFirearmAttachment;

                            if (attachmentPrefab != null)
                                m_CurrentDefaultAttachment = new GUIContent(string.Format("[{0}] {1}", index, attachmentPrefab.displayName));
                            else
                                m_CurrentDefaultAttachment = new GUIContent(string.Format("[{0}] <Empty>", index));
                        }

                        m_Valid = true;
                    }
                }
                else
                {
                    var groupAsset = attachmentGroup.objectReferenceValue as ModularFirearmAttachmentGroup;
                    if (groupAsset == null)
                    {
                        m_CurrentDefaultAttachment = new GUIContent("<Attachment group not set>");
                    }
                    else
                    {
                        int count = groupAsset.attachments.Length;
                        if (count == 0)
                        {
                            m_CurrentDefaultAttachment = new GUIContent("<Attachment group is empty>");
                        }
                        else
                        {
                            // Bounds check
                            int index = attachmentProp.intValue;
                            if (index < 0 || index >= count)
                            {
                                index = 0;
                                attachmentProp.intValue = 0;
                            }

                            var attachmentPrefab = groupAsset.attachments[index].attachment;

                            if (attachmentPrefab != null)
                                m_CurrentDefaultAttachment = new GUIContent(string.Format("[{0}] {1}", index, attachmentPrefab.displayName));
                            else
                                m_CurrentDefaultAttachment = new GUIContent(string.Format("[{0}] <Empty>", index));

                            m_Valid = true;
                        }
                    }
                }
            }

            // Draw the dropdown
            if (m_DefaultAttachmentPrefixLabel == null)
                m_DefaultAttachmentPrefixLabel = new GUIContent(attachmentProp.displayName, attachmentProp.tooltip);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(m_DefaultAttachmentPrefixLabel);
            if (EditorGUILayout.DropdownButton(m_CurrentDefaultAttachment, FocusType.Passive) && m_Valid)
            {
                var menu = new GenericMenu();

                if (sourceProp.enumValueIndex == 1) // this
                {
                    for (int i = 0; i < m_AttachmentsList.serializedProperty.arraySize; ++i)
                    {
                        var element = m_AttachmentsList.serializedProperty.GetArrayElementAtIndex(i);
                        var attachmentPrefab = element.FindPropertyRelative("attachment").objectReferenceValue as ModularFirearmAttachment;
                        if (attachmentPrefab != null)
                            menu.AddItem(new GUIContent(string.Format("[{0}] {1}", i, attachmentPrefab.displayName)), false, OnDefaultAttachmentSelected, i);
                        else
                            menu.AddItem(new GUIContent(string.Format("[{0}] <Empty>", i)), false, OnDefaultAttachmentSelected, i);
                    }
                }
                else // group
                {
                    var groupAsset = attachmentGroup.objectReferenceValue as ModularFirearmAttachmentGroup;
                    for (int i = 0; i < groupAsset.attachments.Length; ++i)
                    {
                        var attachmentPrefab = groupAsset.attachments[i].attachment;
                        if (attachmentPrefab != null)
                            menu.AddItem(new GUIContent(string.Format("[{0}] {1}", i, attachmentPrefab.displayName)), false, OnDefaultAttachmentSelected, i);
                        else
                            menu.AddItem(new GUIContent(string.Format("[{0}] <Empty>", i)), false, OnDefaultAttachmentSelected, i);
                    }
                }

                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
        }

        void OnDefaultAttachmentSelected(object o)
        {
            m_CurrentDefaultAttachment = null;
            serializedObject.FindProperty("m_DefaultAttachment").intValue = (int)o;
            serializedObject.ApplyModifiedProperties();
        }

        //void GetDefaultAttachmentLabel(SerializedProperty sourceProp)

        ReorderableList CheckAttachmentsList()
        {
            if (m_AttachmentsList == null)
            {
                m_AttachmentsList = new ReorderableList(serializedObject, serializedObject.FindProperty("m_Attachments"), true, true, true, true);
                m_AttachmentsList.elementHeightCallback = GetAttachmentsListElementHeight;
                m_AttachmentsList.drawElementCallback = DrawAttachmentsListElement;
                m_AttachmentsList.drawHeaderCallback = DrawAttachmentsListHeader;
                m_AttachmentsList.onRemoveCallback = OnAttachmentsListRemove;
                m_AttachmentsList.onReorderCallbackWithDetails = OnAttachmentsListReorder;
            }
            return m_AttachmentsList;
        }

        private void OnAttachmentsListRemove(ReorderableList list)
        {
            var defaultSource = serializedObject.FindProperty("m_DefaultAttachmentSource");
            if (defaultSource.enumValueIndex == 1)
            {
                // If the default index is out of bounds, reset it
                var defaultAttachment = serializedObject.FindProperty("m_DefaultAttachment");
                if (defaultAttachment.intValue < -1 || defaultAttachment.intValue >= list.serializedProperty.arraySize)
                    defaultAttachment.intValue = -1;

                // Rebuild default GUIContent
                m_CurrentDefaultAttachment = null;
            }

            ReorderableList.defaultBehaviours.DoRemoveButton(list);
        }

        private void OnAttachmentsListReorder(ReorderableList list, int oldIndex, int newIndex)
        {
            var defaultSource = serializedObject.FindProperty("m_DefaultAttachmentSource");
            if (defaultSource.enumValueIndex == 1)
            {
                var defaultAttachment = serializedObject.FindProperty("m_DefaultAttachment");
                int oldDefault = defaultAttachment.intValue;

                // Check if default is being moved
                if (oldDefault == oldIndex)
                    defaultAttachment.intValue = newIndex;
                else
                {
                    // Check if default is being displaced up
                    if (oldDefault < oldIndex && oldDefault >= newIndex)
                        defaultAttachment.intValue = oldDefault + 1;

                    // Check if default is being displaced down
                    if (oldDefault > oldIndex && oldDefault <= newIndex)
                        defaultAttachment.intValue = oldDefault - 1;
                }

                // Rebuild default GUIContent
                m_CurrentDefaultAttachment = null;
            }
        }


        private float GetAttachmentsListElementHeight(int index)
        {
            if (index >= 0)
                return EditorGUIUtility.standardVerticalSpacing * 3f + EditorGUIUtility.singleLineHeight * 2f;
            else
                return EditorGUIUtility.singleLineHeight;
        }

        private void DrawAttachmentsListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Local Attachments");
        }

        private void DrawAttachmentsListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.y += 2f;
            EditorGUI.PropertyField(rect, m_AttachmentsList.serializedProperty.GetArrayElementAtIndex(index), GUIContent.none, true);
        }
    }
}

#endif