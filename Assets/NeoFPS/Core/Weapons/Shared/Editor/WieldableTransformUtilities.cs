#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace NeoFPSEditor
{
    public static class WieldableTransformUtilities
    {
        private readonly static GUIContent k_MatchOriginLabel = new GUIContent("Match Transform", "Drag a child transform here to automatically reposition the weapon geometry so that the child transform is aligned with the NeoFPS camera.");

        public static void ShowOriginPointCameraMatcher(Transform wieldableRoot)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Origin Point", EditorStyles.boldLabel);

            Transform cameraTransform = null;

            // Show the object field
            bool clicked = false;
            using (var scope = new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(k_MatchOriginLabel);

                // Get the full rect
                var fullRect = scope.rect;
                fullRect.x += EditorGUIUtility.labelWidth;
                fullRect.width -= EditorGUIUtility.labelWidth;

                // Get the button rect
                var buttonRect = fullRect;
                buttonRect.x += buttonRect.width - 20;
                buttonRect.width = 20;

                // Check for button click and override
                var e = Event.current;
                if (e.isMouse && buttonRect.Contains(Event.current.mousePosition))
                {
                    if (e.type == EventType.MouseDown)
                    {
                        clicked = true;
                    }
                    Event.current.Use();
                }

                // Show object field
                cameraTransform = EditorGUI.ObjectField(fullRect, GUIContent.none, null, typeof(Transform), true) as Transform;
            }

            // Show the object field
            if (clicked)
                ObjectHierarchyBrowser.GetChildObject(wieldableRoot, false, obj => MatchCameraTransform(wieldableRoot, obj.transform), null);

            if (cameraTransform != null)
                MatchCameraTransform(wieldableRoot, cameraTransform);
        }

        public static void MatchCameraTransform(Transform wieldableRoot, Transform cameraTransform)
        {
            Transform plus1 = null;
            Transform plus2 = null;

            // Check if transform is a child of this object
            bool found = false;
            Transform itr = cameraTransform;
            while (itr != null)
            {
                plus2 = plus1;
                plus1 = itr;
                itr = itr.parent;
                if (itr == wieldableRoot)
                {
                    found = true;
                    break;
                }
            }

            Vector3 rootPosition = wieldableRoot.position;

            // Check for valid hierarchy
            if (!found || plus2 == null || plus2 == cameraTransform)
            {
                Debug.LogError("Transform must be a child of the firearm object and its weapon geometry object");
                return;
            }

            // Get the difference and move the child
            Vector3 diff = cameraTransform.position - rootPosition;
            Undo.RecordObject(plus2, "Align Firearm To Camera");
            plus2.position -= diff;
        }
    }
}

#endif