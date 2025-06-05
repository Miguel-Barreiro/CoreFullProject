using System;
using System.Collections.Generic;
using System.Linq;
using Core.Events;
using Core.Model;
using Core.Model.ModelSystems;
using NUnit.Framework;
using Testing.Core.Editor.UnitTests.Model;

namespace Testing_Core.Editor.UnitTests.Model
{
    [TestFixture]
    public class TypeCacheTests
    {
        private TypeCache _typeCache;

        [SetUp]
        public void Setup()
        {
            _typeCache = TypeCache.Get();
        }

        [Test]
        public void Get_ReturnsSameInstance()
        {
            var instance1 = TypeCache.Get();
            var instance2 = TypeCache.Get();
            
            Assert.That(instance1, Is.SameAs(instance2), "TypeCache.Get() should return the same instance");
        }

        [Test]
        public void GetAllEntityTypes_ContainsBaseEntityType()
        {
            var entityTypes = _typeCache.GetAllEntityTypes().ToList();
            
            Assert.That(entityTypes, Contains.Item(typeof(Entity)), "Entity types should include the base Entity type");
        }

        [Test]
        public void GetAllEntityTypes_ContainsTestEntities()
        {
            var entityTypes = _typeCache.GetAllEntityTypes().ToList();
            
            Assert.That(entityTypes, Contains.Item(typeof(TestEntity)), "Should contain TestEntity");
            Assert.That(entityTypes, Contains.Item(typeof(TestEntityWithNormalComponents)), "Should contain TestEntityWithComponents");
        }

        [Test]
        public void GetAllEventTypes_ReturnsNonGenericEvents()
        {
            var eventTypes = _typeCache.GetAllEventTypes().ToList();
            
            Assert.That(eventTypes, Is.Not.Empty, "Should return some event types");
            Assert.That(eventTypes, Has.None.Matches<Type>(t => t.IsGenericType), "Should not return generic event types");
        }

        [Test]
        public void GetAllEventTypes_ContainsTestEvents()
        {
            var eventTypes = _typeCache.GetAllEventTypes().ToList();
            
            Assert.That(eventTypes, Contains.Item(typeof(TestEvent)), "Should contain TestEvent");
            Assert.That(eventTypes, Contains.Item(typeof(TestEarlyEvent)), "Should contain TestEarlyEvent");
            Assert.That(eventTypes, Contains.Item(typeof(TestLateEvent)), "Should contain TestLateEvent");
        }

        [Test]
        public void GetAllEntityEventTypes_ContainsTestEntityEvents()
        {
            var entityEventTypes = _typeCache.GetAllEntityEventTypes().ToList();
            
            Assert.That(entityEventTypes, Contains.Item(typeof(TestEntityEvent)), "Should contain TestEntityEvent");
        }

        [Test]
        public void GetEventAttributes_ReturnsCorrectAttributes()
        {
            // Test regular event
            var testEventAttributes = _typeCache.GetEventAttributes(typeof(TestEvent));
            Assert.That(testEventAttributes, Is.Not.Null, "TestEvent attributes should not be null");
            Assert.That(testEventAttributes.EventOrder, Is.EqualTo(EventOrder.Default), "TestEvent should have default order");

            // Test early event
            var earlyEventAttributes = _typeCache.GetEventAttributes(typeof(TestEarlyEvent));
            Assert.That(earlyEventAttributes, Is.Not.Null, "TestEarlyEvent attributes should not be null");
            Assert.That(earlyEventAttributes.EventOrder, Is.EqualTo(EventOrder.PreDefault), "TestEarlyEvent should have pre-default order");

            // Test late event
            var lateEventAttributes = _typeCache.GetEventAttributes(typeof(TestLateEvent));
            Assert.That(lateEventAttributes, Is.Not.Null, "TestLateEvent attributes should not be null");
            Assert.That(lateEventAttributes.EventOrder, Is.EqualTo(EventOrder.PostDefault), "TestLateEvent should have post-default order");
        }

