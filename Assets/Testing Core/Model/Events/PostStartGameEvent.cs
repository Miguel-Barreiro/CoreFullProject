using Core.Events;
using UnityEngine;
using Zenject;

namespace Testing_Core.Model.Events
{
	public class PostStartGameEvent : Event<PostStartGameEvent>, ILateEvent
	{

		//WE ARE INJECTING THE GAME MODEL HERE to test if we are updating the object build correctly
		[Inject] private readonly Testing_core_GameModel TestingCoreGameModel = null!;

		public override void Execute()
		{
			// if(TestingCoreGameModel == null)
			// 	Debug.LogError("GameModel is null");
			// else
			// 	Debug.Log($"GameModel instance was injected well"); 

		}
	}
}