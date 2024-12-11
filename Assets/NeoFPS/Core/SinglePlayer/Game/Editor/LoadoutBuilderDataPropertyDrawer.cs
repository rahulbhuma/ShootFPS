#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using NeoFPS.SinglePlayer;

namespace NeoFPSEditor.SinglePlayer
{
    [CustomPropertyDrawer(typeof(LoadoutBuilderData))]
    public class LoadoutBuilderDataPropertyDrawer : PropertyDrawer
    {
        private ReorderableList m_SlotsList = null;
        private float m_ElementFixedHeight = 0f;

        void CheckList(SerializedProperty property)
        {
            if (m_SlotsList == null)
            {
                m_ElementFixedHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2f;
                m_SlotsList = new ReorderableList(property.serializedObject, property.FindPropertyRelative("slots"));
                m_SlotsList.elementHeightCallback = GetSlotsListElementHeight;
                m_SlotsList.drawElementCallback = DrawSlotsListElement;
                m_SlotsList.drawHeaderCallback = DrawSlotsListHeader;
            }
        }

        private void DrawSlotsListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Loadout Slots");
        }

        private void DrawSlotsListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var line = rect;
            line.y += EditorGUIUtility.standardVerticalSpacing;
            line.height = EditorGUIUtility.singleLineHeight;

            var prop = m_SlotsList.serializedProperty.GetArrayElementAtIndex(index);

            EditorGUI.PropertyField(line, prop.FindPropertyRelative("m_DisplayName"));

            rect.y = line.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            prop = prop.FindPropertyRelative("m_Options");
            rect.height = GetArrayHeight(prop);
            EditorGUI.PropertyField(rect, prop, true);
        }

        private float GetSlotsListElementHeight(int index)
        {
            var spawnPointsProp = m_SlotsList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("m_Options");
            return m_ElementFixedHeight + GetArrayHeight(spawnPointsProp);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            CheckList(property);
            return m_SlotsList.GetHeight() + GetArrayHeight(property.FindPropertyRelative("fixedItems")) + EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CheckList(property);

            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            position.y += EditorGUIUtility.standardVerticalSpacing;
            position.height = m_SlotsList.GetHeight();
            m_SlotsList.DoList(position);

            var fixedItems = property.FindPropertyRelative("fixedItems");
            position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
            position.height = GetArrayHeight(fixedItems);
            EditorGUI.PropertyField(position, fixedItems, true);

            EditorGUI.EndProperty();
        }

        float GetArrayHeight(SerializedProperty prop)
        {
            if (prop.isExpanded)
                return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * (prop.arraySize + 2);
            else
                return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}

#endif