using NeoFPS.Samples;
using NeoSaveGames;
using NeoSaveGames.Serialization;
using System;
using UnityEngine;

namespace NeoFPS.SinglePlayer
{
    public abstract class FpsSoloGameCustomisable : FpsSoloGameBase
    {
        [Header("Popup")]

        [SerializeField, NeoPrefabField, Tooltip("The pre-spawn popup prefab to use.")]
        private PreSpawnPopup m_Popup = null;

        public override bool spawnOnStart
        {
            get { return true; }
            set { }
        }

        public virtual bool showPrespawnPopup
        {
            get { return m_Popup != null; }
        }

        protected override bool PreSpawnStep()
        {
            if (showPrespawnPopup)
            {
                // Show the popup
                var popup = PrefabPopupContainer.ShowPrefabPopup(m_Popup);
                popup.Initialise(this, SpawnPlayerCharacter);

                return true;
            }
            else
                return false;
        }
    }
}
