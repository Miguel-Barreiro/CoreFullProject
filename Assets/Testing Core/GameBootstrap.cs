using Core.Initialization;
using Testing_Core.ComponentSystems;
using Testing_Core.EntitySystems;
using Testing_Core.Model;
using UnityEngine;

namespace Testing_Core
{
    public class GameBootstrap : SceneInstaller
    {
        [SerializeField]
        private GameConfig GameConfig;
        
        protected override void Instantiate()
        {
            GameModel gameModel = new GameModel();
            BindInstance(gameModel);
            
            StartGameSystem startGameSystem = new StartGameSystem();
            BindInstance(startGameSystem);
            
            BindInstance(GameConfig);
            
            BindInstance(new AliveComponentSystem());
        }
    }
}