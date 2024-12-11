using UnityEngine;
using UnityEngine.Events;
using NeoSaveGames.Serialization;
using NeoSaveGames;

namespace NeoFPS.ModularFirearms
{
    public abstract class BaseFirearmModuleBehaviour : MonoBehaviour, IFirearmModule
    {
        [SerializeField, Tooltip("Should this module register as the active aimer immediately on start.")]
        private bool m_StartActive = true;

        [SerializeField, Tooltip("How does module activation work. Options are: " +
            "*Component* means the component will be enabled / disabled - use this when you have multiple modules on an object such as the firearm root. " +
            "*GameObject* activates / deactivates the object - use this for attachments where you want the geo to appear/disappear.")]
        private FirearmModuleActivationMode m_ActivationMode = FirearmModuleActivationMode.Component;

        protected FirearmModuleActivationMode activationMode
        {
            get { return m_ActivationMode; }
        }

        public IModularFirearm firearm
        {
            get;
            protected set;
        }

        protected virtual void Awake()
        {
            firearm = GetComponentInParent<IModularFirearm>();
            if (m_StartActive)
                Enable();
            else
                Disable();
        }

        public virtual void Enable()
        {
            enabled = true;
            gameObject.SetActive(true);
        }

        public virtual void Disable()
        {
            // Deactivate
            switch (m_ActivationMode)
            {
                case FirearmModuleActivationMode.Component:
                    enabled = false;
                    break;
                case FirearmModuleActivationMode.GameObject:
                    gameObject.SetActive(false);
                    break;
            }
        }
    }
}
