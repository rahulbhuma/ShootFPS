using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    public interface IWieldable : IMonoBehaviour
    {
        ICharacter wielder { get; }
        void Select();
        void DeselectInstant();
        Waitable Deselect();

        // isBusy
        bool isBlocked { get; }
        void AddBlocker(Object o);
        void RemoveBlocker(Object o);

        event UnityAction<bool> onBlockedChanged;
        event UnityAction<ICharacter> onWielderChanged;

        IWieldableAnimationHandler animationHandler { get; }
    }
}