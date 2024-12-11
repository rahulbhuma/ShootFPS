using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;

namespace NeoFPS
{
	public abstract class FpsInputBase : MonoBehaviour
	{
		private static List<FpsInputBase>[] s_ContextReferences;

		public static FpsInputContext currentContext
		{
			get
			{
				for (int i = FpsInputContext.count - 2; i >= 0; --i)
				{
					if (s_ContextReferences[i].Count > 0)
						return i + 1;
				}
				return FpsInputContext.None;
			}
		}

		protected abstract bool isInputActive { get; }

		private bool m_Pushed = false;
		private bool m_HadFocus = false;

		public virtual FpsInputContext inputContext
		{
			get { return FpsInputContext.None; }
		}

		public bool hasFocus
		{
			get { return inputContext == FpsInputContext.None || inputContext == currentContext; }
		}

		public bool gainedFocusThisFrame
        {
			get;
			private set;
        }

		protected void Awake()
		{
			if (s_ContextReferences == null)
			{
				s_ContextReferences = new List<FpsInputBase>[FpsInputContext.count - 1];
				for (int i = 0; i < FpsInputContext.count - 1; ++i)
					s_ContextReferences[i] = new List<FpsInputBase>(4);
			}
			OnAwake();
		}

		protected virtual void Start()
        {
			if (!isInputActive)
				Debug.LogError("Input handler active with wrong input system: " + name, gameObject);
		}

        protected virtual void OnAwake()
		{ }

		protected virtual void OnEnable()
		{
			PushContext();
		}

		protected virtual void OnDisable()
		{
			PopContext();
		}

		public void PushContext()
		{
			if (m_Pushed || inputContext == FpsInputContext.None)
				return;

			var list = s_ContextReferences[inputContext - 1];
			list.Add(this);
			m_Pushed = true;
		}

		public void PopContext()
		{
			if (!m_Pushed || inputContext == FpsInputContext.None)
				return;

			var list = s_ContextReferences[inputContext - 1];
			list.Remove(this);
			m_Pushed = false;
		}

		protected abstract void UpdateInput();

		protected virtual void OnGainFocus() { }
		protected virtual void OnLoseFocus() { }

		protected virtual void Update()
		{
			if (inputContext == FpsInputContext.None || (inputContext == currentContext && m_Pushed) && isInputActive)
			{
				if (!m_HadFocus)
				{
					m_HadFocus = true;
					OnGainFocus();
					gainedFocusThisFrame = true;
				}

				// CHECK THIS
				//if (!hasFocus || !isInputActive)
				//	return false;

				UpdateInput();
				gainedFocusThisFrame = false;
			}
			else
			{
				if (m_HadFocus)
				{
					m_HadFocus = false;
					OnLoseFocus();
				}
			}
		}
	}
}
