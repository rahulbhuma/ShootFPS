using System;
using UnityEngine;
using UnityEngine.Events;
using NeoCC;
using NeoFPS.Constants;
using NeoFPS.CharacterMotion;

namespace NeoFPS.SinglePlayer
{
    [HelpURL("https://docs.neofps.com/manual/fpcharactersref-mb-fpssolocharacter.html")]
    public class FpsSoloCharacter : BaseCharacter
    {
        public static event UnityAction<FpsSoloCharacter> onLocalPlayerCharacterChange;

        private static FpsSoloCharacter m_LocalPlayerCharacter = null;
        public static FpsSoloCharacter localPlayerCharacter
        {
            get { return m_LocalPlayerCharacter; }
            set
            {
                m_LocalPlayerCharacter = value;
                if (onLocalPlayerCharacterChange != null)
                    onLocalPlayerCharacterChange(m_LocalPlayerCharacter);
            }
        }

        bool m_CheckController = true;

        protected override void OnControllerChanged()
        {
            base.OnControllerChanged();

            if (m_CheckController)
                CheckController();
        }

        void CheckController()
        {
            m_CheckController = false;

            // Check if controller is player
            if (controller != null)
            {
                gameObject.SetActive(true);

                if ((BaseCharacter)controller.currentCharacter != this)
                    controller.currentCharacter = this;

                if (controller.isLocalPlayer)
                {
                    localPlayerCharacter = this;
                    SetFirstPerson(true);
                }
                else
                {
                    if (localPlayerCharacter == this)
                        localPlayerCharacter = null;
                    SetFirstPerson(false);
                }

                // gameObject.SetActive(((MonoBehaviour)controller).isActiveAndEnabled);
                // Is this needed? Looks like it could cause circular issues if OnEnable is evaluated first on character
            }
            else
            {
                if (gameObject.activeInHierarchy)
                {
                    // Disable the object (needs a controller to function)
                    gameObject.SetActive(false);
                }
                else
                {
                    if (localPlayerCharacter == this)
                        localPlayerCharacter = null;
                    SetFirstPerson(false);
                }
            }

            m_CheckController = true;
        }

        protected void OnEnable()
        {
            // Need to check if start has called, because of Unity's insane execution order where
            // OnEnable can be called before Awake on different components on the same object 
            if (m_CheckController)
                CheckController();
        }

        protected void OnDisable()
        {
            SetFirstPerson(false);
            if (localPlayerCharacter == this)
                localPlayerCharacter = null;
        }
    }
}

