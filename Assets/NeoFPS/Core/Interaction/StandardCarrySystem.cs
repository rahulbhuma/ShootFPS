using NeoSaveGames;
using NeoSaveGames.Serialization;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/interactionref-mb-standardcarrysystem.html")]
    public class StandardCarrySystem : RigidbodyCarrySystem
	{
        [Header("Carryables")]

        [SerializeField, Tooltip("With this enabled, you will only be able to pick up rigidbodies with a Carryable component attached. With it disabled you will be able to pick up any rigidbody.")]
        private bool m_AllowOnlyCarryables = true;

		private Carryable carryable = null;

		protected override bool CanCarryTarget(Rigidbody target)
		{
            if (!base.CanCarryTarget(target))
                return false;

            var c = target.GetComponent<Carryable>();
            if (m_AllowOnlyCarryables)
                return c != null && c.CanCarry();
            else
                return c == null || c.CanCarry();
        }

        protected override void OnObjectPickedUp()
        {
            // Get the carryable component
            carryable = carryTarget.GetComponent<Carryable>();

            base.OnObjectPickedUp();

            // Notify the carryable it's been picked up
            if (carryable != null)
                carryable.OnPickedUp(this);
        }

        protected override void OnObjectDropped()
        {
            base.OnObjectDropped();

            // Notify the carryable it's been dropped
            if (carryable != null)
                carryable.OnDropped(this);
            carryable = null;
        }

        protected override bool CanManipulate()
        {
            return base.CanManipulate() && (carryable == null || carryable.manipulatable);
        }

        protected override Vector3 GetOffset()
        {
            if (carryable != null)
                return carryable.centerOffset;
            else
                return base.GetOffset();
        }

        protected override Quaternion GetStartingOrientation(Quaternion current)
        {
            if (carryable != null)
                return carryable.GetStartingOrientation(current);
            else
                return Quaternion.identity;
        }

        /*
        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);

            if (didLoadFromSave && carryTarget != null)
            {
                carryable = carryTarget.GetComponent<Carryable>();
                if (carryable != null)
                    carryable.OnPickedUp(this);
            }
        }
        */
    }
}
