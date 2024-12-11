#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NeoFPS;
using UnityEditor.Animations;
using System;
using System.Linq;

namespace NeoFPSEditor
{
    [CustomPropertyDrawer(typeof(FpsInventoryInstantUseActionCollection))]
    public class FpsInventoryInstantUseActionCollectionDrawer : PropertyDrawer
    {
        private readonly GUIContent m_DropdownLabel = new GUIContent("Add Action");

        private static Type[] m_AvailableActionTypes = null;

        private GameObject m_CurrentGameObject = null;

        static void CheckAvailableActionTypes()
        {
            if (m_AvailableActionTypes != null)
                return;

            // Get action types for current domain
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes());
            m_AvailableActionTypes = types.Where(p => !p.IsAbstract && p.IsSubclassOf(typeof(FpsInventoryInstantUseAction))).ToArray();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var actionsProp = property.FindPropertyRelative("actions");

            float lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            float lineCount = actionsProp.arraySize + 3;

            return lineCount * lineHeight + (2 * EditorGUIUtility.standardVerticalSpacing);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.LabelField(position, GUIContent.none, EditorStyles.helpBox);

            EditorGUI.BeginProperty(position, label, property);

            position.height = EditorGUIUtility.singleLineHeight;

            float step = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            float padding = EditorGUIUtility.standardVerticalSpacing;
            position.y += padding;
            position.x += 2 * padding;
            position.width -= 4 * padding;

            EditorGUI.LabelField(position, "Instant Use Actions", EditorStyles.boldLabel);

            position.y += step;

            var component = property.serializedObject.targetObject as Component;

            if (EditorGUI.DropdownButton(position, m_DropdownLabel, FocusType.Passive) && component != null)
            {
                CheckAvailableActionTypes();

                m_CurrentGameObject = component.gameObject;

                var menu = new GenericMenu();

                foreach (var t in m_AvailableActionTypes)
                    menu.AddItem(new GUIContent(t.Name), false, OnSelectedNewAction, t);

                menu.ShowAsContext();
            }

            var actionsProp = property.FindPropertyRelative("actions");

            //position.x += padding;
            //position.width -= padding;

            int count = actionsProp.arraySize;
            if (count > 0)
            {
                position.y += step;
                EditorGUI.LabelField(position, "Attached Actions:");
                position.width -= 20;
                position.x += 20;

                for (int i = 0; i < count; ++i)
                {
                    position.y += step;

                    var actionRef = actionsProp.GetArrayElementAtIndex(i);
                    var action = actionRef.objectReferenceValue as FpsInventoryInstantUseAction;
                    if (action == null)
                        EditorGUI.LabelField(position, "- <Broken Action Reference>");
                    else
                        EditorGUI.LabelField(position, "- " + action.GetType().Name);
                }
            }
            else
            {
                position.y += step;
                EditorGUI.LabelField(position, "No Actions Attached");
            }

            // Get attached action components
            if (component != null)
            {
                var actions = component.GetComponents<FpsInventoryInstantUseAction>();

                // Check if the list matches the property
                if (actionsProp.arraySize != actions.Length)
                    actionsProp.arraySize = actions.Length;

                for (int i = 0; i < actions.Length; ++i)
                {
                    var actionRef = actionsProp.GetArrayElementAtIndex(i);
                    if (actionRef.objectReferenceValue != actions[i])
                        actionRef.objectReferenceValue = actions[i];
                }
            }

            EditorGUI.EndProperty();
        }

        private void OnSelectedNewAction(object o)
        {
            var t = (Type)o;
            m_CurrentGameObject.AddComponent(t);
        }
    }
}

#endif
