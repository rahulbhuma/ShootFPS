#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using NeoFPS;
using NeoFPSEditor.ScriptGeneration;
using System;

namespace NeoFPSEditor
{
    [CustomEditor(typeof(NeoFpsInputManager))]
    public class NeoFpsInputManagerEditor : NeoFpsInputManagerBaseEditor
    {
        protected override void InspectInternal()
        {
            InspectInputButtons();
            EditorGUILayout.Space();

            NeoFpsEditorGUI.Separator();
            InspectInputAxes();
            EditorGUILayout.Space();

            NeoFpsEditorGUI.Separator();
            InspectGamepadProfiles();
            EditorGUILayout.Space();

            NeoFpsEditorGUI.Separator();
            base.InspectInternal();
        }

        #region INPUT BUTTONS

        class InputButtonSelectionList : ReorderableSelectionList
        {
            private static readonly GUIContent s_StanceListEmpty = new GUIContent("Button list is empty");
            private static readonly GUIContent s_StanceListSelection = new GUIContent("Select an input button from the list above to edit its properties");

            private NeoFpsInputManagerEditor m_Editor = null;
            private SerializedProperty m_RequiresRebuild = null;
            private SerializedProperty m_ButtonPropsDirty = null;
            private SerializedProperty m_ButtonsError = null;

            public int numNameValidErrors { get; private set; }
            public int numNameDuplicateErrors { get; private set; }
            public int numNameReservedErrors { get; private set; }
            public int numDisplayValidErrors { get; private set; }
            public int numDisplayDuplicateErrors { get; private set; }

            public bool isValid
            {
                get { return numNameValidErrors + numNameDuplicateErrors + numNameReservedErrors + numDisplayValidErrors + numDisplayDuplicateErrors == 0; }
            }

            public InputButtonSelectionList(NeoFpsInputManagerEditor editor, SerializedObject so, SerializedProperty prop) :
                base(so, prop, "FPS Input Buttons", true, true)
            {
                m_Editor = editor;
                m_RequiresRebuild = so.FindProperty("m_ButtonsRequireRebuild");
                m_ButtonPropsDirty = so.FindProperty("m_InputButtonsDirty");
                m_ButtonsError = so.FindProperty("m_InputButtonsError");
            }

            protected override void AppendElement(SerializedProperty prop)
            {
                // Grow the array
                int lastIndex = prop.arraySize++;
                var newElement = serializedProperty.GetArrayElementAtIndex(lastIndex);

                // Reset properties
                newElement.FindPropertyRelative("m_Name").stringValue = string.Empty;
                newElement.FindPropertyRelative("m_DisplayName").stringValue = string.Empty;
                newElement.FindPropertyRelative("m_Category").enumValueIndex = 0;
                newElement.FindPropertyRelative("m_Context").enumValueIndex = 0;
                newElement.FindPropertyRelative("m_DefaultPrimary").enumValueIndex = 0;
                newElement.FindPropertyRelative("m_DefaultSecondary").enumValueIndex = 0;

                // Reset errors
                newElement.FindPropertyRelative("m_NameInvalidError").boolValue = true;
                newElement.FindPropertyRelative("m_NameDuplicateError").boolValue = false;
                newElement.FindPropertyRelative("m_NameReservedError").boolValue = false;
                newElement.FindPropertyRelative("m_DisplayNameInvalidError").boolValue = true;
                newElement.FindPropertyRelative("m_DisplayNameDuplicateError").boolValue = false;
            }

            protected override void OnReorder(SerializedProperty prop, int oldIndex, int newIndex)
            {
                // Signal changed
                m_RequiresRebuild.boolValue = true;
            }

            protected override void OnRemove(SerializedProperty prop, int index)
            {
                // Signal changed
                m_RequiresRebuild.boolValue = true;
            }

            protected override void OnSelectionChanged(SerializedProperty listProp, int index)
            {
                base.OnSelectionChanged(listProp, index);
                throw new ExitGUIException();
            }

