using Core.Initialization;
using Core.View.UI;
using UnityEngine;

namespace Testing_Core.UI_Test
{
	public class TestUI_Bootstrap : SceneInstaller
	{
		[SerializeField] private GameObject screenA_UI;
		[SerializeField] private UIScreenDefinition screenADefinition;
		[SerializeField] private TestUIController testUIController;
		
		
		
		protected override void Instantiate()
		{
			UIView<ScreenAUIMessenger> uiView = screenA_UI.GetComponent<UIView<ScreenAUIMessenger>>();
			ScreenAUIMessenger screenAMessenger = new ScreenAUIMessenger();

			BindInstance(screenAMessenger);
			RegisterUIScreenDefinition(screenADefinition, uiView, screenAMessenger);
			
			BindInstance(testUIController);
		}
	}
}