#nullable enable

using System;
using Core.Editor;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Model.Stats;
using Core.Model.Time;
using Core.Utils;
using FixedPointy;
using NUnit.Framework;
using UnityEngine;
using Zenject;

namespace Testing_Core.Editor.UnitTests.Timer
{
    public class TimerSystemTests : UnitTest
    {
        private MockEntity _entity;
        
        [SerializeField] private StatConfig TestStat;

        [Inject] private readonly TimerSystem TimerSystem = null!;
        [Inject] private readonly StatsSystem StatsSystem = null!;
        [Inject] private readonly EntitiesContainer EntitiesContainer = null!;
        
        protected override void InstallTestSystems(IUnitTestInstaller installer) 
        {
            // installer.AddTestSystem<ITimerSystem>(new TimerSystemImplementation());
            // installer.AddTestSystem<TimerModel>(new TimerModel());
            // installer.AddTestSystem<StatsSystem>(new StatsSystemImplementation());
        }

        protected override void ResetComponentContainers(DataContainersController dataController)
        {
        }


        [SetUp]
        public void Setup()
        {
            _entity = new MockEntity();
        }

        [TearDown]
        public void TearDown()
        {
            _entity.Destroy();
            EntitiesContainer.Reset();
            StatsSystem.Reset();
        }
        
        
        [Test]
        public void SetTimer_WithExpirationMs_CreatesTimer()
        {
            // Arrange
            string timerId = "testTimer";
            float expirationMs = 1000;
            
            // Act
            TimerSystem.SetTimer(_entity.ID, timerId, expirationMs, false, false);
            
            // Assert
            Assert.IsTrue(TimerSystem.HasTimer(_entity.ID, timerId));
            OperationResult<float> result = TimerSystem.GetMillisecondsLeft(_entity.ID, timerId);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(expirationMs, result.Result);
        }

        [Test]
        public void SetTimer_WithStatConfig_CreatesTimer()
        {
            // Arrange
            string timerId = "testTimer";
            
            Fix statValue = StatsSystem.GetStatValue(_entity.ID, TestStat);
            
            // Act
            TimerSystem.SetTimer(_entity.ID, timerId, TestStat, false, false);
            
            // Assert
            Assert.IsTrue(TimerSystem.HasTimer(_entity.ID, timerId));
            OperationResult<float> result = TimerSystem.GetMillisecondsLeft(_entity.ID, timerId);
            
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual((double)statValue, result.Result);
        }

        [Test]
        public void RemoveTimer_RemovesExistingTimer()
        {
            // Arrange
            string timerId = "testTimer";
            TimerSystem.SetTimer(_entity.ID, timerId, 1000, false, false);
            
            // Act
            TimerSystem.RemoveTimer(_entity.ID, timerId);
            
            // Assert
            Assert.IsFalse(TimerSystem.HasTimer(_entity.ID, timerId));
        }

        [Test]
        public void ResetTimer_ResetsTimerProgress()
        {
            // Arrange
            string timerId = "testTimer";
            float expirationMs = 1000;
            TimerSystem.SetTimer(_entity.ID, timerId, expirationMs, false, false);
            
            // Simulate time passing
            ExecuteFrameMs(500);
            
            // Verify time has passed
            OperationResult<float> beforeReset = TimerSystem.GetMillisecondsLeft(_entity.ID, timerId);
            Assert.IsTrue(beforeReset.IsSuccess);
            Assert.AreEqual(500, beforeReset.Result);
            
            // Act
            TimerSystem.ResetTimer(_entity.ID, timerId);
            
            // Assert
            OperationResult<float> afterReset = TimerSystem.GetMillisecondsLeft(_entity.ID, timerId);
            Assert.IsTrue(afterReset.IsSuccess);
            Assert.AreEqual(expirationMs, afterReset.Result);
        }

        [Test]
        public void Update_TriggersOnFinishListener_WhenTimerExpires()
        {
            // Arrange
            string timerId = "testTimer";
            float expirationMs = 1000;
            bool listenerCalled = false;
            
            TimerSystem.SetTimer(_entity.ID, timerId, expirationMs, false, false);
            TimerSystem.AddOnFinishListener(_entity.ID, timerId, (entId) => {
                listenerCalled = true;
                Assert.AreEqual(_entity.ID, entId);
            });
            
            // Act
            ExecuteFrameMs(1000);
            
            // Assert
            Assert.IsTrue(listenerCalled);
        }

        [Test]
        public void Update_AutoResetTimer_ResetsAfterExpiration()
        {
            // Arrange
            string timerId = "testTimer";
            float expirationMs = 1000;
            
            TimerSystem.SetTimer(_entity.ID, timerId, expirationMs, true, false);
            
            
            // Act
            ExecuteFrameMs(1500);
            
            // Assert
            OperationResult<float> result = TimerSystem.GetMillisecondsLeft(_entity.ID, timerId);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(500, result.Result);
        }

        [Test]
        public void Update_NonAutoResetTimer_StopsAfterExpiration()
        {
            // Arrange
            string timerId = "testTimer";
            float expirationMs = 1000;
            
            TimerSystem.SetTimer(_entity.ID, timerId, expirationMs, false, false);
            
            // Act
            ExecuteFrameMs(1500);
            
            // Assert
            OperationResult<float> result = TimerSystem.GetMillisecondsLeft(_entity.ID, timerId);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(0, result.Result);
        }

        [Test]
        public void RemoveOnFinishListener_RemovesListener()
        {
            // Arrange
            string timerId = "testTimer";
            float expirationMs = 1000;
            bool listenerCalled = false;
            Action<EntId> listener = (id) => { listenerCalled = true; };
            
            TimerSystem.SetTimer(_entity.ID, timerId, expirationMs, false, false);
            TimerSystem.AddOnFinishListener(_entity.ID, timerId, listener);
            TimerSystem.RemoveOnFinishListener(_entity.ID, timerId, listener);
            
            // Act
            ExecuteFrameMs(1000);
            
            // Assert
            Assert.IsFalse(listenerCalled);
        }

        [Test]
        public void GetMillisecondsLeft_ReturnsFailure_WhenTimerDoesNotExist()
        {
            // Act
            OperationResult<float> result = TimerSystem.GetMillisecondsLeft(_entity.ID, "nonExistentTimer");
            
            // Assert
            Assert.IsFalse(result.IsSuccess);
        }

        [Test]
        public void OnDestroy_RemovesAllTimersForEntity()
        {
            // Arrange
            string timerId = "testTimer";
            TimerSystem.SetTimer(_entity.ID, timerId, 1000, false, false);
            
            // Act
            _entity.Destroy();
            ExecuteFrameMs(1);
            
            // Assert
            Assert.IsFalse(TimerSystem.HasTimer(_entity.ID, timerId));
        }
        
        
        private class MockEntity : Entity{ }
    }
} 