            protected override void InspectArrayElement(Rect r, SerializedProperty elementProp, int index)
            {
                var nameProp = elementProp.FindPropertyRelative("m_Name");

                bool hasError = false;
                hasError |= elementProp.FindPropertyRelative("m_NameInvalidError").boolValue;
                hasError |= elementProp.FindPropertyRelative("m_NameDuplicateError").boolValue;
                hasError |= elementProp.FindPropertyRelative("m_NameReservedError").boolValue;
                hasError |= elementProp.FindPropertyRelative("m_DisplayNameInvalidError").boolValue;
                hasError |= elementProp.FindPropertyRelative("m_DisplayNameDuplicateError").boolValue;

                if (hasError)
                    GUI.color = new Color(1f, 0.5f, 0.5f);

                var buttonName = nameProp.stringValue;
                if (string.IsNullOrWhiteSpace(buttonName))
                    EditorGUI.LabelField(r, "<Unnamed>");
                else
                    EditorGUI.LabelField(r, buttonName);

                GUI.color = Color.white;
            }

            protected override void InspectNoSelection(bool empty)
            {
                NoSelectionHelper(empty, s_StanceListEmpty, s_StanceListSelection);
            }

            protected override void InspectSelection(SerializedProperty selectedElement)
            {
                bool error = false;

                // Check the name
                var nameProp = selectedElement.FindPropertyRelative("m_Name");
                var nameValidError = selectedElement.FindPropertyRelative("m_NameInvalidError");
                var nameDuplicateError = selectedElement.FindPropertyRelative("m_NameDuplicateError");
                var nameReservedError = selectedElement.FindPropertyRelative("m_NameReservedError");
                if (nameValidError.boolValue)
                {
                    error = true;
                    ++numNameValidErrors;
                }
                if (nameDuplicateError.boolValue)
                {
                    error = true;
                    ++numNameDuplicateErrors;
                }
                if (nameReservedError.boolValue)
                {
                    error = true;
                    ++numNameReservedErrors;
                }

                // Show name field
                EditorGUI.BeginChangeCheck();

                if (error)
                    GUI.color = new Color(1f, 0.5f, 0.5f);
                EditorGUILayout.DelayedTextField(nameProp);
                GUI.color = Color.white;

                if (EditorGUI.EndChangeCheck())
                {
                    m_RequiresRebuild.boolValue = true;

                    nameValidError.boolValue = !ScriptGenerationUtilities.CheckClassOrPropertyName(nameProp.stringValue, false);
                    nameReservedError.boolValue = ScriptGenerationUtilities.CheckNameCollision(nameProp.stringValue, NeoFpsInputManager.fixedInputButtons);

                    // Check for duplicates
                    if (nameDuplicateError.boolValue)
                    {
                        for (int i = 0; i < serializedProperty.arraySize; ++i)
                        {
                            var e = serializedProperty.GetArrayElementAtIndex(i);
                            e.FindPropertyRelative("m_NameDuplicateError").boolValue = !ScriptGenerationUtilities.CheckNameIsUnique(serializedProperty, i, "m_Name");
                        }
                    }
                    else
                    {
                        List<int> duplicates = new List<int>();
                        if (ScriptGenerationUtilities.CheckForDuplicateNames(serializedProperty, index, "m_Name", duplicates) > 0)
                        {
                            for (int i = 0; i < duplicates.Count; ++i)
                                serializedProperty.GetArrayElementAtIndex(duplicates[i]).FindPropertyRelative("m_NameDuplicateError").boolValue = true;
                        }
                        else
                            nameDuplicateError.boolValue = false;
                    }
                }
                // Check the display name
                error = false;
                var displayNameProp = selectedElement.FindPropertyRelative("m_DisplayName");
                var displayNameValidError = selectedElement.FindPropertyRelative("m_DisplayNameInvalidError");
                var displayNameDuplicateError = selectedElement.FindPropertyRelative("m_DisplayNameDuplicateError");
                if (displayNameValidError.boolValue)
                {
                    error = true;
                    ++numDisplayValidErrors;
                }
                if (displayNameDuplicateError.boolValue)
                {
                    error = true;
                    ++numDisplayDuplicateErrors;
                }

                // Show the display name field
                EditorGUI.BeginChangeCheck();

                if (error)
                    GUI.color = new Color(1f, 0.5f, 0.5f);
                EditorGUILayout.DelayedTextField(displayNameProp);
                GUI.color = Color.white;

                if (EditorGUI.EndChangeCheck())
                {
                    m_ButtonPropsDirty.boolValue = true;

                    displayNameValidError.boolValue = string.IsNullOrEmpty(displayNameProp.stringValue);

                    // Check for duplicates
                    if (displayNameDuplicateError.boolValue)
                    {
                        for (int i = 0; i < serializedProperty.arraySize; ++i)
                        {
                            var e = serializedProperty.GetArrayElementAtIndex(i);
                            e.FindPropertyRelative("m_DisplayNameDuplicateError").boolValue = !ScriptGenerationUtilities.CheckNameIsUnique(serializedProperty, i, "m_Name");
                        }
                    }
                    else
                    {
                        List<int> duplicates = new List<int>();
                        if (ScriptGenerationUtilities.CheckForDuplicateNames(serializedProperty, index, "m_DisplayName", duplicates) > 0)
                        {
                            for (int i = 0; i < duplicates.Count; ++i)
                                serializedProperty.GetArrayElementAtIndex(duplicates[i]).FindPropertyRelative("m_DisplayNameDuplicateError").boolValue = true;
                        }
                        else
                            displayNameDuplicateError.boolValue = false;
                    }
                }

                EditorGUI.BeginChangeCheck();

                // Show category
                EditorGUILayout.PropertyField(selectedElement.FindPropertyRelative("m_Category"));

                // Show context
                EditorGUILayout.PropertyField(selectedElement.FindPropertyRelative("m_Context"));

                // Layout default keys
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Default Keys");
                EditorGUILayout.PropertyField(selectedElement.FindPropertyRelative("m_DefaultPrimary"), GUIContent.none);
                EditorGUILayout.PropertyField(selectedElement.FindPropertyRelative("m_DefaultSecondary"), GUIContent.none);
                EditorGUILayout.EndHorizontal();

                // Check for dirty
                if (EditorGUI.EndChangeCheck())
                    m_ButtonPropsDirty.boolValue = true;
            }

