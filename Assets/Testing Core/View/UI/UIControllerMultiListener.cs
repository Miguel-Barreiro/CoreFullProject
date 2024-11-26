using Core.View.UI;

namespace Testing_Core.View.UI
{
	public sealed class UIControllerMultiListener : UIController<UIMessengerA>, UIController<UIMessengerB>
	{
		public void Register(UIMessengerA uiMessenger) { throw new System.NotImplementedException(); }
		public void Register(UIMessengerB uiMessenger) { throw new System.NotImplementedException(); }
	}
}