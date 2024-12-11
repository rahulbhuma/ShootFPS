using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace NeoFPS.Samples
{
    [HelpURL("http://docs.neofps.com/manual/samples-ui.html")]
    public class MultiInputWidgetList : Selectable
	{
		[SerializeField, Tooltip("Which direction should widget navigation work. For vertical up/down is previous/next. For horizontal left/right is previous/next")]
		private Layout m_Layout = Layout.Vertical;

		[SerializeField, Tooltip("The neighbouring widget list to the left")]
		private MultiInputWidgetList m_NavigateLeft = null;

		[SerializeField, Tooltip("The neighbouring widget list to the right")]
		private MultiInputWidgetList m_NavigateRight = null;

		[SerializeField, Tooltip("The neighbouring widget list above")]
		private MultiInputWidgetList m_NavigateUp = null;

		[SerializeField, Tooltip("The neighbouring widget list below")]
		private MultiInputWidgetList m_NavigateDown = null;

		private List<MultiInputWidget> m_NavigationList = new List<MultiInputWidget>();

		public Layout layout
        {
			get { return m_Layout; }
        }

		public enum Layout
        {
			Vertical,
			Horizontal
        }

        public override void OnSelect(BaseEventData eventData)
        {
			Invoke ("SelectFirst", 0f);
        }

        public bool SelectDirection (MoveDirection dir)
		{
			switch (dir)
            {
				case MoveDirection.Left:
					if (m_Layout == Layout.Vertical) 
					{
						if (m_NavigateLeft != null && m_NavigateLeft.isActiveAndEnabled)
							m_NavigateLeft.SelectFirst();
						return true;
					}
					break;
				case MoveDirection.Right:
					if (m_Layout == Layout.Vertical)
					{
						if (m_NavigateRight != null && m_NavigateRight.isActiveAndEnabled)
							m_NavigateRight.SelectFirst();
						return true;
					}
					break;
				case MoveDirection.Up:
					if (m_Layout == Layout.Horizontal)
					{
						if (m_NavigateUp != null && m_NavigateUp.isActiveAndEnabled)
							m_NavigateUp.SelectFirst();
						return true;
					}
					break;
				case MoveDirection.Down:
					if (m_Layout == Layout.Horizontal)
					{
						if (m_NavigateDown != null && m_NavigateDown.isActiveAndEnabled)
							m_NavigateDown.SelectFirst();
						return true;
					}
					break;
			}

			return false;
		}

		public void SelectFirst ()
		{
			MultiInputWidget first = GetComponentInChildren<MultiInputWidget> (false);
			if (first != null)
				EventSystem.current.SetSelectedGameObject (first.gameObject);
		}

		public void SelectFirstInteractable()
		{
			GetComponentsInChildren(false, m_NavigationList);
			for (int i = 0; i < m_NavigationList.Count; ++i)
            {
				if (m_NavigationList[i].interactable)
				{
					EventSystem.current.SetSelectedGameObject(m_NavigationList[i].gameObject);
					break;
				}
            }
			m_NavigationList.Clear();
		}

		public void ResetWidgetNavigation ()
		{
			GetComponentsInChildren (false, m_NavigationList);

			if (m_Layout == Layout.Vertical)
			{
				switch (m_NavigationList.Count)
				{
					case 0:
						break;
					case 1:
						{
							Navigation n = m_NavigationList[0].navigation;
							n.selectOnUp = m_NavigateUp;
							n.selectOnDown = m_NavigateDown;
							m_NavigationList[0].navigation = n;
						}
						break;
					case 2:
						{
							if (m_NavigateUp != null || m_NavigateDown != null)
							{
								// Set up first widget
								Navigation n = m_NavigationList[0].navigation;
								n.selectOnUp = m_NavigateUp;
								n.selectOnDown = m_NavigationList[1];
								m_NavigationList[0].navigation = n;

								// set up second widget
								n = m_NavigationList[0].navigation;
								n.selectOnUp = m_NavigationList[0];
								n.selectOnDown = m_NavigateDown;
								m_NavigationList[1].navigation = n;
							}
							else
							{
								// Set up first widget
								Navigation n = m_NavigationList[0].navigation;
								n.selectOnUp = m_NavigationList[1];
								n.selectOnDown = m_NavigationList[1];
								m_NavigationList[0].navigation = n;

								// set up second widget
								n = m_NavigationList[0].navigation;
								n.selectOnUp = m_NavigationList[0];
								n.selectOnDown = m_NavigationList[0];
								m_NavigationList[1].navigation = n;
							}
						}
						break;
					default:
						{
							if (m_NavigateUp != null || m_NavigateDown != null)
							{
								// Set up first widget
								Navigation n = m_NavigationList[0].navigation;
								n.selectOnUp = m_NavigateUp;
								n.selectOnDown = m_NavigationList[1];
								m_NavigationList[0].navigation = n;

								// Set up last widget
								n = m_NavigationList[m_NavigationList.Count - 1].navigation;
								n.selectOnUp = m_NavigationList[m_NavigationList.Count - 2];
								n.selectOnDown = m_NavigateDown;
								m_NavigationList[m_NavigationList.Count - 1].navigation = n;
							}
							else
							{
								// Set up first widget
								Navigation n = m_NavigationList[0].navigation;
								n.selectOnUp = m_NavigationList[m_NavigationList.Count - 1];
								n.selectOnDown = m_NavigationList[1];
								m_NavigationList[0].navigation = n;

								// Set up last widget
								n = m_NavigationList[m_NavigationList.Count - 1].navigation;
								n.selectOnUp = m_NavigationList[m_NavigationList.Count - 2];
								n.selectOnDown = m_NavigationList[0];
								m_NavigationList[m_NavigationList.Count - 1].navigation = n;
							}

							// Set up middle widgets
							for (int i = 1; i < m_NavigationList.Count - 1; ++i)
							{
								Navigation n = m_NavigationList[i].navigation;
								n.selectOnUp = m_NavigationList[i - 1];
								n.selectOnDown = m_NavigationList[i + 1];
								m_NavigationList[i].navigation = n;
							}
						}
						break;
				}
			}
			else
			{
				switch (m_NavigationList.Count)
				{
					case 0:
						break;
					case 1:
						{
							Navigation n = m_NavigationList[0].navigation;
							n.selectOnLeft = m_NavigateLeft;
							n.selectOnRight = m_NavigateRight;
							m_NavigationList[0].navigation = n;
						}
						break;
					case 2:
						{
							if (m_NavigateUp != null || m_NavigateDown != null)
							{
								// Set up first widget
								Navigation n = m_NavigationList[0].navigation;
								n.selectOnLeft = m_NavigateLeft;
								n.selectOnRight = m_NavigationList[1];
								m_NavigationList[0].navigation = n;

								// set up second widget
								n = m_NavigationList[0].navigation;
								n.selectOnLeft = m_NavigationList[0];
								n.selectOnRight = m_NavigateRight;
								m_NavigationList[1].navigation = n;
							}
							else
							{
								// Set up first widget
								Navigation n = m_NavigationList[0].navigation;
								n.selectOnLeft = m_NavigationList[1];
								n.selectOnRight = m_NavigationList[1];
								m_NavigationList[0].navigation = n;

								// set up second widget
								n = m_NavigationList[0].navigation;
								n.selectOnLeft = m_NavigationList[0];
								n.selectOnRight = m_NavigationList[0];
								m_NavigationList[1].navigation = n;
							}
						}
						break;
					default:
						{
							if (m_NavigateUp != null || m_NavigateDown != null)
							{
								// Set up first widget
								Navigation n = m_NavigationList[0].navigation;
								n.selectOnLeft = m_NavigateLeft;
								n.selectOnRight = m_NavigationList[1];
								m_NavigationList[0].navigation = n;

								// Set up last widget
								n = m_NavigationList[m_NavigationList.Count - 1].navigation;
								n.selectOnLeft = m_NavigationList[m_NavigationList.Count - 2];
								n.selectOnRight = m_NavigateRight;
								m_NavigationList[m_NavigationList.Count - 1].navigation = n;
							}
							else
							{
								// Set up first widget
								Navigation n = m_NavigationList[0].navigation;
								n.selectOnLeft = m_NavigationList[m_NavigationList.Count - 1];
								n.selectOnRight = m_NavigationList[1];
								m_NavigationList[0].navigation = n;

								// Set up last widget
								n = m_NavigationList[m_NavigationList.Count - 1].navigation;
								n.selectOnLeft = m_NavigationList[m_NavigationList.Count - 2];
								n.selectOnRight = m_NavigationList[0];
								m_NavigationList[m_NavigationList.Count - 1].navigation = n;
							}

							// Set up middle widgets
							for (int i = 1; i < m_NavigationList.Count - 1; ++i)
							{
								Navigation n = m_NavigationList[i].navigation;
								n.selectOnLeft = m_NavigationList[i - 1];
								n.selectOnRight = m_NavigationList[i + 1];
								m_NavigationList[i].navigation = n;
							}
						}
						break;
				}
			}

			m_NavigationList.Clear();
		}
	}
}

