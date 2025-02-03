using Core.Initialization;
using Core.View.UI;
using Core.Zenject.Source.Main;
using UnityEngine;

namespace Testing_Core.UI_Test
{
	public class TestUI_Bootstrap : SceneBootstrap
	{
		[SerializeField] private GameObject screenA_UI;
		[SerializeField] private UIScreenDefinition screenADefinition;
		[SerializeField] private TestUIController testUIController;

		private TestUISystems _testUISystems;

		private void Awake()
		{
			_testUISystems = new TestUISystems(screenA_UI, screenADefinition, testUIController, Container);
		}

		public override SystemsInstallerBase GetLogicInstaller()
		{
			return _testUISystems;
		}
	}


	public sealed class TestUISystems : SystemsInstallerBase
	{
		private readonly GameObject screenA_UI;
		private readonly UIScreenDefinition ScreenADefinition;
		private readonly TestUIController TestUIController;

		public TestUISystems(GameObject screenAUI, UIScreenDefinition screenADefinition, 
								TestUIController testUIController, DiContainer container) 
			: base(container)
		{
			screenA_UI = screenAUI;
			ScreenADefinition = screenADefinition;
			TestUIController = testUIController;
		}

		protected override void InstallSystems()
		{
			UIView<ScreenAUIMessenger> uiView = screenA_UI.GetComponent<UIView<ScreenAUIMessenger>>();
			ScreenAUIMessenger screenAMessenger = new ScreenAUIMessenger();

			BindInstance(screenAMessenger);
			RegisterUIScreenDefinition(ScreenADefinition, uiView, screenAMessenger);
			
			BindInstance(TestUIController);
		}
	}
}