using Core.Editor;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using NUnit.Framework;
using System.Linq;
using Core.Model.Data;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Testing_Core.Editor.UnitTests.AbilityTests
{
    public class OwnerEntity : Entity, IHierarchyEntity { }

    public class VSAbilityTests : UnitTest
    {
        [Inject] private readonly IEntityHierarchySystem hierarchySystem = null!;
        [Inject] private readonly EntitiesContainer entitiesContainer = null!;
        [Inject] private readonly BasicCompContainer<VSAbilityData> VSAbilityContainer = null!;

        [SerializeField] private VSAbilityDataConfig TEST_ABILITY_CONFIG;

        
        protected override void InstallTestSystems(IUnitTestInstaller installer)
        {
        }

        protected override void ResetComponentContainers(DataContainersController dataContainersController)
        {
            dataContainersController.ResizeComponentsContainer<HierarchyData>(300);
        }

        [TearDown]
        public void TearDown()
        {
            var allEntities = entitiesContainer.GetAllEntitiesByType<Entity>().ToList();
            foreach (var entity in allEntities)
                EntitiesContainer.DestroyEntity(entity.ID);

            ExecuteFrame(0.1f);
            EntitiesContainer.Reset();
        }

        [Test]
        public void Ability_ShouldBeRegisteredAsChildOfOwner()
        {
            var owner = new OwnerEntity();
            var ability = new VSAbility(owner.ID, TEST_ABILITY_CONFIG);

            var children = hierarchySystem.GetChildrenList(owner.ID);
            Assert.That(children, Contains.Item(ability.ID));
            Assert.That(children.Count, Is.EqualTo(1));
        }

        [Test]
        public void Ability_OwnerID_ShouldMatchParentInHierarchySystem()
        {
            var owner = new OwnerEntity();
            var ability = new VSAbility(owner.ID, TEST_ABILITY_CONFIG);

            EntId parentID = hierarchySystem.GetParent(ability.ID);
            Assert.That(parentID, Is.EqualTo(owner.ID));
            Assert.That(hierarchySystem.GetParent(ability.ID), Is.EqualTo(owner.ID));
        }

        [Test]
        public void Ability_WhenOwnerDestroyed_AbilityShouldBeDestroyed()
        {
            var owner = new OwnerEntity();
            var ability = new VSAbility(owner.ID, TEST_ABILITY_CONFIG);

            EntId ownerID = owner.ID;
            EntId abilityID = ability.ID;

            EntitiesContainer.DestroyEntity(ownerID);
            ExecuteFrame(0.1f);

            Assert.That(entitiesContainer.GetEntity(ownerID), Is.Null);
            Assert.That(entitiesContainer.GetEntity(abilityID), Is.Null);
        }

        [Test]
        public void Ability_WhenAbilityDestroyed_ShouldBeRemovedFromOwnerChildren()
        {
            var owner = new OwnerEntity();
            var ability = new VSAbility(owner.ID, TEST_ABILITY_CONFIG);

            EntId ownerID = owner.ID;
            EntId abilityID = ability.ID;

            EntitiesContainer.DestroyEntity(abilityID);
            ExecuteFrame(0.1f);

            var children = hierarchySystem.GetChildrenList(ownerID).ToList();
            Assert.That(children, Does.Not.Contains(abilityID));
            Assert.That(entitiesContainer.GetEntity(ownerID), Is.Not.Null);
        }

        [Test]
        public void MultipleAbilities_ShouldAllBeChildrenOfOwner()
        {
            var owner = new OwnerEntity();
            var ability1 = new VSAbility(owner.ID, TEST_ABILITY_CONFIG);
            var ability2 = new VSAbility(owner.ID, TEST_ABILITY_CONFIG);
            var ability3 = new VSAbility(owner.ID, TEST_ABILITY_CONFIG);

            var children = hierarchySystem.GetChildrenList(owner.ID);
            Assert.That(children.Count, Is.EqualTo(3));
            Assert.That(children, Contains.Item(ability1.ID));
            Assert.That(children, Contains.Item(ability2.ID));
            Assert.That(children, Contains.Item(ability3.ID));
        }
    }
}
