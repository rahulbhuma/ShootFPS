using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace NeoFPS.Samples
{
    [HelpURL("http://docs.neofps.com/manual/samples-ui.html")]
    public abstract class MenuNavControls : Focusable
	{
        [SerializeField] private UnityEvent m_OnBackButtonPressed = new UnityEvent();

		protected MultiInputWidgetList widgetList { get; private set; }

		public BaseMenu menu { get; private set; }

		public virtual void Initialise (BaseMenu menu)
		{
			this.menu = menu;
			widgetList = GetComponentInParent<MultiInputWidgetList> ();
		}

		public virtual void Show ()
		{
			gameObject.SetActive (true);
            NeoFpsInputManagerBase.PushEscapeHandler(Back);
		}

        protected void OnEnable ()
		{
			if (widgetList != null)
				widgetList.ResetWidgetNavigation ();
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