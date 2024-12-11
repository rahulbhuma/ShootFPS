using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using NeoSaveGames.Serialization;
using System.Collections.Generic;

namespace NeoSaveGames
{
    [HelpURL("https://docs.neofps.com/manual/savegamesref-mb-additivescenesaveinfo.html")]
    public class AdditiveSceneSaveInfo : NeoSerializedScene
    {
        [SerializeField, Tooltip("")]
        private string m_DisplayName = "Unnamed Scene";
        
        public string displayName
        {
            get { return m_DisplayName; }
        }

        public override bool isMainScene
        {
            get { return false; }
        }
    }
}

