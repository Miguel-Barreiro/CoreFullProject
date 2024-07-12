using Core.Events;
using UnityEngine;
using Zenject;

namespace Testing_Core.Model.Events
{
	public sealed class StartGameEvent:  Event<StartGameEvent>
	{
		[Inject] private readonly EventQueue EventQueue = null!;
		
		private static int loopCheck = 0;
		
		public override void Execute()
		{
			Debug.Log($"start game execute {TestArgument} {TestArgument2} {loopCheck}");

			loopCheck++;
			
			if (TestArgument > 0 && TestArgument2 < 10 && loopCheck < 100)
			{
				StartGameEvent newEvent = EventQueue.Execute<StartGameEvent>();
			}

			TestArgument2++;
		}

		protected override void OnDespawned()
		{
			base.OnDespawned();
			TestArgument = 0;
		}

		public int TestArgument { get; set; }
		public int TestArgument2 { get; set; } = 0;

	}
}