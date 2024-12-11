using NeoSaveGames;
using NeoSaveGames.Serialization;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS.ModularFirearms
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-modularfirearmmodeswitcher.html")]
    public class ModularFirearmModeSwitcher : MonoBehaviour, IModularFirearmModeSwitcher, INeoSerializableComponent
    {
        [SerializeField, Tooltip("The different firearm modes. Switching modes will loop through these.")]
        private FirearmMode[] m_Modes = new FirearmMode[0];

        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Trigger, true, false), Tooltip("The name of a trigger parameter in the weapon's animator controller that will be set when the mode is switched.")]
        private string m_OnSwitchAnimTrigger = string.Empty;

        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Int, true, false), Tooltip("The name of an integer parameter in the weapon's animator controller that will be set to the current mode index when the mode is switched.")]
        private string m_ModeIndexAnimInteger = string.Empty;

        [SerializeField, Tooltip("An event fired whenever the firearm mode is switched.")]
        private UnityEvent m_OnSwitchModes = new UnityEvent();

        [SerializeField, Tooltip("Should the mode switcher be registered with the firearm it's attached to on start. If so then it will be exposed to the \"switch modes\" input. If not then it will need triggering manually via code or events.")]
        private bool m_RegisterOnStart = true;

        [SerializeField, Tooltip("Should the event be fired when the weapon picks its initial mode, or only when the mode is manually switched")]
        private bool m_FireEventOnStart = true;

        private static readonly NeoSerializationKey k_ModeIndexKey = new NeoSerializationKey("modeIndex");

        private int m_Index = -1;
        private bool m_Loaded = false;
        private int m_OnSwitchTriggerHash = -1;
        private int m_ModeIndexHash = -1;

        [Serializable]
        public struct FirearmMode
        {
            [Tooltip("The name displayed on the HUD for this mode. Leave this blank if you do not want the mode name displayed.")]
            public string descriptiveName;

            [Tooltip("The components to enable for this mode (for firearm module components, this will disable the old component automatically).")]
            public Behaviour[] components;

            public void OnValidate()
            {
                // Check for broken references
                int broken = 0;
                for (int i = 0; i < components.Length; ++i)
                {
                    if (components[i] == null)
                        ++broken;
                }

                // Create a new array, populate with correct refs, and swap
                if (broken > 0)
                {
                    var swap = new Behaviour[components.Length - broken];
                    for (int i = 0, j = 0; i < components.Length; ++i)
                    {
                        if (components[i] != null)
                        {
                            swap[j] = components[i];
                            ++j;
                        }
                    }

                    components = swap;
                }
            }
        }

        public event UnityAction onSwitchModes
        {
            add { m_OnSwitchModes.AddListener(value); }
            remove { m_OnSwitchModes.RemoveListener(value); }
        }

        public IModularFirearm firearm
        {
            get;
            private set;
        }

        public int currentIndex
        {
            get { return m_Index; }
        }

        public string currentMode
        {
            get
            {
                if (m_Index != -1)
                    return m_Modes[m_Index].descriptiveName;
                else
                    return string.Empty;
            }
        }

        protected void OnValidate()
        {
            for (int i = 0; i < m_Modes.Length; ++i)
                m_Modes[i].OnValidate();
        }

        IEnumerator Start()
        {
            // Get the firearm this is attached to
            firearm = GetComponentInParent<IModularFirearm>();

            // Get the animator parameter hashes if required
            if (!string.IsNullOrWhiteSpace(m_OnSwitchAnimTrigger))
                m_OnSwitchTriggerHash = Animator.StringToHash(m_OnSwitchAnimTrigger);
            if (!string.IsNullOrWhiteSpace(m_ModeIndexAnimInteger))
                m_ModeIndexHash = Animator.StringToHash(m_ModeIndexAnimInteger);

            // Wait to make sure the firearm is fully set up
            yield return null;

            // Register the mode switcher
            if (m_RegisterOnStart)
            {
                if (firearm != null)
                    firearm.modeSwitcher = this;
            }
        }

        public void GetStartingMode()
        {
            if (!m_Loaded && m_Modes.Length != 0)
            {
                m_Index = 0;

                // Apply
                ApplyModeSwitchInternal(m_FireEventOnStart);
            }
        }

        public void SwitchModes()
        {
            if (m_Index != -1)
            {
                // Get the new index
                if (++m_Index >= m_Modes.Length)
                    m_Index -= m_Modes.Length;

                // Apply
                ApplyModeSwitchInternal(true);
            }
        }

        public void SwitchModes(int index)
        {
            if (m_Index != -1)
            {
                if (index < 0 || index >= m_Modes.Length)
                {
                    Debug.LogError("Attempting to set firearm mode by index, out of range.");
                    return;
                }

                // Get the new index
                m_Index = index;

                // Apply
                ApplyModeSwitchInternal(true);
            }
        }
        
        public void SwitchModes(string modeName)
        {
            if (m_Index != -1)
            {
                if (string.IsNullOrEmpty(modeName))
                {
                    Debug.LogError("Attempting to set firearm mode by null or empty mode name.");
                    return;
                }

                // Get the new index
                for (int i = 0; i < m_Modes.Length; ++i)
                {
                    if (m_Modes[i].descriptiveName == modeName)
                    {
                        m_Index = i;

                        // Apply
                        ApplyModeSwitchInternal(true);

                        return;
                    }
                }

                // Not found
                Debug.LogError("Firearm mode not found with name: " + modeName);
            }
        }

        void ApplyModeSwitchInternal(bool fireEvent)
        {
            // Enable components
            var components = m_Modes[m_Index].components;
            for (int i = 0; i < components.Length; ++i)
            {
                if (components[i] != null)
                {
                    var module = components[i] as IFirearmModule;
                    if (module != null)
                        module.Enable();
                    else
                        components[i].enabled = true;
                }
            }

            // Set the animator's index parameter
            if (m_ModeIndexHash != -1)
                firearm.animator.SetInteger(m_ModeIndexHash, m_Index);

            // Fire event
            if (fireEvent)
            {
                m_OnSwitchModes.Invoke();

                // Set animator trigger
                if (m_OnSwitchTriggerHash != -1)
                    firearm.animator.SetTrigger(m_OnSwitchTriggerHash);
            }
        }

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValue(k_ModeIndexKey, m_Index);
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            if (reader.TryReadValue(k_ModeIndexKey, out m_Index, m_Index))
            {
                if (m_Index != -1)
                {
                    var components = m_Modes[m_Index].components;
                    for (int i = 0; i < components.Length; ++i)
                    {
                        if (components[i] != null)
                            components[i].enabled = true;
                    }

                    // Fire event
                    m_OnSwitchModes.Invoke();

                    m_Loaded = true;
                }
            }
        }
    }
}