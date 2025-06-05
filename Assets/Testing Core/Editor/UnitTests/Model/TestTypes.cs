using Core.Events;
using Core.Model;
using Core.Model.ModelSystems;
using UnityEngine;

namespace Testing.Core.Editor.UnitTests.Model
{
    // Test entity types
    public sealed class TestEntity : Entity { }
    public sealed class TestEntityWithNormalComponents : Entity, ITestComponent, IAnotherComponent { }
    
    public sealed class TestEntityWithDuplicatedComponents : Entity,
                                                             IDoubleIncludeComponent,
                                                             ITripleIncludeComponent, 
                                                             ITestComponent, 
                                                             IAnotherComponent { }
    
    
    public interface ITestComponent : Component<TestComponentData> { } 
    public interface IAnotherComponent : Component<AnotherComponentData> { }

    public interface IDoubleIncludeComponent : Component<DoubleIncludeComponentData>,
                                               ITestComponent,
                                               IAnotherComponent
    { }
    
    public interface ITripleIncludeComponent : IDoubleIncludeComponent, 
                                               Component<TripleIncludeComponentData>, 
                                               IAnotherComponent 
    { }

    public struct TripleIncludeComponentData : IComponentData
    {
        public EntId ID { get; set; }
        public void Init() { }
    }

    public struct DoubleIncludeComponentData : IComponentData
    {
        public int Value;
        public EntId ID { get; set; }
        public void Init() { }
    }
    
    public struct TestComponentData : IComponentData
    {
        public int Value;
        public EntId ID { get; set; }
        public void Init() { }
    }
    
    public struct AnotherComponentData : IComponentData
    {
        public float Value;
        public EntId ID { get; set; }
        public void Init() { }
    }
    
    // Test event types
    public class TestEvent : Event<TestEvent>
    {
        public override void Execute() { }
    }
    public class TestEarlyEvent : Event<TestEarlyEvent>, IEarlyEvent
    {
        public override void Execute() {  }
    }
    public class TestLateEvent : Event<TestLateEvent>, ILateEvent
    {
        public override void Execute() {  }
    }
    
    
    public class TestEntityEvent : EntityEvent<TestEntityEvent> { }
    public class TestEntity2Event : EntityEvent<TestEntity2Event> { }

} 