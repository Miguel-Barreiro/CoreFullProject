using Core.Model;
using Core.Systems;
using Core.Utils.CachedDataStructures;
using Core.VSEngine;
using Core.VSEngine.Events;
using Testing_Core.Model;
using Zenject;

namespace Testing_Core.VSEngineTests
{
	public sealed class Testing_core_VSEngine : IStartSystem
	{
		[Inject] private readonly GameConfig GameConfig = null!;
		[Inject] private readonly VSEngineCore VSBasicEngine = null!;

		
		public void StartSystem()
		{
			using CachedList<VSBaseEngine.Listener> listeners = ListCache<VSBaseEngine.Listener>.Get();

			
			VSBaseEngine.GetEventListenerNodes(GameConfig.EnemyBehaviorGraph, listeners);
			foreach (VSBaseEngine.Listener listener in listeners)
			{
				if (listener.EventType == typeof(TestingEvent))
				{
					TestingEvent vsEventBase = new TestingEvent();
					VSBasicEngine.RunEvent(listener.Node, vsEventBase, new EntId(1));
				}
			}
			
			
		}
	}
}