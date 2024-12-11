#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using NeoFPS;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif

namespace NeoFPSEditor
{
    [CustomPropertyDrawer(typeof(NeoObjectInHierarchyFieldAttribute))]
    public class NeoObjectInHierarchyFieldAttributeDrawer : PropertyDrawer
    {
        private static SerializedProperty s_PendingBrowserProperty = null;

        bool m_IsValid;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.hasMultipleDifferentValues || Application.isPlaying)
            {
                return base.GetPropertyHeight(property, label);
            }
            else
            {
                var castAttr = attribute as NeoObjectInHierarchyFieldAttribute;
                m_IsValid = !castAttr.required || property.objectReferenceValue != null;
                if (m_IsValid)
                    return base.GetPropertyHeight(property, label);
                else
                    return base.GetPropertyHeight(property, label) + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        static Transform GetRootTransform(NeoObjectInHierarchyFieldAttribute attr, SerializedProperty prop)
        {
            if (attr.rootProperty == null)
            {
                var target = prop.serializedObject.targetObject;
                var component = target as Component;
                if (component != null)
                    return component.transform;
                else
                    return null;
            }
            else
            {
                switch (attr.rootPropertyType)
                {
                    case RootPropertyType.Transform:
                        {
                            // For relative, does the following work?
                            //var targetProp = prop.FindPropertyRelative("../attr.rootProperty");
                            var targetProp = prop.serializedObject.FindProperty(attr.rootProperty);
                            if (targetProp == null)
                                return null;
                            return targetProp.objectReferenceValue as Transform;
                        }
                    case RootPropertyType.GameObject:
                        {
                            var targetProp = prop.serializedObject.FindProperty(attr.rootProperty);
                            if (targetProp == null)
                                return null;
                            var gameObject = targetProp.objectReferenceValue as GameObject;
                            if (gameObject != null)
                                return gameObject.transform;
                            else
                                return null;
                        }
                    case RootPropertyType.Component:
                        {
                            var targetProp = prop.serializedObject.FindProperty(attr.rootProperty);
                            if (targetProp == null)
                                return null;
                            var component = targetProp.objectReferenceValue as Component;
                            if (component != null)
                                return component.transform;
                            else
                                return null;
                        }
                }
            }

            return null;
        }

        static bool IsChildFiltered(UnityEngine.Object obj, Transform root, NeoObjectInHierarchyFieldAttribute attr, Type fieldType)
        {
            if (obj != null)
            {
                if (fieldType == typeof(GameObject))
                {
                    var gameobject = obj as GameObject;
                    if (gameobject == null || (attr.filter != null && !attr.filter(gameobject)) || !gameobject.transform.IsChildOf(root) || (!attr.allowRoot && gameobject.transform == root))
                        return false;
                }
                else
                {
                    var component = obj as Component;
                    if (component == null || (attr.filter != null && !attr.filter(component.gameObject)) || !component.transform.IsChildOf(root) || (!attr.allowRoot && component.transform == root))
                        return false;
                }
            }
            return true;
        }

        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, prop);

            if (prop.hasMultipleDifferentValues || Application.isPlaying)
            {
                prop.objectReferenceValue = EditorGUI.ObjectField(position, label, prop.objectReferenceValue, fieldInfo.FieldType, true);
            }
            else
            {
                var castAttr = attribute as NeoObjectInHierarchyFieldAttribute;
                var root = GetRootTransform(castAttr, prop);
                var fieldType = fieldInfo.FieldType;


                // NOTE: Disabled forcing null if not child. Ideally it needs a new setting instead of allowRoot, but that would be too big a change
                bool warning = !IsChildFiltered(prop.objectReferenceValue, root, castAttr, fieldType);

                // Validity
                m_IsValid = !castAttr.required || prop.objectReferenceValue != null;
                if (!m_IsValid)
                    position.height = EditorGUIUtility.singleLineHeight;

                // Show the object field
                if (DrawCustomObjectField(position, prop, fieldType, warning))
                {
                    if (fieldType == typeof(GameObject))
                        ObjectHierarchyBrowser.GetChildObject(root, castAttr.allowRoot, OnGameObjectPicked, OnObjectPickingCancelled, castAttr.filter);
                    else
                    {
                        ObjectHierarchyBrowser.GetChildObject(root, castAttr.allowRoot, OnGameObjectPicked, OnObjectPickingCancelled, (o) =>
                        {
                            return o != null && o.GetComponent(fieldType) != null && (castAttr.filter == null || castAttr.filter(o));
                        });
                    }
                }

                // Draw error
                if (!m_IsValid)
                {
                    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    Color c = GUI.color;
                    GUI.color = NeoFpsEditorGUI.errorRed;
                    EditorGUI.HelpBox(position, "Required value", MessageType.Error);
                    GUI.color = c;
                }
            }

