#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using NeoFPS.SinglePlayer;

namespace NeoFPSEditor.SinglePlayer
{
    [CustomPropertyDrawer(typeof(LoadoutSelectorData))]
    public class LoadoutSelectorDataPropertyDrawer : PropertyDrawer
    {
        private ReorderableList m_LoadoutsList = null;

        void CheckList(SerializedProperty property)
        {
            if (m_LoadoutsList == null)
            {
                m_LoadoutsList = new ReorderableList(property.serializedObject, property.FindPropertyRelative("loadouts"));
                m_LoadoutsList.elementHeight = EditorGUIUtility.singleLineHeight * 4f + EditorGUIUtility.standardVerticalSpacing *5f;
                m_LoadoutsList.drawElementCallback = DrawLoadoutsListElement;
                m_LoadoutsList.drawHeaderCallback = DrawLoadoutsListHeader;
            }
        }

        private void DrawLoadoutsListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Available Loadouts");
        }

        private void DrawLoadoutsListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.y += EditorGUIUtility.standardVerticalSpacing;

            var prop = m_LoadoutsList.serializedProperty.GetArrayElementAtIndex(index);

            EditorGUI.PropertyField(rect, prop.FindPropertyRelative("m_Loadout"));
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(rect, prop.FindPropertyRelative("m_DisplayName"));
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(rect, prop.FindPropertyRelative("m_Description"));
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(rect, prop.FindPropertyRelative("m_Sprite"));
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            CheckList(property);
            
            return m_LoadoutsList.GetHeight() + EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CheckList(property);

            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            position.y += EditorGUIUtility.standardVerticalSpacing;
            position.height -= EditorGUIUtility.standardVerticalSpacing;

            m_LoadoutsList.DoList(position);

            EditorGUI.EndProperty();
        }
    }
}

#endif