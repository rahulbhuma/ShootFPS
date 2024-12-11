#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using NeoFPS;
using NeoFPS.ModularFirearms;
using NeoSaveGames.Serialization;

namespace NeoFPSEditor.ModularFirearms
{
    [CustomEditor(typeof(ModularFirearm), true)]
    public class ModularFirearmEditor : Editor
    {
        private readonly GUIContent k_AddShooterLabel = new GUIContent("Add Shooter");
        private readonly GUIContent k_AddTriggerLabel = new GUIContent("Add Trigger");
        private readonly GUIContent k_AddAimerLabel = new GUIContent("Add Aimer");
        private readonly GUIContent k_AddReloaderLabel = new GUIContent("Add Reloader");
        private readonly GUIContent k_AddAmmoLabel = new GUIContent("Add Ammo");
        private readonly GUIContent k_AddAmmoEffectLabel = new GUIContent("Add Ammo Effect");
        private readonly GUIContent k_AddMuzzleEffectLabel = new GUIContent("Add Muzzle Effect");
        private readonly GUIContent k_AddShellEjectorLabel = new GUIContent("Add Shell Ejector");
        private readonly GUIContent k_AddRecoilHandlerLabel = new GUIContent("Add Recoil Handler");

        private static List<IShooter> s_Shooters = new List<IShooter>();
        private static List<ITrigger> s_Triggers = new List<ITrigger>();
        private static List<IReloader> s_Reloaders = new List<IReloader>();
        private static List<IAmmo> s_AmmoModules = new List<IAmmo>();
        private static List<IAmmoEffect> s_AmmoEffects = new List<IAmmoEffect>();
        private static List<IAimer> s_Aimers = new List<IAimer>();
        private static List<IRecoilHandler> s_RecoilHandlers = new List<IRecoilHandler>();
        private static List<IMuzzleEffect> s_MuzzleEffects = new List<IMuzzleEffect>();
        private static List<IEjector> s_Ejectors = new List<IEjector>();

        private static bool s_UseStandardInput = true;
        private static bool s_UseSaveSystem = true;
        private static InventoryType s_UseInventory = InventoryType.Standard;

        private ModularFirearm m_Firearm = null;
        private ModularFirearm m_FirearmPrefab = null;
        private Texture2D m_Icon = null;
        private GameObject m_GeoSelection = null;

        public enum InventoryType
        {
            Standard,
            Stacked,
            Swappable,
            Custom
        }

