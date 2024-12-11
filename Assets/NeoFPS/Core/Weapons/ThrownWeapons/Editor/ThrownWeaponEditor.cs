#if UNITY_EDITOR

using NeoFPS;
using UnityEditor;
using UnityEngine;

namespace NeoFPSEditor
{
    [CustomEditor(typeof(ThrownWeapon), true)]
    public class ThrownWeaponEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // Show origin point camera matcher
            WieldableTransformUtilities.ShowOriginPointCameraMatcher((target as Component).transform);
        }
    }
}

#endif