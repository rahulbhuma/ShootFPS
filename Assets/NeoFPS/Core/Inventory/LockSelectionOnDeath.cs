using NeoSaveGames.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [RequireComponent(typeof(IHealthManager))]
    [RequireComponent(typeof(FpsInventoryBase))]
	[HelpURL("https://docs.neofps.com/manual/inventoryref-mb-lockselectionondeath.html")]
    public class LockSelectionOnDeath : MonoBehaviour
    {
        [Header("Drop Weapons")]

        [SerializeField, Tooltip("Should the selected weapon be dropped on death")]
        private WeaponDrop m_WeaponDrop = WeaponDrop.Selected;

        [Header("Drop Items")]

        [SerializeField, Tooltip("A multi-drop object that will be spawned containing the non-wieldable contents of the character's inventory")]
        private FpsInventoryMultiDrop m_ItemDrop = null;

        [SerializeField, Tooltip("Should wieldable items be ignored by the drop object? Since picking up some types of wieldables can cause you to replace the one you hold, you might not want to add them to the drops.")]
        private bool m_IgnoreWieldables = true;

        [SerializeField, FpsInventoryKey, Tooltip("Specific items to ignore.")]
        private int[] m_ItemsToIgnore = { };

        private FpsInventoryBase m_Inventory = null;

        private enum WeaponDrop
        {
            Selected,
            All,
            None
        }

        private void Awake()
        {
            var healthManager = GetComponent<IHealthManager>();
            if (healthManager != null)
                healthManager.onIsAliveChanged += OnIsAliveChanged;

            m_Inventory = GetComponent<FpsInventoryBase>();
        }

        private void OnIsAliveChanged(bool alive)
        {
            if (!alive)
            {
                // Drop weapons
                switch (m_WeaponDrop)
                {
                    case WeaponDrop.Selected:
                        m_Inventory.DropSelected();
                        break;
                    case WeaponDrop.All:
                        m_Inventory.DropAllWieldables();
                        break;
                }

                // Lock selection
                m_Inventory.LockSelectionToNothing(this, false);

                // Drop items
                if (m_ItemDrop != null)
                {
                    var buildIndex = gameObject.scene.buildIndex;
                    var scene = NeoSerializedScene.GetByBuildIndex(buildIndex);
                    if (scene != null)
                    {
                        var drop = scene.InstantiatePrefab(m_ItemDrop); // Position, velocity, etc
                        drop.Drop(m_Inventory, FilterItem, transform.position + Vector3.up, transform.forward, Vector3.up * 3f);
                    }
                    else
                    {
                        var drop = Instantiate(m_ItemDrop); // Position, velocity, etc
                        drop.Drop(m_Inventory, FilterItem, transform.position + Vector3.up, transform.forward, Vector3.up * 3f);
                    }

                    m_Inventory.ClearAllItems(null);
                }
            }
            else
                m_Inventory.UnlockSelection(this);

        }

        bool FilterItem(IInventoryItem item)
        {
            // Filter wieldables
            if (m_IgnoreWieldables && item is IQuickSlotItem)
                return false;

            // Filter ignored items
            for (int i = 0; i < m_ItemsToIgnore.Length; ++i)
            {
                if (item.itemIdentifier == m_ItemsToIgnore[i])
                    return false;
            }

            return true;
        }
    }
}