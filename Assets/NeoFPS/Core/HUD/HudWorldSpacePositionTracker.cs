using NeoFPS.ModularFirearms;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
	public class HudWorldSpacePositionTracker : PlayerCharacterHudBase
	{
		public Transform targetTransform { get; set; }

		private RectTransform m_LocalTransform = null;
		private RectTransform m_FullRect = null;
		private Camera m_Camera = null;
		private Transform m_CameraTransform = null;

		public enum TargetLocation
        {
			Character,
			Weapon
        }

        protected override void Start()
        {
            base.Start();

			m_LocalTransform = transform as RectTransform;
			m_FullRect = transform.parent as RectTransform;

			m_LocalTransform.anchorMin = Vector2.zero;
			m_LocalTransform.anchorMax = Vector2.zero;
		}

        public override void OnPlayerCharacterChanged(ICharacter character)
		{
			targetTransform = null;

			if (character != null)
			{
				var target = character.GetComponentInChildren<HudWorldSpaceTarget>();
				if (target != null)
				{
					targetTransform = target.transform;
					m_CameraTransform = character.fpCamera.cameraTransform;
					m_Camera = m_CameraTransform.GetComponent<Camera>();
				}
			}
			else
			{
				m_Camera = null;
				m_CameraTransform = null;
			}
		}

        protected void LateUpdate()
		{
			// Track objects
			if (m_Camera != null && targetTransform != null)
			{
				var cameraPosition = m_CameraTransform.position;

				var worldPosition = targetTransform.position;
				var viewportPosition = m_Camera.WorldToViewportPoint(worldPosition);

				// Check if should hide marker (z is distance in front of camera)
				bool hide = viewportPosition.z < 0f;
				hide |= viewportPosition.x < 0f || viewportPosition.x > 1f;
				hide |= viewportPosition.y < 0f || viewportPosition.y > 1f;

				if (hide && m_LocalTransform.gameObject.activeSelf)
					m_LocalTransform.gameObject.SetActive(false);
				if (!hide && !m_LocalTransform.gameObject.activeSelf)
					m_LocalTransform.gameObject.SetActive(true);

				// Set the position and remove the screen offset
				m_LocalTransform.anchoredPosition = new Vector2(viewportPosition.x * m_FullRect.rect.width, viewportPosition.y * m_FullRect.rect.height);
			}
			else
            {
				// Lerp to zero
            }
		}
    }

	/*
	public class HudWorldSpacePositionTracker : WorldSpaceHudMarkerBase
	{
		[SerializeField, Tooltip("The item marker for the target")]
		private HudWorldSpacePositionItem m_ItemMarker = null;

		public override void OnPlayerCharacterChanged(ICharacter character)
		{
			base.OnPlayerCharacterChanged(character);

			m_ItemMarker.targetTransform = null;
			m_ItemMarker.enabled = false;

			if (character != null)
            {
				var target = character.GetComponentInChildren<HudWorldSpaceTarget>();
				if (target != null)
				{
					m_ItemMarker.targetTransform = target.transform;
					m_ItemMarker.enabled = true;
				}
            }
		}
	}
	*/
}