        [Test]
        public void GetAllComponentDataTypes_ReturnsValueTypes()
        {
            var componentDataTypes = _typeCache.GetAllComponentDataTypes().ToList();
            
            Assert.That(componentDataTypes, Is.Not.Empty, "Should return some component data types");
            Assert.That(componentDataTypes, Has.All.Matches<Type>(t => t.IsValueType), "All component data types should be value types");
        }

        [Test]
        public void GetAllComponentDataTypes_ContainsTestComponentData()
        {
            var componentDataTypes = _typeCache.GetAllComponentDataTypes().ToList();
            
            Assert.That(componentDataTypes, Contains.Item(typeof(TestComponentData)), "Should contain TestComponentData");
            Assert.That(componentDataTypes, Contains.Item(typeof(AnotherComponentData)), "Should contain AnotherComponentData");
        }

        [Test]
        public void GetComponentsOf_ReturnsComponentsForEntityType()
        {
            var components = _typeCache.GetComponentsOf(typeof(TestEntityWithNormalComponents)).ToList();
            
            Assert.That(components, Is.Not.Empty, "TestEntityWithComponents should have components");
            Assert.That(components, Contains.Item(typeof(ITestComponent)), "Should contain ITestComponent component");
        }

        [Test]
        public void GetComponentDatasOfEntityType_ReturnsCorrectDataTypes()
        {
            var componentDatas = _typeCache.GetComponentDatasOfEntityType(typeof(TestEntityWithNormalComponents)).ToList();
            
            Assert.That(componentDatas, Is.Not.Empty, "TestEntityWithComponents should have component data types");
            Assert.That(componentDatas, Contains.Item(typeof(TestComponentData)), "Should contain TestComponentData type");
        }

        [Test]
        public void GetAllEventTypes_ReturnsUniqueResults()
        {
            var eventTypes = _typeCache.GetAllEventTypes().ToList();
            var uniqueEventTypes = eventTypes.Distinct().ToList();
            
            Assert.That(eventTypes.Count, Is.EqualTo(uniqueEventTypes.Count), 
                "GetAllEventTypes should not return duplicate types");
        }

        [Test]
        public void GetAllEntityEventTypes_ReturnsUniqueResults()
        {
            var entityEventTypes = _typeCache.GetAllEntityEventTypes().ToList();
            var uniqueEntityEventTypes = entityEventTypes.Distinct().ToList();
            
            Assert.That(entityEventTypes.Count, Is.EqualTo(uniqueEntityEventTypes.Count), 
                "GetAllEntityEventTypes should not return duplicate types");
        }

        [Test]
        public void GetAllEntityTypes_ReturnsUniqueResults()
        {
            var entityTypes = _typeCache.GetAllEntityTypes().ToList();
            var uniqueEntityTypes = entityTypes.Distinct().ToList();
            
            Assert.That(entityTypes.Count, Is.EqualTo(uniqueEntityTypes.Count), 
                "GetAllEntityTypes should not return duplicate types");
        }

        [Test]
        public void GetComponentsOf_ReturnsUniqueResults()
        {
            var entityType = typeof(TestEntityWithDuplicatedComponents);
            var components = _typeCache.GetComponentsOf(entityType).ToList();
            var uniqueComponents = components.Distinct().ToList();
            
            Assert.That(components.Count, Is.EqualTo(uniqueComponents.Count), 
                $"GetComponentsOf should not return duplicate types for {entityType.Name}");
        }

        [Test]
        public void GetComponentDatasOfEntityType_ReturnsUniqueResults()
        {
            var entityType = typeof(TestEntityWithDuplicatedComponents);
            var componentDatas = _typeCache.GetComponentDatasOfEntityType(entityType).ToList();
            var uniqueComponentDatas = componentDatas.Distinct().ToList();
            
            Assert.That(componentDatas.Count, Is.EqualTo(uniqueComponentDatas.Count), 
                $"GetComponentDatasOfEntityType should not return duplicate types for {entityType.Name}");
        }

