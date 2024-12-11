using UnityEngine;

namespace NeoFPS
{
	public interface IDamageSource
	{
		DamageFilter outDamageFilter 
		{
			get;
			set;
		}

		IController controller
		{
			get;
		}

		Transform damageSourceTransform
		{
			get;
		}

		string description
		{
			get;
		}
	}

	public static class IDamageSourceExtensions
	{
		public static ICharacter GetSourceCharacter(this IDamageSource source)
		{
			return source.controller?.currentCharacter;
		}

		public static Transform GetOriginalSourceTransform(this IDamageSource source)
		{
			Transform characterTransform = source.controller?.currentCharacter?.transform;
			if (characterTransform != null)
				return characterTransform;
			else
				return source.damageSourceTransform;
		}
	}
}