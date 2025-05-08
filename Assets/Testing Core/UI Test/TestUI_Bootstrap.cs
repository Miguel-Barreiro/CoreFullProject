using Core.Initialization;
using Core.Model.ModelSystems;
using Core.View.UI;
using Core.Zenject.Source.Main;
using UnityEngine;

namespace Testing_Core.UI_Test
{
	public class TestUI_Bootstrap : SceneBootstrap
	{
		[SerializeField] private GameObject screenA_UI;
		[SerializeField] private UIScreenDefinition screenADefinition;
		[SerializeField] private TestUI_Controller testUIController;

		private TestUISystemsInstaller _testUISystemsInstaller;
		
		public override SystemsInstallerBase GetLogicInstaller()
		{
			if(_testUISystemsInstaller == null)
				_testUISystemsInstaller = new TestUISystemsInstaller(screenA_UI, screenADefinition, testUIController, Container);
			return _testUISystemsInstaller;
		}
	}


	public sealed class TestUISystemsInstaller : SystemsInstallerBase
	{
		private readonly GameObject screenA_UI;
		private readonly UIScreenDefinition ScreenADefinition;
		private readonly TestUI_Controller TestUIController;

		public TestUISystemsInstaller(GameObject screenAUI, UIScreenDefinition screenADefinition, 
								TestUI_Controller testUIController, DiContainer container) 
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

		public override void ResetComponentContainers(DataContainersController dataContainersController) {  }

	}
}