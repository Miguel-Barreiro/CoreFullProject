using Core.View;
using Testing_Core.Model;
using UnityEngine;

namespace Testing_Core.ViewSystems
{
    public class BasicTestViewSystem : BaseEntitiesView<EnemyEntity>
    {
        protected override void OnSpawn(EnemyEntity entity, GameObject newGameObject)
        {
            base.OnSpawn(entity, newGameObject);
        }
    }
}