        [Test]
        public void GetAllEntityComponentTypes_ReturnsUniqueResults()
        {
            var componentTypes = _typeCache.GetAllEntityComponentTypes().ToList();
            var uniqueComponentTypes = componentTypes.Distinct().ToList();
            
            Assert.That(componentTypes.Count, Is.EqualTo(uniqueComponentTypes.Count), 
                "GetAllEntityComponentTypes should not return duplicate types");
        }

        [Test]
        public void GetAllComponentDataTypes_ReturnsUniqueResults()
        {
            var componentDataTypes = _typeCache.GetAllComponentDataTypes().ToList();
            var uniqueComponentDataTypes = componentDataTypes.Distinct().ToList();
            
            Assert.That(componentDataTypes.Count, Is.EqualTo(uniqueComponentDataTypes.Count), 
                "GetAllComponentDataTypes should not return duplicate types");
        }

        [Test]
        public void GetComponentDataTypeFromComponentType_ReturnsCorrectDataTypes()
        {
            // Test for normal component
            var normalComponentDataType = _typeCache.GetComponentDataTypeFromComponentType(typeof(ITestComponent));
            Assert.That(normalComponentDataType, Is.EqualTo(typeof(TestComponentData)), 
                "ITestComponent should map to TestComponentData");

            // Test for another component
            var anotherComponentDataType = _typeCache.GetComponentDataTypeFromComponentType(typeof(IAnotherComponent));
            Assert.That(anotherComponentDataType, Is.EqualTo(typeof(AnotherComponentData)), 
                "IAnotherComponent should map to AnotherComponentData");

            // Test for another component
            var doubleComponentDataType = _typeCache.GetComponentDataTypeFromComponentType(typeof(IDoubleIncludeComponent));
            Assert.That(doubleComponentDataType, Is.EqualTo(typeof(DoubleIncludeComponentData)), 
                        "IDoubleIncludeComponent should map to AnotherComponentData");

            // Test for another component
            var tripleComponentDataType = _typeCache.GetComponentDataTypeFromComponentType(typeof(ITripleIncludeComponent));
            Assert.That(tripleComponentDataType, Is.EqualTo(typeof(TripleIncludeComponentData)), 
                        "ITripleIncludeComponent should map to AnotherComponentData");

        }

        [Test]
        public void GetComponentDataTypeFromComponentType_ThrowsForInvalidComponentType()
        {
            // Test with a type that is not a component
            Assert.Throws<KeyNotFoundException>(() => 
                _typeCache.GetComponentDataTypeFromComponentType(typeof(TestEntity)),
                "Should throw KeyNotFoundException for non-component type");
        }

        [Test]
        public void GetComponentDataTypeFromComponentType_ReturnsConsistentResults()
        {
            // Test that multiple calls return the same result
            var firstCall = _typeCache.GetComponentDataTypeFromComponentType(typeof(ITripleIncludeComponent));
            var secondCall = _typeCache.GetComponentDataTypeFromComponentType(typeof(ITripleIncludeComponent));
            
            Assert.That(firstCall, Is.SameAs(secondCall), 
                "Multiple calls should return the same instance");
        }

        [Test]
        public void GetComponentDataTypeFromComponentType_HandlesAllTestComponents()
        {
            // Get all component types from our test entities
            var componentTypes = _typeCache.GetAllEntityComponentTypes()
                .Where(t => t.Namespace == typeof(TestEntity).Namespace)
                .ToList();

            foreach (var componentType in componentTypes)
            {
                var componentDataType = _typeCache.GetComponentDataTypeFromComponentType(componentType);
                
                Assert.That(componentDataType, Is.Not.Null, 
                    $"Component data type should not be null for {componentType.Name}");
                Assert.That(componentDataType.IsValueType, 
                    $"Component data type should be a value type for {componentType.Name}");
                Assert.That(componentDataType.GetInterfaces().Contains(typeof(IComponentData)), 
                    $"Component data type should implement IComponentData for {componentType.Name}");
            }
        }
    }
} 