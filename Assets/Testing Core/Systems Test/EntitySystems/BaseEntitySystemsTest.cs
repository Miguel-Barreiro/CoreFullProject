using Core.Model;
using Core.Systems;
using UnityEngine;

namespace Testing_Core.EntitySystems
{
	public class BaseEntitySystemsTest : EntitySystem<BaseEntity>
	{
		public override SystemGroup Group => CoreSystemGroups.CoreSystemGroup;

		public override void OnNew(BaseEntity newEntity)
		{
			Debug.Log($"OnNew: {newEntity.GetType()} with {newEntity.ID} ");
		}

		public override void OnDestroy(BaseEntity newEntity)
		{
			Debug.Log($"OnDestroy: {newEntity.GetType()} with {newEntity.ID} "); 
		}

		public override void Update(BaseEntity entity, float deltaTime)
		{
			
		}
	}
}