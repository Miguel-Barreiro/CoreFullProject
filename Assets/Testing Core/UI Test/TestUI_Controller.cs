using Core.Initialization;
using Core.View.UI;
using UnityEngine;
using VInspector;
using Zenject;

namespace Testing_Core.UI_Test
{
	public class TestUI_Controller : MonoBehaviour
	{
		[SerializeField] private UIScreenDefinition uiScreenDefinition;

		[Inject] private readonly UIRoot UIRoot = null!;
		[Inject] private readonly ScenesController ScenesController = null!;
		
		private bool toggleUI = true;
		
		[Button]
		protected void ToggleUI()
		{
			toggleUI = !toggleUI;
			Debug.Log($"TOGGLE UI is now {toggleUI}");

			if (toggleUI)
			{
				UIRoot.Show(uiScreenDefinition);
			} else
			{
				UIRoot.Hide(uiScreenDefinition);
			}

		}

		[Button]
		protected void GoToOtherScene()
		{
			Debug.Log($"switching to testing_core"); 

			ScenesController.SwitchScene("testing_core");
		}

	}
}