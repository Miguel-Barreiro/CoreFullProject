using Core.Editor;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using NUnit.Framework;
using System.Linq;
using Zenject;

namespace Testing_Core.Editor.UnitTests.EntityHierarchySystem
{
    public class TestEntity : Entity, IHierarchyEntity { }

    public class EntityHierarchySystemTests : UnitTest
    {
        [Inject] private readonly IEntityHierarchySystem hierarchySystem = null!;
        [Inject] private readonly EntitiesContainer entitiesContainer = null!;

        protected override void InstallTestSystems(IUnitTestInstaller installer)
        {
            // The EntityHierarchySystem is already installed by the core systems
            
        }

        protected override void ResetComponentContainers(DataContainersController dataContainersController)
        {
            dataContainersController.ResizeComponentsContainer<HierarchyData>(300);
            // Reset any component containers if needed
        }

        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up any existing entities before each test
            var allEntities = entitiesContainer.GetAllEntitiesByType<Entity>().ToList();
            foreach (var entity in allEntities)
                EntitiesContainer.DestroyEntity(entity.ID);

            ExecuteFrame(0.1f);
            EntitiesContainer.Reset();
        }

        
        [Test]
        public void AddChild_ShouldEstablishParentChildRelationship()
        {
            // Arrange
            var parent = new TestEntity();
            var child = new TestEntity();

            // Act
            hierarchySystem.AddChild(parent.ID, child.ID);

            // Assert
            Assert.That(hierarchySystem.GetParent(child.ID), Is.EqualTo(parent.ID));
            var children = hierarchySystem.GetChildrenList(parent.ID);
            Assert.That(children, Contains.Item(child.ID));
            Assert.That(children.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveChild_ShouldRemoveParentChildRelationship()
        {
            // Arrange
            var parent = new TestEntity();
            var child = new TestEntity();
            hierarchySystem.AddChild(parent.ID, child.ID);

            // Act
            hierarchySystem.RemoveChild(parent.ID, child.ID);

            // Assert
            Assert.That(hierarchySystem.GetParent(child.ID), Is.EqualTo(EntId.Invalid));
            var children = hierarchySystem.GetChildrenList(parent.ID);
            Assert.That(children, Is.Empty);
        }

        [Test]
        public void GetParent_ShouldReturnNullForEntityWithoutParent()
        {
            // Arrange
            var entity = new TestEntity();

            // Act
            var parent = hierarchySystem.GetParent(entity.ID);

            // Assert
            Assert.That(parent, Is.EqualTo(EntId.Invalid));
        }

        [Test]
        public void GetChildren_ShouldReturnEmptyForEntityWithoutChildren()
        {
            // Arrange
            var entity = new TestEntity();

            // Act
            var children = hierarchySystem.GetChildrenList(entity.ID);

            // Assert
            Assert.That(children, Is.Empty);
        }

        [Test]
        public void MultipleChildren_ShouldAllBeReturned()
        {
            // Arrange
            var parent = new TestEntity();
            var child1 = new TestEntity();
            var child2 = new TestEntity();
            var child3 = new TestEntity();

            // Act
            hierarchySystem.AddChild(parent.ID, child1.ID);
            hierarchySystem.AddChild(parent.ID, child2.ID);
            hierarchySystem.AddChild(parent.ID, child3.ID);

            // Assert
            var children = hierarchySystem.GetChildrenList(parent.ID);
            Assert.That(children.Count, Is.EqualTo(3));
            Assert.That(children, Contains.Item(child1.ID));
            Assert.That(children, Contains.Item(child2.ID));
            Assert.That(children, Contains.Item(child3.ID));
        }

        [Test]
        public void RemoveChild_ShouldOnlyRemoveSpecificChild()
        {
            // Arrange
            var parent = new TestEntity();
            var child1 = new TestEntity();
            var child2 = new TestEntity();
            var child3 = new TestEntity();

            hierarchySystem.AddChild(parent.ID, child1.ID);
            hierarchySystem.AddChild(parent.ID, child2.ID);
            hierarchySystem.AddChild(parent.ID, child3.ID);

            // Act
            hierarchySystem.RemoveChild(parent.ID, child2.ID);

            // Assert
            var children = hierarchySystem.GetChildrenList(parent.ID);
            Assert.That(children.Count, Is.EqualTo(2));
            Assert.That(children, Contains.Item(child1.ID));
            Assert.That(children, Contains.Item(child3.ID));
            Assert.That(children, Does.Not.Contains(child2.ID));
            Assert.That(hierarchySystem.GetParent(child2.ID), Is.EqualTo(EntId.Invalid));
        }

        [Test]
        public void OnDestroyEntity_ShouldDestroyAllChildren()
        {
            // Arrange
            var parent = new TestEntity();
            var child1 = new TestEntity();
            var child2 = new TestEntity();
            var grandchild = new TestEntity();

            EntId parentID = parent.ID;
            EntId child1ID = child1.ID;
            EntId child2ID = child2.ID;
            EntId grandchildID = grandchild.ID;
            
            hierarchySystem.AddChild(parentID, child1ID);
            hierarchySystem.AddChild(parentID, child2ID);
            hierarchySystem.AddChild(child1ID, grandchildID);

            // Act
            EntitiesContainer.DestroyEntity(parentID);
            ExecuteFrame(0.1f); // Process the destruction

            // Assert
            Assert.That(entitiesContainer.GetEntity(parentID), Is.Null);
            Assert.That(entitiesContainer.GetEntity(child1ID), Is.Null);
            Assert.That(entitiesContainer.GetEntity(child2ID), Is.Null);
            Assert.That(entitiesContainer.GetEntity(grandchildID), Is.Null);
        }

        [Test]
        public void OnDestroyEntity_ShouldCleanUpHierarchyData()
        {
            // Arrange
            var parent = new TestEntity();
            var child = new TestEntity();
            hierarchySystem.AddChild(parent.ID, child.ID);

            // Act
            EntitiesContainer.DestroyEntity(parent.ID);
            ExecuteFrame(0.1f); // Process the destruction

            // Assert
            Assert.That(hierarchySystem.GetParent(child.ID), Is.EqualTo(EntId.Invalid));
        }

        [Test]
        public void RemoveChild_ShouldHandleNonExistentRelationship()
        {
            // Arrange
            var parent = new TestEntity();
            var child = new TestEntity();

            // Act & Assert - Should not throw exception
            Assert.DoesNotThrow(() => hierarchySystem.RemoveChild(parent.ID, child.ID));
        }

        [Test]
        public void AddChild_ShouldHandleDuplicateAddition()
        {
            // Arrange
            var parent = new TestEntity();
            var child = new TestEntity();

            // Act
            hierarchySystem.AddChild(parent.ID, child.ID);
            hierarchySystem.AddChild(parent.ID, child.ID); // Add again

            // Assert
            Assert.That(hierarchySystem.GetParent(child.ID), Is.EqualTo(parent.ID));
            
            
            var children = hierarchySystem.GetChildrenList(parent.ID);
            Assert.That(children.Count, Is.EqualTo(1)); // Should not duplicate
            Assert.That(children, Contains.Item(child.ID));
        }

        [Test]
        public void AddChild_WhenChildAlreadyHasParent_ReparentsToNewParent()
        {
            // Arrange
            var parent1 = new TestEntity();
            var parent2 = new TestEntity();
            var child = new TestEntity();

            hierarchySystem.AddChild(parent1.ID, child.ID);

            // Act
            hierarchySystem.AddChild(parent2.ID, child.ID);

            // Assert
            Assert.That(hierarchySystem.GetParent(child.ID), Is.EqualTo(parent2.ID));
            Assert.That(hierarchySystem.GetChildrenList(parent2.ID), Contains.Item(child.ID));
            Assert.That(hierarchySystem.GetChildrenList(parent1.ID), Does.Not.Contains(child.ID));
        }

        [Test]
        public void OnDestroyChildEntity_ShouldBeRemovedFromParentChildren()
        {
            // Arrange
            var parent = new TestEntity();
            var child1 = new TestEntity();
            var child2 = new TestEntity();
            hierarchySystem.AddChild(parent.ID, child1.ID);
            hierarchySystem.AddChild(parent.ID, child2.ID);

            // Act - destroy one child independently
            EntitiesContainer.DestroyEntity(child1.ID);
            ExecuteFrame(0.1f);

            // Assert
            var children = hierarchySystem.GetChildrenList(parent.ID).ToList();
            Assert.That(children, Does.Not.Contains(child1.ID));
            Assert.That(children, Contains.Item(child2.ID));
        }

        [Test]
        public void ComplexHierarchy_ShouldMaintainCorrectRelationships()
        {
            // Arrange
            var root = new TestEntity();
            var child1 = new TestEntity();
            var child2 = new TestEntity();
            var grandchild1 = new TestEntity();
            var grandchild2 = new TestEntity();

            // Act
            hierarchySystem.AddChild(root.ID, child1.ID);
            hierarchySystem.AddChild(root.ID, child2.ID);
            hierarchySystem.AddChild(child1.ID, grandchild1.ID);
            hierarchySystem.AddChild(child2.ID, grandchild2.ID);

            // Assert
            Assert.That(hierarchySystem.GetParent(child1.ID), Is.EqualTo(root.ID));
            Assert.That(hierarchySystem.GetParent(child2.ID), Is.EqualTo(root.ID));
            Assert.That(hierarchySystem.GetParent(grandchild1.ID), Is.EqualTo(child1.ID));
            Assert.That(hierarchySystem.GetParent(grandchild2.ID), Is.EqualTo(child2.ID));

            var rootChildren = hierarchySystem.GetChildrenList(root.ID).ToList();
            Assert.That(rootChildren.Count, Is.EqualTo(2));
            Assert.That(rootChildren, Contains.Item(child1.ID));
            Assert.That(rootChildren, Contains.Item(child2.ID));

            var child1Children = hierarchySystem.GetChildrenList(child1.ID).ToList();
            Assert.That(child1Children.Count, Is.EqualTo(1));
            Assert.That(child1Children, Contains.Item(grandchild1.ID));

            var child2Children = hierarchySystem.GetChildrenList(child2.ID).ToList();
            Assert.That(child2Children.Count, Is.EqualTo(1));
            Assert.That(child2Children, Contains.Item(grandchild2.ID));
        }
    }
} 