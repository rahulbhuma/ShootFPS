using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Events;

namespace NeoFPS.ModularFirearms
{
	public enum FirearmDelayType
	{
		None,
		ElapsedTime,
		ExternalTrigger
	}

    public interface IModularFirearm : IMonoBehaviour
    {
        ICharacter wielder { get; }
        Animator animator { get; }
        IWieldableAnimationHandler animationHandler { get; }

        ITrigger trigger { get; }
		IShooter shooter { get; }
		IAmmo ammo { get; }
		IReloader reloader { get; }
		IAimer aimer { get; }
		IEjector ejector { get; }
		IMuzzleEffect muzzleEffect { get; }
        IRecoilHandler recoilHandler { get; }
		string mode { get; }

		void SetTrigger (ITrigger to);
		void SetShooter (IShooter to);
		void SetAmmo (IAmmo to);
		void SetReloader (IReloader to);
		void SetAimer (IAimer to);
		void SetEjector (IEjector to);
		void SetMuzzleEffect (IMuzzleEffect to);
        void SetHandling(IRecoilHandler to);

        event UnityAction<ICharacter> onWielderChanged;
        event UnityAction<IModularFirearm, ITrigger> onTriggerChange;
		event UnityAction<IModularFirearm, IShooter> onShooterChange;
		event UnityAction<IModularFirearm, IAmmo> onAmmoChange;
		event UnityAction<IModularFirearm, IReloader> onReloaderChange;
		event UnityAction<IModularFirearm, IAimer> onAimerChange;
		event UnityAction<IModularFirearm, string> onModeChange;

		void SetRecoilMultiplier (float move, float rotation);
		void HideGeometry ();
		void ShowGeometry ();

		bool Reload ();
        void AddTriggerBlocker(UnityEngine.Object o);
        void RemoveTriggerBlocker(UnityEngine.Object o);

        void PlaySound(AudioClip clip, float volume = 1f);

        ToggleOrHold aimToggleHold { get; }
		
        FirearmDelayType raiseDelayType { get; }
		void ManualWeaponRaised ();

		IModularFirearmModeSwitcher modeSwitcher { get; set; }
		void SwitchMode ();

        // Implement optional (default off) delay to lower current weapon when switching
        // FirearmDelayType lowerDelayType { get; }
        // void ManualWeaponLowered ();
    }
}