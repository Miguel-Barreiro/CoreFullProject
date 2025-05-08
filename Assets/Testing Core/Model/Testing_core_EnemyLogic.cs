using Core.Model;
using Testing_Core.Components;
using UnityEngine;
using Zenject;

namespace Testing_Core.Model
{
	public sealed class Testing_core_EnemyLogic
	{
		[Inject] private readonly ComponentContainer<IKineticEntityData> kineticComponentContainer = null!;
		[Inject] private readonly ComponentContainer<AliveComponentData> AliveComponentContainer = null!;


		public EntId SpawnEnemy(int maxHealh, Vector2 position, GameObject enemyPrefab)
		{
			EnemyEntity enemyEntity = new EnemyEntity();

			ref IKineticEntityData kineticEntityData = ref kineticComponentContainer.GetComponent(enemyEntity.ID);
			kineticEntityData.Position = position;
			kineticEntityData.Prefab = enemyPrefab;

			ref AliveComponentData aliveComponentData = ref AliveComponentContainer.GetComponent(enemyEntity.ID);
			aliveComponentData.MaxHealth = maxHealh;
			aliveComponentData.Health = maxHealh;
			
			return enemyEntity.ID;
		}
	}
}