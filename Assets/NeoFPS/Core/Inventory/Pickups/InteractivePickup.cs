using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NeoSaveGames.Serialization;
using NeoSaveGames;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inventoryref-mb-interactivepickup.html")]
	[RequireComponent (typeof (AudioSource))]
	public class InteractivePickup : InteractiveObject
    {
		[SerializeField, Tooltip("The root object (destroyed when the item is picked up).")]
        private Transform m_Root = null;

        [SerializeField, Tooltip("The item prefab to add to the character inventory.")]
		private FpsInventoryItemBase m_Item = null;

        [SerializeField, Tooltip("Optional items that will only be picked up if the main item is. These do not affect whether the pickup is destroyed or deactivated once the main item is.")]
        private FpsInventoryItemBase[] m_AdditionalItems = { };

        [SerializeField, Tooltip("Should the pickup be destroyed / pooled if partially picked up.")]
        private bool m_ConsumeOnPartial = false;

        private AudioSource m_AudioSource = null;
        private NeoSerializedGameObject m_Nsgo = null;
        private bool m_PickUpAdditional = true;

        private static readonly NeoSerializationKey k_ItemKey = new NeoSerializationKey("item");
        private static readonly NeoSerializationKey k_AdditionalKey = new NeoSerializationKey("additional");

        public event UnityAction<IInventory, IInventoryItem> onPickedUp;

        public FpsInventoryItemBase item
        {
            get;
            private set;
        }

        public bool pickUpAdditional
        {
            get { return m_PickUpAdditional; }
            set { m_PickUpAdditional = value; }
        }
		
		public int itemIdentifier
		{
			get
			{
				if (m_Item != null)
					return m_Item.itemIdentifier;
				else
					return 0;
			}
		}

#if UNITY_EDITOR
        protected override void OnValidate()
		{
            base.OnValidate();
            if (m_Root == null)
            {
                Transform itr = transform;
                while (itr.parent != null)
                    itr = itr.parent;
                m_Root = itr;
            }
        }
		#endif

        protected override void Awake ()
		{
            base.Awake();

            m_Nsgo = GetComponent<NeoSerializedGameObject>();
            m_AudioSource = GetComponent<AudioSource>();
        }

        protected void OnEnable()
        {
            if (item == null)
            {
                // Instantiate from prefab if not in scene
                if (m_Item.gameObject.scene.rootCount == 0)
                {
                    if (m_Nsgo != null)
                        item = m_Nsgo.InstantiatePrefab(m_Item, Vector3.zero, Quaternion.identity);
                    else
                        item = Instantiate(m_Item, Vector3.zero, Quaternion.identity, transform);
                    item.gameObject.SetActive(false);
                    item.transform.localScale = Vector3.one;
                }
                else
                    item = m_Item;
            }
            else
                item.quantity = m_Item.quantity;
        }

        public override void Interact (ICharacter character)
		{
			base.Interact (character);

			IInventory inventory = character.inventory;
            if (inventory != null)
            {
                switch (inventory.AddItem(item))
                {
                    case InventoryAddResult.Full:
                        AddAdditionalItems(inventory);
                        OnPickedUp();
                        onPickedUp?.Invoke(inventory, item);
                        break;
                    case InventoryAddResult.AppendedFull:
                        AddAdditionalItems(inventory);
                        OnPickedUp();
                        onPickedUp?.Invoke(inventory, item);
                        break;
                    case InventoryAddResult.Partial:
                        AddAdditionalItems(inventory);
                        if (m_ConsumeOnPartial)
                            OnPickedUp();
                        else
                            OnPickedUpPartial();
                        onPickedUp?.Invoke(inventory, item);
                        break;
                }
            }                
		}

		protected virtual void OnPickedUp ()
        {
            // NB: The item will have been moved into the inventory heirarchy
			if (m_AudioSource != null && m_AudioSource.clip != null)
                NeoFpsAudioManager.PlayEffectAudioAtPosition(m_AudioSource.clip, transform.position);

            var pooled = m_Root.GetComponent<PooledObject>();
            if (pooled != null)
                pooled.ReturnToPool();
            else
                Destroy(m_Root.gameObject);
		}

        protected virtual void AddAdditionalItems(IInventory inventory)
        {
            if (m_PickUpAdditional)
            {
                for (int i = 0; i < m_AdditionalItems.Length; ++i)
                {
                    if (m_AdditionalItems[i] != null)
                        inventory.AddItemFromPrefab(m_AdditionalItems[i].gameObject);
                }
            }
        }

		protected virtual void OnPickedUpPartial ()
		{
			m_AudioSource.Play ();
            m_PickUpAdditional = false;
        }

        public override void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            base.WriteProperties(writer, nsgo, saveMode);
            writer.WriteComponentReference(k_ItemKey, item, nsgo);
            writer.WriteValue(k_AdditionalKey, m_PickUpAdditional);
        }

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);

            FpsInventoryItemBase result = null;
            if (reader.TryReadComponentReference(k_ItemKey, out result, nsgo))
                item = result;

            reader.TryReadValue(k_AdditionalKey, out m_PickUpAdditional, m_PickUpAdditional);
        }
    }
}