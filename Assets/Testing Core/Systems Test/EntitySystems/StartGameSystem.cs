using Core.Systems;
using Testing_Core.Model;
using Zenject;

namespace Testing_Core.EntitySystems
{
    public class StartGameSystem : IStartSystem
    {
        [Inject] private readonly Testing_core_GameModel TestingCoreGameModel = null!;

        public void StartSystem()
        {
            TestingCoreGameModel.Start();
        }
    }
}