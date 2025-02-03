using Core.Initialization;
using Core.Zenject.Source.Main;
using Testing_Core.Systems_Test.Events;

namespace Testing_Core
{
    public sealed class TestingCoreRuntimeProjectBootstrap  : RuntimeProjectBootstrap
    {
        private TestingCoreProjectInstaller Installer = null;

        public override SystemsInstallerBase GetLogicInstaller()
        {
            if (Installer == null)
            {
                Installer = new TestingCoreProjectInstaller(Container);
            }

            return Installer;
        }
    }

    public sealed class TestingCoreProjectInstaller : SystemsInstallerBase
    {
        protected override void InstallSystems()
        {
            BindInstance(new TestingCore_EventReactSystem());
        }

        public TestingCoreProjectInstaller(DiContainer container) : base(container) { }
    }

}


