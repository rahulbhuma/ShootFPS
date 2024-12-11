using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    public interface ICarrySystem : IMonoBehaviour
	{
		event UnityAction<CarryState> onCarryStateChanged;

		CarryState carryState { get; }
		Rigidbody carryTarget { get; }
		float massLimit { get; }

		void ManipulateObject(Vector2 mouseDelta, Vector2 analogue);
		void PushObject(float scroll, int directionInput);
        void PickUpObject();
		void DropObject();
		void ThrowObject();
	}

	public enum CarryState
	{
		Inactive,
		ValidTarget,
		InvalidTarget,
		TargetTooHeavy,
		Carrying
	}
}
