using System.Collections.Generic;
using System.Text.RegularExpressions;
using Core.Model;
using Core.Model.ModelSystems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Testing_Core.Editor.UnitTests.Model
{
    public struct MultiTestComponentData : IComponentData
    {
        public int Value;
        public bool Initialized;
        public EntId ID { get; set; }

        public void Init()
        {
            Initialized = true;
            Value = 0;
        }
    }

    public class TestMultipleComponentEntityContainer : MultipleComponentEntityContainer<MultiTestComponentData>
    {
        public TestMultipleComponentEntityContainer(uint maxNumber) : base(maxNumber) { }
    }

    [TestFixture]
    public class MultipleComponentEntityContainerTests
    {
        private const uint MAX_ENTITIES = 4;
        private const uint MAX_COMPONENTS_PER_ENTITY = 10;

        private TestMultipleComponentEntityContainer _container;

        private static readonly EntId EntityA = new EntId(1);
        private static readonly EntId EntityB = new EntId(2);
        private static readonly EntId EntityC = new EntId(3);
        private static readonly EntId UnknownEntity = new EntId(999);

        [SetUp]
        public void Setup()
        {
            _container = new TestMultipleComponentEntityContainer(MAX_ENTITIES);
        }

        #region Helpers

        private List<MultiTestComponentData> GetAllComponentsOf(EntId owner)
        {
            List<MultiTestComponentData> result = new List<MultiTestComponentData>();
            ref EntityComponentsHeader header = ref _container.GetEntityComponentsHeader(owner);
            for (int i = 0; i < header.Count; i++)
                result.Add(_container.GetComponent(header.Indexes[i]));
            return result;
        }

        /// Sets up an entity with the given values, one component per value
        private void SetupEntityWithValues(EntId owner, params int[] values)
        {
            _container.SetupComponent(owner);
            ref MultiTestComponentData multiTestComponentData = ref _container.GetComponent(owner);

            multiTestComponentData.Value = values[0];
            for (int i = 1; i < values.Length; i++)
            {
                uint childOrder = _container.AddComponent(owner);
                uint index = _container.GetChildIndex(owner, childOrder);
                ref MultiTestComponentData testComponentData = ref _container.GetComponent(index);
                testComponentData.Value = values[i];
            }
        }

        private void AssertEntityComponents(EntId owner, params int[] expectedValues)
        {
            List<MultiTestComponentData> components = GetAllComponentsOf(owner);
            Assert.That(components.Count, Is.EqualTo(expectedValues.Length), $"Entity {owner.Id} has wrong number of components");
            for (int i = 0; i < components.Count; i++)
            {
                Assert.That(components[i].ID, Is.EqualTo(owner), $"Component {i} of entity {owner.Id} has wrong owner");
                Assert.That(components[i].Value, Is.EqualTo(expectedValues[i]), $"Component {i} of entity {owner.Id} has wrong value");
            }
        }

        #endregion

        #region Construction

        [Test]
        public void Constructor_StartsEmpty()
        {
            Assert.That(_container.Count, Is.EqualTo(0));
            Assert.That(_container.TopEmptyIndex, Is.EqualTo(0));
            Assert.That(_container.TopEntityEmptyIndex, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_AllocatesComponentsForEveryEntitySlot()
        {
            Assert.That(_container.MaxCount, Is.EqualTo(MAX_ENTITIES * MAX_COMPONENTS_PER_ENTITY));
        }

        [Test]
        public void Constructor_AllocatesEntityHeaders()
        {
            Assert.That(_container.EntityComponentsHeaders, Is.Not.Null);
            Assert.That(_container.EntityComponentsHeaders.Length, Is.EqualTo(MAX_ENTITIES));
        }

        [Test]
        public void Constructor_AllComponentsHaveInvalidId()
        {
            foreach (MultiTestComponentData component in _container.Components)
                Assert.That(component.ID, Is.EqualTo(EntId.Invalid));
        }

        #endregion

        #region SetupComponent

        [Test]
        public void SetupComponent_CreatesComponentForOwner()
        {
            _container.SetupComponent(EntityA);

            Assert.That(_container.GetComponent(EntityA).ID, Is.EqualTo(EntityA));
            Assert.That(_container.Count, Is.EqualTo(1));
            Assert.That(_container.TopEntityEmptyIndex, Is.EqualTo(1));
        }

        [Test]
        public void SetupComponent_InitializesComponent()
        {
            _container.SetupComponent(EntityA);

            Assert.That(_container.GetComponent(EntityA).Initialized, Is.True);
        }

        [Test]
        public void SetupComponent_CreatesHeaderWithSingleIndex()
        {
            _container.SetupComponent(EntityA);

            ref var header = ref _container.GetEntityComponentsHeader(EntityA);
            Assert.That(header.Count, Is.EqualTo(1));
            Assert.That(_container.GetComponent(header.Indexes[0]).ID, Is.EqualTo(EntityA));
        }

        [Test]
        public void SetupComponent_MultipleEntities_AreIndependent()
        {
            SetupEntityWithValues(EntityA, 10);
            SetupEntityWithValues(EntityB, 20);

            Assert.That(_container.GetComponent(EntityA).Value, Is.EqualTo(10));
            Assert.That(_container.GetComponent(EntityB).Value, Is.EqualTo(20));
            Assert.That(_container.Count, Is.EqualTo(2));
            Assert.That(_container.TopEntityEmptyIndex, Is.EqualTo(2));
        }

        [Test]
        public void SetupComponent_DuplicateOwner_LogsErrorAndDoesNothing()
        {
            SetupEntityWithValues(EntityA, 10);

            LogAssert.Expect(LogType.Error, new Regex("already exists"));
            _container.SetupComponent(EntityA);

            Assert.That(_container.Count, Is.EqualTo(1));
            Assert.That(_container.TopEntityEmptyIndex, Is.EqualTo(1));
            Assert.That(_container.GetComponent(EntityA).Value, Is.EqualTo(10));
        }

        [Test]
        public void SetupComponent_MoreEntitiesThanMax_LogsErrorAndDoesNotThrow()
        {
            for (uint i = 1; i <= MAX_ENTITIES; i++)
                _container.SetupComponent(new EntId(i));

            EntId overflowEntity = new EntId(MAX_ENTITIES + 1);
            LogAssert.Expect(LogType.Error, new Regex("No available space"));
            Assert.DoesNotThrow(() => _container.SetupComponent(overflowEntity));

            Assert.That(_container.TopEntityEmptyIndex, Is.EqualTo(MAX_ENTITIES));
            Assert.That(_container.GetComponent(overflowEntity).ID, Is.EqualTo(EntId.Invalid));
        }

        #endregion

        #region AddComponent

        [Test]
        public void AddComponent_ReturnsIndexOfNewComponent()
        {
            _container.SetupComponent(EntityA);
            uint childOrder = _container.AddComponent(EntityA);
            uint index = _container.GetChildIndex(EntityA, childOrder);
            
            Assert.That(index, Is.Not.EqualTo(uint.MaxValue));
            Assert.That(_container.GetComponent(index).ID, Is.EqualTo(EntityA));
            Assert.That(_container.GetComponent(index).Initialized, Is.True);
        }

        [Test]
        public void AddComponent_AppendsIndexToHeader()
        {
            _container.SetupComponent(EntityA);
            uint childOrder = _container.AddComponent(EntityA);
            uint index = _container.GetChildIndex(EntityA, childOrder);
            
            
            ref var header = ref _container.GetEntityComponentsHeader(EntityA);
            Assert.That(header.Count, Is.EqualTo(2));
            Assert.That(header.Indexes[1], Is.EqualTo(index));
        }

        [Test]
        public void AddComponent_IncrementsComponentCountButNotEntityCount()
        {
            _container.SetupComponent(EntityA);
            _container.AddComponent(EntityA);
            _container.AddComponent(EntityA);

            Assert.That(_container.Count, Is.EqualTo(3));
            Assert.That(_container.TopEntityEmptyIndex, Is.EqualTo(1));
        }

        [Test]
        public void AddComponent_GetComponentByOwnerStillReturnsFirstComponent()
        {
            SetupEntityWithValues(EntityA, 10, 20, 30);

            Assert.That(_container.GetComponent(EntityA).Value, Is.EqualTo(10));
        }

        [Test]
        public void AddComponent_InterleavedEntities_KeepTheirOwnComponents()
        {
            _container.SetupComponent(EntityA);
            _container.GetComponent(EntityA).Value = 1;
            _container.SetupComponent(EntityB);
            _container.GetComponent(EntityB).Value = 100;

            uint childOrder = _container.AddComponent(EntityA);
            uint index = _container.GetChildIndex(EntityA, childOrder);
            _container.GetComponent(index).Value = 2;

            childOrder = _container.AddComponent(EntityB);
            index = _container.GetChildIndex(EntityB, childOrder);
            _container.GetComponent(index).Value = 200;
            
            childOrder = _container.AddComponent(EntityA);
            index = _container.GetChildIndex(EntityA, childOrder);
            _container.GetComponent(index).Value = 3;

            AssertEntityComponents(EntityA, 1, 2, 3);
            AssertEntityComponents(EntityB, 100, 200);
        }

        [Test]
        public void AddComponent_UnknownOwner_LogsErrorAndReturnsMaxValue()
        {
            LogAssert.Expect(LogType.Error, new Regex("does not exist"));
            uint childOrder = _container.AddComponent(UnknownEntity);

            Assert.That(childOrder, Is.EqualTo(uint.MaxValue));
            Assert.That(_container.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddComponent_UpToMaxComponentsPerEntity_Succeeds()
        {
            _container.SetupComponent(EntityA);
            for (int i = 1; i < MAX_COMPONENTS_PER_ENTITY; i++)
                Assert.That(_container.AddComponent(EntityA), Is.Not.EqualTo(uint.MaxValue));

            Assert.That(_container.GetEntityComponentsHeader(EntityA).Count, Is.EqualTo(MAX_COMPONENTS_PER_ENTITY));
            Assert.That(_container.Count, Is.EqualTo(MAX_COMPONENTS_PER_ENTITY));
        }

        [Test]
        public void AddComponent_BeyondMaxComponentsPerEntity_LogsErrorAndDoesNotLeakSlot()
        {
            _container.SetupComponent(EntityA);
            for (int i = 1; i < MAX_COMPONENTS_PER_ENTITY; i++)
                _container.AddComponent(EntityA);

            LogAssert.Expect(LogType.Error, new Regex("already has maximum number of components"));
            uint childOrder = _container.AddComponent(EntityA);

            Assert.That(childOrder, Is.EqualTo(uint.MaxValue));
            Assert.That(_container.GetEntityComponentsHeader(EntityA).Count, 
                        Is.EqualTo(MAX_COMPONENTS_PER_ENTITY));
        }

        #endregion

        #region GetComponent / GetEntityComponentsHeader

        [Test]
        public void GetComponent_UnknownOwner_ReturnsInvalid()
        {
            ref MultiTestComponentData component = ref _container.GetComponent(UnknownEntity);

            Assert.That(component.ID, Is.EqualTo(EntId.Invalid));
        }

        [Test]
        public void GetComponent_ReturnsReference()
        {
            _container.SetupComponent(EntityA);

            ref MultiTestComponentData component = ref _container.GetComponent(EntityA);
            component.Value = 42;

            Assert.That(_container.GetComponent(EntityA).Value, Is.EqualTo(42));
        }

        [Test]
        public void GetComponentByIndex_ReturnsReference()
        {
            _container.SetupComponent(EntityA);
            uint index = _container.AddComponent(EntityA);

            _container.GetComponent(index).Value = 7;

            Assert.That(_container.Components[index].Value, Is.EqualTo(7));
        }

        [Test]
        public void GetEntityComponentsHeader_UnknownOwner_LogsErrorAndReturnsEmptyHeader()
        {
            LogAssert.Expect(LogType.Error, new Regex("not found"));
            ref var header = ref _container.GetEntityComponentsHeader(UnknownEntity);

            Assert.That(header.Count, Is.EqualTo(0));
        }

        #endregion

        #region RemoveComponent

        [Test]
        public void RemoveComponent_UnknownOwner_DoesNothing()
        {
            SetupEntityWithValues(EntityA, 10);

            Assert.DoesNotThrow(() => _container.RemoveComponent(UnknownEntity));

            Assert.That(_container.Count, Is.EqualTo(1));
            Assert.That(_container.TopEntityEmptyIndex, Is.EqualTo(1));
            Assert.That(_container.GetComponent(EntityA).Value, Is.EqualTo(10));
        }

        [Test]
        public void RemoveComponent_OnlyEntity_ContainerBecomesEmpty()
        {
            _container.SetupComponent(EntityA);

            _container.RemoveComponent(EntityA);

            Assert.That(_container.Count, Is.EqualTo(0));
            Assert.That(_container.TopEntityEmptyIndex, Is.EqualTo(0));
            Assert.That(_container.GetComponent(EntityA).ID, Is.EqualTo(EntId.Invalid));
        }

        [Test]
        public void RemoveComponent_EntityWithMultipleComponents_RemovesAllOfThem()
        {
            SetupEntityWithValues(EntityA, 1, 2, 3);

            _container.RemoveComponent(EntityA);

            Assert.That(_container.Count, Is.EqualTo(0));
            Assert.That(_container.TopEntityEmptyIndex, Is.EqualTo(0));
            foreach (MultiTestComponentData component in _container.Components)
                Assert.That(component.ID, Is.Not.EqualTo(EntityA));
        }

        [Test]
        public void RemoveComponent_LastEntity_KeepsOtherEntitiesIntact()
        {
            SetupEntityWithValues(EntityA, 1, 2);
            SetupEntityWithValues(EntityB, 10, 20);

            _container.RemoveComponent(EntityB);

            Assert.That(_container.Count, Is.EqualTo(2));
            Assert.That(_container.TopEntityEmptyIndex, Is.EqualTo(1));
            Assert.That(_container.GetComponent(EntityB).ID, Is.EqualTo(EntId.Invalid));
            AssertEntityComponents(EntityA, 1, 2);
        }

        [Test]
        public void RemoveComponent_FirstEntity_KeepsOtherEntitiesIntact()
        {
            SetupEntityWithValues(EntityA, 1, 2);
            SetupEntityWithValues(EntityB, 10, 20);

            _container.RemoveComponent(EntityA);

            Assert.That(_container.Count, Is.EqualTo(2));
            Assert.That(_container.TopEntityEmptyIndex, Is.EqualTo(1));
            Assert.That(_container.GetComponent(EntityA).ID, Is.EqualTo(EntId.Invalid));

            AssertEntityComponents(EntityB, 10, 20);
        }

        [Test]
        public void RemoveComponent_MiddleEntity_KeepsOtherEntitiesIntact()
        {
            SetupEntityWithValues(EntityA, 1, 2, 3);
            SetupEntityWithValues(EntityB, 10);
            SetupEntityWithValues(EntityC, 100, 200);

            _container.RemoveComponent(EntityB);

            Assert.That(_container.Count, Is.EqualTo(5));
            Assert.That(_container.TopEntityEmptyIndex, Is.EqualTo(2));
            AssertEntityComponents(EntityA, 1, 2, 3);
            AssertEntityComponents(EntityC, 100, 200);
        }

        [Test]
        public void RemoveComponent_InterleavedComponents_KeepsOtherEntitiesIntact()
        {
            _container.SetupComponent(EntityA);
            _container.GetComponent(EntityA).Value = 1;
            _container.SetupComponent(EntityB);
            _container.GetComponent(EntityB).Value = 100;
            
            uint childOrder = _container.AddComponent(EntityA);
            uint index = _container.GetChildIndex(EntityA, childOrder);
            _container.GetComponent(index).Value = 2;

            childOrder = _container.AddComponent(EntityB);
            index = _container.GetChildIndex(EntityB, childOrder);
            _container.GetComponent(index).Value = 200;

            childOrder = _container.AddComponent(EntityA);
            index = _container.GetChildIndex(EntityA, childOrder);
            _container.GetComponent(index).Value = 3;

            childOrder = _container.AddComponent(EntityB);
            index = _container.GetChildIndex(EntityB, childOrder);
            _container.GetComponent(index).Value = 300;

            _container.RemoveComponent(EntityA);

            Assert.That(_container.Count, Is.EqualTo(3));
            AssertEntityComponents(EntityB, 100, 200, 300);
        }

        [Test]
        public void RemoveComponent_AllEntities_ContainerBecomesEmpty()
        {
            SetupEntityWithValues(EntityA, 1, 2);
            SetupEntityWithValues(EntityB, 10, 20, 30);
            SetupEntityWithValues(EntityC, 100);

            _container.RemoveComponent(EntityB);
            _container.RemoveComponent(EntityA);
            _container.RemoveComponent(EntityC);

            Assert.That(_container.Count, Is.EqualTo(0));
            Assert.That(_container.TopEntityEmptyIndex, Is.EqualTo(0));
        }

        [Test]
        public void RemoveComponent_ThenSetupSameOwner_Works()
        {
            SetupEntityWithValues(EntityA, 1, 2);
            _container.RemoveComponent(EntityA);

            _container.SetupComponent(EntityA);

            Assert.That(_container.GetComponent(EntityA).ID, Is.EqualTo(EntityA));
            Assert.That(_container.GetEntityComponentsHeader(EntityA).Count, Is.EqualTo(1));
            Assert.That(_container.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveComponent_ThenSetupNewEntity_DoesNotCorruptExisting()
        {
            SetupEntityWithValues(EntityA, 1, 2);
            SetupEntityWithValues(EntityB, 10, 20);
            _container.RemoveComponent(EntityA);

            SetupEntityWithValues(EntityC, 100, 200, 300);

            AssertEntityComponents(EntityB, 10, 20);
            AssertEntityComponents(EntityC, 100, 200, 300);
            Assert.That(_container.Count, Is.EqualTo(5));
            Assert.That(_container.TopEntityEmptyIndex, Is.EqualTo(2));
        }

        [Test]
        public void RemoveComponent_Twice_SecondCallDoesNothing()
        {
            SetupEntityWithValues(EntityA, 1);
            SetupEntityWithValues(EntityB, 10);

            _container.RemoveComponent(EntityA);
            _container.RemoveComponent(EntityA);

            Assert.That(_container.Count, Is.EqualTo(1));
            Assert.That(_container.TopEntityEmptyIndex, Is.EqualTo(1));
            AssertEntityComponents(EntityB, 10);
        }

        #endregion

        #region RebuildWithMax

        [Test]
        public void RebuildWithMax_ClearsAllEntities()
        {
            SetupEntityWithValues(EntityA, 1, 2);
            SetupEntityWithValues(EntityB, 10);

            _container.RebuildWithMax(MAX_ENTITIES);

            Assert.That(_container.Count, Is.EqualTo(0));
            Assert.That(_container.TopEntityEmptyIndex, Is.EqualTo(0));
            Assert.That(_container.GetComponent(EntityA).ID, Is.EqualTo(EntId.Invalid));
            Assert.That(_container.GetComponent(EntityB).ID, Is.EqualTo(EntId.Invalid));
        }

        [Test]
        public void RebuildWithMax_ResizesStorage()
        {
            const uint newMax = 8;

            _container.RebuildWithMax(newMax);

            Assert.That(_container.MaxCount, Is.EqualTo(newMax * MAX_COMPONENTS_PER_ENTITY));
            Assert.That(_container.EntityComponentsHeaders.Length, Is.EqualTo(newMax));
        }

        [Test]
        public void RebuildWithMax_CanBeFilledToCapacity()
        {
            const uint newMax = 3;
            _container.RebuildWithMax(newMax);

            for (uint e = 1; e <= newMax; e++)
            {
                EntId owner = new EntId(e);
                _container.SetupComponent(owner);
                for (int c = 1; c < MAX_COMPONENTS_PER_ENTITY; c++)
                    Assert.That(_container.AddComponent(owner), Is.Not.EqualTo(uint.MaxValue));
            }

            Assert.That(_container.Count, Is.EqualTo(newMax * MAX_COMPONENTS_PER_ENTITY));
            Assert.That(_container.TopEntityEmptyIndex, Is.EqualTo(newMax));
        }

        #endregion

        #region SetupInvalid

        [Test]
        public void SetupInvalid_InvalidComponentHasInvalidId()
        {
            _container.SetupInvalid();

            Assert.That(_container.Invalid.ID, Is.EqualTo(EntId.Invalid));
        }

        #endregion
    }
}
