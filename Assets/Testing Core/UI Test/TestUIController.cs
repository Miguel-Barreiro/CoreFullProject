using Core.View.UI;
using EasyButtons;
using UnityEngine;
using Zenject;

namespace Testing_Core.UI_Test
{
	public class TestUIController : MonoBehaviour
	{
		[SerializeField] private UIScreenDefinition uiScreenDefinition;

		[Inject] private readonly UIRoot UIRoot = null!;
		
		private bool toggleUI = false;
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

	}
}