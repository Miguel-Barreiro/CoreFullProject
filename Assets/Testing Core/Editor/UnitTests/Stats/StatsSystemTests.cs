using System.Collections.Generic;
using System.Linq;
using Core.Editor;
using Core.Model;
using Core.Runtime.Editor;
using FixedPointy;
using NUnit.Framework;
using UnityEngine;
using Zenject;

namespace Testing_Core.Editor.UnitTests.Stats
{
    [TestFixture]
    public class StatsSystemTests : UnitTest
    {
        
        [Inject] private readonly StatsSystem StatsSystem = null!;

        [SerializeField] private StatConfig testStat;

        
        // private StatsSystem _statsSystem;
        private EntityA _owner;
        private EntityB _target;
        
        protected override void InstallTestSystems(IUnitTestInstaller installer) 
        {
            installer.AddTestSystem<StatsSystem>(new StatsSystemImplementation());
        }
        
        [SetUp]
        public void SetUp()
        {
            _owner = new EntityA();
            _target = new EntityB();
        }

        [TearDown]
        public void TearDown()
        {
            _owner.Destroy();
            _target.Destroy();
            EntitiesContainer.Reset();
            StatsSystem.Reset();
        }

        [Test]
        [Category("Stats")]
        public void AddModifier_AdditiveType_CorrectlyModifiesStat()
        {
            // Arrange
            Fix modifierValue = 2;

            // Act
            StatModId modId = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifierValue, 
                                                       StatModifierType.Additive);
            Fix resultValue = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(resultValue, Is.EqualTo(testStat.DefaultBaseValue + modifierValue));
        }

