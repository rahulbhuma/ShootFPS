using UnityEngine;
using System;
using UnityEngine.Events;
using NeoSaveGames.Serialization;
using NeoSaveGames;

namespace NeoFPS.ModularFirearms
{
	public abstract class BaseTriggerBehaviour : BaseFirearmModuleBehaviour, ITrigger, IFirearmModuleValidity, INeoSerializableComponent
    {
        private static readonly NeoSerializationKey k_BlockedKey = new NeoSerializationKey("blocked");

        private IWieldable m_FirearmWieldable = null;

        private bool m_Blocked = false;
		public bool blocked
		{
			get
            {
                if (m_FirearmWieldable == null)
                    return m_Blocked;
                else
                    return m_Blocked || m_FirearmWieldable.isBlocked; }
			set
            {
                m_Blocked = value;
                CheckForBlockedChange();
            }
		}

        virtual public bool cancelOnReload
        {
            get { return false; }
        }

        protected void FixedUpdate ()
		{
			if (!blocked)
				FixedTriggerUpdate ();
		}

        protected override void Awake()
        {
            base.Awake();
            m_FirearmWieldable = firearm as IWieldable;
        }

        protected virtual void OnEnable ()
        {
            firearm.SetTrigger(this);

            if (m_FirearmWieldable != null)
                m_FirearmWieldable.onBlockedChanged += OnFirearmBlockedChange;
        }

        protected virtual void OnDisable()
        {
            if (m_FirearmWieldable != null)
                m_FirearmWieldable.onBlockedChanged -= OnFirearmBlockedChange;
        }

        public override void Enable()
        {
            base.Enable();

            if (activationMode == FirearmModuleActivationMode.Ignore)
                firearm.SetTrigger(this);
        }

        protected virtual void OnSetBlocked (bool to)
		{
		}

        void CheckForBlockedChange()
        {
            if (m_FirearmWieldable == null)
                OnSetBlocked(m_Blocked);
            else
                OnSetBlocked(m_Blocked || m_FirearmWieldable.isBlocked);
        }

        void OnFirearmBlockedChange (bool firearmBlocked)
        {
            CheckForBlockedChange();
        }

		public abstract bool pressed { get; }
		public virtual void Press ()
        {
            if (onStateChanged != null)
                onStateChanged(true);
        }

		public virtual void Release ()
        {
            if (onStateChanged != null)
                onStateChanged(false);
        }

        public virtual void Cancel ()
        {
            if (pressed)
                Release();
        }

		protected abstract void FixedTriggerUpdate ();

		public event UnityAction onShoot;
        public event UnityAction<bool> onStateChanged;
        public event UnityAction<bool> onShootContinuousChanged;

        protected void Shoot ()
		{
			if (onShoot != null)
				onShoot ();
        }

		protected void StartShootContinuous()
        {
			if (onShootContinuousChanged != null)
				onShootContinuousChanged(true);
		}

		protected void StopShootContinuous()
		{
			if (onShootContinuousChanged != null)
				onShootContinuousChanged(false);
		}
        
        public virtual bool isModuleValid
        {
            get { return true; }
        }

        public virtual void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            if (saveMode == SaveMode.Default)
                writer.WriteValue(k_BlockedKey, m_Blocked);
        }

        public virtual void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            reader.TryReadValue(k_BlockedKey, out m_Blocked, m_Blocked);
        }
    }
}