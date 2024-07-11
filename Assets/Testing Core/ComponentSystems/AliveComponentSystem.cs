using Core.Model;
using Core.Model.ModelSystems;
using Testing_Core.Components;
using UnityEngine;

namespace Testing_Core.ComponentSystems
{
    public class AliveComponentSystem : ComponentSystem<IAlive>
    {
        public override void OnNew(IAlive newComponent)
        {
            Debug.Log($" new ALIVE component: {newComponent.ID}");
        }

        public override void OnDestroy(IAlive newComponent)
        {
            Debug.Log($" ALIVE component destroyed: {newComponent.ID}");
        }

        public override void Update(IAlive component, float deltaTime)
        {
            if (!component.IsAlive)
            {
                component.Destroy();
            }
        }
    }
}