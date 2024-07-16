using Core.Events;

namespace Testing_Core.Model.Events
{
	public class PostStartGameEvent : Event<PostStartGameEvent>, ILateEvent
	{
		public override void Execute()
		{
			
		}
	}
}