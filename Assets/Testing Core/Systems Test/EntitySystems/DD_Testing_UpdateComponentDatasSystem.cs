using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Testing_Core.Model.DataDrivenTests;
using UnityEngine;
using Zenject;

namespace Testing_Core.EntitySystems
{
	public class DD_Testing_UpdateComponentDatasSystem : IStartSystem, UpdateComponents<TestDD1ComponentData>
	{

		// [Inject] private readonly EntitiesContainer EntitiesContainer = null!;


		[Inject] private readonly CustomComponentContainerDD1 TestDD1ComponentContainer = null!;


		private int timesDebug = 10;


		public void StartSystem()
		{
			EntityTestDD entityTestDd1 = new EntityTestDD();
			EntityTestDD entityTestDd2 = new EntityTestDD();

			ref TestDD1ComponentData testDd1ComponentData = ref TestDD1ComponentContainer.GetComponent(entityTestDd1.ID);
			ref TestDD1ComponentData testDd1ComponentData2 = ref TestDD1ComponentContainer.GetComponent(entityTestDd2.ID);
			
			testDd1ComponentData.Value = 99;
			testDd1ComponentData2.Value = 999;
			
			Debug.Log($"DD_Testing_UpdateComponentDatasSystem: INIT components {entityTestDd1.ID} {entityTestDd2.ID}");

			timesDebug = 10;
		}
		
		public void UpdateComponents(float deltaTime)
		{

			// Debug.Log($"DD_Testing_UpdateComponentDatasSystem: Updating components");
			
			if(timesDebug < 0) 
				return;

			timesDebug--;

			var componentsArray = TestDD1ComponentContainer.Components[0];
			TestDD1ComponentData[] components = componentsArray.Items;
			uint topEmptyIndex = componentsArray.Count;
			for (uint i = 0; i < topEmptyIndex; i++)
			{
				ref TestDD1ComponentData component = ref components[i];

				if (timesDebug == 0)
					EntitiesContainer.DestroyEntity(component.ID);

				component.Value++;
				Debug.Log($"DD_Testing_UpdateComponentDatasSystem: UPDATE {component.ID} value: {component.Value}");

				
			}
		}
		
		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = CoreSystemGroups.CoreSystemGroup;
	}

	public sealed class DD_Testing_ComponentDatasLifeCycleSystem : OnCreateComponent<TestDD1ComponentData>,
																	OnDestroyComponent<TestDD1ComponentData>
	{
		[Inject] private readonly CustomComponentContainerDD1 ComponentContainer = null!;
		
		
		public void OnCreateComponent(EntId newComponentId)
		{
			ref TestDD1ComponentData testDd1ComponentData = ref ComponentContainer.GetComponent(newComponentId);
			Debug.Log($"created a new TestDD1ComponentData({newComponentId}) with value {testDd1ComponentData.Value}"); 
		}

		public void OnDestroyComponent(EntId destroyedComponentId)
		{
			ref TestDD1ComponentData testDd1ComponentData = ref ComponentContainer.GetComponent(destroyedComponentId);
			Debug.Log($"destroyed a new TestDD1ComponentData({destroyedComponentId}) with value {testDd1ComponentData.Value}");
		}

		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = CoreSystemGroups.CoreSystemGroup;
	}



}