#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;

namespace NeoFPSEditor
{
    public abstract class ReorderableSelectionList
    {
        private string m_Header;

        protected ReorderableList list
        {
            get;
            private set;
        }

        protected bool showInsertControls
        {
            get;
            private set;
        }

        public ReorderableSelectionList(SerializedObject so, SerializedProperty prop, string header, bool addRemove, bool insert)
        {
            list = new ReorderableList(so, prop, true, true, addRemove, addRemove);
            list.drawHeaderCallback = DrawHeader;
            list.drawElementCallback = DrawElement;
            list.onAddDropdownCallback = OnAddDropdown;
            list.onRemoveCallback = OnRemove;
            list.onCanRemoveCallback = OnCanRemove;
            list.onReorderCallbackWithDetails = OnReorderWithDetails;
            list.onSelectCallback = OnSelect;

            m_Header = header + " (select to edit)";
            showInsertControls = insert;
        }

        public SerializedProperty serializedProperty
        {
            get { return list.serializedProperty; }
        }
        
        public SerializedObject serializedObject
        {
            get { return list.serializedProperty.serializedObject; }
        }

        public int index
        {
            get { return list.index; }
            set
            {
                list.index = value;
                OnSelectionChanged(serializedProperty, value);
            }
        }

        public virtual void DoLayout()
        {
            //list.serializedProperty.serializedObject.UpdateIfRequiredOrScript();
            // Don't call update or it will discard changes made before this
            
            list.DoLayoutList();

            NeoFpsEditorGUI.Separator();

            if (list.index == -1)
                InspectNoSelection(list.count == 0);
            else
                InspectSelection(list.serializedProperty.GetArrayElementAtIndex(list.index));

            //NeoFpsEditorGUI.Separator();
        }

        private void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, m_Header);
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var elementProp = list.serializedProperty.GetArrayElementAtIndex(index);

            rect.y += 1;
            rect.height -= 2;

            if (showInsertControls)
            {
                var elementRect = rect;
                elementRect.width -= 40; // ???

                InspectArrayElement(rect, elementProp, index);

                var buttonRect = rect;
                buttonRect.width = 20; // ???
                buttonRect.x += elementRect.width;

                // Draw buttons
            }
            else
                InspectArrayElement(rect, elementProp, index);
        }

        private void OnAddDropdown(Rect buttonRect, ReorderableList list)
        {
            AppendElement(list.serializedProperty);
        }

        private bool OnCanRemove(ReorderableList list)
        {
            return list.index != -1;
        }

        private void OnRemove(ReorderableList list)
        {
            OnRemove(list.serializedProperty, list.index);
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
        }

        private void OnReorderWithDetails(ReorderableList list, int oldIndex, int newIndex)
        {
            OnReorder(list.serializedProperty, oldIndex, newIndex);
        }

        private void OnSelect(ReorderableList list)
        {
            OnSelectionChanged(list.serializedProperty, list.index);
        }

        protected abstract void InspectArrayElement(Rect r, SerializedProperty elementProp, int index);
        protected abstract void InspectSelection(SerializedProperty prop);
        protected abstract void InspectNoSelection(bool empty);
        protected abstract void AppendElement(SerializedProperty prop);
        protected virtual void OnRemove(SerializedProperty prop, int index) { }
        protected virtual void OnReorder(SerializedProperty prop, int oldIndex, int newIndex) { }
        protected virtual void OnSelectionChanged(SerializedProperty listProp, int index) { }

        public static string StringPropertyToName(SerializedProperty stringProp, int index)
        {
            string elementName = stringProp.stringValue;
            if (string.IsNullOrEmpty(elementName))
                return $"[{index}] <Empty Name>";
            else
                return $"[{index}] {elementName}";
        }

        public static void NoSelectionHelper(bool empty, GUIContent emptyMessage, GUIContent noSelectionMessage)
        {
            if (empty)
                NeoFpsEditorGUI.InstructionsNote(emptyMessage, true);
            else
                NeoFpsEditorGUI.InstructionsNote(noSelectionMessage, true);
        }
    }
}

#endif