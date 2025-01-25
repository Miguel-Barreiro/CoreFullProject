using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Core.Model;
using FixedPointy;
using NUnit.Framework;
using UnityEngine;

namespace Testing_Core.Editor.UnitTests.Stats
{
    [TestFixture]
    public class StatsSystemTests : ScriptableObject
    {

        [SerializeField] private StatConfig testStat;
        
        private StatsSystem _statsSystem;
        private EntityA _owner;
        private EntityB _target;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Any one-time setup if needed
            EntitiesContainer.CreateInstance();
        }

        [SetUp]
        public void SetUp()
        {
            _statsSystem = new StatsSystemImplementation();
            _owner = new EntityA();
            _target = new EntityB();
        }

        [TearDown]
        public void TearDown()
        {
            _owner.Destroy();
            _target.Destroy();
            EntitiesContainer.Reset();
        }

        [Test]
        [Category("Stats")]
        public void AddModifier_AdditiveType_CorrectlyModifiesStat()
        {
            // Arrange
            Fix modifierValue = (Fix)2;

            // Act
            StatModId modId = _statsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifierValue, 
                                                       StatModifierType.Additive);
            Fix resultValue = _statsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(resultValue, Is.EqualTo(testStat.DefaultBaseValue + modifierValue));
        }

        [Test]
        [Category("Stats")]
        public void AddModifier_MultipleModifiersFromSameOwner_AllApply()
        {
            // Arrange
            Fix modifier1 = (Fix)2;
            Fix modifier2 = (Fix)3;

            // Act
            StatModId modId1 = _statsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifier1, StatModifierType.Additive);
            StatModId modId2 = _statsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifier2, StatModifierType.Additive);
            Fix resultValue = _statsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(resultValue, Is.EqualTo(testStat.DefaultBaseValue + modifier1 + modifier2));
        }

        [Test]
        [Category("Stats")]
        public void AddModifier_PercentageType_CalculatesCorrectly()
        {
            // Arrange
            Fix baseValue = testStat.DefaultBaseValue;
            Fix percentageIncrease = Fix.Ratio(1, 4); // 25% increase

            // Act
            StatModId modId = _statsSystem.AddModifier(_owner.ID, _target.ID, testStat, percentageIncrease, 
                                                       StatModifierType.Percentage);
            Fix resultValue = _statsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Fix expectedValue = baseValue + (baseValue * percentageIncrease);
            Assert.That(resultValue, Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("Stats")]
        public void AddModifier_AdditivePostMultiplicative_AppliesAfterMultiplicative()
        {
            // Arrange
            Fix multiplicative = (Fix)2;    // x2
            Fix postAdditive = (Fix)3;      // +3 after multiplication

            // Act
            _statsSystem.AddModifier(_owner.ID, _target.ID, testStat, multiplicative, StatModifierType.Multiplicative);
            _statsSystem.AddModifier(_owner.ID, _target.ID, testStat, postAdditive, StatModifierType.AdditivePostMultiplicative);
            Fix resultValue = _statsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Fix expectedValue = (testStat.DefaultBaseValue * multiplicative) + postAdditive;
            Assert.That(resultValue, Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("Stats")]
        public void ModifierOrder_CorrectCalculationOrder()
        {
            // Arrange
            Fix baseValue = 1;
            Fix additive = (Fix)2;          // +2
            Fix percentage = Fix.Ratio(1, 2);   // +50%
            Fix multiplicative = (Fix)2;     // x2
            Fix postAdditive = (Fix)1;      // +1

            //Arrange
            _statsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            
            // Act
            _statsSystem.AddModifier(_owner.ID, _target.ID, testStat, additive, StatModifierType.Additive);
            _statsSystem.AddModifier(_owner.ID, _target.ID, testStat, percentage, StatModifierType.Percentage);
            _statsSystem.AddModifier(_owner.ID, _target.ID, testStat, multiplicative, StatModifierType.Multiplicative);
            _statsSystem.AddModifier(_owner.ID, _target.ID, testStat, postAdditive, StatModifierType.AdditivePostMultiplicative);

            Fix resultValue = _statsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            // Expected: (((base + additive) * (1 + percentage)) * multiplicative) + postAdditive
            Fix baseAndAdditive = baseValue + additive;
            Fix afterPercentage = baseAndAdditive + (baseAndAdditive * percentage);
            Fix afterMultiplicative = afterPercentage * multiplicative;
            Fix expectedValue = afterMultiplicative + postAdditive;
            
            Assert.That(resultValue, Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("Stats")]
        public void MultipleModifierTypes_CalculatesCorrectly()
        {
            // Arrange
            Fix baseValue = 1;
            Fix additive = 2;                                       // +2
            Fix percentage = Fix.Ratio(1, 2);       // +50%
            Fix multiplicative = 2;                                 // x2

            //Arrange
            _statsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            
            // Act
            _statsSystem.AddModifier(_owner.ID, _target.ID, testStat, additive, StatModifierType.Additive);
            _statsSystem.AddModifier(_owner.ID, _target.ID, testStat, percentage, StatModifierType.Percentage);
            _statsSystem.AddModifier(_owner.ID, _target.ID, testStat, multiplicative, StatModifierType.Multiplicative);

            Fix finalValue = _statsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            // Expected calculation: ((1 + 2) * (1 + 0.5)) * 2 = 9
            Fix expectedValue = (Fix)9;
            Assert.That(finalValue, Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("Stats")]
        public void GetStatValue_NonExistentEntity_ReturnsDefaultValue()
        {
            // Arrange
            EntId nonExistentEntity = new EntId(999);

            // Act
            Fix value = _statsSystem.GetStatValue(nonExistentEntity, testStat);

            // Assert
            Assert.That(value, Is.EqualTo(testStat.DefaultBaseValue));
        }

        [Test]
        [Category("Stats")]
        public void RemoveModifier_ExistingModifier_RemovesEffect()
        {
            // Arrange
            Fix baseValue = 1;
            Fix modifierValue = 2;

            _statsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            
            StatModId modId = _statsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifierValue,
                                                       StatModifierType.Additive);

            // Act
            Fix valueBeforeRemoval = _statsSystem.GetStatValue(_target.ID, testStat);
            _statsSystem.RemoveModifier(modId);
            
            Fix valueAfterRemoval = _statsSystem.GetStatValue(_target.ID, testStat);
 
            // Assert
            Assert.That(valueBeforeRemoval, Is.EqualTo(baseValue + modifierValue));
            Assert.That(valueAfterRemoval, Is.EqualTo(baseValue));
        }

        [Test]
        [Category("Stats")]
        public void GetStatCurrentValue_NoModifiers_MatchesBaseValue()
        {
            // Arrange
            Fix baseValue = (Fix)5;
            _statsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            Fix currentValue = _statsSystem.GetStatDepletedValue(_target.ID, testStat);

            // Assert
            Assert.That(currentValue, Is.EqualTo(baseValue));
        }

        [Test]
        [Category("Stats")]
        public void ChangeDepletedValue_Positive_IncreasesCurrentValue()
        {
            // Arrange
            Fix baseValue = 10;
            Fix firstDelta = -4;
            Fix secondDelta = 2;
            _statsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            _statsSystem.ChangeDepletedValue(_target.ID, testStat, firstDelta); // Initial depletion
            Fix beforeChange = _statsSystem.GetStatDepletedValue(_target.ID, testStat);

            _statsSystem.ChangeDepletedValue(_target.ID, testStat, secondDelta);
            Fix afterChange = _statsSystem.GetStatDepletedValue(_target.ID, testStat);

            // Assert
            Assert.That(beforeChange, Is.EqualTo(baseValue + firstDelta));
            Assert.That(afterChange, Is.EqualTo(beforeChange + secondDelta));
    
        }


        [Test]
        [Category("Stats")]
        public void ChangeDepletedValue_Negative_DecreasesCurrentValue()
        {
            // Arrange
            Fix baseValue = 10;
            Fix delta = -3;
            _statsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            Fix beforeChange = _statsSystem.GetStatDepletedValue(_target.ID, testStat);
            _statsSystem.ChangeDepletedValue(_target.ID, testStat, delta);
            Fix afterChange = _statsSystem.GetStatDepletedValue(_target.ID, testStat);

            // Assert
            Assert.That(afterChange, Is.EqualTo(beforeChange + delta));
        }

        [Test]
        [Category("Stats")]
        public void ChangeDepletedValue_ExceedsMaximum_ClampedToMaximum()
        {
            // Arrange
            Fix baseValue = 10;
            Fix delta = -4;
            Fix secondDelta = 999;

            _statsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            _statsSystem.ChangeDepletedValue(_target.ID, testStat, delta); // Initial depletion
            _statsSystem.ChangeDepletedValue(_target.ID, testStat, secondDelta); // Try to heal more than depleted
            Fix currentValue = _statsSystem.GetStatDepletedValue(_target.ID, testStat);

            // Assert
            Assert.That(currentValue, Is.EqualTo(baseValue)); // Should be clamped to max
        }

        [Test]
        [Category("Stats")]
        public void ChangeDepletedValue_BelowZero_ClampedToMinValue()
        {
            // Arrange
            Fix baseValue = 10;
            Fix delta = -15;
            _statsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            _statsSystem.ChangeDepletedValue(_target.ID, testStat, delta); // Try to deplete more than total
            Fix currentValue = _statsSystem.GetStatDepletedValue(_target.ID, testStat);

            // Assert
            Assert.That(currentValue, Is.EqualTo((Fix)testStat.DefaultMinValue)); // Should be clamped 
        }

        [Test]
        [Category("Stats")]
        public void ChangeDepletedValue_WithModifiers_RespectsModifiedLimits()
        {
            // Arrange
            Fix baseValue = 10;
            Fix additiveModifier = 5;
            Fix expectedMax = baseValue + additiveModifier;
            Fix expectedMin = 0;
            
            Fix delta = expectedMax + 2;
            Fix second_delta = -delta;

            _statsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            _statsSystem.AddModifier(_owner.ID, _target.ID, testStat, additiveModifier, StatModifierType.Additive);
            _statsSystem.ChangeDepletedValue(_target.ID, testStat, delta);

            // Assert
            Fix currentValue = _statsSystem.GetStatDepletedValue(_target.ID, testStat);
            Assert.That(currentValue, Is.EqualTo(expectedMax));
            
            // Act
            _statsSystem.ChangeDepletedValue(_target.ID, testStat, second_delta);
            
            // Assert
            currentValue = _statsSystem.GetStatDepletedValue(_target.ID, testStat);
            Assert.That(currentValue, Is.EqualTo(expectedMin));
        }

        [Test]
        [Category("Stats")]
        public void ChangeDepletedValue_NonExistentEntity_DoesNotThrow()
        {
            // Arrange
            EntId nonExistentEntity = new EntId(999);

            // Act & Assert
            Assert.DoesNotThrow(() => _statsSystem.ChangeDepletedValue(nonExistentEntity, testStat, (Fix)5));
        }

        [Test]
        [Category("Stats")]
        public void ChangeDepletedValue_ZeroDelta_DoesNotChangeValue()
        {
            // Arrange
            Fix baseValue = 10;
            Fix delta = -4;
            
            _statsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            _statsSystem.ChangeDepletedValue(_target.ID, testStat, delta);
            Fix afterFirstchange = _statsSystem.GetStatDepletedValue(_target.ID, testStat);
            
            _statsSystem.ChangeDepletedValue(_target.ID, testStat, 0);
            Fix afterSecondChange = _statsSystem.GetStatDepletedValue(_target.ID, testStat);

            // Assert
            Assert.That(afterFirstchange, Is.EqualTo(baseValue + delta));
            
            Assert.That(afterSecondChange, Is.EqualTo(afterFirstchange));
        }

        
        
        public class EntityA : BaseEntity { }
        public class EntityB : BaseEntity { }
    }
} 