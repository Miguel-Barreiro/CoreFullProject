using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Testing_Core.Components;
using UnityEngine;
using Zenject;

namespace Testing_Core.ComponentSystems
{
    public class AliveComponentSystem :  ISystem, 
                                        OnDestroyComponent<AliveComponentData>,
                                        OnCreateComponent<AliveComponentData>, 
                                        UpdateComponents<AliveComponentData>
    {

        [Inject] private readonly EntitiesContainer EntitiesContainer = null!;
        [Inject] private readonly BasicCompContainer<AliveComponentData> container = null!;
        
        public void OnDestroyComponent(EntId destroyedComponentId)
        {
            Debug.Log($" ALIVE component destroyed: {destroyedComponentId}");
        }
        
        public void OnCreateComponent(EntId newComponentId)
        {
            Debug.Log($" new ALIVE component: {newComponentId}");
        }
        
        public void UpdateComponents(float deltaTime)
        {
            uint topEmptyIndex = container.TopEmptyIndex;
            for (int i = 0; i < topEmptyIndex; i++)
            {
                ref AliveComponentData aliveComponentData = ref container.Components[i];
                if (aliveComponentData.Health <= 0)
                    EntitiesContainer.GetEntity(aliveComponentData.ID)?.Destroy();
            }
        }

        public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = CoreSystemGroups.CoreSystemGroup;
        
	}
}