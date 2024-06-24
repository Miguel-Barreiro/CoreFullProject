using Core.Model;
using Testing_Core.Components;
using UnityEngine;

namespace Testing_Core.ComponentSystems
{
    public class AliveComponentSystem : ComponentSystem<IAlive>
    {
        public override void OnNewComponent(IAlive newComponent)
        {
            Debug.Log($" new ALIVE component: {newComponent.ID}");
        }

        public override void OnComponentDestroy(IAlive newComponent)
        {
            Debug.Log($" ALIVE component destroyed: {newComponent.ID}");
        }

        public override void UpdateComponent(IAlive component, float deltaTime)
        {
            if (!component.IsAlive)
            {
                component.Destroy();
            }
        }
    }
}