using Core.Events;
using Testing_Core.Model.Events;
using UnityEngine;

namespace Testing_Core.Systems_Test.Events
{
	public class EventReactSystemTest : IEventListener<StartGameEvent>, IEventListener<PostStartGameEvent>
	{
		public void OnEvent(StartGameEvent onEvent)
		{
			Debug.Log($"EventReactSystemTest: ON StartGameEvent");

		}

		public void OnEvent(PostStartGameEvent onEvent)
		{
			Debug.Log($"EventReactSystemTest: ON POST StartGameEvent");
		}
	}
}