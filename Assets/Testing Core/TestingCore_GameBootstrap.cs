using System;
using System.ComponentModel;
using Core.Initialization;
using Core.Model.ModelSystems;
using Core.Zenject.Source.Main;
using SRDebugger;
using Testing_Core.ComponentSystems;
using Testing_Core.EntitySystems;
using Testing_Core.Model;
using Testing_Core.Systems_Test;
using Testing_Core.VSEngineTests;
using UnityEngine;
using UnityEngine.Serialization;

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

#if DEBUG

        [Serializable]
        public class TestOptions
        {
            [Category("XXXXXXXXXX")]
            public float Number { get; set; } = 1;
            [Category("XXXXXXXXXX")]
            public string Text { get; set; } = "NONE";
            
            // Options will be grouped by category
            [Category("My Category")] 
            public float MyProperty {
                get { return Number; }
                set { Number = value; }
            }
	
            private float _myRangeProperty = 0f;
	
            // The NumberRange attribute will ensure that the value never leaves the range 0-10
            [NumberRange(0, 10)]
            [Category("My Category")]
            public float MyRangeProperty {
                get { return _myRangeProperty; }
                set { _myRangeProperty = value; }
            }
        }

        protected override void AddDebugOptions()
        {
            AddDebugOption(new TestOptions());
        }
#endif
        
        public override void SetupConfigurations() { }

        protected override void InstallSystems()
        {
            Testing_core_GameModel testingCoreGameModel = new Testing_core_GameModel();
            BindInstance(testingCoreGameModel);
            
            StartGameSystem startGameSystem = new StartGameSystem();
            BindInstance(startGameSystem);
            
            BindInstance(new Testing_core_EnemyLogic());
            BindInstance(new Testing_core_VSEngine());
            
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