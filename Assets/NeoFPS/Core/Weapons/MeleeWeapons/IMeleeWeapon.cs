using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
	public interface IMeleeWeapon : IMonoBehaviour
    {
        ICharacter wielder { get; }
        IWieldableAnimationHandler animationHandler { get; }

        bool blocking { get; }
        bool attacking { get; }

        void PrimaryPress();
        void PrimaryRelease();
        void SecondaryPress();
        void SecondaryRelease();

        event UnityAction<bool> onAttackingChange;
        event UnityAction<bool> onBlockStateChange;
    }
}