            EditorGUI.EndProperty();
        }

        bool DrawCustomObjectField(Rect position, SerializedProperty prop, Type t, bool warning)
        {
            bool result = false;

            EditorGUI.PrefixLabel(position, new GUIContent(prop.displayName, prop.tooltip));

            position.x += EditorGUIUtility.labelWidth;
            position.width -= EditorGUIUtility.labelWidth;

            // Get the button rect
            var buttonRect = position;
            buttonRect.x += buttonRect.width - 20;
            buttonRect.width = 20;

            // Check for button click and override
            var e = Event.current;
            if (e.isMouse && buttonRect.Contains(Event.current.mousePosition))
            {
                if (e.type == EventType.MouseDown)
                {
                    s_PendingBrowserProperty = prop;
                    result = true;
                }
                Event.current.Use();
            }

            // Show object field
            var original = prop.objectReferenceValue;
            if (warning)
                GUI.color = new Color(1f, 0.75f, 0.5f);
            var reference = EditorGUI.ObjectField(position, GUIContent.none, prop.objectReferenceValue, t, true);
            GUI.color = Color.white;

            // Check if the value has changed
            if (original != reference)
            {
                // Null is easy
                if (reference == null)
                    prop.objectReferenceValue = null;
                else
                {
                    // Check if there's a prefab stage open
                    var stage = PrefabStageUtility.GetCurrentPrefabStage();
                    if (stage != null)
                    {
                        // Get the component we're inspecting
                        var inspecting = prop.serializedObject.targetObject as Component;

                        // Get the gameobject for the new value
                        var referenceGameObject = reference as GameObject;
                        if (referenceGameObject == null)
                            referenceGameObject = (reference as Component).gameObject;

                        // Check for prefab vs prefab stage mix up
                        // (User is trying to assign an object from the prefab stage hierarchy to a component's object field on the prefab asset
                        bool isReferenceStaged = stage.IsPartOfPrefabContents(referenceGameObject);
                        bool isInspectingStaged = stage.IsPartOfPrefabContents(inspecting.gameObject);
                        if (isReferenceStaged && !isInspectingStaged)
                        {
                            // Get the component in the prefab stage that matches the one the user is inspecting
#if UNITY_2020_1_OR_NEWER
                            var corresponding = PrefabUtility.GetCorrespondingObjectFromSourceAtPath(inspecting, stage.assetPath);
#else
                            var corresponding = PrefabUtility.GetCorrespondingObjectFromSourceAtPath(inspecting, stage.prefabAssetPath);
#endif
                            if (corresponding != null)
                            {
                                // Get the root of the staged prefab
                                var prefabRoot = stage.prefabContentsRoot;

                                // Get the equivalent gameobject/component on the prefab asset
                                if (reference is GameObject)
                                    reference = NeoFpsEditorUtility.GetRelativeGameObject(prefabRoot, corresponding.gameObject, referenceGameObject);
                                else
                                    reference = NeoFpsEditorUtility.GetRelativeComponent(prefabRoot, corresponding.gameObject, reference as Component);

                                // Report back
                                if (reference != null)
                                {
                                    var castAttr = attribute as NeoObjectInHierarchyFieldAttribute;

                                    referenceGameObject = reference as GameObject;
                                    if (referenceGameObject == null)
                                        referenceGameObject = (reference as Component).gameObject;

                                    // Check
                                    if (!castAttr.allowRoot && referenceGameObject == inspecting.gameObject)
                                    {
                                        Debug.Log("Target must be a child of the object you are inspecting.");
                                    }
                                    else
                                    {
                                        Debug.Log("Correcting for prefab-stage. Be aware that the prefab open for editing is not the same object as the prefab in the project browser.");
                                        prop.objectReferenceValue = reference;
                                    }
                                }
                                else
                                {
                                    Debug.LogError("Failed to get equivalent in prefab. Please select the object from the prefab hierarchy, not the project browser. Click this message to highlight the correct object.", prefabRoot);
                                    EditorGUIUtility.PingObject(prefabRoot);
                                }
                            }
                        }
                        else
                        {
                            if (!isReferenceStaged && isInspectingStaged)
                            {
                                // Check if the reference is a component on the root object
                                var refComp = reference as Component;
                                if (refComp != null)
                                {
                                    // Get components on reference gameobject
                                    var refType = refComp.GetType();
                                    var referenceComponents = refComp.GetComponents(refType);

                                    // Get the index of the reference component
                                    int index = 0;
                                    for (; index < referenceComponents.Length; ++index)
                                    {
                                        if (referenceComponents[index] == refComp)
                                            break;
                                    }

                                    // Get the component that matches the index from the prefab stage object
                                    var inspectingComponents = inspecting.GetComponents(refType);
                                    if (inspectingComponents.Length > index)
                                    {
                                        reference = inspectingComponents[index];
                                        Debug.Log("Correcting for prefab-stage. Be aware that the prefab open for editing is not the same object as the prefab in the project browser.");
                                    }
                                }
                                else
                                {
                                    // Assigning the prefab asset root object (can't be a child since they're not visible to drag and drop)
                                    reference = stage.prefabContentsRoot;
                                    Debug.Log("Correcting for prefab-stage. Be aware that the prefab open for editing is not the same object as the prefab in the project browser.");
                                }
                            }
                            prop.objectReferenceValue = reference;
                        }
                    }
                    else
                        prop.objectReferenceValue = reference;
                }
            }

            return result;
        }

        void OnGameObjectPicked(GameObject obj)
        {
            if (obj == null)
                s_PendingBrowserProperty.objectReferenceValue = null;
            else
            {
                var t = fieldInfo.FieldType;
                if (t == typeof(GameObject))
                    s_PendingBrowserProperty.objectReferenceValue = obj;
                else
                    s_PendingBrowserProperty.objectReferenceValue = obj.GetComponent(t);
            }

            s_PendingBrowserProperty.serializedObject.ApplyModifiedProperties();
            s_PendingBrowserProperty = null;
        }

        static void OnObjectPickingCancelled()
        {
            s_PendingBrowserProperty = null;
        }        
    }
}

#endif