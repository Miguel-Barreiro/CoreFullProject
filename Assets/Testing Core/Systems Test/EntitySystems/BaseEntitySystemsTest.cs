using Core.Model;
using Core.Systems;
using UnityEngine;

namespace Testing_Core.EntitySystems
{
	public class BaseEntitySystemsTest : EntitySystem<Entity>
	{
		public override SystemGroup Group => CoreSystemGroups.CoreSystemGroup;

		public override void OnNew(Entity newEntity)
		{
			Debug.Log($"OnNew: {newEntity.GetType()} with {newEntity.ID} ");
		}

		public override void OnDestroy(Entity newEntity)
		{
			Debug.Log($"OnDestroy: {newEntity.GetType()} with {newEntity.ID} "); 
		}
		
	}
}