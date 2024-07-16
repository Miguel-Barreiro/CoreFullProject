using Core.Systems;
using Testing_Core.Model;
using Zenject;

namespace Testing_Core.EntitySystems
{
    public class StartGameSystem : IStartSystem
    {
        [Inject] private readonly GameModel gameModel = null!;

        public void StartSystem()
        {
            gameModel.Start();
        }
    }
}