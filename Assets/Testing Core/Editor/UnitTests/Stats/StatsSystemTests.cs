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
            Fix modifierValue = (Fix)2;

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
            Fix modifier1 = (Fix)2;
            Fix modifier2 = (Fix)3;

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
            Fix multiplicative = (Fix)2;    // x2
            Fix postAdditive = (Fix)3;      // +3 after multiplication

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
            Fix additive = (Fix)2;          // +2
            Fix percentage = Fix.Ratio(1, 2);   // +50%
            Fix multiplicative = (Fix)2;     // x2
            Fix postAdditive = (Fix)1;      // +1

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
            Fix baseValue = (Fix)5;
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
            Fix afterFirstchange = StatsSystem.GetStatDepletedValue(_target.ID, testStat);
            
            StatsSystem.ChangeDepletedValue(_target.ID, testStat, 0);
            Fix afterSecondChange = StatsSystem.GetStatDepletedValue(_target.ID, testStat);

            // Assert
            Assert.That(afterFirstchange, Is.EqualTo(baseValue + delta));
            
            Assert.That(afterSecondChange, Is.EqualTo(afterFirstchange));
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
            StatModId modId = StatsSystem.AddModifier(_owner.ID, _target.ID, testStat, modifierValue, 
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

        public class EntityA : BaseEntity { }
        public class EntityB : BaseEntity { }

    }
} 