        [Test]
        [Category("Stats")]
        public void AddModifier_MultipleModifiersFromSameOwner_AllApply()
        {
            // Arrange
            Fix modifier1 = 2;
            Fix modifier2 = 3;

            // Act
            StatModId modId1 = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifier1, StatModifierType.Additive);
            StatModId modId2 = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifier2, StatModifierType.Additive);
            Fix resultValue = StatsSystem.GetStatValue(_target.ID, testStat);

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
            StatModId modId = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, percentageIncrease, 
                                                       StatModifierType.Percentage);
            Fix resultValue = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Fix expectedValue = baseValue + (baseValue * percentageIncrease);
            Assert.That(resultValue, Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("Stats")]
        public void AddModifier_AdditivePostMultiplicative_AppliesAfterMultiplicative()
        {
            // Arrange
            Fix multiplicative = 2;    // x2
            Fix postAdditive = 3;      // +3 after multiplication

            // Act
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, multiplicative, StatModifierType.Multiplicative);
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, postAdditive, StatModifierType.AdditivePostMultiplicative);
            Fix resultValue = StatsSystem.GetStatValue(_target.ID, testStat);

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
            Fix additive = 2;          // +2
            Fix percentage = Fix.Ratio(1, 2);   // +50%
            Fix multiplicative = 2;     // x2
            Fix postAdditive = 1;      // +1

            //Arrange
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            
            // Act
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, additive, StatModifierType.Additive);
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, percentage, StatModifierType.Percentage);
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, multiplicative, StatModifierType.Multiplicative);
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, postAdditive, StatModifierType.AdditivePostMultiplicative);

            Fix resultValue = StatsSystem.GetStatValue(_target.ID, testStat);

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
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            
            // Act
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, additive, StatModifierType.Additive);
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, percentage, StatModifierType.Percentage);
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, multiplicative, StatModifierType.Multiplicative);

            Fix finalValue = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            // Expected calculation: ((1 + 2) * (1 + 0.5)) * 2 = 9
            Fix expectedValue = 9;
            Assert.That(finalValue, Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("Stats")]
        public void GetStatValue_NonExistentEntity_ReturnsDefaultValue()
        {
            // Arrange
            EntId nonExistentEntity = new EntId(999);

            // Act
            Fix value = StatsSystem.GetStatValue(nonExistentEntity, testStat);

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

            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            
            StatModId modId = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifierValue,
                                                       StatModifierType.Additive);

            // Act
            Fix valueBeforeRemoval = StatsSystem.GetStatValue(_target.ID, testStat);
            StatsSystem.RemoveModifier(modId);
            
            Fix valueAfterRemoval = StatsSystem.GetStatValue(_target.ID, testStat);
 
            // Assert
            Assert.That(valueBeforeRemoval, Is.EqualTo(baseValue + modifierValue));
            Assert.That(valueAfterRemoval, Is.EqualTo(baseValue));
        }

        [Test]
        [Category("Stats")]
        public void GetStatCurrentValue_NoModifiers_MatchesBaseValue()
        {
            // Arrange
            Fix baseValue = 5;
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            Fix currentValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);

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
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            StatsSystem.ChangeDepletedValue(_target.ID, testStat, firstDelta); // Initial depletion
            Fix beforeChange = StatsSystem.GetStatDepletedValue(_target.ID, testStat);

            StatsSystem.ChangeDepletedValue(_target.ID, testStat, secondDelta);
            Fix afterChange = StatsSystem.GetStatDepletedValue(_target.ID, testStat);

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
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            Fix beforeChange = StatsSystem.GetStatDepletedValue(_target.ID, testStat);
            StatsSystem.ChangeDepletedValue(_target.ID, testStat, delta);
            Fix afterChange = StatsSystem.GetStatDepletedValue(_target.ID, testStat);

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

            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            StatsSystem.ChangeDepletedValue(_target.ID, testStat, delta); // Initial depletion
            StatsSystem.ChangeDepletedValue(_target.ID, testStat, secondDelta); // Try to heal more than depleted
            Fix currentValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);

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
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            StatsSystem.ChangeDepletedValue(_target.ID, testStat, delta); // Try to deplete more than total
            Fix currentValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);

            // Assert
            Assert.That(currentValue, Is.EqualTo(testStat.DefaultMinValue)); // Should be clamped 
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

            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, additiveModifier, StatModifierType.Additive);
            StatsSystem.ChangeDepletedValue(_target.ID, testStat, delta);

            // Assert
            Fix currentValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);
            Assert.That(currentValue, Is.EqualTo(expectedMax));
            
            // Act
            StatsSystem.ChangeDepletedValue(_target.ID, testStat, second_delta);
            
            // Assert
            currentValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);
            Assert.That(currentValue, Is.EqualTo(expectedMin));
        }

        [Test]
        [Category("Stats")]
        public void ChangeDepletedValue_NonExistentEntity_DoesNotThrow()
        {
            // Arrange
            EntId nonExistentEntity = new EntId(999);

            // Act & Assert
            Assert.DoesNotThrow(() => StatsSystem.ChangeDepletedValue(nonExistentEntity, testStat, 5));
        }

        [Test]
        [Category("Stats")]
        public void ChangeDepletedValue_ZeroDelta_DoesNotChangeValue()
        {
            // Arrange
            Fix baseValue = 10;
            Fix delta = -4;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            StatsSystem.ChangeDepletedValue(_target.ID, testStat, delta);
            Fix afterFirsChange = StatsSystem.GetStatDepletedValue(_target.ID, testStat);
            
            StatsSystem.ChangeDepletedValue(_target.ID, testStat, 0);
            Fix afterSecondChange = StatsSystem.GetStatDepletedValue(_target.ID, testStat);

            // Assert
            Assert.That(afterFirsChange, Is.EqualTo(baseValue + delta));
            
            Assert.That(afterSecondChange, Is.EqualTo(afterFirsChange));
        }

        [Test]
        [Category("Stats")]
        public void OnDestroy_EntityWithModifiers_RemovesAllModifiersFromEntity()
        {
            // Arrange
            Fix baseValue = 1;
            Fix modifierValue = 2;
            Fix expectedValue = 3;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            
            StatModId modId = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifierValue,
                                                       StatModifierType.Additive);
            Fix valueBeforeDestroy = StatsSystem.GetStatValue(_target.ID, testStat);

            // Act
            _owner.Destroy();

            // Wait for end of this frame 
            ExecuteFrame(1);

            Fix valueAfterDestroy = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(valueBeforeDestroy, Is.EqualTo(expectedValue));
            Assert.That(valueAfterDestroy, Is.EqualTo(baseValue));
        }

        [Test]
        [Category("Stats")]
        public void OnDestroy_EntityWithIncomingModifiers_RemovesModifiersEffects()
        {
            // Arrange
            EntityA owner1 = new EntityA();
            EntityA owner2 = new EntityA();

            Fix modifier1 = 2;
            Fix modifier2 = 3;
            Fix expectedBeforeValue = testStat.DefaultBaseValue + modifier2 + modifier1;
            
            
            StatModId modId1 = StatsSystem.AddModifier(owner1.ID, _target.ID, testStat, modifier1, 
                                                       StatModifierType.Additive);
            StatModId modId2 = StatsSystem.AddModifier(owner2.ID, _target.ID, testStat, modifier2,
                                                       StatModifierType.Additive);
            
            Fix valueBeforeDestroy = StatsSystem.GetStatValue(_target.ID, testStat);

            // Act
            owner1.Destroy();

            // Wait for end of this frame 
            ExecuteFrame(1);
            Fix valueAfterFirstDestroy = StatsSystem.GetStatValue(_target.ID, testStat);
            
            // Act
            owner2.Destroy();

            // Wait for end of this frame 
            ExecuteFrame(1);
            Fix valueAfterSecondDestroy = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(valueBeforeDestroy, Is.EqualTo(expectedBeforeValue));
            
            Assert.That(valueAfterFirstDestroy, Is.EqualTo(testStat.DefaultBaseValue + modifier2));
            Assert.That(valueAfterSecondDestroy, Is.EqualTo(testStat.DefaultBaseValue));
        }

        [Test]
        [Category("Stats")]
        public void OnDestroy_TargetEntityDestroyed_CleansUpAllStats()
        {
            // Arrange
            Fix baseValue = 10;
            Fix modifierValue = 2;
            Fix expectedValue = baseValue * modifierValue;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            StatModId _ = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifierValue, 
                                                        StatModifierType.Multiplicative);

            Fix valueBeforeDestroy = StatsSystem.GetStatValue(_target.ID, testStat);
            
            // Act
            _target.Destroy();
            
            // Wait for end of this frame 
            ExecuteFrame(1);

            Fix valueAfterDestroy = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(valueBeforeDestroy, Is.EqualTo(expectedValue));
            Assert.That(valueAfterDestroy, Is.EqualTo(testStat.DefaultBaseValue));
        }

        [Test]
        [Category("Stats")]
        public void OnDestroy_MultipleEntitiesWithModifiers_CleanupCorrectly()
        {
            // Arrange
            EntityB target2 = new EntityB();
            EntityA owner2 = new EntityA();
            
            Fix modifier1 = 2;
            Fix modifier2 = 3;
            
            // Add modifiers to both targets
            StatModId modId1 = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifier1, 
                                                        StatModifierType.Additive);
            StatModId modId2 = StatsSystem.AddModifier(owner2.ID, target2.ID, testStat, modifier2,
                                                        StatModifierType.Additive);
            
            Fix target1ValueBefore = StatsSystem.GetStatValue(_target.ID, testStat);
            Fix target2ValueBefore = StatsSystem.GetStatValue(target2.ID, testStat);

            // Act
            _owner.Destroy();
            
            // Wait for end of this frame 
            ExecuteFrame(1);

            Fix target1ValueAfterOwner1Destroy = StatsSystem.GetStatValue(_target.ID, testStat);
            Fix target2ValueAfterOwner1Destroy = StatsSystem.GetStatValue(target2.ID, testStat);
            
            target2.Destroy();
            
            // Wait for end of this frame 
            ExecuteFrame(1);

            Fix target2ValueAfterDestroy = StatsSystem.GetStatValue(target2.ID, testStat);

            // Assert
            Assert.That(target1ValueAfterOwner1Destroy, Is.EqualTo(testStat.DefaultBaseValue));
            Assert.That(target2ValueAfterOwner1Destroy, Is.EqualTo(testStat.DefaultBaseValue + modifier2));
            Assert.That(target2ValueAfterDestroy, Is.EqualTo(testStat.DefaultBaseValue));

        }

        [Test]
        [Category("Stats")]
        public void OnDestroy_EntityWithDepletedStats_CleansUpCorrectly()
        {
            // Arrange
            Fix baseValue = 10;
            Fix delta = -4;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            StatsSystem.ChangeDepletedValue(_target.ID, testStat, delta);
            
            Fix valueBeforeDestroy = StatsSystem.GetStatDepletedValue(_target.ID, testStat);

            // Act
            _target.Destroy();
            
            // Wait for end of this frame 
            ExecuteFrame(1);
            
            Fix valueAfterDestroy = StatsSystem.GetStatDepletedValue(_target.ID, testStat);

            // Assert
            Assert.That(valueBeforeDestroy, Is.EqualTo(baseValue + delta));
            Assert.That(valueAfterDestroy, Is.EqualTo(testStat.DefaultBaseValue));
        }

        [Test]
        [Category("Stats")]
        [Category("EntityLifecycle")]
        public void EntityLifecycle_WhenOwnerDies_ModifiersAreRemovedFromTarget()
        {
            // Arrange
            Fix baseValue = 10;
            Fix modifierValue = 5;
            Fix expectedValue = baseValue * modifierValue;

            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            
            StatModId modId = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifierValue, StatModifierType.Multiplicative);
            Fix valueBeforeDeath = StatsSystem.GetStatValue(_target.ID, testStat);

            // Act
            _owner.Destroy();
            ExecuteFrame(1); // Wait for end of frame cleanup

            Fix valueAfterDeath = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(valueBeforeDeath, Is.EqualTo(expectedValue));
            Assert.That(valueAfterDeath, Is.EqualTo(baseValue));
        }

        [Test]
        [Category("Stats")]
        [Category("EntityLifecycle")]
        public void EntityLifecycle_WhenTargetDies_StatsAndModifiersAreCleanedUp()
        {
            // Arrange
            Fix modifierValue = 5;
            StatModId modId = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifierValue,
                                                      StatModifierType.Additive);

            Fix valueBeforeDeath = StatsSystem.GetStatValue(_target.ID, testStat);

            // Act
            _target.Destroy();
            ExecuteFrame(1); // Wait for end of frame cleanup

            Fix valueAfterDeath = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(valueBeforeDeath, Is.EqualTo(testStat.DefaultBaseValue + modifierValue) );
            Assert.That(valueAfterDeath, Is.EqualTo(testStat.DefaultBaseValue));
        }

        [Test]
        [Category("Stats")]
        [Category("EntityLifecycle")]
        public void EntityLifecycle_MultipleOwnersWhenOneDies_OnlyTheirModifiersAreRemoved()
        {
            // Arrange
            EntityA secondOwner = new EntityA();
            Fix baseValue = 10;
            Fix firstModifier = 5;
            Fix secondModifier = 3;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            StatModId modId1 = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, firstModifier, StatModifierType.Additive);
            StatModId modId2 = StatsSystem.AddModifier(secondOwner.ID, _target.ID, testStat, secondModifier, StatModifierType.Additive);

            Fix valueBeforeDeath = StatsSystem.GetStatValue(_target.ID, testStat);

            // Act
            _owner.Destroy();
            ExecuteFrame(1); // Wait for end of frame cleanup

            Fix valueAfterFirstDeath = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(valueBeforeDeath, Is.EqualTo(baseValue + firstModifier + secondModifier));
            Assert.That(valueAfterFirstDeath, Is.EqualTo(baseValue + secondModifier));

            // Cleanup
            secondOwner.Destroy();
            ExecuteFrame(1);
        }

        [Test]
        [Category("Stats")]
        [Category("EntityLifecycle")]
        public void EntityLifecycle_ChainedDeaths_ModifiersCleanupCorrectly()
        {
            // Arrange
            EntityA middleEntity = new EntityA();
            EntityB finalTarget = new EntityB();
            
            Fix baseValue = 10;
            Fix firstModifier = 5;
            Fix secondModifier = 3;
            
            StatsSystem.SetBaseValue(finalTarget.ID, testStat, baseValue);
            
            // Chain: _owner -> middleEntity -> finalTarget
            StatModId modId1 = StatsSystem.AddModifier(_owner.ID, finalTarget.ID, testStat, firstModifier, 
                                                       StatModifierType.Additive);
            StatModId modId2 = StatsSystem.AddModifier(middleEntity.ID, finalTarget.ID, testStat, secondModifier, 
                                                       StatModifierType.Additive);

            Fix initialValue = StatsSystem.GetStatValue(finalTarget.ID, testStat);

            // Act - Destroy entities in sequence
            _owner.Destroy();
            ExecuteFrame(1);
            Fix valueAfterFirstDeath = StatsSystem.GetStatValue(finalTarget.ID, testStat);
            
            middleEntity.Destroy();
            ExecuteFrame(1);
            Fix valueAfterSecondDeath = StatsSystem.GetStatValue(finalTarget.ID, testStat);

            // Assert
            Assert.That(initialValue, Is.EqualTo(baseValue + firstModifier + secondModifier));
            Assert.That(valueAfterFirstDeath, Is.EqualTo(baseValue + secondModifier));
            Assert.That(valueAfterSecondDeath, Is.EqualTo(baseValue));

            // Cleanup
            finalTarget.Destroy();
            ExecuteFrame(1);
        }

        [Test]
        [Category("Stats")]
        [Category("EntityLifecycle")]
        public void EntityLifecycle_SimultaneousDeaths_AllModifiersAreRemoved()
        {
            // Arrange
            EntityA owner2 = new EntityA();
            EntityA owner3 = new EntityA();
            
            Fix baseValue = 10;
            Fix mod1 = 2;
            Fix mod2 = 3;
            Fix mod3 = 4;


            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            
            StatModId modId1 = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, mod1, StatModifierType.Additive);
            StatModId modId2 = StatsSystem.AddModifier(owner2.ID, _target.ID, testStat, mod2, StatModifierType.Additive);
            StatModId modId3 = StatsSystem.AddModifier(owner3.ID, _target.ID, testStat, mod3, StatModifierType.Multiplicative);

            Fix valueBeforeDeaths = StatsSystem.GetStatValue(_target.ID, testStat);

            // Act - Destroy all owners in same frame
            _owner.Destroy();
            owner2.Destroy();
            owner3.Destroy();
            ExecuteFrame(1); // Single frame cleanup

            Fix valueAfterDeaths = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(valueBeforeDeaths, Is.EqualTo((baseValue + mod1 + mod2) * mod3)); // Base + all modifiers
            Assert.That(valueAfterDeaths, Is.EqualTo(baseValue));
        }

        [Test]
        [Category("Stats")]
        [Category("Modifiers")]
        public void GetModifierValue_ExistingModifier_ReturnsCorrectValue()
        {
            // Arrange
            Fix expectedValue = 5;

            // Act
            StatModId modId = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, expectedValue, StatModifierType.Additive);

            Fix actualValue = StatsSystem.GetModifierValue(modId);
            // Assert
            Assert.That(actualValue, Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("Stats")]
        [Category("Modifiers")]
        public void ChangeModifier_UpdatesValue_RecalculatesStats()
        {
            // Arrange
            Fix initialModValue = 5;
            Fix newModValue = 10;
            StatModId modId = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, initialModValue, 
                                                        StatModifierType.Additive);
            Fix statBeforeChange = StatsSystem.GetStatValue(_target.ID, testStat);

            // Act
            StatsSystem.ChangeModifier(modId, newModValue);
            Fix modifierValue = StatsSystem.GetModifierValue(modId);
            Fix statAfterChange = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(statBeforeChange, Is.EqualTo(testStat.DefaultBaseValue + initialModValue));
            Assert.That(modifierValue, Is.EqualTo(newModValue));
            Assert.That(statAfterChange, Is.EqualTo(statBeforeChange + (newModValue - initialModValue)));
        }

        [Test]
        [Category("Stats")]
        [Category("Modifiers")]
        public void GetModifiersOwnedBy_MultipleModifiers_ReturnsAllModifiers()
        {
            // Arrange
            EntityB target2 = new EntityB();
            StatModId modId1 = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, 5,
                                                            StatModifierType.Additive);
            StatModId modId2 = StatsSystem.AddModifier(_owner.ID, target2.ID, testStat, 10, 
                                                            StatModifierType.Multiplicative);
            StatModId modId3 = StatsSystem.AddModifier(_owner.ID, target2.ID, testStat, 3, 
                                                            StatModifierType.Additive);

            // Act
            IEnumerable<StatModId> modifiers = StatsSystem.GetModifiersOwnedBy(_owner.ID);

            // Assert
            Assert.That(modifiers, Has.Count.EqualTo(3));
            Assert.That(modifiers, Does.Contain(modId1));
            Assert.That(modifiers, Does.Contain(modId2));
            Assert.That(modifiers, Does.Contain(modId3));

            // Cleanup
            target2.Destroy();
        }

        [Test]
        [Category("Stats")]
        [Category("Modifiers")]
        public void GetModifiers_BetweenEntities_ReturnsOnlyRelevantModifiers()
        {
            // Arrange
            EntityB target2 = new EntityB();
            StatModId modId1_1 = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, 5, 
                                                            StatModifierType.Additive);
            StatModId modId1_2 = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, 5, 
                                                            StatModifierType.Additive);

            StatModId modId2 = StatsSystem.AddModifier(_owner.ID, target2.ID, testStat, 3, 
                                                            StatModifierType.Multiplicative);

            // Act
            IEnumerable<StatModId> modifiersToTarget1 = StatsSystem.GetModifiers(_owner.ID, _target.ID);
            IEnumerable<StatModId> modifiersToTarget2 = StatsSystem.GetModifiers(_owner.ID, target2.ID);

            // Assert
            int modifiersToTarget1Count = modifiersToTarget1.Count();
            int modifiersToTarget2Count = modifiersToTarget2.Count();
            
            
            Assert.That(modifiersToTarget1Count, Is.EqualTo(2));
            Assert.That(modifiersToTarget1, Does.Contain(modId1_1));
            Assert.That(modifiersToTarget1, Does.Contain(modId1_2));
            
            Assert.That(modifiersToTarget2Count, Is.EqualTo(1));
            Assert.That(modifiersToTarget2, Does.Contain(modId2));

            // Cleanup
            target2.Destroy();
        }

        [Test]
        [Category("Stats")]
        [Category("EntityLifecycle")]
        public void GetModifierValue_AfterOwnerDestroyed_ModifierNoLongerExists()
        {
            // Arrange
            Fix modValue = 5;
            StatModId modId = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modValue, 
                                                            StatModifierType.Additive);
            Fix initialValue = StatsSystem.GetModifierValue(modId);

            // Act
            _owner.Destroy();
            ExecuteFrame(1);
            Fix valueAfterDestroy = StatsSystem.GetModifierValue(modId);

            // Assert
            Assert.That(initialValue, Is.EqualTo(modValue));
            Assert.That(valueAfterDestroy, Is.EqualTo((Fix)0));
        }

        [Test]
        [Category("Stats")]
        [Category("EntityLifecycle")]
        public void GetModifiersOwnedBy_AfterEntityDestroyed_ReturnsEmpty()
        {
            // Arrange
            StatModId modId1 = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, 5, 
                                                            StatModifierType.Percentage);
            StatModId modId2 = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, 3,
                                                            StatModifierType.Multiplicative);
            
            IEnumerable<StatModId> modifiersBefore = StatsSystem.GetModifiersOwnedBy(_owner.ID);
            int numberModifiersBefore = modifiersBefore.Count();
            // Act
            _owner.Destroy();
            ExecuteFrame(1);
            IEnumerable<StatModId> modifiersAfter = StatsSystem.GetModifiersOwnedBy(_owner.ID);

            // Assert
            Assert.That(numberModifiersBefore, Is.EqualTo(2));
            Assert.That(modifiersAfter, Is.Empty);
        }

        [Test]
        [Category("Stats")]
        [Category("EntityLifecycle")]
        public void GetModifiers_AfterTargetDestroyed_ReturnsEmpty()
        {
            // Arrange
            StatModId modId = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, 5, 
                                                            StatModifierType.Percentage);
            IEnumerable<StatModId> modifiersBefore = StatsSystem.GetModifiers(_owner.ID, _target.ID);

            int numberModifiersBefore = modifiersBefore.Count();
            // Act
            _target.Destroy();
            ExecuteFrame(1);
            IEnumerable<StatModId> modifiersAfter = StatsSystem.GetModifiers(_owner.ID, _target.ID);

            // Assert
            Assert.That(numberModifiersBefore, Is.EqualTo(1));
            Assert.That(modifiersAfter, Is.Empty);
        }

        [Test]
        [Category("Stats")]
        [Category("EntityLifecycle")]
        public void ChangeModifier_AfterOwnerDestroyed_HasNoEffect()
        {
            // Arrange
            StatModId modId = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, 2, 
                                                        StatModifierType.Additive);
            StatModId modId2 = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, 1, 
                                                        StatModifierType.AdditivePostMultiplicative);
            StatModId modId3 = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, 4, 
                                                        StatModifierType.AdditivePostMultiplicative);
            StatModId modId4 = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, 3, 
                                                        StatModifierType.Multiplicative);                                                       
            Fix statBeforeDestroy = StatsSystem.GetStatValue(_target.ID, testStat);

            // Act
            _owner.Destroy();
            ExecuteFrame(1);
            StatsSystem.ChangeModifier(modId, 0);
            Fix statAfterChange = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(statAfterChange, Is.EqualTo(testStat.DefaultBaseValue));
        }

        [Test]
        [Category("Stats")]
        [Category("EntityLifecycle")]
        public void EntityDeath_WithModifiers_AllModifiersAreRemoved()
        {
            // Arrange
            Fix baseValue = 10;
            Fix modifierValue = 5;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            StatModId modId = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifierValue, StatModifierType.Additive);
            Fix valueBeforeDeath = StatsSystem.GetStatValue(_target.ID, testStat);

            // Act
            _target.Destroy();
            ExecuteFrame(1);

            // Assert
            Fix valueAfterDeath = StatsSystem.GetStatValue(_target.ID, testStat);
            IEnumerable<StatModId> remainingModifiers = StatsSystem.GetModifiers(_owner.ID, _target.ID);
            
            Assert.That(valueBeforeDeath, Is.EqualTo(baseValue + modifierValue));
            Assert.That(valueAfterDeath, Is.EqualTo(testStat.DefaultBaseValue));
            Assert.That(remainingModifiers, Is.Empty);
        }

        [Test]
        [Category("Stats")]
        [Category("EntityLifecycle")]
        public void EntityDeath_WithMultipleModifiers_AllModifiersAreRemoved()
        {
            // Arrange
            EntityA owner2 = new EntityA();
            EntityA owner3 = new EntityA();
            
            Fix baseValue = 10;
            Fix mod1Add = 2;
            Fix mod2Mult = 3;
            Fix mod3Perc = Fix.Ratio(1, 2); // 50%

            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            
            StatModId modId1 = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, mod1Add,
                                                             StatModifierType.Additive);
            StatModId modId2 = StatsSystem.AddModifier(owner2.ID, _target.ID, testStat, mod2Mult, 
                                                            StatModifierType.Multiplicative);
            StatModId modId3 = StatsSystem.AddModifier(owner3.ID, _target.ID, testStat, mod3Perc,
                                                             StatModifierType.Percentage);

            Fix valueBeforeDeath = StatsSystem.GetStatValue(_target.ID, testStat);

            // Act
            _target.Destroy();
            ExecuteFrame(1);

            // Assert
            Fix valueAfterDeath = StatsSystem.GetStatValue(_target.ID, testStat);
            IEnumerable<StatModId> remainingModifiersOwner1 = StatsSystem.GetModifiers(_owner.ID, _target.ID);
            IEnumerable<StatModId> remainingModifiersOwner2 = StatsSystem.GetModifiers(owner2.ID, _target.ID);
            IEnumerable<StatModId> remainingModifiersOwner3 = StatsSystem.GetModifiers(owner3.ID, _target.ID);
            
            Fix expectedValue = (baseValue + mod1Add) * ((Fix)1 + mod3Perc) * mod2Mult;
            Assert.That(valueBeforeDeath, Is.EqualTo(expectedValue));
            Assert.That(valueAfterDeath, Is.EqualTo(testStat.DefaultBaseValue));
            Assert.That(remainingModifiersOwner1, Is.Empty);
            Assert.That(remainingModifiersOwner2, Is.Empty);
            Assert.That(remainingModifiersOwner3, Is.Empty);

            // Cleanup
            owner2.Destroy();
            owner3.Destroy();
            ExecuteFrame(1);
        }

        [Test]
        [Category("Stats")]
        [Category("EntityLifecycle")]
        public void EntityDeath_WithComplexModifiers_AllEffectsAreRemoved()
        {
            // Arrange
            EntityA owner2 = new EntityA();
            
            Fix baseValue = 10;
            Fix additive = 2;
            Fix percentage = Fix.Ratio(1, 2); // 50%
            Fix multiplicative = 2;
            Fix postAdditive = 3;

            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            
            // Add different types of modifiers
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, additive, StatModifierType.Additive);
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, percentage, StatModifierType.Percentage);
            StatsSystem.AddModifier(owner2.ID, _target.ID, testStat, multiplicative, StatModifierType.Multiplicative);
            StatsSystem.AddModifier(owner2.ID, _target.ID, testStat, postAdditive, StatModifierType.AdditivePostMultiplicative);

            Fix valueBeforeDeath = StatsSystem.GetStatValue(_target.ID, testStat);

            // Act
            _target.Destroy();
            ExecuteFrame(1);

            // Assert
            Fix valueAfterDeath = StatsSystem.GetStatValue(_target.ID, testStat);
            Fix expectedBeforeDeath = ((baseValue + additive) * ((Fix)1 + percentage) * multiplicative) + postAdditive;
            
            Assert.That(valueBeforeDeath, Is.EqualTo(expectedBeforeDeath));
            Assert.That(valueAfterDeath, Is.EqualTo(testStat.DefaultBaseValue));
            Assert.That(StatsSystem.GetModifiers(_owner.ID, _target.ID), Is.Empty);
            Assert.That(StatsSystem.GetModifiers(owner2.ID, _target.ID), Is.Empty);

            // Cleanup
            owner2.Destroy();
            ExecuteFrame(1);
        }

        [Test]
        [Category("Stats")]
        [Category("EntityLifecycle")]
        public void EntityDeath_WithDepletedStats_ResetsToDefault()
        {
            // Arrange
            Fix baseValue = 10;
            Fix depletion = -4;
            Fix modifierValue = 5;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifierValue, StatModifierType.Additive);
            StatsSystem.ChangeDepletedValue(_target.ID, testStat, depletion);
            
            Fix depletedValueBefore = StatsSystem.GetStatDepletedValue(_target.ID, testStat);
            Fix totalValueBefore = StatsSystem.GetStatValue(_target.ID, testStat);

            // Act
            _target.Destroy();
            ExecuteFrame(1);

            // Assert
            Fix depletedValueAfter = StatsSystem.GetStatDepletedValue(_target.ID, testStat);
            Fix totalValueAfter = StatsSystem.GetStatValue(_target.ID, testStat);
            
            Assert.That(totalValueBefore, Is.EqualTo(baseValue + modifierValue));
            Assert.That(depletedValueBefore, Is.EqualTo(baseValue + modifierValue + depletion));
            Assert.That(totalValueAfter, Is.EqualTo(testStat.DefaultBaseValue));
            Assert.That(depletedValueAfter, Is.EqualTo(testStat.DefaultBaseValue));
        }

        [Test]
        [Category("Stats")]
        [Category("Performance")]
        public void Performance_AddingMultipleModifiers_MaintainsConsistentSpeed()
        {
            // Arrange
            const int numModifiers = 1000;
            List<EntityA> owners = new List<EntityA>();
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            
            // Act & Assert - Adding modifiers
            stopwatch.Start();
            for (int i = 0; i < numModifiers; i++)
            {
                EntityA owner = new EntityA();
                owners.Add(owner);
                Fix value = (i % 10);
                StatModId modId = StatsSystem.AddModifier(owner.ID, _target.ID, testStat, value, StatModifierType.Additive);
                
                if (i % 100 == 0)
                {
                    ExecuteFrame(1);
                    Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(100), 
                        "Performance degraded after adding {0} modifiers", i);
                }
            }
            stopwatch.Stop();

            // Cleanup
            foreach (EntityA owner in owners)
            {
                owner.Destroy();
            }
            ExecuteFrame(1);
        }

        [Test]
        [Category("Stats")]
        [Category("Performance")]
        public void Performance_MultipleEntitiesWithModifiers_HandlesFrameUpdatesEfficiently()
        {
            // Arrange
            const int numEntities = 100;
            const int modifiersPerEntity = 10;
            List<EntityA> targets = new List<EntityA>();
            List<EntityA> owners = new List<EntityA>();
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

            // Setup entities and modifiers
            for (int i = 0; i < numEntities; i++)
            {
                EntityA target = new EntityA();
                targets.Add(target);
                
                for (int j = 0; j < modifiersPerEntity; j++)
                {
                    EntityA owner = new EntityA();
                    owners.Add(owner);
                    StatsSystem.AddModifier(owner.ID, target.ID, testStat, (Fix)(j + 1), StatModifierType.Additive);
                }
            }

            // Act & Assert - Frame updates
            const int numFrames = 60; // Simulate 1 second at 60 FPS
            for (int frame = 0; frame < numFrames; frame++)
            {
                stopwatch.Restart();
                
                // Simulate some stat changes each frame
                for (int i = 0; i < numEntities; i++)
                {
                    if (i % 10 == frame % 10) // Spread operations across frames
                    {
                        StatsSystem.GetStatValue(targets[i].ID, testStat);
                    }
                    
                    if (i % 2 == frame % 2) // Spread operations across frames
                    {
                        StatsSystem.GetStatDepletedValue(targets[i].ID, testStat);
                    }

                    if (i % 5 == frame % 5) // Spread operations across frames
                    {
                        Fix depletedValue = StatsSystem.GetStatDepletedValue(targets[i].ID, testStat);
                        StatsSystem.ChangeDepletedValue(targets[i].ID, testStat, depletedValue - 1);
                    }
                }
                
                ExecuteFrame(1);
                stopwatch.Stop();
                
                Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(16), // 16ms = 60FPS
                    $"Frame {frame} took too long to process");
            }

            // Cleanup
            foreach (EntityA target in targets) target.Destroy();
            foreach (EntityA owner in owners) owner.Destroy();
            ExecuteFrame(1);
        }

        [Test]
        [Category("Stats")]
        [Category("Performance")]
        public void Performance_EntityDestructionWithManyModifiers_HandlesCleanupEfficiently()
        {
            // Arrange
            const int numTargets = 50;
            const int modifiersPerTarget = 20;
            List<EntityA> targets = new List<EntityA>();
            List<EntityA> owners = new List<EntityA>();
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

            // Setup
            for (int i = 0; i < numTargets; i++)
            {
                EntityA target = new EntityA();
                targets.Add(target);
                
                for (int j = 0; j < modifiersPerTarget; j++)
                {
                    EntityA owner = new EntityA();
                    owners.Add(owner);
                    StatsSystem.AddModifier(owner.ID, target.ID, testStat, (Fix)(j + 1), 
                        (StatModifierType)(j % 4)); // Use different modifier types
                }
            }

            // Act & Assert - Measure cleanup performance
            stopwatch.Start();
            
            for (int i = 0; i < numTargets; i++)
            {
                targets[i].Destroy();
                
                if (i % 10 == 0)
                {
                    ExecuteFrame(1);
                    Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(100), 
                        "Cleanup performance degraded after destroying {0} entities", i);
                }
            }
            
            ExecuteFrame(1);
            stopwatch.Stop();

            // Cleanup remaining entities
            foreach (EntityA owner in owners)
                owner.Destroy();
            ExecuteFrame(1);
        }

        [Test]
        [Category("Stats")]
        [Category("Performance")]
        public void Performance_ModifierUpdates_HandlesFrequentChangesEfficiently()
        {
            // Arrange
            const int numModifiers = 100;
            List<StatModId> modifiers = new List<StatModId>();
            List<EntityA> owners = new List<EntityA>();
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

            // Setup
            for (int i = 0; i < numModifiers; i++)
            {
                EntityA owner = new EntityA();
                owners.Add(owner);
                StatModId modId = StatsSystem.AddModifier(owner.ID, _target.ID, testStat, (Fix)1, StatModifierType.Additive);
                modifiers.Add(modId);
            }

            // Act & Assert - Measure update performance
            const int numFrames = 60;
            for (int frame = 0; frame < numFrames; frame++)
            {
                stopwatch.Restart();
                
                // Update some modifiers each frame
                for (int i = 0; i < numModifiers; i++)
                {
                    if (i % 10 == frame % 10) // Update 10% of modifiers each frame
                    {
                        Fix newValue = (Fix)(frame % 5);
                        StatsSystem.ChangeModifier(modifiers[i], newValue);
                    }
                }
                
                ExecuteFrame(1);
                stopwatch.Stop();
                
                Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(16), 
                    "Frame {0} took too long to process modifier updates", frame);
            }

            // Cleanup
            foreach (EntityA owner in owners) owner.Destroy();
            ExecuteFrame(1);
        }

        [Test]
        [Category("Stats")]
        public void SetBaseValue_WithoutResetDepletedValue_MaintainsDepletedProportion()
        {
            // Arrange
            Fix initialBase = 10;
            Fix deltaDepleted = -4;
            Fix newBase = 20;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, initialBase);

            StatsSystem.ChangeDepletedValue(_target.ID, testStat, deltaDepleted);
            Fix initialDepletedValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);

            // Act
            StatsSystem.SetBaseValue(_target.ID, testStat, newBase, false);

            // Assert
            Fix finalDepletedValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);
            Fix finalTotalValue = StatsSystem.GetStatValue(_target.ID, testStat);
            
            Assert.That(finalTotalValue, Is.EqualTo(newBase));
            Assert.That(finalDepletedValue, Is.EqualTo(newBase + deltaDepleted ));
        }

        [Test]
        [Category("Stats")]
        public void SetBaseValue_WithResetDepletedValue_ResetsToFull()
        {
            // Arrange
            Fix initialBase = 10;
            Fix depletion = -4;
            Fix newBase = 20;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, initialBase);
            StatsSystem.ChangeDepletedValue(_target.ID, testStat, depletion);

            // Act
            StatsSystem.SetBaseValue(_target.ID, testStat, newBase, true);

            // Assert
            Fix finalDepletedValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);
            Fix finalTotalValue = StatsSystem.GetStatValue(_target.ID, testStat);
            
            Assert.That(finalTotalValue, Is.EqualTo(newBase));
            Assert.That(finalDepletedValue, Is.EqualTo(newBase));
        }

        [Test]
        [Category("Stats")]
        public void SetBaseValue_WithModifiers_UpdatesAbsolutely()
        {
            // Arrange
            Fix initialBase = 10;
            Fix modifierValue = 5;

            Fix depletion = -6;
            Fix newBase = 20;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, initialBase);
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifierValue, StatModifierType.Additive);
            StatsSystem.ChangeDepletedValue(_target.ID, testStat, depletion); 
            // Depleted value should be (10 + 5) - 6 = 9

            // Act
            StatsSystem.SetBaseValue(_target.ID, testStat, newBase);
            // Depleted value should be (20 + 5) - 6 = 19

            // Assert
            Fix finalTotalValue = StatsSystem.GetStatValue(_target.ID, testStat);
            Fix finalDepletedValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);
            Fix expectedFinalStatValue = newBase + modifierValue;
            
            Assert.That(finalTotalValue, Is.EqualTo(expectedFinalStatValue));
            Assert.That(finalDepletedValue, Is.EqualTo(expectedFinalStatValue + depletion));
        }

        [Test]
        [Category("Stats")]
        public void SetDepletedValue_WithinValidRange_SetsCorrectly()
        {
            // Arrange
            Fix baseValue = 10;
            Fix depletedValue = 4;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            StatsSystem.SetDepletedValue(_target.ID, testStat, depletedValue);

            // Assert
            Fix currentValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);
            Assert.That(currentValue, Is.EqualTo(depletedValue));
        }

        [Test]
        [Category("Stats")]
        public void SetDepletedValue_AboveMaxValue_ClampsToMax()
        {
            // Arrange
            Fix baseValue = 10;
            Fix invalidDepletedValue = 15;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            StatsSystem.SetDepletedValue(_target.ID, testStat, invalidDepletedValue);

            // Assert
            Fix currentValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);
            Assert.That(currentValue, Is.EqualTo(baseValue));
        }

        [Test]
        [Category("Stats")]
        public void SetDepletedValue_BelowMinValue_ClampsToMin()
        {
            // Arrange
            Fix baseValue = 10;
            Fix invalidDepletedValue = -5;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            StatsSystem.SetDepletedValue(_target.ID, testStat, invalidDepletedValue);

            // Assert
            Fix currentValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);
            Assert.That(currentValue, Is.EqualTo(testStat.DefaultMinValue));
        }

        [Test]
        [Category("Stats")]
        public void SetDepletedValue_WithModifiers_HandlesCorrectly()
        {
            // Arrange
            Fix baseValue = 10;
            Fix modifierValue = 5;
            Fix depletedValue = 7;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifierValue, StatModifierType.Additive);

            // Act
            StatsSystem.SetDepletedValue(_target.ID, testStat, depletedValue);

            // Assert
            Fix currentValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);
            Fix totalValue = StatsSystem.GetStatValue(_target.ID, testStat);
            
            Assert.That(currentValue, Is.EqualTo(depletedValue));
            Assert.That(totalValue, Is.EqualTo(baseValue + modifierValue));
        }

        [Test]
        [Category("Stats")]
        public void ResetDepletedValueToMax_ResetsToCurrentMaxValue()
        {
            // Arrange
            Fix baseValue = 10;
            Fix modifierValue = 5;
            Fix depletion = 7;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifierValue, StatModifierType.Additive);
            StatsSystem.ChangeDepletedValue(_target.ID, testStat, -depletion);

            // Act
            StatsSystem.ResetDepletedValueToMax(_target.ID, testStat);

            // Assert
            Fix currentValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);
            Fix totalValue = StatsSystem.GetStatValue(_target.ID, testStat);
            
            Assert.That(currentValue, Is.EqualTo(totalValue));
            Assert.That(currentValue, Is.EqualTo(baseValue + modifierValue));
        }

        [Test]
        [Category("Stats")]
        public void ResetDepletedValueToMin_ResetsToMinValue()
        {
            // Arrange
            Fix baseValue = 10;
            Fix modifierValue = 5;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifierValue, StatModifierType.Additive);

            // Act
            StatsSystem.ResetDepletedValueToMin(_target.ID, testStat);

            // Assert
            Fix currentValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);
            Fix totalValue = StatsSystem.GetStatValue(_target.ID, testStat);
            
            Assert.That(currentValue, Is.EqualTo(testStat.DefaultMinValue));
            Assert.That(totalValue, Is.EqualTo(baseValue + modifierValue));
        }

        [Test]
        [Category("Stats")]
        public void ResetDepletedValueToMax_WithNoModifiers_ResetsToBaseValue()
        {
            // Arrange
            Fix baseValue = 10;
            Fix depletion = -4;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            StatsSystem.ChangeDepletedValue(_target.ID, testStat, depletion);

            // Act
            StatsSystem.ResetDepletedValueToMax(_target.ID, testStat);

            // Assert
            Fix currentValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);
            Assert.That(currentValue, Is.EqualTo(baseValue));
        }

        [Test]
        [Category("Stats")]
        public void ResetDepletedValueToMin_WithNoModifiers_ResetsToMinValue()
        {
            // Arrange
            Fix baseValue = 10;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            StatsSystem.ResetDepletedValueToMin(_target.ID, testStat);

            // Assert
            Fix currentValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);
            Assert.That(currentValue, Is.EqualTo(testStat.DefaultMinValue));
        }

        private class EntityA : BaseEntity { }

        private class EntityB : BaseEntity { }

    }
} 