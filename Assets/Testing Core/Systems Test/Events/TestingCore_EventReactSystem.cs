using Core.Events;
using Testing_Core.Model.Events;
using UnityEngine;

namespace Testing_Core.Systems_Test.Events
{
	public class TestingCore_EventReactSystem : IEventListener<StartGameEvent>, IEventListener<PostStartGameEvent>
	{
		public void OnEvent(StartGameEvent onEvent)
		{
			Debug.Log($"EventReactSystemTest: ON PRE StartGameEvent");

		}

		public void OnEvent(PostStartGameEvent onEvent)
		{
			Debug.Log($"EventReactSystemTest: ON PRE PostStartGameEvent");
		}
	}
	
	
	public class TestingCore_PostEventReactSystem : IPostEventListener<StartGameEvent>
	{
		public void OnPostEvent(StartGameEvent onEvent)
		{
			Debug.Log($"PostEventReactSystemTest: ON Post StartGameEvent");

		}

	}

}