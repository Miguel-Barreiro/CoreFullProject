using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using UnityEngine;

namespace Testing_Core.EntitySystems
{
	
	[EntitySystemProperties(LifetimePriority = SystemPriority.Late, UpdatePriority = SystemPriority.Late)]
	public sealed class PrioritySystemTest : ComponentSystem<I2DPhysicsEntity>
	{
		public override void OnNew(I2DPhysicsEntity newComponent)
		{
			Debug.Log($"on NEW physics entity: LATE");
		}
		public override void OnDestroy(I2DPhysicsEntity newComponent) {  }

		public override void Update(I2DPhysicsEntity component, float deltaTime)
		{
			Debug.Log($"on update physics entity: LATE");
		}

		public override SystemGroup Group { get; } = CoreSystemGroups.CoreSystemGroup;
	}
}