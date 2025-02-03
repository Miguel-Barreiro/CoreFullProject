using Core.Initialization;
using Core.Zenject.Source.Main;
using Testing_Core.ComponentSystems;
using Testing_Core.EntitySystems;
using Testing_Core.Model;
using UnityEngine;

namespace Testing_Core
{
    public class TestingCoreGameBootstrap : SceneBootstrap
    {
        [SerializeField]
        private GameConfig GameConfig;

        private TestGameBootstrap _testGameBootstrap;

        public override SystemsInstallerBase GetLogicInstaller()
        {
            if (_testGameBootstrap == null)
            {
                _testGameBootstrap = new TestGameBootstrap(GameConfig, Container);
            }

            return _testGameBootstrap;
        }
    }

    public sealed class TestGameBootstrap : SystemsInstallerBase
    {
        private readonly GameConfig GameConfig;

        public TestGameBootstrap(GameConfig gameConfig, DiContainer container) : base(container)
        {
            GameConfig = gameConfig;
        }

        protected override void InstallSystems()
        {
            GameModel gameModel = new GameModel();
            BindInstance(gameModel);
            
            StartGameSystem startGameSystem = new StartGameSystem();
            BindInstance(startGameSystem);
            
            BindInstance(GameConfig);
            
            BindInstance(new AliveComponentSystem());
            BindInstance(new PrioritySystemTest());
            BindInstance(new BaseEntitySystemsTest());
        }
    }
}