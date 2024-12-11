using System.Collections;
using System.Collections.Generic;
using NeoSaveGames;
using NeoSaveGames.SceneManagement;
using NeoSaveGames.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeoFPS.SinglePlayer
{
    [HelpURL("https://docs.neofps.com/manual/neofpsref-mb-fpssologameminimal.html")]
    public class FpsSoloGameMinimal : FpsSoloGameBase
    {
        [Header("Character")]

        [SerializeField, NeoPrefabField(required = true), Tooltip("The player prefab to instantiate if none exists.")]
        private FpsSoloPlayerController m_PlayerPrefab = null;

        [SerializeField, NeoPrefabField(required = true), Tooltip("The character prefab to use.")]
        private FpsSoloCharacter m_CharacterPrefab = null;

        [Header ("Spawning")]

        [SerializeField, Tooltip("Should the game mode automatically spawn a player character immediately on start.")]
        private bool m_SpawnOnStart = true;

        [SerializeField, Tooltip("What to do when the player character dies.")]
        private DeathAction m_DeathAction = DeathAction.Respawn;

        [Header("Loadout")]

        [SerializeField, Tooltip("An optional inventory loadout for the character on spawn (this will replace their starting items).")]
        private FpsInventoryLoadout m_StartingLoadout = null;

        [SerializeField, Tooltip("How should the loadout be applied to the character. Should it replace the character's starting items or be added alongside them.")]
        private bool m_ReplaceStartingItems = true;

        private IInventoryItem[] m_OldItems = null;

        public override bool spawnOnStart
        {
            get { return m_SpawnOnStart; }
            set { m_SpawnOnStart = value; }
        }

        protected override IController GetPlayerControllerPrototype()
        {
            return m_PlayerPrefab;
        }

        protected override ICharacter GetPlayerCharacterPrototype(IController player)
        {
            return m_CharacterPrefab;
        }

        protected override void OnCharacterSpawned(ICharacter character)
        {
            // Apply old items (if found)
            if (m_OldItems != null)
            {
                // Apply loadout from old items
                character.GetComponent<IInventory>()?.ApplyLoadout(m_OldItems, false, true);
            }
            else
            {
                // Apply inventory loadout
                if (m_StartingLoadout != null)
                    character.GetComponent<IInventory>()?.ApplyLoadout(m_StartingLoadout, m_ReplaceStartingItems);
            }
        }

        protected override void DelayedDeathAction()
        {
            switch (m_DeathAction)
            {
                // Respawn
                case DeathAction.Respawn:
                    Respawn(player);
                    break;

                // Reload scene
                case DeathAction.ReloadScene:
                    SceneManager.LoadScene(gameObject.scene.name);
                    break;

                // Return to main menu
                case DeathAction.MainMenu:
                    SceneManager.LoadScene(NeoSceneManager.defaultMainMenuSceneIndex);
                    break;

                // Continue from last save
                case DeathAction.ContinueFromSave:
                    if (SaveGameManager.canContinue)
                        SaveGameManager.Continue();
                    else
                        SceneManager.LoadScene(NeoSceneManager.defaultMainMenuSceneIndex);
                    break;

                // Respawn with inventory loadout from death
                case DeathAction.RespawnWithItems:
                    RespawnWithItems(player);
                    break;
            }
        }

        public void RespawnWithItems(IController player)
        {
            var oldCharacter = player.currentCharacter;
            if (oldCharacter != null && oldCharacter.inventory != null)
            {
                var inventory = oldCharacter.GetComponent<IInventory>();
                var quickSlots = inventory as IQuickSlots;
                if (inventory != null)
                {
                    // Store the old items
                    m_OldItems = inventory.GetItems((IInventoryItem item) => { return item != quickSlots.backupSlot as IInventoryItem; });

                    // Remove from old inventory
                    for (int i = 0; i < m_OldItems.Length; ++i)
                        inventory.RemoveItem(m_OldItems[i]);
                }
                else m_OldItems = null;
            }

            Respawn(player);
        }
    }
}