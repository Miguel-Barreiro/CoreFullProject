using Core.Initialization;
using Core.Model.ModelSystems;
using Core.Zenject.Source.Main;
using Testing_Core.Systems_Test.Events;

namespace Testing_Core
{
    public sealed class TestingCore_RuntimeProjectBootstrap  : RuntimeProjectBootstrap
    {
        private TestingCore_ProjectInstaller Installer = null;

        public override SystemsInstallerBase GetLogicInstaller()
        {
            if (Installer == null)
            {
                Installer = new TestingCore_ProjectInstaller(Container);
            }

            return Installer;
        }
    }

    public sealed class TestingCore_ProjectInstaller : SystemsInstallerBase
    {
        protected override void InstallSystems()
        {
            BindInstance(new TestingCore_EventReactSystem());
            BindInstance(new TestingCore_PostEventReactSystem());
        }

        public override void ResetComponentContainers(DataContainersController dataContainersController) {  }
        

        public TestingCore_ProjectInstaller(DiContainer container) : base(container) { }
    }

}


