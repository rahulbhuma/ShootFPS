#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS.SinglePlayer;
using UnityEditorInternal;

namespace NeoFPSEditor.SinglePlayer
{
    [CustomPropertyDrawer(typeof (SpawnZoneSelectorData))]
    public class SpawnZoneSelectorDataPropertyDrawer : PropertyDrawer
    {
        /* SpawnZoneSelectorData
        [RequiredObjectProperty, Tooltip("The spawn areas (groups of spawn points) available on this map.")]
        public Sprite mapSprite = null;

        [Tooltip("The spawn areas (groups of spawn points) available on this map.")]
        public SpawnZoneInfo[] spawnZones = { };
        */

        /* SpawnZoneInfo
        [SerializeField, Tooltip("The name to show in a spawn point selection UI.")]
        private string m_DisplayName = string.Empty;

        [SerializeField, Tooltip("The position of this spawn zone on the map (0 to 1 on each axis).")]
        private Vector2 m_MapPosition = new Vector2(0.5f, 0.5f);

        [SerializeField, Tooltip("The spawn point objects assigned to this spawn area.")]
        private SpawnPoint[] m_SpawnPoints = { };
        */

        private float m_PropertyFixedHeight = 0f;
        private float m_ElementFixedHeight = 0f;
        private ReorderableList m_SpawnZonesList = null;

        void CheckList(SerializedProperty property)
        {
            if (m_SpawnZonesList == null)
            {
                m_PropertyFixedHeight = EditorGUIUtility.singleLineHeight * 1f + EditorGUIUtility.standardVerticalSpacing * 2f;
                m_ElementFixedHeight = EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing * 3f;
                m_SpawnZonesList = new ReorderableList(property.serializedObject, property.FindPropertyRelative("spawnZones"));
                m_SpawnZonesList.elementHeightCallback = GetSpawnZonesListElementHeight;
                m_SpawnZonesList.drawElementCallback = DrawSpawnZonesListElement;
                m_SpawnZonesList.drawHeaderCallback = DrawSpawnZonesListHeader;
            }
        }

        private void DrawSpawnZonesListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Spawn Zones");
        }

        private float GetSpawnZonesListElementHeight(int index)
        {
            var spawnPointsProp = m_SpawnZonesList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("m_SpawnPoints");
            if (spawnPointsProp.isExpanded)
                return m_ElementFixedHeight + (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * (spawnPointsProp.arraySize + 2);
            else
                return m_ElementFixedHeight + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        private void DrawSpawnZonesListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var line = rect;
            line.y += EditorGUIUtility.standardVerticalSpacing;
            line.height = EditorGUIUtility.singleLineHeight;

            var prop = m_SpawnZonesList.serializedProperty.GetArrayElementAtIndex(index);

            EditorGUI.PropertyField(line, prop.FindPropertyRelative("m_DisplayName"));
            line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(line, prop.FindPropertyRelative("m_MapPosition"));

            rect.y = line.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            rect.height = m_ElementFixedHeight;
            EditorGUI.PropertyField(rect, prop.FindPropertyRelative("m_SpawnPoints"), true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            CheckList(property);

            return m_SpawnZonesList.GetHeight() + m_PropertyFixedHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CheckList(property);

            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            position.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.PropertyField(position, property.FindPropertyRelative("mapSprite"));

            position.y += position.height + EditorGUIUtility.standardVerticalSpacing * 2f;
            position.height = m_SpawnZonesList.GetHeight();

            m_SpawnZonesList.DoList(position);

            EditorGUI.EndProperty();
        }
    }
}

#endif