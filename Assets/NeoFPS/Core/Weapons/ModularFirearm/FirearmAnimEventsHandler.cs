using System;
using UnityEngine;

namespace NeoFPS.ModularFirearms
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-firearmanimeventshandler.html")]
	public class FirearmAnimEventsHandler : MonoBehaviour
	{
		public ModularFirearm firearm
        {
			get;
			private set;
        }

		protected void Awake ()
		{
			firearm = GetComponentInParent<ModularFirearm> ();
			if (firearm == null)
				Debug.LogError ("FirearmAnimEventsHandler requires a ModularFirearm component on this or a parent object.", gameObject);
		}

		public virtual void WeaponRaised ()
		{
			if (firearm != null)
				firearm.ManualWeaponRaised ();
        }
        public virtual void WeaponLowered()
        {
            if (firearm != null)
                firearm.ManualWeaponLowered();
        }
        public virtual void FirearmReloadPartial ()
		{
			if (firearm != null && firearm.reloader != null)
				firearm.reloader.ManualReloadPartial ();
		}
		public virtual void FirearmReloadComplete ()
		{
			if (firearm != null && firearm.reloader != null)
				firearm.reloader.ManualReloadComplete ();
		}
		public virtual void FirearmEjectShell ()
		{
			if (firearm != null && firearm.ejector != null)
				firearm.ejector.Eject ();
		}
	}
}

