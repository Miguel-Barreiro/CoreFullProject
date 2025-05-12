using Core.Model;
using Core.Model.ModelSystems;

namespace Testing_Core.Model.DataDrivenTests
{
	
	public sealed class EntityTestDD : Entity, TestComponentOwnerDD2, TestComponentOwnerDD1, IStatsComponent
	{ }
	
	
	public struct TestDD2ComponentData : IComponentData
	{
		public EntId ID { get; set; }
		public int Value2;
		public void Init() { Value2 = 0; }
	}
	public interface TestComponentOwnerDD2 : Component<TestDD2ComponentData> { }
	

	public struct TestDD1ComponentData : IComponentData
	{
		public EntId ID { get; set; }
		public int Value;
		public void Init() { Value = 0; }
	}
	public interface TestComponentOwnerDD1 : Component<TestDD1ComponentData> { }



}