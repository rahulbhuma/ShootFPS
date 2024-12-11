using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS.SinglePlayer
{
    [HelpURL("https://docs.neofps.com/manual/savegamesref-mb-subsceneunloadtrigger.html")]
    public class SubSceneUnloadTrigger : SoloCharacterTriggerZone
    {
        [SerializeField, Tooltip("The index of the scene within the SubSceneCollection to unload.")]
        private int m_SubSceneIndex = -1;

        protected override void OnCharacterEntered(FpsSoloCharacter c)
        {
            base.OnCharacterEntered(c);

            if (m_SubSceneIndex != -1)
                SubSceneManager.UnloadScene(m_SubSceneIndex);
        }
    }
}