using Core.View.UI;
using UnityEngine;

namespace Testing_Core.View.UI
{
	[RequireComponent(typeof(RectTransform))]
	public class UIViewA : MonoBehaviour, UIView<UIMessengerA>
	{
		public void Register(UIMessengerA uiEvent)
		{
			
		}
	}
}