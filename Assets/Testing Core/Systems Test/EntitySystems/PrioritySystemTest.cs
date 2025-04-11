using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using UnityEngine;

namespace Testing_Core.EntitySystems
{
	
	[EntitySystemProperties(LifetimePriority = SystemPriority.Late, UpdatePriority = SystemPriority.Late)]
	public sealed class PrioritySystemTest : ComponentSystem<IPositionEntity>
	{
		public override void OnNew(IPositionEntity newComponent)
		{
			// Debug.Log($"on NEW physics entity: LATE");
		}
		public override void OnDestroy(IPositionEntity newComponent) {  }

		public override void Update(IPositionEntity component, float deltaTime)
		{
			// Debug.Log($"on update physics entity: LATE");
		}

		public override SystemGroup Group { get; } = CoreSystemGroups.CoreSystemGroup;
	}
}