        protected void Awake ()
        {
            m_Firearm = target as ModularFirearm;
            m_FirearmPrefab = PrefabUtility.GetCorrespondingObjectFromSource(m_Firearm);

            // Load icon
            var guids = AssetDatabase.FindAssets("EditorImage_NeoFpsInspectorIcon");
            if (guids != null && guids.Length > 0)
                m_Icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
            else
                m_Icon = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            //NeoFpsEditorGUI.ScriptField(serializedObject);

            var animatorProperty = serializedObject.FindProperty("m_Animator");

            bool noGeometry = m_Firearm.transform.childCount == 0 && animatorProperty.objectReferenceValue == null;
            if (!noGeometry)
            {
                // Show properties
                base.OnInspectorGUI();

                // Show origin point camera matcher
                WieldableTransformUtilities.ShowOriginPointCameraMatcher((target as Component).transform);
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Modular Firearm Details", EditorStyles.boldLabel);

            if (GUILayout.Button("Modular Firearms Documentation"))
                Application.OpenURL("https://docs.neofps.com/manual/weapons-modular-firearms.html");

            string message = string.Empty;
            bool error = noGeometry || CheckFirearm(out message);
            if (noGeometry)
                message = "Firearm has no geometry. Please assign an animator component or use the quick setup below to set up the weapon heirarchy.";

            if (m_Icon != null)
            {
                EditorGUILayout.BeginHorizontal();

                // Show NeoFPS icon
                Rect r = EditorGUILayout.GetControlRect(GUILayout.Width(32), GUILayout.Height(32));
                if (m_Icon != null)
                {
                    r.width += 8f;
                    r.height += 8f;
                    GUI.Label(r, m_Icon);
                }
                
                EditorGUILayout.BeginVertical();
                if (error)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(message, EditorStyles.wordWrappedLabel);
                }
                else
                {
                    Color c = GUI.color;
                    GUI.color = NeoFpsEditorGUI.errorRed;
                    EditorGUILayout.LabelField(message, NeoFpsEditorGUI.wordWrappedBoldLabel);
                    GUI.color = c;
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            else
            {
                if (error)
                    EditorGUILayout.HelpBox(message, MessageType.Info);
                else
                    EditorGUILayout.HelpBox(message, MessageType.Error);
            }

            if (noGeometry)
            {
                ShowQuickSetup();
            }
            else
            {
                ShowModuleDetails(s_Shooters, k_AddShooterLabel, "shooter", false);
                EditorGUILayout.Space();
                ShowModuleDetails(s_Triggers, k_AddTriggerLabel, "trigger", false);
                EditorGUILayout.Space();
                ShowModuleDetails(s_Aimers, k_AddAimerLabel, "aimer", true);
                EditorGUILayout.Space();
                ShowModuleDetails(s_Reloaders, k_AddReloaderLabel, "reloader", false);
                EditorGUILayout.Space();
                ShowModuleDetails(s_AmmoModules, k_AddAmmoLabel, "ammo", false);
                EditorGUILayout.Space();
                ShowAmmoEffectDetails(s_AmmoEffects);
                EditorGUILayout.Space();
                ShowModuleDetails(s_RecoilHandlers, k_AddRecoilHandlerLabel, "recoil handler", false);
                EditorGUILayout.Space();
                ShowModuleDetails(s_MuzzleEffects, k_AddMuzzleEffectLabel, "muzzle effect", true);
                EditorGUILayout.Space();
                ShowModuleDetails(s_Ejectors, k_AddShellEjectorLabel, "shell ejector", true);
            }

            EditorGUILayout.EndVertical();
        }

        void ShowQuickSetup()
        {
            var animatorProperty = serializedObject.FindProperty("m_Animator");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(animatorProperty);
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);

            // Check if inspected firearm is a prefab and edited in the project view. Changing a prefab's heirarchy can only be done with PrefabUtility.ReplacePrefab
            // Which will cause Unity to fatal exception if the user hits undo afterwards
            // Might be able to remove this restriction for 2018.3+ due to new PrefabUtility.ApplyPrefabInstance method
            if (m_Firearm.gameObject.scene.rootCount == 0)
            {
                EditorGUILayout.HelpBox("Prefab quick setup cannot be used from the project heirarchy.\n\nPlease open the prefab and edit from the scene heirarchy to continue.", MessageType.Error);
                return;
            }

            s_UseInventory = (InventoryType)EditorGUILayout.EnumPopup("Use Inventory", (Enum)s_UseInventory);
            s_UseStandardInput = EditorGUILayout.Toggle("Use Standard Input", s_UseStandardInput);
            s_UseSaveSystem = EditorGUILayout.Toggle("Use Save System", s_UseSaveSystem);
            m_GeoSelection = EditorGUILayout.ObjectField("Weapon Geometry", m_GeoSelection, typeof(GameObject), false) as GameObject;

            // Check if no object selected
            bool valid = true;
            if (m_GeoSelection == null)
            {
                NeoFpsEditorGUI.MiniError("Please select a geometry object");
                valid = false;
            }
            else
            {
                if (m_GeoSelection.GetComponentInChildren<MeshRenderer>() == null && m_GeoSelection.GetComponentInChildren<SkinnedMeshRenderer>() == null)
                {
                    NeoFpsEditorGUI.MiniError("Selection does not contain any mesh renderers");
                    valid = false;
                }

                if (m_GeoSelection.GetComponentInChildren<Animator>() == null)
                {
                    NeoFpsEditorGUI.MiniWarning("Selection does not contain an animator");
                }
            }

            if (valid)
                NeoFpsEditorGUI.MiniInfo("Geometry object is valid. Hit the button to set up your weapon");
            else
                GUI.enabled = false;

            if (GUILayout.Button("Set Up Firearm"))
            {
                // Add stance manager
                Undo.AddComponent<FirearmWieldableStanceManager>(m_Firearm.gameObject);

                // Add inventory
                switch (s_UseInventory)
                {
                    case InventoryType.Standard:
                        if (m_Firearm.GetComponent<FpsInventoryWieldable>() == null)
                            Undo.AddComponent<FpsInventoryWieldable>(m_Firearm.gameObject);
                        break;
                    case InventoryType.Stacked:
                        if (m_Firearm.GetComponent<FpsInventoryWieldable>() == null)
                            Undo.AddComponent<FpsInventoryWieldable>(m_Firearm.gameObject);
                        break;
                    case InventoryType.Swappable:
                        if (m_Firearm.GetComponent<FpsInventoryWieldableSwappable>() == null)
                            Undo.AddComponent<FpsInventoryWieldableSwappable>(m_Firearm.gameObject);
                        break;
                }

                // Add input
                if (s_UseStandardInput && m_Firearm.GetComponent<InputFirearm>() == null)
                    Undo.AddComponent<InputFirearm>(m_Firearm.gameObject);

                // Add NSGO
                if (s_UseSaveSystem && m_Firearm.GetComponent<NeoSerializedGameObject>() == null)
                    Undo.AddComponent<NeoSerializedGameObject>(m_Firearm.gameObject);

                // Create the weapon spring
                var springGO = new GameObject("WeaponSpring");
                Undo.RegisterCreatedObjectUndo(springGO, "Set up weapon");
                springGO.transform.SetParent(m_Firearm.transform);
                springGO.transform.localPosition = Vector3.zero;
                springGO.transform.localRotation = Quaternion.identity;
                springGO.transform.localScale = Vector3.one;

                // Add AdditiveTransformHandler
                Undo.AddComponent<AdditiveTransformHandler>(springGO);

                // Add additive effects
                Undo.AddComponent<FirearmRecoilEffect>(springGO);
                Undo.AddComponent<AdditiveKicker>(springGO);
                Undo.AddComponent<AdditiveJiggle>(springGO);
                Undo.AddComponent<BreathingEffect>(springGO);
                Undo.AddComponent<WeaponAimAmplifier>(springGO);
                var bob = new SerializedObject(Undo.AddComponent<PositionBob>(springGO));
                bob.FindProperty("m_BobType").enumValueIndex = 1;

                // Add geometry
                var geoTransform = Instantiate(m_GeoSelection, springGO.transform).transform;
                Undo.RegisterCreatedObjectUndo(geoTransform.gameObject, "Set up weapon");
                geoTransform.localPosition = Vector3.zero;
                geoTransform.localRotation = Quaternion.identity;
                geoTransform.localScale = Vector3.one;            
            }

            GUI.enabled = true;
        }

        void ShowModuleDetails<T>(List<T> buffer, GUIContent buttonLabel, string moduleName, bool optional)
        {
            if (EditorGUILayout.DropdownButton(buttonLabel, FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();
                List<Type> validTypes = GetScriptTypes(typeof(T));
                foreach (var t in validTypes)
                    menu.AddItem(new GUIContent(t.ToString()), false, AddModule, t);
                menu.ShowAsContext();
            }

            int activeShooters = 0;
            GetModules(buffer);

            bool[] startEnabled = new bool[buffer.Count];
            for (int i = 0; i < buffer.Count; ++i)
            {
                // Check if monobehaviour disabled
                var mb = buffer[i] as MonoBehaviour;
                if (!mb.enabled)
                {
                    startEnabled[i] = false;
                    continue;
                }

                // Only check components on active objects
                if (mb.gameObject.activeSelf)
                {
                    // Check if has "Start Active" property and it's enabled
                    var so = new SerializedObject(mb);
                    var prop = so.FindProperty("m_StartActive");

                    // If set to start active or doesn't have property, count it
                    startEnabled[i] = (prop == null || prop.boolValue);
                    if (startEnabled[i])
                        ++activeShooters;
                }
            }

            // Draw warnings
            switch (activeShooters)
            {
                case 0:
                    if (!optional)
                        EditorGUILayout.HelpBox(string.Format("Firearm requires a {0} module that is active on start.", moduleName), MessageType.Error);
                    break;
                case 1:
                    break;
                default:
                    EditorGUILayout.HelpBox(string.Format("Firearm has too many {0} modules that are active on start.\nEither remove the excess or make sure only one is set to start active.", moduleName), MessageType.Warning);
                    break;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Existing: ", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.BeginVertical();
            if (buffer.Count > 0)
            {
                for (int i = 0; i < buffer.Count; ++i)
                {
                    var module = buffer[i];
                    var moduleBehaviour = buffer[i] as MonoBehaviour;

                    // Check if module is valid
                    bool valid = moduleBehaviour != null && CheckModuleValid(module);
                    
                    // Error colour
                    Color c = GUI.color;
                    if (!valid)
                        GUI.color = NeoFpsEditorGUI.errorRed;

                    // Check if module is on root object
                    bool isRoot = (moduleBehaviour.transform == m_Firearm.transform);

                    // Label string
                    string labelString = isRoot ?
                        module.GetType().Name :
                        string.Format("{0} ({1})", module.GetType().Name, moduleBehaviour.gameObject.name);

                    // Get label rect
                    var rect = EditorGUILayout.GetControlRect();
                    bool canViewChild = !isRoot && moduleBehaviour.gameObject.scene.IsValid();
                    if (canViewChild)
                        rect.width -= 20;

                    // Show label (start enabled as bold)
                    if (startEnabled[i])
                        EditorGUI.LabelField(rect, labelString, EditorStyles.boldLabel);
                    else
                        EditorGUI.LabelField(rect, labelString);

                    if (canViewChild)
                    {
                        rect.x += rect.width;
                        rect.width = 20;
                        if (GUI.Button(rect, EditorGUIUtility.FindTexture("d_ViewToolOrbit"), EditorStyles.label))
                            EditorGUIUtility.PingObject(moduleBehaviour.gameObject);
                    }

                    if (!valid)
                        GUI.color = c;
                }
            }
            else
                EditorGUILayout.LabelField("<none>");
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            buffer.Clear();
        }

        void ShowAmmoEffectDetails(List<IAmmoEffect> buffer)
        {
            if (EditorGUILayout.DropdownButton(k_AddAmmoEffectLabel, FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();
                List<Type> validTypes = GetScriptTypes(typeof(IAmmoEffect));
                foreach (var t in validTypes)
                    menu.AddItem(new GUIContent(t.ToString()), false, AddModule, t);
                menu.ShowAsContext();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Existing: ", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.BeginVertical();
            GetModules(buffer);
            if (buffer.Count > 0)
            {
                foreach (var m in buffer)
                {
                    // Label string
                    if (m is MonoBehaviour behaviour)
                    {
                        string labelString = (behaviour.transform == m_Firearm.transform) ?
                            m.GetType().Name :
                            string.Format("{0} ({1})", m.GetType().Name, behaviour.gameObject.name);
                        EditorGUILayout.LabelField(labelString);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Firearm requires a ammo effect module that is active on start.", MessageType.Error);
                EditorGUILayout.LabelField("<none>");
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            buffer.Clear();
        }

        void AddModule(object o)
        {
            Undo.AddComponent(m_Firearm.gameObject, (Type)o);
        }

        List<Type> GetScriptTypes(Type baseClass)
        {
            List<Type> result = new List<Type>();

            var guids = AssetDatabase.FindAssets("t:MonoScript");
            for (int i = 0; i < guids.Length; ++i)
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guids[i]));
                var t = script.GetClass();
                if (t != null && baseClass.IsAssignableFrom(t) && script.GetClass().IsSubclassOf(typeof(MonoBehaviour)))
                    result.Add(t);
            }

            return result;
        }

        bool CheckFirearm (out string message)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                message = string.Empty;
                return true;
            }

            string issues = string.Empty;
            bool result = true;

            // Get all modules
            GetModules(s_Shooters);
            GetModules(s_Triggers);
            GetModules(s_Reloaders);
            GetModules(s_AmmoModules);
            GetModules(s_AmmoEffects);
            GetModules(s_RecoilHandlers);
            GetModules(s_Aimers);
            GetModules(s_MuzzleEffects);
            GetModules(s_Ejectors);

            // Check shooters
            int numActive = GetActiveCount(s_Shooters);
            if (numActive != 1)
            {
                if (numActive == 0)
                    issues += "\n- Firearm requires a shooter module";
                else
                    issues += "\n- Too many shooter modules active on start";
                result = false;
            }
            if (!CheckModulesValid(s_Shooters))
            {
                issues += "\n- One or more shooter modules has an error";
                result = false;
            }

            // Check triggers
            numActive = GetActiveCount(s_Triggers);
            if (numActive != 1)
            {
                if (numActive == 0)
                    issues += "\n- Firearm requires a trigger module";
                else
                    issues += "\n- Too many trigger modules active on start";
                result = false;
            }
            if (!CheckModulesValid(s_Triggers))
            {
                issues += "\n- One or more trigger modules has an error";
                result = false;
            }

            // Check reloaders
            numActive = GetActiveCount(s_Reloaders);
            if (numActive != 1)
            {
                if (numActive == 0)
                    issues += "\n- Firearm requires a reloader module";
                else
                    issues += "\n- Too many reloader modules active on start";
                result = false;
            }
            if (!CheckModulesValid(s_Reloaders))
            {
                issues += "\n- One or more reloader modules has an error";
                result = false;
            }

            // Check ammo
            numActive = GetActiveCount(s_AmmoModules);
            if (numActive != 1)
            {
                if (numActive == 0)
                    issues += "\n- Firearm requires an ammo module";
                else
                    issues += "\n- Too many ammo modules active on start";
                result = false;
            }
            if (!CheckModulesValid(s_AmmoModules))
            {
                issues += "\n- One or more ammo modules has an error";
                result = false;
            }

            // Check ammo effects
            if (s_AmmoEffects.Count == 0)
            {
                issues += "\n- Firearm requires an ammo effect";
                result = false;
            }
            if (!CheckModulesValid(s_AmmoEffects))
            {
                issues += "\n- One or more ammo effect modules has an error";
                result = false;
            }

            // Check recoil handlers
            numActive = GetActiveCount(s_RecoilHandlers);
            if (numActive != 1)
            {
                if (numActive == 0)
                    issues += "\n- Firearm requires a recoil handler module";
                else
                    issues += "\n- Too many recoil handler modules active on start";
                result = false;
            }
            if (!CheckModulesValid(s_RecoilHandlers))
            {
                issues += "\n- One or more recoil handler modules has an error";
                result = false;
            }

            // Check optional handlers
            if (!CheckModulesValid(s_Aimers))
            {
                issues += "\n- One or more aimer modules has an error";
                result = false;
            }
            if (!CheckModulesValid(s_MuzzleEffects))
            {
                issues += "\n- One or more muzzle effect modules has an error";
                result = false;
            }
            if (!CheckModulesValid(s_Ejectors))
            {
                issues += "\n- One or more ejector modules has an error";
                result = false;
            }

            // Clear buffers
            s_Shooters.Clear();
            s_Triggers.Clear();
            s_Reloaders.Clear();
            s_AmmoModules.Clear();
            s_AmmoEffects.Clear();
            s_RecoilHandlers.Clear();
            s_Aimers.Clear();
            s_MuzzleEffects.Clear();
            s_Ejectors.Clear();

            if (result)
                message = "Firearm has all the required modules set up correctly";
            else
                message = "Firearm has the following issues:" + issues;

            return result;
        }


        void GetModules<T>(List<T> buffer)
        {
            buffer.Clear();
            m_Firearm.GetComponentsInChildren(true, buffer);

            var firearm = m_Firearm as IModularFirearm;
            var firearmPrefab = m_FirearmPrefab as IModularFirearm;

            // Trim modules for a separate firearm
            for (int i = buffer.Count - 1; i >= 0; --i)
            {
                var behaviour = buffer[i] as MonoBehaviour;
                var parentFirearm = behaviour.GetComponentInParent<IModularFirearm>();
                if (parentFirearm == null)
                    parentFirearm = behaviour.GetComponent<IModularFirearm>();

                if (parentFirearm != firearm && parentFirearm != firearmPrefab)
                    buffer.RemoveAt(i);
            }
        }

        int GetActiveCount<T>(List<T> buffer)
        {
            int result = 0;
            foreach (var module in buffer)
            {
                var mb = module as MonoBehaviour;
                if (!mb.enabled || (mb.transform != m_Firearm.transform && !mb.gameObject.activeSelf))
                    continue;

                // Check if has "Start Active" property and it's enabled
                var so = new SerializedObject(mb);
                var prop = so.FindProperty("m_StartActive");

                // If set to start active or doesn't have property, count it
                if (prop == null || prop.boolValue)
                    ++result;
            }

            return result;
        }

        bool CheckModulesValid<T>(List<T> buffer)
        {
            for (int i = 0; i < buffer.Count; ++i)
            {
                if (!CheckModuleValid(buffer[i]))
                    return false;
            }
            return true;
        }

        bool CheckModuleValid<T>(T component)
        {
            // Check module interface
            var module = component as IFirearmModuleValidity;
            if (module != null && !module.isModuleValid)
                return false;

            // Check for bad animator parameter keys (annoyingly has to be done from editor - IFirearmModule implementations can't do it)
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // Get all fields
                var fields = component.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    // Check if they have the AnimatorParameterKeyAttribute
                    var attributes = field.GetCustomAttributes(typeof(AnimatorParameterKeyAttribute), false);
                    if (attributes != null && attributes.Length > 0)
                    {
                        // Check if the value is valid
                        SerializedObject so = new SerializedObject(component as MonoBehaviour);
                        var prop = so.FindProperty(field.Name);
                        foreach (AnimatorParameterKeyAttribute attr in attributes)
                        {
                            if (!AnimatorParameterKeyDrawer.CheckValid(attr, prop))
                                return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}

#endif