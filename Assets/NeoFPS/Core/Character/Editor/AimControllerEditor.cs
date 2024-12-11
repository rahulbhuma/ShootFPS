#if UNITY_EDITOR

using UnityEditor;
using NeoFPS;

namespace NeoFPSEditor
{
    [ CustomEditor(typeof(AimController), true)]
    public class AimControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
}

#endif