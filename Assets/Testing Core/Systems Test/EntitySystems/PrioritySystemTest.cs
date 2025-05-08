using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using UnityEngine;
using Zenject;

namespace Testing_Core.EntitySystems
{
	
	// [OnDestroyProperties(LifetimePriority = SystemPriority.Late, UpdatePriority = SystemPriority.Late)]
	[OnCreateComponentProperties(Priority = SystemPriority.Late)]
	[UpdateComponentProperties(Priority = SystemPriority.Late)]
	public sealed class PrioritySystemTest : ISystem, OnCreateComponent<PositionEntity>, UpdateComponents<PositionEntity>
	{
		// [Inject] private readonly ComponentContainer<PositionEntity> ComponentContainer = null!;
		

		public void OnCreateComponent(EntId newComponentId)
		{
			Debug.Log($"on NEW physics entity: LATE"); 
		}

		public void UpdateComponents(ComponentContainer<PositionEntity> componentsContainer, float deltaTime)
		{
			componentsContainer.ResetIterator();
			while (componentsContainer.MoveNext())
			{
				// PositionEntity positionEntity = ComponentContainer.GetCurrent();
				Debug.Log($"on update physics entity: LATE");
			}

		}
		
		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = CoreSystemGroups.CoreSystemGroup;

	}
}