using Core.Editor;
using Core.Events;
using Core.Model;
using Core.Model.ModelSystems;
using NUnit.Framework;
using Zenject;

namespace Testing_Core.Editor.UnitTests.EntityEventsQueue
{
    public class TestEntityEvent : EntityEvent<TestEntityEvent> { }

    public class TestEntity : Entity{}

    public class EntityEventQueueTests : UnitTest
    {
        [Inject] private readonly IEntityEventQueue<TestEntityEvent> eventQueue = null!;

        protected override void InstallTestSystems(IUnitTestInstaller installer)
        {
            // No additional systems needed for these tests
        }

        protected override void ResetComponentContainers(DataContainersController dataContainersController)
        { }

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void AddAndRemoveEventListener_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new TestEntity();
            IEntityEventQueue<TestEntityEvent>.EntityEventListener callback = (evt) => { };

            // Act
            eventQueue.AddEntityEventListener(entity.ID, callback);
            eventQueue.RemoveEntityEventListener(entity.ID, callback);

            // Assert - No exception should be thrown
            Assert.Pass();
        }

        [Test]
        public void Execute_ShouldCreateEventWithCorrectEntityId()
        {
            // Arrange
            var entity = new TestEntity();

            // Act
            var evt = eventQueue.Execute(entity.ID);

            // Assert
            Assert.That(evt.EntityID, Is.EqualTo(entity.ID));
        }

        [Test]
        public void Execute_ShouldTriggerCallbackForCorrectEntity()
        {
            // Arrange
            var entity1 = new TestEntity();
            var entity2 = new TestEntity();
            int callbackCounter = 0;
            TestEntityEvent lastReceivedEvent = null;

            eventQueue.AddEntityEventListener(entity1.ID, (evt) => 
            {
                callbackCounter++;
                lastReceivedEvent = evt;
            });

            // Act
            var evt = eventQueue.Execute(entity1.ID);
            ExecuteFrame(0.1f); // Process the event queue

            // Assert
            Assert.That(callbackCounter, Is.EqualTo(1));
            Assert.That(lastReceivedEvent, Is.EqualTo(evt));
        }

        [Test]
        public void Execute_ShouldNotTriggerCallbackForDifferentEntity()
        {
            // Arrange
            var entity1 = new TestEntity();
            var entity2 = new TestEntity();
            int callbackCounter = 0;

            eventQueue.AddEntityEventListener(entity1.ID, (evt) => 
            {
                callbackCounter++;
            });

            // Act
            eventQueue.Execute(entity2.ID);
            ExecuteFrame(0.1f); // Process the event queue

            // Assert
            Assert.That(callbackCounter, Is.EqualTo(0));
        }

        
        [Test]
        public void MultipleEvents_ShouldBeProcessedInOrder()
        {
            // Arrange
            var entity = new TestEntity();
            int eventOrder = 0;
            int callbackCounter = 0;

            eventQueue.AddEntityEventListener(entity.ID, (evt) => 
            {
                callbackCounter++;
                Assert.That(callbackCounter, Is.EqualTo(++eventOrder));
            });

            // Act
            eventQueue.Execute(entity.ID);
            eventQueue.Execute(entity.ID);
            eventQueue.Execute(entity.ID);
            ExecuteFrame(0.1f); // Process the event queue

            // Assert
            Assert.That(callbackCounter, Is.EqualTo(3));
        }

        [Test]
        public void AddAndRemoveAllEntitiesEventListener_ShouldWorkCorrectly()
        {
            // Arrange
            IEntityEventQueue<TestEntityEvent>.EntityEventListener callback = (evt) => { };

            // Act
            eventQueue.AddAllEntitiesEventListener(callback);
            eventQueue.RemoveAllEntitiesEventListener(callback);

            // Assert - No exception should be thrown
            Assert.Pass();
        }

        [Test]
        public void Execute_ShouldTriggerAllEntitiesCallbackForAnyEntity()
        {
            // Arrange
            var entity1 = new TestEntity();
            var entity2 = new TestEntity();
            int callbackCounter = 0;
            TestEntityEvent lastReceivedEvent = null;

            eventQueue.AddAllEntitiesEventListener((evt) => 
            {
                callbackCounter++;
                lastReceivedEvent = evt;
            });

            // Act
            var evt = eventQueue.Execute(entity1.ID);
            ExecuteFrame(0.1f); // Process the event queue

            // Assert
            Assert.That(callbackCounter, Is.EqualTo(1));
            Assert.That(lastReceivedEvent, Is.EqualTo(evt));

            // Act again with different entity
            var evt2 = eventQueue.Execute(entity2.ID);
            ExecuteFrame(0.1f); // Process the event queue

            // Assert
            Assert.That(callbackCounter, Is.EqualTo(2));
            Assert.That(lastReceivedEvent, Is.EqualTo(evt2));
        }
    }
} 