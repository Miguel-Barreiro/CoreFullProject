using System.Collections.Generic;
using System.Linq;
using Core.Editor;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Model.Stats;
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
        [SerializeField] private StatConfig overflowTestStat;

        
        // private StatsSystem _statsSystem;
        private EntityA _owner;
        private EntityB _target;
        
        protected override void InstallTestSystems(IUnitTestInstaller installer) 
        {
            // installer.AddTestSystem<StatsSystem>(new StatsSystemImplementation());
        }

        protected override void ResetComponentContainers(DataContainersController dataController)
        {
        }

        [SetUp]
        public void SetUp()
        {
            _owner = new EntityA();
            _target = new EntityB();
            this.ExecuteFrame(1);
        }

        [TearDown]
        public void TearDown()
        {
            EntitiesContainer.DestroyEntity(_owner.ID);
            EntitiesContainer.DestroyEntity(_target.ID);
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

        [Test]
        [Category("Stats")]
        [Category("PermanentModifiers")]
        public void AddPermanentModifier_AdditiveType_CorrectlyModifiesStat()
        {
            // Arrange
            Fix modifierValue = 2;

            // Act
            StatsSystem.AddPermanentModifier(_target.ID, testStat, modifierValue, 
                                                       StatModifierType.Additive);
            Fix resultValue = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(resultValue, Is.EqualTo(testStat.DefaultBaseValue + modifierValue));
        }

        [Test]
        [Category("Stats")]
        [Category("PermanentModifiers")]
        public void AddPermanentModifier_MultipleModifiers_AllApply()
        {
            // Arrange
            Fix modifier1 = 2;
            Fix modifier2 = 3;

            // Act
            StatsSystem.AddPermanentModifier(_target.ID, testStat, modifier1, StatModifierType.Additive);
            StatsSystem.AddPermanentModifier(_target.ID, testStat, modifier2, StatModifierType.Additive);
            Fix resultValue = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(resultValue, Is.EqualTo(testStat.DefaultBaseValue + modifier1 + modifier2));
        }

        [Test]
        [Category("Stats")]
        [Category("PermanentModifiers")]
        public void AddPermanentModifier_PercentageType_CalculatesCorrectly()
        {
            // Arrange
            Fix baseValue = testStat.DefaultBaseValue;
            Fix percentageIncrease = Fix.Ratio(1, 4); // 25% increase

            // Act
            StatsSystem.AddPermanentModifier(_target.ID, testStat, percentageIncrease, 
                                                       StatModifierType.Percentage);
            Fix resultValue = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Fix expectedValue = baseValue + (baseValue * percentageIncrease);
            Assert.That(resultValue, Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("Stats")]
        [Category("PermanentModifiers")]
        public void AddPermanentModifier_WithRegularModifiers_BothApply()
        {
            // Arrange
            Fix permanentModifier = 3;
            Fix regularModifier = 2;

            // Act
            StatsSystem.AddPermanentModifier(_target.ID, testStat, permanentModifier, 
                                                       StatModifierType.Additive);
            StatModId regModId = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, regularModifier, 
                                                       StatModifierType.Additive);
            Fix resultValue = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(resultValue, Is.EqualTo(testStat.DefaultBaseValue + permanentModifier + regularModifier));
        }

        [Test]
        [Category("Stats")]
        [Category("PermanentModifiers")]
        public void AddPermanentModifier_RemoveModifier_OnlyRemovesRegularModifier()
        {
            // Arrange
            Fix permanentModifier = 3;
            Fix regularModifier = 2;

            StatsSystem.AddPermanentModifier(_target.ID, testStat, permanentModifier, 
                                                       StatModifierType.Additive);
            StatModId regModId = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, regularModifier, 
                                                       StatModifierType.Additive);
            
            Fix valueBeforeRemoval = StatsSystem.GetStatValue(_target.ID, testStat);

            // Act
            StatsSystem.RemoveModifier(regModId);
            Fix valueAfterRegularRemoval = StatsSystem.GetStatValue(_target.ID, testStat);
            
            // Assert
            Assert.That(valueBeforeRemoval, Is.EqualTo(testStat.DefaultBaseValue + permanentModifier + regularModifier));
            Assert.That(valueAfterRegularRemoval, Is.EqualTo(testStat.DefaultBaseValue + permanentModifier));
        }

        [Test]
        [Category("Stats")]
        [Category("PermanentModifiers")]
        public void AddPermanentModifier_EntityDestroyed_ModifiersRemain()
        {
            // Arrange
            Fix permanentModifier = 3;
            Fix regularModifier = 2;

            StatsSystem.AddPermanentModifier(_target.ID, testStat, permanentModifier, 
                                                       StatModifierType.Additive);
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, regularModifier, 
                                                       StatModifierType.Additive);
            
            Fix valueBeforeDestroy = StatsSystem.GetStatValue(_target.ID, testStat);

            // Act
            _owner.Destroy();
            ExecuteFrame(1); // Wait for end of frame cleanup

            Fix valueAfterDestroy = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(valueBeforeDestroy, Is.EqualTo(testStat.DefaultBaseValue + permanentModifier + regularModifier));
            Assert.That(valueAfterDestroy, Is.EqualTo(testStat.DefaultBaseValue + permanentModifier));
        }

        [Test]
        [Category("Stats")]
        [Category("PermanentModifiers")]
        public void AddPermanentModifier_TargetDestroyed_ModifiersRemoved()
        {
            // Arrange
            Fix permanentModifier = 3;

            StatsSystem.AddPermanentModifier(_target.ID, testStat, permanentModifier, 
                                                       StatModifierType.Additive);
            
            Fix valueBeforeDestroy = StatsSystem.GetStatValue(_target.ID, testStat);

            // Act
            _target.Destroy();
            ExecuteFrame(1); // Wait for end of frame cleanup

            Fix valueAfterDestroy = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(valueBeforeDestroy, Is.EqualTo(testStat.DefaultBaseValue + permanentModifier));
            Assert.That(valueAfterDestroy, Is.EqualTo(testStat.DefaultBaseValue));
        }


        [Test]
        [Category("Stats")]
        [Category("PermanentModifiers")]
        public void AddPermanentModifier_DifferentTypes_CalculatesCorrectly()
        {
            // Arrange
            Fix baseValue = 10;
            Fix additive = 2;
            Fix percentage = Fix.Ratio(1, 2); // 50%
            Fix multiplicative = 2;
            Fix postAdditive = 3;
            
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // 10 + 2
            //  12 * 1.5
            //     18 * 2
            //         36 + 3
            //             39
                
            // Act
            StatsSystem.AddPermanentModifier(_target.ID, testStat, additive, StatModifierType.Additive);
            StatsSystem.AddPermanentModifier(_target.ID, testStat, percentage, StatModifierType.Percentage);
            StatsSystem.AddPermanentModifier(_target.ID, testStat, multiplicative, StatModifierType.Multiplicative);
            StatsSystem.AddPermanentModifier(_target.ID, testStat, postAdditive, StatModifierType.AdditivePostMultiplicative);

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
        [Category("Overflow")]
        public void ChangeDepletedValue_WithOverflow_ExceedsMaximum()
        {
            // Arrange
            Fix baseValue = 10;
            Fix maxValue = 15;
            Fix overflowAmount = 10;
            StatsSystem.SetBaseValue(_target.ID, overflowTestStat, baseValue);

            // Act
            StatsSystem.ChangeDepletedValue(_target.ID, overflowTestStat, overflowAmount);
            Fix resultValue = StatsSystem.GetStatDepletedValue(_target.ID, overflowTestStat);

            // Assert
            Assert.That(resultValue, Is.EqualTo(baseValue + overflowAmount));
            Assert.That(resultValue, Is.GreaterThan(maxValue));
        }

        [Test]
        [Category("Stats")]
        [Category("Overflow")]
        public void ChangeDepletedValue_WithoutOverflow_ClampsToMaximum()
        {
            // Arrange
            Fix baseValue = 10;
            Fix maxValue = baseValue;
            Fix overflowAmount = 10;
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue, true);

            // Act
            StatsSystem.ChangeDepletedValue(_target.ID, testStat, overflowAmount);
            Fix resultValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);

            // Assert
            Assert.That(resultValue, Is.EqualTo(maxValue));
        }

        [Test]
        [Category("Stats")]
        [Category("Overflow")]
        public void SetDepletedValue_WithOverflow_ExceedsMaximum()
        {
            // Arrange
            Fix baseValue = 10;
            Fix maxValue = baseValue;
            Fix overflowValue = 20;
            StatsSystem.SetBaseValue(_target.ID, overflowTestStat, baseValue);

            // Act
            StatsSystem.SetDepletedValue(_target.ID, overflowTestStat, overflowValue);
            Fix resultValue = StatsSystem.GetStatDepletedValue(_target.ID, overflowTestStat);

            // Assert
            Assert.That(resultValue, Is.EqualTo(overflowValue));
            Assert.That(resultValue, Is.GreaterThan(maxValue));
        }

        [Test]
        [Category("Stats")]
        [Category("Overflow")]
        public void SetDepletedValue_WithoutOverflow_ClampsToMaximum()
        {
            // Arrange
            Fix baseValue = 10;
            Fix maxValue = baseValue;
            Fix overflowValue = 20;
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            StatsSystem.SetDepletedValue(_target.ID, testStat, overflowValue);
            Fix resultValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);

            // Assert
            Assert.That(resultValue, Is.EqualTo(maxValue));
        }

        [Test]
        [Category("Stats")]
        [Category("Overflow")]
        public void ChangeDepletedValue_WithOverflowAndModifiers_RespectsOverflow()
        {
            // Arrange
            Fix baseValue = 10;
            Fix maxValue = 15;
            Fix overflowAmount = 10;
            Fix modifierValue = 5;
            StatsSystem.SetBaseValue(_target.ID, overflowTestStat, baseValue);
            StatsSystem.AddModifier(_owner.ID, _target.ID, overflowTestStat, modifierValue, StatModifierType.Additive);

            // Act
            StatsSystem.ChangeDepletedValue(_target.ID, overflowTestStat, overflowAmount);
            Fix resultValue = StatsSystem.GetStatDepletedValue(_target.ID, overflowTestStat);

            // Assert
            Fix expectedValue = baseValue + modifierValue + overflowAmount;
            Assert.That(resultValue, Is.EqualTo(expectedValue));
            Assert.That(resultValue, Is.GreaterThan(maxValue + modifierValue));
        }

        [Test]
        [Category("Stats")]
        [Category("Overflow")]
        public void ChangeDepletedValue_WithoutOverflowAndModifiers_ClampsToModifiedMaximum()
        {
            // Arrange
            Fix baseValue = 10;
            Fix modifierValue = 5;
            Fix maxValue = baseValue + modifierValue;
            Fix overflowAmount = 10;
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifierValue, StatModifierType.Additive);

            // Act
            StatsSystem.ChangeDepletedValue(_target.ID, testStat, overflowAmount);
            Fix resultValue = StatsSystem.GetStatDepletedValue(_target.ID, testStat);

            // Assert
            Assert.That(resultValue, Is.EqualTo(maxValue));
        }

        [Test]
        [Category("Stats")]
        public void RemoveAllModifiersFrom_MultipleModifiers_RemovesAllModifiers()
        {
            // Arrange
            Fix baseValue = 10;
            Fix modifier1 = 2;
            Fix modifier2 = 3;
            Fix modifier3 = 4;

            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);
            
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifier1, StatModifierType.Additive);
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifier2, StatModifierType.Percentage);
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifier3, StatModifierType.Multiplicative);

            // Act
            Fix valueBeforeRemoval = StatsSystem.GetStatValue(_target.ID, testStat);
            StatsSystem.RemoveAllModifiersFrom(_owner.ID, _target.ID, testStat);
            Fix valueAfterRemoval = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(valueAfterRemoval, Is.EqualTo(baseValue));
            Assert.That(valueBeforeRemoval, Is.Not.EqualTo(baseValue));
        }

        [Test]
        [Category("Stats")]
        public void RemoveAllModifiersFrom_MultipleStats_OnlyRemovesFromSpecifiedStat()
        {
            // Arrange
            Fix baseValue1 = 10;
            Fix baseValue2 = 20;
            Fix modifier1 = 2;
            Fix modifier2 = 3;

            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue1);
            StatsSystem.SetBaseValue(_target.ID, overflowTestStat, baseValue2);
            
            StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifier1, StatModifierType.Additive);
            StatsSystem.AddModifier(_owner.ID, _target.ID, overflowTestStat, modifier2, StatModifierType.Additive);

            // Act
            Fix value1BeforeRemoval = StatsSystem.GetStatValue(_target.ID, testStat);
            Fix value2BeforeRemoval = StatsSystem.GetStatValue(_target.ID, overflowTestStat);
            
            StatsSystem.RemoveAllModifiersFrom(_owner.ID, _target.ID, testStat);
            
            Fix value1AfterRemoval = StatsSystem.GetStatValue(_target.ID, testStat);
            Fix value2AfterRemoval = StatsSystem.GetStatValue(_target.ID, overflowTestStat);

            // Assert
            Assert.That(value1AfterRemoval, Is.EqualTo(baseValue1));
            Assert.That(value2AfterRemoval, Is.EqualTo(value2BeforeRemoval));
            Assert.That(value1BeforeRemoval, Is.Not.EqualTo(baseValue1));
        }

        [Test]
        [Category("Stats")]
        public void RemoveAllModifiersFrom_NoModifiers_NoEffect()
        {
            // Arrange
            Fix baseValue = 10;
            StatsSystem.SetBaseValue(_target.ID, testStat, baseValue);

            // Act
            Fix valueBeforeRemoval = StatsSystem.GetStatValue(_target.ID, testStat);
            StatsSystem.RemoveAllModifiersFrom(_owner.ID, _target.ID, testStat);
            Fix valueAfterRemoval = StatsSystem.GetStatValue(_target.ID, testStat);

            // Assert
            Assert.That(valueAfterRemoval, Is.EqualTo(baseValue));
            Assert.That(valueBeforeRemoval, Is.EqualTo(baseValue));
        }

        [Test]
        [Category("Stats")]
        public void RemoveAllModifiersFrom_NonExistentEntity_DoesNotThrow()
        {
            // Arrange
            EntId nonExistentEntity = new EntId(999);

            // Act & Assert
            Assert.DoesNotThrow(() => StatsSystem.RemoveAllModifiersFrom(_owner.ID, nonExistentEntity, testStat));
        }

        [Test]
        [Category("Stats")]
        public void CopyStats_CopiesAllStatsAndModifiers()
        {
            // Arrange
            var source = new EntityA();
            var target = new EntityB();

            Fix baseValue = 10;
            Fix depletedValue = 5;
            Fix modifierValue = 3;
            Fix modifierValueMult = 2;
            Fix permanentValue = 2;
            Fix expectedValue = (baseValue + modifierValue + permanentValue) * modifierValueMult;

            StatsSystem.SetBaseValue(source.ID, testStat, baseValue);
            StatModId modId = StatsSystem.AddModifier(_owner.ID, source.ID, testStat, modifierValue, StatModifierType.Additive);
            StatModId modIdMult = StatsSystem.AddModifier(_owner.ID, source.ID, testStat, modifierValueMult, StatModifierType.Multiplicative);
            StatsSystem.AddPermanentModifier(source.ID, testStat, permanentValue, StatModifierType.Additive);
            StatsSystem.SetDepletedValue(source.ID, testStat, depletedValue);

            // Act
            StatsSystem.CopyStats(source.ID, target.ID);

            // Assert
            
            Assert.That(StatsSystem.GetStatValue(target.ID, testStat), Is.EqualTo(expectedValue));
            Assert.That(StatsSystem.GetStatDepletedValue(target.ID, testStat), Is.EqualTo(depletedValue));

            // Modifiers should exist and be owned by the same owner
            var targetModifiers = StatsSystem.GetModifiers(_owner.ID, target.ID);
            List<StatModId> statModIds = new List<StatModId>(targetModifiers);

            Assert.That(statModIds.Count(), Is.EqualTo(2));

            foreach (StatModId statModId in statModIds)
                StatsSystem.RemoveModifier(statModId);

            Fix newExpectedValue = baseValue + permanentValue;
            Assert.That(StatsSystem.GetStatValue(target.ID, testStat), Is.EqualTo(newExpectedValue));
            

            // Cleanup
            source.Destroy();
            target.Destroy();
        }

        [Test]
        [Category("Stats")]
        public void CopyStats_MultipleModifierTypes_CopiesAllTypesCorrectly()
        {
            // Arrange
            var source = new EntityA();
            var target = new EntityB();
            var owner2 = new EntityA();

            Fix baseValue = 10;
            Fix additiveMod = 2;
            Fix percentageMod = Fix.Ratio(1, 2); // 50%
            Fix multiplicativeMod = 3;
            Fix postAdditiveMod = 1;

            StatsSystem.SetBaseValue(source.ID, testStat, baseValue);
            StatsSystem.AddModifier(_owner.ID, source.ID, testStat, additiveMod, StatModifierType.Additive);
            StatsSystem.AddModifier(owner2.ID, source.ID, testStat, percentageMod, StatModifierType.Percentage);
            StatsSystem.AddModifier(_owner.ID, source.ID, testStat, multiplicativeMod, StatModifierType.Multiplicative);
            StatsSystem.AddModifier(owner2.ID, source.ID, testStat, postAdditiveMod, StatModifierType.AdditivePostMultiplicative);

            // Act
            StatsSystem.CopyStats(source.ID, target.ID);

            // Assert
            Fix expectedValue = ((baseValue + additiveMod) * ((Fix)1 + percentageMod) * multiplicativeMod) + postAdditiveMod;
            Assert.That(StatsSystem.GetStatValue(target.ID, testStat), Is.EqualTo(expectedValue));

            // Check that modifiers from both owners were copied
            var owner1Modifiers = StatsSystem.GetModifiers(_owner.ID, target.ID);
            var owner2Modifiers = StatsSystem.GetModifiers(owner2.ID, target.ID);
            Assert.That(owner1Modifiers.Count(), Is.EqualTo(2)); // additive + multiplicative
            Assert.That(owner2Modifiers.Count(), Is.EqualTo(2)); // percentage + postAdditive

            // Cleanup
            source.Destroy();
            target.Destroy();
            owner2.Destroy();
            ExecuteFrame(1);
        }

        [Test]
        [Category("Stats")]
        public void CopyStats_MultipleStats_CopiesAllStats()
        {
            // Arrange
            var source = new EntityA();
            var target = new EntityB();

            Fix baseValue1 = 10;
            Fix baseValue2 = 20;
            Fix modifier1 = 3;
            Fix modifier2 = 5;

            StatsSystem.SetBaseValue(source.ID, testStat, baseValue1);
            StatsSystem.SetBaseValue(source.ID, overflowTestStat, baseValue2);
            StatsSystem.AddModifier(_owner.ID, source.ID, testStat, modifier1, StatModifierType.Additive);
            StatsSystem.AddModifier(_owner.ID, source.ID, overflowTestStat, modifier2, StatModifierType.Additive);

            // Act
            StatsSystem.CopyStats(source.ID, target.ID);

            // Assert
            Assert.That(StatsSystem.GetStatValue(target.ID, testStat), Is.EqualTo(baseValue1 + modifier1));
            Assert.That(StatsSystem.GetStatValue(target.ID, overflowTestStat), Is.EqualTo(baseValue2 + modifier2));

            // Cleanup
            source.Destroy();
            target.Destroy();
            ExecuteFrame(1);
        }

        [Test]
        [Category("Stats")]
        public void CopyStats_WithPermanentModifiers_CopiesPermanentModifiers()
        {
            // Arrange
            var source = new EntityA();
            var target = new EntityB();

            Fix baseValue = 10;
            Fix permanentAdditive = 5;
            Fix permanentMultiplicative = 2;
            Fix permanentPercentage = Fix.Ratio(1, 4); // 25%

            StatsSystem.SetBaseValue(source.ID, testStat, baseValue);
            StatsSystem.AddPermanentModifier(source.ID, testStat, permanentAdditive, StatModifierType.Additive);
            StatsSystem.AddPermanentModifier(source.ID, testStat, permanentMultiplicative, StatModifierType.Multiplicative);
            StatsSystem.AddPermanentModifier(source.ID, testStat, permanentPercentage, StatModifierType.Percentage);

            // Act
            StatsSystem.CopyStats(source.ID, target.ID);

            // Assert
            Fix expectedValue = (baseValue + permanentAdditive) * ((Fix)1 + permanentPercentage) * permanentMultiplicative;
            Assert.That(StatsSystem.GetStatValue(target.ID, testStat), Is.EqualTo(expectedValue));

            // Cleanup
            source.Destroy();
            target.Destroy();
            ExecuteFrame(1);
        }

        [Test]
        [Category("Stats")]
        public void CopyStats_EmptySource_NoEffectOnTarget()
        {
            // Arrange
            var source = new EntityA();
            var target = new EntityB();

            Fix targetBaseValue = 15;
            StatsSystem.SetBaseValue(target.ID, testStat, targetBaseValue);

            // Act
            StatsSystem.CopyStats(source.ID, target.ID);

            // Assert
            Assert.That(StatsSystem.GetStatValue(target.ID, testStat), Is.EqualTo(targetBaseValue));

            // Cleanup
            source.Destroy();
            target.Destroy();
            ExecuteFrame(1);
        }

        [Test]
        [Category("Stats")]
        public void CopyStats_NonExistentSource_NoEffectOnTarget()
        {
            // Arrange
            var target = new EntityB();
            EntId nonExistentSource = new EntId(999);

            Fix targetBaseValue = 15;
            StatsSystem.SetBaseValue(target.ID, testStat, targetBaseValue);

            // Act & Assert
            Assert.DoesNotThrow(() => StatsSystem.CopyStats(nonExistentSource, target.ID));
            Assert.That(StatsSystem.GetStatValue(target.ID, testStat), Is.EqualTo(targetBaseValue));

            // Cleanup
            target.Destroy();
            ExecuteFrame(1);
        }

        [Test]
        [Category("Stats")]
        public void CopyStats_WithDepletedValues_CopiesDepletedValues()
        {
            // Arrange
            var source = new EntityA();
            var target = new EntityB();

            Fix baseValue = 10;
            Fix depletedValue = 3;
            Fix modifierValue = 2;

            StatsSystem.SetBaseValue(source.ID, testStat, baseValue);
            StatsSystem.SetDepletedValue(source.ID, testStat, depletedValue);
            StatsSystem.AddModifier(_owner.ID, source.ID, testStat, modifierValue, StatModifierType.Additive);

            // Act
            StatsSystem.CopyStats(source.ID, target.ID);

            // Assert
            Fix expectedTotalValue = baseValue + modifierValue;
            Fix expectedDepletedValue = depletedValue + modifierValue;
            Assert.That(StatsSystem.GetStatValue(target.ID, testStat), Is.EqualTo(expectedTotalValue));
            Assert.That(StatsSystem.GetStatDepletedValue(target.ID, testStat), Is.EqualTo(expectedDepletedValue));

            // Cleanup
            source.Destroy();
            target.Destroy();
            ExecuteFrame(1);
        }

        [Test]
        [Category("Stats")]
        public void CopyStats_OverwritesExistingTargetStats()
        {
            // Arrange
            var source = new EntityA();
            var target = new EntityB();

            Fix sourceBaseValue = 10;
            Fix targetBaseValue = 20;
            Fix modifierValue = 3;

            StatsSystem.SetBaseValue(source.ID, testStat, sourceBaseValue);
            StatsSystem.SetBaseValue(target.ID, testStat, targetBaseValue);
            StatsSystem.AddModifier(_owner.ID, source.ID, testStat, modifierValue, StatModifierType.Additive);

            // Act
            StatsSystem.CopyStats(source.ID, target.ID);

            // Assert
            Fix expectedValue = sourceBaseValue + modifierValue;
            Assert.That(StatsSystem.GetStatValue(target.ID, testStat), Is.EqualTo(expectedValue));

            // Cleanup
            source.Destroy();
            target.Destroy();
            ExecuteFrame(1);
        }

        private class EntityA : Entity{ }

        private class EntityB : Entity { }

    }
} 