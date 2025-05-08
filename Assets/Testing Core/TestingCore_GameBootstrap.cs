using Core.Initialization;
using Core.Model.ModelSystems;
using Core.Zenject.Source.Main;
using Testing_Core.ComponentSystems;
using Testing_Core.EntitySystems;
using Testing_Core.Model;
using Testing_Core.Systems_Test;
using UnityEngine;

namespace Testing_Core
{
    public class TestingCore_GameBootstrap : SceneBootstrap
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
            Testing_core_GameModel testingCoreGameModel = new Testing_core_GameModel();
            BindInstance(testingCoreGameModel);
            
            StartGameSystem startGameSystem = new StartGameSystem();
            BindInstance(startGameSystem);
            
            BindInstance(new Testing_core_EnemyLogic());
            
            BindInstance(GameConfig);
            
            BindInstance(new AliveComponentSystem());
            BindInstance(new PrioritySystemTest());

            
            BindInstance(new Testing_core_TestingISystem());
            
            BindInstance(new DD_Testing_UpdateComponentDatasSystem());
            BindInstance(new DD_Testing_ComponentDatasLifeCycleSystem());
        }

        public override void ResetComponentContainers(DataContainersController dataContainersController)
        {
            
        }

    }
}