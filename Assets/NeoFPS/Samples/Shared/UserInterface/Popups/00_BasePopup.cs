using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using NeoFPS.Constants;

namespace NeoFPS.Samples
{
    [HelpURL("http://docs.neofps.com/manual/samples-ui.html")]
    public abstract class BasePopup : Focusable
	{
		public BaseMenu menu { get; private set; }
        
		public virtual bool cancellable
		{
			get { return true; }
		}

		public virtual bool clickOffToCancel
		{
			get { return true; }
		}

		public virtual void Initialise (BaseMenu menu)
		{
			this.menu = menu;
		}

		public virtual void Show ()
		{
			gameObject.SetActive (true);

            // Set escape / back button handler
            NeoFpsInputManagerBase.PushEscapeHandler(OnEscape);
        }

		public virtual void Hide ()
		{
			gameObject.SetActive (false);

            // Remove escape / back button handler
            NeoFpsInputManagerBase.PopEscapeHandler(OnEscape);
        }

		void OnEscape()
		{
			if (cancellable)
				Back();
		}

		public void ClickOff()
        {
			if (clickOffToCancel)
				Back();
        }

		public virtual void Back()
		{
			menu.ShowPopup(null);
		}
	}
}