            public override void DoLayout()
            {
                numNameValidErrors = 0;
                numNameDuplicateErrors = 0;
                numNameReservedErrors = 0;
                numDisplayValidErrors = 0;
                numDisplayDuplicateErrors = 0;

                base.DoLayout();

                // Get state
                var state = InputButtonsState.Valid;
                if (m_RequiresRebuild.boolValue)
                    state |= InputButtonsState.RequiresRebuild;
                if (numNameValidErrors > 0)
                    state |= InputButtonsState.NameValidErrors;
                if (numNameDuplicateErrors > 0)
                    state |= InputButtonsState.NameDuplicateErrors;
                if (numNameReservedErrors > 0)
                    state |= InputButtonsState.NameReservedErrors;
                if (numDisplayValidErrors > 0)
                    state |= InputButtonsState.DisplayValidErrors;
                if (numDisplayDuplicateErrors > 0)
                    state |= InputButtonsState.DisplayDuplicateErrors;
                m_ButtonsError.intValue = (int)state;
            }
        }

        [Flags]
        enum InputButtonsState
        {
            Valid = 0,
            RequiresRebuild = 1,
            NameValidErrors = 2,
            NameDuplicateErrors = 4,
            NameReservedErrors = 8,
            DisplayValidErrors = 16,
            DisplayDuplicateErrors = 32
        }

