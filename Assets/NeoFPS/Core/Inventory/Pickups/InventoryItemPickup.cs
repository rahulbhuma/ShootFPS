using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoSaveGames.Serialization;
using NeoSaveGames;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inventoryref-mb-inventoryitempickup.html")]
	[RequireComponent (typeof (AudioSource))]
	public class InventoryItemPickup : Pickup, INeoSerializableComponent
    {
        [SerializeField, Tooltip("What to do to the pickup object once its item has been transferred to the character inventory.")]
        private PickupConsumeResult m_ConsumeResult = PickupConsumeResult.PoolOrDestroy;

		[SerializeField, Tooltip("The inventory item prefab to give to the character.")]
		private FpsInventoryItemBase m_ItemPrefab = null;

        [SerializeField, Tooltip("Optional items that will only be picked up if the main item is. These do not affect whether the pickup is destroyed or deactivated once the main item is.")]
        private FpsInventoryItemBase[] m_AdditionalItems = { };

        [SerializeField, Tooltip("Should the pickup be spawned immediately, or triggered externally.")]
		private bool m_SpawnOnAwake = true;

		[SerializeField, Tooltip("How long to wait before respawning if the consume result is set to \"Respawn\".")]
        private float m_RespawnDuration = 20f;

		[SerializeField, Tooltip("The display mesh of the pickup. This should not be the same game object as this, so that if this is disabled the pickup will still respawn if required.")]
		private GameObject m_DisplayMesh = null;

        private static readonly NeoSerializationKey k_RespawnKey = new NeoSerializationKey("respawn");
        private static readonly NeoSerializationKey k_AdditionalKey = new NeoSerializationKey("additional");

        public enum PickupConsumeResult
		{
			PoolOrDestroy,
			Disable,
			Respawn,
            DisableOnPartial,
            RespawnOnPartial,
            Infinite
		}

        private NeoSerializedGameObject m_Nsgo = null;
        private AudioSource m_AudioSource = null;
        private Collider m_Collider = null;
        private PooledObject m_PooledObject = null;
        private IEnumerator m_DelayedSpawnCoroutine = null;
        private float m_RespawnTimer = 0f;
        private bool m_PickUpAdditional = true;

        public FpsInventoryItemBase item
        {
            get;
            private set;
        }
		
		public int itemIdentifier
		{
			get
			{
				if (m_ItemPrefab != null)
					return m_ItemPrefab.itemIdentifier;
				else
					return 0;
			}
		}

        public bool pickUpAdditional
        {
            get { return m_PickUpAdditional; }
            set { m_PickUpAdditional = value; }
        }

#if UNITY_EDITOR
        protected void OnValidate ()
        {
            m_RespawnDuration = Mathf.Clamp(m_RespawnDuration, 0.5f, 300f);

            // Get the display mesh object
            if (m_DisplayMesh == null)
            {
                var mesh = GetComponentInChildren<MeshRenderer>(true);
                if (mesh != null && mesh.gameObject != gameObject)
                    m_DisplayMesh = mesh.gameObject;
            }
        }
#endif

        protected void Awake ()
		{
			m_Collider = GetComponent<Collider> ();
			m_AudioSource = GetComponent<AudioSource> ();
            m_Nsgo = GetComponent<NeoSerializedGameObject>();
            m_PooledObject = GetComponent<PooledObject>();
        }

        protected void OnEnable()
        {
            EnablePickup(false);
            if (m_SpawnOnAwake)
                StartCoroutine(DelayedStart());
        }

        IEnumerator DelayedStart()
        {
            yield return null;

            SpawnItem();
        }

        public override void Trigger (ICharacter character)
		{
			base.Trigger (character);

            IInventory inventory = character.inventory;
            if (inventory != null)
            {
                if (m_ConsumeResult >= PickupConsumeResult.DisableOnPartial)
                    PickUpFromPrefab(inventory);
                else
                    PickUpLocal(inventory);
            }
        }

        protected virtual void PickUpLocal(IInventory inventory)
        {
            if (item != null)
            {
                switch (inventory.AddItem(item))
                {
                    case InventoryAddResult.Full:
                        AddAdditionalItems(inventory);
                        OnPickedUp();
                        break;
                    case InventoryAddResult.AppendedFull:
                        AddAdditionalItems(inventory);
                        OnPickedUp();
                        break;
                    case InventoryAddResult.Partial:
                        AddAdditionalItems(inventory);
                        OnPickedUpPartial();
                        break;
                }
            }
        }

        protected virtual void PickUpFromPrefab(IInventory inventory)
        {
            if (inventory.AddItemFromPrefab(m_ItemPrefab.gameObject) != InventoryAddResult.Rejected)
            {
                AddAdditionalItems(inventory);
                OnPickedUp();
            }
        }

		protected virtual void OnPickedUp ()
		{
			if (m_DelayedSpawnCoroutine != null)
				StopCoroutine (m_DelayedSpawnCoroutine);
            // NB: The item will have been moved into the inventory heirarchy
            switch (m_ConsumeResult)
            {
                case PickupConsumeResult.PoolOrDestroy:
                    if (m_AudioSource.clip != null)
                        NeoFpsAudioManager.PlayEffectAudioAtPosition(m_AudioSource.clip, transform.position);
                    // Return to pool if it's a pooled object, destroy if not
                    if (m_PooledObject != null)
                        m_PooledObject.ReturnToPool();
                    else
                        Destroy(gameObject);
                    item = null;
                    break;
                case PickupConsumeResult.Disable:
                    m_AudioSource.Play();
                    EnablePickup(false);
                    item = null;
                    break;
                case PickupConsumeResult.Respawn:
                    m_AudioSource.Play();
                    EnablePickup(false);
                    item = null;
                    m_RespawnTimer = m_RespawnDuration;
                    m_DelayedSpawnCoroutine = DelayedSpawn();
                    StartCoroutine(m_DelayedSpawnCoroutine);
                    break;
                case PickupConsumeResult.DisableOnPartial:
                    m_AudioSource.Play();
                    EnablePickup(false);
                    break;
                case PickupConsumeResult.RespawnOnPartial:
                    m_AudioSource.Play();
                    EnablePickup(false);
                    m_RespawnTimer = m_RespawnDuration;
                    m_DelayedSpawnCoroutine = DelayedSpawn();
                    StartCoroutine(m_DelayedSpawnCoroutine);
                    break;
                case PickupConsumeResult.Infinite:
                    m_AudioSource.Play();
                    break;
            }
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

		public virtual void EnablePickup (bool value)
        {
			// Enable the mesh
			if (m_DisplayMesh != null)
				m_DisplayMesh.SetActive (value);

            // Enable the collider
            m_Collider.enabled = value;
        }

        public virtual bool SpawnItem ()
		{
			if (item == null)
			{
                // Instantiate the item if not using prefab directly
                if (m_ConsumeResult < PickupConsumeResult.DisableOnPartial)
                {
                    // Instantiate
                    if (m_Nsgo != null)
                        item = m_Nsgo.InstantiatePrefab(m_ItemPrefab, Vector3.zero, Quaternion.identity);
                    else
                        item = Instantiate(m_ItemPrefab, Vector3.zero, Quaternion.identity, transform);

                    // Disable object
                    item.gameObject.SetActive(false);
                }

				// Enable pickup
				EnablePickup (true);

                // Re-enable picking up additional
                m_PickUpAdditional = true;

                return true;
			}
			return false;
		}

		IEnumerator DelayedSpawn ()
        {
            m_PickUpAdditional = false;
            while (m_RespawnTimer > 0f)
            {
                yield return null;
                m_RespawnTimer -= Time.deltaTime;
            }
            SpawnItem ();
			m_DelayedSpawnCoroutine = null;
        }

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            // Write if respawning
            if (m_DelayedSpawnCoroutine != null)
                writer.WriteValue(k_RespawnKey, m_RespawnTimer);

            writer.WriteValue(k_AdditionalKey, m_PickUpAdditional);

            // Item quantity is handled by the item itself
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            // Respawn timer (start the coroutine if the property is found)
            float respawn = 0f;
            if (reader.TryReadValue(k_RespawnKey, out respawn, 0f))
            {
                m_RespawnTimer = respawn;
                m_DelayedSpawnCoroutine = DelayedSpawn();
                StartCoroutine(m_DelayedSpawnCoroutine);
            }

            reader.TryReadValue(k_AdditionalKey, out m_PickUpAdditional, m_PickUpAdditional);

            // Item quantity is handled by the item itself
        }
    }
}