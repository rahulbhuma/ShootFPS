using System;
using UnityEngine;

namespace NeoFPS
{
	public interface IDamageHandler : IMonoBehaviour
	{
		DamageFilter inDamageFilter 
		{
			get;
			set;
		}

		IHealthManager healthManager
        {
			get;
        }

		DamageResult AddDamage(float damage);
        DamageResult AddDamage(float damage, RaycastHit hit);
        DamageResult AddDamage(float damage, IDamageSource source);
        DamageResult AddDamage(float damage, RaycastHit hit, IDamageSource source);
	}

	public enum DamageResult
	{
		Standard,
		Critical,
		Ignored,
        Blocked
	}
}