        InputButtonSelectionList m_InputButtons = null;
        private InputButtonSelectionList inputButtonsList
        {
            get
            {
                if (m_InputButtons == null)
                    m_InputButtons = new InputButtonSelectionList(this, serializedObject, serializedObject.FindProperty("m_InputButtons"));
                return m_InputButtons;
            }
        }

        void InspectInputButtons()
        {
            var expandButtons = serializedObject.FindProperty("m_ExpandInputButtons");
            var requiresRebuild = serializedObject.FindProperty("m_ButtonsRequireRebuild");
            var buttonsDirty = serializedObject.FindProperty("m_InputButtonsDirty");
            var inputButtons = serializedObject.FindProperty("m_InputButtons");
            var revertTo = serializedObject.FindProperty("m_Revert");
            var snapshot = serializedObject.FindProperty("m_Snapshot");
            var errors = serializedObject.FindProperty("m_InputButtonsError");

            EditorGUILayout.LabelField("Input Buttons", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DefaultKeyboardLayout"));

            // Draw the list
            EditorGUI.BeginChangeCheck();
            expandButtons.boolValue = EditorGUILayout.Foldout(expandButtons.boolValue, "Input Button Details", true);
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            if (expandButtons.boolValue)
            {
                //EditorGUILayout.HelpBox("The following button names are reserved:\n• None\n• Menu\n• Back\n• Cancel", MessageType.None);
                EditorGUILayout.HelpBox("Reordering input buttons will break inspector properties that reference them such as the inventory slot buttons.\n\nRemoving input buttons might cause script errors.\n\nThe following button names are reserved:\n• None\n• Menu\n• Back\n• Cancel", MessageType.Warning);
                EditorGUILayout.Space();
                inputButtonsList.DoLayout();
                EditorGUILayout.Space();
            }

            // Get buttons state
            var state = (InputButtonsState)errors.intValue;

            // Show buttons state
            switch (state)
            {
                case InputButtonsState.Valid:
                    EditorGUILayout.HelpBox("FpsInputButton constants are valid and up to date.", MessageType.Info);
                    break;
                case InputButtonsState.RequiresRebuild:
                    EditorGUILayout.HelpBox("FpsInputButton constants settings have changed and need generating.", MessageType.Warning);
                    break;
                default:
                    string message = "The following errors were found:";
                    if ((state & InputButtonsState.NameValidErrors) != InputButtonsState.Valid)
                        message += "\n- One or more button names are not valid.";
                    if ((state & InputButtonsState.NameDuplicateErrors) != InputButtonsState.Valid)
                        message += "\n- Duplicate button names were found.";
                    if ((state & InputButtonsState.NameReservedErrors) != InputButtonsState.Valid)
                        message += "\n- One or more button names is reserved.";
                    if ((state & InputButtonsState.DisplayValidErrors) != InputButtonsState.Valid)
                        message += "\n- One or more button display names are not valid.";
                    if ((state & InputButtonsState.DisplayDuplicateErrors) != InputButtonsState.Valid)
                        message += "\n- Duplicate button display names were found.";
                    EditorGUILayout.HelpBox(message, MessageType.Error);
                    break;
            }

            // Disable if unchanged or invalid
            if (!requiresRebuild.boolValue)
                GUI.enabled = false;

            bool modified = false;

            // Generate FpsInputButton Constants
            if (GUILayout.Button("Generate FpsInputButton Constants"))
            {
                // Get the source script
                var folderObjectProp = serializedObject.FindProperty("m_ScriptFolder");
                var sourceAsset = serializedObject.FindProperty("m_ScriptTemplate").objectReferenceValue as TextAsset;
                if (sourceAsset != null)
                {
                    // Get the lines as an array
                    string[] lines = new string[inputButtons.arraySize];
                    for (int i = 0; i < inputButtons.arraySize; ++i)
                        lines[i] = inputButtons.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue;

                    // Apply lines
                    string source = sourceAsset.text;
                    source = ScriptGenerationUtilities.ReplaceKeyword(source, "NAME", "FpsInputButton");
                    source = ScriptGenerationUtilities.InsertMultipleLines(source, "VALUES", (int index) =>
                    {
                        int fixedCount = NeoFpsInputManager.fixedInputButtons.Length;
                        if (index >= lines.Length + fixedCount)
                            return null;

                        if (index < fixedCount)
                            return string.Format("\t\tpublic const int {0} = {1};", NeoFpsInputManager.fixedInputButtons[index], index);
                        else
                            return string.Format("\t\tpublic const int {0} = {1};", lines[index - fixedCount], index);
                    }, "COUNT");
                    source = ScriptGenerationUtilities.InsertMultipleLines(source, "VALUE_NAMES", (int index) =>
                    {
                        if (index >= lines.Length + NeoFpsInputManager.fixedInputButtons.Length)
                            return null;

                        int fixedCount = NeoFpsInputManager.fixedInputButtons.Length;
                        if (index < fixedCount)
                            return string.Format("\t\t\t\"{0}\",", NeoFpsInputManager.fixedInputButtons[index]);
                        else
                        {
                            if (index < lines.Length + fixedCount - 1)
                                return string.Format("\t\t\t\"{0}\",", lines[index - fixedCount]);
                            else
                                return string.Format("\t\t\t\"{0}\"", lines[index - fixedCount]);
                        }
                    });

                    // Write the script
                    ScriptGenerationUtilities.WriteScript(ScriptGenerationUtilities.GetFullScriptPath(folderObjectProp, "FpsInputButton"), source, true);

                    // Reset dirty flags
                    requiresRebuild.boolValue = false;
                    buttonsDirty.boolValue = false;
                    errors.intValue = 0;
                    CopyInputButtons(inputButtons, revertTo);
                    snapshot.arraySize = 0;
                }

                // Delete the user settings file
                var guids = AssetDatabase.FindAssets("t:NeoFPS.FpsKeyBindings");
                if (guids != null && guids.Length > 0)
                {
                    var settingsResource = AssetDatabase.LoadAssetAtPath<FpsKeyBindings>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    if (settingsResource != null)
                        settingsResource.DeleteSaveFile();
                }

                modified = true;
            }

            // Disable if unchanged
            GUI.enabled = requiresRebuild.boolValue || buttonsDirty.boolValue;

            // Create snapshot
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Snapshot"))
            {
                CopyInputButtons(inputButtons, snapshot);
                modified = true;
            }

            // Disable if snapshot doesn't exist
            GUI.enabled = serializedObject.FindProperty("m_Snapshot").arraySize > 0;

            // Revert to snapshot
            if (GUILayout.Button("Revert To Snapshot"))
            {
                CopyInputButtons(snapshot, inputButtons);
                modified = true;
            }
            EditorGUILayout.EndHorizontal();

            // Disable if unchanged
            GUI.enabled = requiresRebuild.boolValue || buttonsDirty.boolValue;

            // Revert changes
            if (GUILayout.Button("Revert To Last Generated"))
            {
                requiresRebuild.boolValue = false;
                buttonsDirty.boolValue = false;

                CopyInputButtons(revertTo, inputButtons);
                snapshot.arraySize = 0;

                modified = true;
            }

            GUI.enabled = true;

            EditorGUILayout.Space();

            if (modified)
            {
                CheckInputButtons(serializedObject.FindProperty("m_InputButtons"));
                serializedObject.ApplyModifiedProperties();
                GUIUtility.hotControl = 0;
                GUIUtility.ExitGUI();
            }
        }

        void CopyInputButtons(SerializedProperty source, SerializedProperty destination)
        {
            destination.arraySize = source.arraySize;
            for (int i = 0; i < destination.arraySize; ++i)
            {
                var copyFrom = source.GetArrayElementAtIndex(i);
                var copyTo = destination.GetArrayElementAtIndex(i);

                copyTo.FindPropertyRelative("m_Name").stringValue = copyFrom.FindPropertyRelative("m_Name").stringValue;
                copyTo.FindPropertyRelative("m_DisplayName").stringValue = copyFrom.FindPropertyRelative("m_DisplayName").stringValue;
                copyTo.FindPropertyRelative("m_Category").enumValueIndex = copyFrom.FindPropertyRelative("m_Category").enumValueIndex;
                copyTo.FindPropertyRelative("m_Context").enumValueIndex = copyFrom.FindPropertyRelative("m_Context").enumValueIndex;
                copyTo.FindPropertyRelative("m_DefaultPrimary").enumValueIndex = copyFrom.FindPropertyRelative("m_DefaultPrimary").enumValueIndex;
                copyTo.FindPropertyRelative("m_DefaultSecondary").enumValueIndex = copyFrom.FindPropertyRelative("m_DefaultSecondary").enumValueIndex;
            }
        }

        void CheckInputButtons(SerializedProperty arrayProp)
        {
            int numNameValidErrors = 0;
            int numNameDuplicateErrors = 0;
            int numNameReservedErrors = 0;
            int numDisplayValidErrors = 0;
            int numDisplayDuplicateErrors = 0;
            List<int> duplicates = new List<int>();

            for (int index = 0; index < arrayProp.arraySize; ++index)
            {
                var element = arrayProp.GetArrayElementAtIndex(index);

                // Check the name
                var nameProp = element.FindPropertyRelative("m_Name");
                var nameValidError = element.FindPropertyRelative("m_NameInvalidError");
                var nameDuplicateError = element.FindPropertyRelative("m_NameDuplicateError");
                var nameReservedError = element.FindPropertyRelative("m_NameReservedError");
                var displayNameProp = element.FindPropertyRelative("m_DisplayName");
                var displayNameValidError = element.FindPropertyRelative("m_DisplayNameInvalidError");
                var displayNameDuplicateError = element.FindPropertyRelative("m_DisplayNameDuplicateError");

                nameValidError.boolValue = !ScriptGenerationUtilities.CheckClassOrPropertyName(nameProp.stringValue, false);
                nameReservedError.boolValue = ScriptGenerationUtilities.CheckNameCollision(nameProp.stringValue, NeoFpsInputManager.fixedInputButtons);

                // Check name
                if (string.IsNullOrWhiteSpace(nameProp.stringValue))
                {
                    nameValidError.boolValue = true;
                    ++numNameValidErrors;
                }
                else
                    nameValidError.boolValue = false;

                if (ScriptGenerationUtilities.CheckNameCollision(nameProp.stringValue, NeoFpsInputManager.fixedInputButtons))
                {
                    nameReservedError.boolValue = true;
                    ++numNameReservedErrors;
                }
                else
                    nameReservedError.boolValue = false;

                if (ScriptGenerationUtilities.CheckForDuplicateNames(arrayProp, index, "m_Name", duplicates) > 0)
                {
                    nameDuplicateError.boolValue = true;
                    ++numNameDuplicateErrors;
                }
                else
                    nameDuplicateError.boolValue = false;

                // Check display name
                if (string.IsNullOrWhiteSpace(displayNameProp.stringValue))
                {
                    displayNameValidError.boolValue = true;
                    ++numDisplayValidErrors;
                }
                else
                    displayNameValidError.boolValue = false;

                if (ScriptGenerationUtilities.CheckForDuplicateNames(arrayProp, index, "m_DisplayName", duplicates) > 0)
                {
                    displayNameDuplicateError.boolValue = true;
                    ++numDisplayDuplicateErrors;
                }
                else
                    displayNameDuplicateError.boolValue = false;
            }

            // Get state
            var state = InputButtonsState.Valid;
            if (numNameValidErrors > 0)
                state |= InputButtonsState.NameValidErrors;
            if (numNameDuplicateErrors > 0)
                state |= InputButtonsState.NameDuplicateErrors;
            if (numNameReservedErrors > 0)
                state |= InputButtonsState.NameReservedErrors;
            if (numDisplayValidErrors > 0)
                state |= InputButtonsState.DisplayValidErrors;
            if (numDisplayDuplicateErrors > 0)
                state |= InputButtonsState.DisplayDuplicateErrors;
            serializedObject.FindProperty("m_InputButtonsError").intValue = (int)state;
        }

        #endregion

        #region INPUT AXES

        private ConstantsGenerator m_InputAxesGenerator = null;
        public ConstantsGenerator inputAxesGenerator
        {
            get
            {
                if (m_InputAxesGenerator == null)
                    m_InputAxesGenerator = new ConstantsGenerator(serializedObject, "m_InputAxisInfo", "m_InputAxisDirty", "m_InputAxisError");
                return m_InputAxesGenerator;
            }
        }

        void InspectInputAxes()
        {
            EditorGUILayout.LabelField("Input Axes", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MouseXAxis"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MouseYAxis"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MouseScrollAxis"));

            if (inputAxesGenerator.DoLayoutGenerator())
            {
                var scriptSource = serializedObject.FindProperty("m_ScriptTemplate").objectReferenceValue as TextAsset;
                if (scriptSource == null)
                {
                    Debug.LogError("Attempting to generate constants script when no source files has been set");
                }
                else
                {
                    var folderObject = serializedObject.FindProperty("m_ScriptFolder");
                    inputAxesGenerator.GenerateConstants(folderObject, "FpsInputAxis", scriptSource.text);
                }
            }
        }

        #endregion

        #region GAMEPAD PROFILES

        void InspectGamepadProfiles()
        {
            EditorGUILayout.LabelField("Gamepad Profiles", EditorStyles.boldLabel);

            var gamepadProfiles = serializedObject.FindProperty("m_GamepadProfiles");

            if (GUILayout.Button("Add Profile"))
            {
                ++gamepadProfiles.arraySize;
            }

            for (int i = 0; i < gamepadProfiles.arraySize; ++i)
            {
                // Get the profile
                var profile = gamepadProfiles.GetArrayElementAtIndex(i);


                var expanded = profile.FindPropertyRelative("expanded");
                var profileName = profile.FindPropertyRelative("m_Name");

                expanded.boolValue = EditorGUILayout.Foldout(expanded.boolValue, profileName.stringValue, true);

                if (expanded.boolValue)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.PropertyField(profileName);
                    GUILayout.Space(2);
                    EditorGUILayout.PropertyField(profile.FindPropertyRelative("m_AnalogueSetup"));
                    GUILayout.Space(2);

                    var buttonMappings = profile.FindPropertyRelative("m_ButtonMappings");
                    for (int j = 0; j < buttonMappings.arraySize; ++j)
                        InspectGamepadButtonMapping(buttonMappings.GetArrayElementAtIndex(j), ((GamepadButton)j).ToString());

                    GUILayout.Space(2);
                    if (GUILayout.Button("Remove Profile"))
                    {
                        SerializedArrayUtility.RemoveAt(gamepadProfiles, i);
                        break;
                    }

                    EditorGUILayout.EndVertical();
                }
            }
        }

        void InspectGamepadButtonMapping(SerializedProperty prop, string gamepadButton)
        {
            var buttonsProp = prop.FindPropertyRelative("m_Buttons");
            for (int i = 0; i <= buttonsProp.arraySize; ++i)
            {
                var rect = EditorGUILayout.GetControlRect();

                // Draw label
                if (i == 0)
                {
                    var labelRect = rect;
                    labelRect.width = EditorGUIUtility.labelWidth;
                    EditorGUI.LabelField(labelRect, gamepadButton);
                }

                // Draw dropdowns
                if (i == buttonsProp.arraySize)
                {
                    // Get field rect
                    rect.width -= EditorGUIUtility.labelWidth;
                    rect.x += EditorGUIUtility.labelWidth;

                    if (EditorGUI.DropdownButton(rect, new GUIContent("Add Button..."), FocusType.Passive))
                    {
                        List<FpsInputButton> validButtons = GetValidButtons(buttonsProp);
                        if (validButtons.Count > 0)
                        {
                            var menu = new GenericMenu();

                            for (int j = 0; j < validButtons.Count; ++j)
                                menu.AddItem(new GUIContent(FpsInputButton.names[validButtons[j]]), false, (o) => {
                                    int index = buttonsProp.arraySize++;
                                    buttonsProp.GetArrayElementAtIndex(index).FindPropertyRelative("m_Value").intValue = (int)o;
                                    buttonsProp.serializedObject.ApplyModifiedProperties();
                                }, (int)validButtons[j]);

                            menu.ShowAsContext();
                        }
                    }
                }
                else
                {
                    // Get field rect
                    rect.width -= EditorGUIUtility.labelWidth + 62;
                    rect.x += EditorGUIUtility.labelWidth;

                    // Draw the button name
                    var mapping = buttonsProp.GetArrayElementAtIndex(i);
                    var fpsButtonName = FpsInputButton.names[mapping.FindPropertyRelative("m_Value").intValue];
                    EditorGUI.SelectableLabel(rect, fpsButtonName, EditorStyles.textField);

                    // Draw the remove button
                    rect.x += rect.width + 2;
                    rect.width = 60;
                    if (GUI.Button(rect, "Remove"))
                    {
                        SerializedArrayUtility.RemoveAt(buttonsProp, i);
                        buttonsProp.serializedObject.ApplyModifiedProperties();
                        throw new ExitGUIException();
                    }
                }
            }
            GUILayout.Space(2);
        }

        List<FpsInputButton> GetValidButtons(SerializedProperty prop)
        {
            var result = new List<FpsInputButton>();
            if (prop.arraySize == 0)
            {
                for (int i = 0; i < FpsInputButton.count; ++i)
                    result.Add(i);
                return result;
            }
            else
            {
                var used = new List<FpsInputButton>();
                for (int i = 0; i < prop.arraySize; ++i)
                    used.Add(prop.GetArrayElementAtIndex(i).FindPropertyRelative("m_Value").intValue);

                // Check against used
                for (int i = 0; i < FpsInputButton.count; ++i)
                {
                    if (!used.Contains(i))
                        result.Add(i);
                }

                /*
                // Get used
                var used = new List<KeyBindingContext>();
                for (int i = 0; i < prop.arraySize; ++i)
                    used.Add(GetContextForButton(prop.GetArrayElementAtIndex(i).FindPropertyRelative("m_Value").intValue));

                // Check against used
                for (int i = 0; i < FpsInputButton.count; ++i)
                {
                    bool canOverlap = true;
                    for (int j = 0; j < used.Count; ++j)
                        canOverlap &= KeyBindingContextMatrix.CanOverlap(GetContextForButton(i), used[j]);

                    if (canOverlap)
                        result.Add(i);
                }
                */
                return result;
            }
        }

        KeyBindingContext GetContextForButton(FpsInputButton b)
        {
            if (b < NeoFpsInputManager.fixedInputButtons.Length)
                return KeyBindingContext.Default;

            var buttonsSetup = serializedObject.FindProperty("m_InputButtons");
            var button = buttonsSetup.GetArrayElementAtIndex(b - NeoFpsInputManager.fixedInputButtons.Length);
            return (KeyBindingContext)(button.FindPropertyRelative("m_Context").enumValueIndex);
        }

        #endregion
    }
}

#endif