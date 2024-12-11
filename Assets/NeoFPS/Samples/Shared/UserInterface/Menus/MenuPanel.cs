using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace NeoFPS.Samples
{
    [HelpURL("http://docs.neofps.com/manual/samples-ui.html")]
    public class MenuPanel : Focusable
	{
        [SerializeField] private UnityEvent m_OnBackButtonPressed = new UnityEvent();

		private MultiInputWidgetList m_List = null;

        public BaseMenu menu { get; private set; }

		protected event UnityAction onBackButtonPressed
        {
			add { m_OnBackButtonPressed.AddListener(value); }
			remove { m_OnBackButtonPressed.RemoveListener(value); }
		}

		public virtual void Initialise (BaseMenu menu)
		{
			this.menu = menu;
			m_List = GetComponentInParent<MultiInputWidgetList> ();
		}

		public virtual void Show ()
		{
			gameObject.SetActive (true);
            NeoFpsInputManagerBase.PushEscapeHandler(Back);
		}

        protected void OnEnable ()
		{
			if (m_List != null)
				m_List.ResetWidgetNavigation ();
		}

		public virtual void Hide ()
		{
			gameObject.SetActive (false);
            NeoFpsInputManagerBase.PopEscapeHandler(Back);
		}

		public virtual void Back ()
		{
			m_OnBackButtonPressed.Invoke ();
		}
	}
}