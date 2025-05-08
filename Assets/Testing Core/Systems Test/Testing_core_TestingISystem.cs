using Core.Systems;
using UnityEngine;

namespace Testing_Core.Systems_Test
{
	public sealed class Testing_core_TestingISystem: IUpdateSystem
	{
		public bool Active { get; set; } = true;
		public SystemGroup Group { get; } = CoreSystemGroups.CoreSystemGroup;

		public void UpdateSystem(float deltaTime)
		{
			Debug.Log($"Testing_core_TestingISystem"); 
		}
	}
}