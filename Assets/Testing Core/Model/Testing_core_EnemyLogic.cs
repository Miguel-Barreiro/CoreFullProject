using Core.Model;
using Testing_Core.Components;
using UnityEngine;
using Zenject;

namespace Testing_Core.Model
{
	public sealed class Testing_core_EnemyLogic
	{
		[Inject] private readonly ComponentContainer<KineticComponentData> kineticComponentContainer = null!;
		[Inject] private readonly ComponentContainer<AliveComponentData> AliveComponentContainer = null!;


		public EntId SpawnEnemy(int maxHealh, Vector2 position, GameObject enemyPrefab)
		{
			EnemyComponent enemyComponent = new EnemyComponent();

			ref KineticComponentData kineticComponentData = ref kineticComponentContainer.GetComponent(enemyComponent.ID);
			kineticComponentData.Position = position;
			kineticComponentData.Prefab = enemyPrefab;

			ref AliveComponentData aliveComponentData = ref AliveComponentContainer.GetComponent(enemyComponent.ID);
			aliveComponentData.MaxHealth = maxHealh;
			aliveComponentData.Health = maxHealh;
			
			return enemyComponent.ID;
		}
	}
}