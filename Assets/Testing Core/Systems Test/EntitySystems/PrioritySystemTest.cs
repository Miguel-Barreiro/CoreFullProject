using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Testing_Core.Components;
using UnityEngine;
using Zenject;

namespace Testing_Core.EntitySystems
{
	
	[OnCreateComponentProperties(Priority = SystemPriority.Late)]
	[UpdateComponentProperties(Priority = SystemPriority.Late)]
	public sealed class PrioritySystemTest : OnCreateComponent<AliveComponentData>,
											UpdateComponents<AliveComponentData>
	{
		[Inject] private readonly BasicCompContainer<AliveComponentData> ComponentContainer = null!;
		

		public void OnCreateComponent(EntId newComponentId)
		{
			Debug.Log($"on NEW IAlive entity: LATE"); 
		}

		public void UpdateComponents(float deltaTime)
		{
			var components = ComponentContainer.Components;
			uint topEmptyIndex = ComponentContainer.TopEmptyIndex;
			for (int i = 0; i < topEmptyIndex; i++)
			{
				ref AliveComponentData component = ref components[i];
				Debug.Log($"on update IAlive entity: LATE");
			}
		}
		
		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = CoreSystemGroups.CoreSystemGroup;

	}
}