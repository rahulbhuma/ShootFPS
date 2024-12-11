using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;
using System;
using NeoSaveGames.Serialization;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inventoryref-mb-fpsinventorymultidrop.html")]
    [RequireComponent(typeof(AudioSource))]
    public class FpsInventoryMultiDrop : MonoBehaviour
    {
        [SerializeField, NeoObjectInHierarchyField(true, required = true), Tooltip("The object rigidbody for the drop. This will be thrown away from the character that drops it.")]
        private Rigidbody m_RigidBody = null;

        private List<IInventoryItem> m_Items = new List<IInventoryItem>();
        private AudioSource m_AudioSource = null;
        private NeoSerializedGameObject m_Nsgo = null;
        private bool m_Dropped = false;

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (m_RigidBody == null)
                m_RigidBody = GetComponentInChildren<Rigidbody>();
        }
#endif

        protected void Awake()
        {
            m_Nsgo = GetComponent<NeoSerializedGameObject>();
            m_AudioSource = GetComponent<AudioSource>();
        }

        protected void Start()
        {
            var interactiveObject = GetComponentInChildren<InteractiveObject>();
            if (interactiveObject == null)
            {
                Debug.LogError("FpsInventoryMultiDrop must have an interactive object in its child hierarchy");
                Destroy(gameObject);
            }
            else
            {
                interactiveObject.onUsed += OnUsed;
            }

            // Rebuild item list - probably spawned from save
            if (!m_Dropped)
            {
                GetComponentsInChildren(true, m_Items);
                m_Dropped = true;
            }
        }

        private void OnUsed(ICharacter character)
        {
            IInventory inventory = character.inventory;
            if (inventory != null)
            {
                bool added = false;

                // Add each item
                for (int i = 0; i < m_Items.Count; ++i)
                    added |= (inventory.AddItem(m_Items[i]) != InventoryAddResult.Rejected);

                if (added)
                {
                    // Remove consumed items
                    for (int i = m_Items.Count - 1; i >= 0; --i)
                    {
                        if (m_Items[i].quantity == 0)
                        {
                            Destroy(m_Items[i].gameObject);
                            m_Items.RemoveAt(i);
                        }
                    }

                    // If none left, destroy the pickup
                    if (m_Items.Count == 0)
                    {
                        if (m_AudioSource != null && m_AudioSource.clip != null)
                            NeoFpsAudioManager.PlayEffectAudioAtPosition(m_AudioSource.clip, transform.position);
                        Destroy(gameObject);
                    }
                    else
                    {
                        m_AudioSource.Play();
                    }
                }
            }
        }

        public virtual void Drop(IInventory inventory, InventoryCallbacks.FilterItem filter, Vector3 position, Vector3 forward, Vector3 velocity)
        {
            m_RigidBody.position = position;
            m_RigidBody.rotation = Quaternion.LookRotation(forward, Vector3.up);
			#if UNITY_6000_0_OR_NEWER
            m_RigidBody.linearVelocity = velocity;
			#else
            m_RigidBody.velocity = velocity;
			#endif

            // Get drop items
            inventory.GetItems(m_Items, filter);

            // Remove items from inventory and add to drop object
            for (int i = m_Items.Count - 1; i >= 0; --i)
            {
                // Convert to concrete class
                var item = m_Items[i] as FpsInventoryItemBase;
                if (item == null)
                {
                    m_Items.RemoveAt(i);
                    continue;
                }

                // Remove from inventory
                inventory.RemoveItem(item);

                // Get item transform
                var itemTransform = item.transform;

                // Reparent
                NeoSerializedGameObject itemNSGO;
                if (m_Nsgo != null && item.TryGetComponent(out itemNSGO))
                    itemNSGO.SetParent(m_Nsgo);
                else
                    itemTransform.SetParent(transform);

                // Reset state
                item.gameObject.SetActive(false);
                itemTransform.localScale = Vector3.one;
                itemTransform.localPosition = Vector3.zero;
                itemTransform.localRotation = Quaternion.identity;
            }

            m_Dropped = true;
        }
    }
}