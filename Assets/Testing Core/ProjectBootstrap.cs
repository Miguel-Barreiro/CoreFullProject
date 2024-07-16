using Core.Initialization;
using Testing_Core.Systems_Test.Events;

namespace Testing_Core
{
    public sealed class ProjectBootstrap  : ProjectInstaller
    {
        protected override void Instantiate()
        {
            BindInstance(new EventReactSystemTest());
        }
    }
}