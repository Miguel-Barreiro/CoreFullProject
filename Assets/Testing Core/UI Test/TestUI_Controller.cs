using Core.Initialization;
using Core.View.UI;
using EasyButtons;
using UnityEngine;
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
			ScenesController.SwitchScene("testing_core");
		}

	}
}