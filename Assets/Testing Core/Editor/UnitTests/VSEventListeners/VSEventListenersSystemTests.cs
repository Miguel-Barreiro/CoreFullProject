using System;
using System.Collections.Generic;
using System.Reflection;
using Core.Editor;
using Core.Events;
using Core.Model;
using Core.Model.ModelSystems;
using Core.VSEngine;
using Core.VSEngine.Nodes.Events;
using Core.VSEngine.Systems;
using NUnit.Framework;
using UnityEngine;
using XNode;
using Zenject;
using Object = UnityEngine.Object;

namespace Testing_Core.Editor.UnitTests.VSEventListeners
{
    // ── Test event types ─────────────────────────────────────────────────────

    internal sealed class VSListenerTestGlobalEvent : Event<VSListenerTestGlobalEvent>
    {
        public override void Execute() { }
    }

    internal sealed class VSListenerOtherGlobalEvent : Event<VSListenerOtherGlobalEvent>
    {
        public override void Execute() { }
    }

    internal sealed class VSListenerTestEntityEvent : EntityEvent<VSListenerTestEntityEvent> { }

    internal sealed class VSListenerOtherEntityEvent : EntityEvent<VSListenerOtherEntityEvent> { }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [TestFixture]
    public class VSEventListenersSystemTests : UnitTest
    {
        // ── Fake engine that records Run() calls ─────────────────────────────
        private sealed class FakeVSBaseEngine : VSBaseEngine
        {
            public readonly List<(BaseEventListenNode node, EntId ownerId)> GlobalRuns = new();
            public readonly List<(BaseEventListenNode node, EntId ownerId)> EntityRuns = new();

            protected override void RunInternalEvent(NodeGraph nodeGraph, BaseEventListenNode eventListenNode, BaseEvent vsEvent, EntId ownerId)
                => GlobalRuns.Add((eventListenNode, ownerId));

            protected override void RunInternalEntityEvent(NodeGraph nodeGraph, BaseEventListenNode eventListenNode, 
                                                           BaseEntityEvent coreEvent, EntId ownerId)
                => EntityRuns.Add((eventListenNode, ownerId));
            
        }

        // ── Fields ────────────────────────────────────────────────────────────
        [Inject] private VSEventListenersSystem system = null!;
        [Inject] private VSEventListenersEntity _entity = null!;

        // Non-serialized: created once in InstallTestSystems, reset per-test in SetUp
        private FakeVSBaseEngine _fakeBaseEngine;

        private readonly List<ScriptableObject> createdNodes = new();

        // private static readonly FieldInfo VSEngineField =
        //     typeof(VSEventListenersSystem).GetField("VSEngineCore",
        //         BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly FieldInfo ActiveField =
            typeof(BaseEventListenNode).GetField("active",
                BindingFlags.NonPublic | BindingFlags.Instance)!;

        // ── UnitTest overrides ─────────────────────────────────────────────────
        protected override void InstallTestSystems(IUnitTestInstaller installer)
        {
            _fakeBaseEngine = new FakeVSBaseEngine();
            installer.AddTestSystem<VSBaseEngine>(_fakeBaseEngine);
            installer.AddTestSystem(new VSEventListenersEntity());
            installer.AddTestSystem(new VSEventListenersSystem());
        }

        protected override void ResetComponentContainers(DataContainersController dataController) { }

        // ── Setup / Teardown ──────────────────────────────────────────────────
        [SetUp]
        public void SetUp()
        {
            // Guarantee wiring in case injection ordering differs
            // VSEngineField.SetValue(system, _fakeBaseEngine);

            _fakeBaseEngine.GlobalRuns.Clear();
            _fakeBaseEngine.EntityRuns.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            this.system.ClearAllListeners();
            
            // // Clear all listener maps stored in the entity's component data
            // ref VSEventListenersData data = ref _entity.GetData();
            // data.preGlobal.Clear();     data.defaultGlobal.Clear();    data.postGlobal.Clear();
            // data.preOwner.Clear();      data.defaultOwner.Clear();     data.postOwner.Clear();
            // data.preAll.Clear();        data.defaultAll.Clear();       data.postAll.Clear();
            // data.preDynamic.Clear();    data.defaultDynamic.Clear();   data.postDynamic.Clear();
            // data.ownerIndex.Clear();

            foreach (var node in createdNodes)
                if (node != null) DestroyImmediate(node);
            createdNodes.Clear();
        }

        // ── Node factories ────────────────────────────────────────────────────
        private EventListenNode MakeGlobalNode(VSEventTiming timing = VSEventTiming.Default)
        {
            var node = ScriptableObject.CreateInstance<EventListenNode>();
            createdNodes.Add(node);
            node.Timing = timing;
            return node;
        }

        private EntityEventListenNode MakeEntityNode(
            VSEventTiming timing = VSEventTiming.Default,
            VSEntityListenTarget target = VSEntityListenTarget.Owner)
        {
            var node = ScriptableObject.CreateInstance<EntityEventListenNode>();
            createdNodes.Add(node);
            node.Timing = timing;
            node.ListenTarget = target;
            return node;
        }

        // ═════════════════════════════════════════════════════════════════════
        // GLOBAL EVENTS — Timing
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void GlobalEvent_DefaultTiming_FiredByExecuteEvent()
        {
            var owner = new EntId(1);
            var node = MakeGlobalNode(VSEventTiming.Default);
            system.AddListener(owner, typeof(VSListenerTestGlobalEvent), node);

            system.ExecuteEvent(new VSListenerTestGlobalEvent());

            Assert.That(_fakeBaseEngine.GlobalRuns.Count, Is.EqualTo(1));
        }

        [Test]
        public void GlobalEvent_PreTiming_FiredByExecutePreEvent()
        {
            var owner = new EntId(1);
            var node = MakeGlobalNode(VSEventTiming.Pre);
            system.AddListener(owner, typeof(VSListenerTestGlobalEvent), node);

            system.ExecutePreEvent(new VSListenerTestGlobalEvent());

            Assert.That(_fakeBaseEngine.GlobalRuns.Count, Is.EqualTo(1));
        }

        [Test]
        public void GlobalEvent_PostTiming_FiredByExecutePostEvent()
        {
            var owner = new EntId(1);
            var node = MakeGlobalNode(VSEventTiming.Post);
            system.AddListener(owner, typeof(VSListenerTestGlobalEvent), node);

            system.ExecutePostEvent(new VSListenerTestGlobalEvent());

            Assert.That(_fakeBaseEngine.GlobalRuns.Count, Is.EqualTo(1));
        }

        [Test]
        public void GlobalEvent_ExecuteEvent_DoesNotFire_PreOrPostListeners()
        {
            var owner = new EntId(1);
            system.AddListener(owner, typeof(VSListenerTestGlobalEvent), MakeGlobalNode(VSEventTiming.Pre));
            system.AddListener(owner, typeof(VSListenerTestGlobalEvent), MakeGlobalNode(VSEventTiming.Post));

            system.ExecuteEvent(new VSListenerTestGlobalEvent());

            Assert.That(_fakeBaseEngine.GlobalRuns.Count, Is.EqualTo(0));
        }

        [Test]
        public void GlobalEvent_ExecutePreEvent_DoesNotFire_DefaultOrPostListeners()
        {
            var owner = new EntId(1);
            system.AddListener(owner, typeof(VSListenerTestGlobalEvent), MakeGlobalNode(VSEventTiming.Default));
            system.AddListener(owner, typeof(VSListenerTestGlobalEvent), MakeGlobalNode(VSEventTiming.Post));

            system.ExecutePreEvent(new VSListenerTestGlobalEvent());

            Assert.That(_fakeBaseEngine.GlobalRuns.Count, Is.EqualTo(0));
        }

        [Test]
        public void GlobalEvent_ExecutePostEvent_DoesNotFire_DefaultOrPreListeners()
        {
            var owner = new EntId(1);
            system.AddListener(owner, typeof(VSListenerTestGlobalEvent), MakeGlobalNode(VSEventTiming.Default));
            system.AddListener(owner, typeof(VSListenerTestGlobalEvent), MakeGlobalNode(VSEventTiming.Pre));

            system.ExecutePostEvent(new VSListenerTestGlobalEvent());

            Assert.That(_fakeBaseEngine.GlobalRuns.Count, Is.EqualTo(0));
        }

        [Test]
        public void GlobalEvent_AllThreeTimings_EachFiredByCorrectHook()
        {
            var owner = new EntId(1);
            system.AddListener(owner, typeof(VSListenerTestGlobalEvent), MakeGlobalNode(VSEventTiming.Pre));
            system.AddListener(owner, typeof(VSListenerTestGlobalEvent), MakeGlobalNode(VSEventTiming.Default));
            system.AddListener(owner, typeof(VSListenerTestGlobalEvent), MakeGlobalNode(VSEventTiming.Post));

            var ev = new VSListenerTestGlobalEvent();
            system.ExecutePreEvent(ev);
            system.ExecuteEvent(ev);
            system.ExecutePostEvent(ev);

            Assert.That(_fakeBaseEngine.GlobalRuns.Count, Is.EqualTo(3));
        }

        // ═════════════════════════════════════════════════════════════════════
        // GLOBAL EVENTS — Removal, isolation, inactive
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void GlobalEvent_RemoveListener_NodeNotFiredAfterRemoval()
        {
            var owner = new EntId(1);
            var node = MakeGlobalNode();
            system.AddListener(owner, typeof(VSListenerTestGlobalEvent), node);
            system.RemoveListener(owner, typeof(VSListenerTestGlobalEvent), node);

            system.ExecuteEvent(new VSListenerTestGlobalEvent());

            Assert.That(_fakeBaseEngine.GlobalRuns.Count, Is.EqualTo(0));
        }

        [Test]
        public void GlobalEvent_OnlyMatchingEventType_IsFired()
        {
            var owner = new EntId(1);
            system.AddListener(owner, typeof(VSListenerTestGlobalEvent), MakeGlobalNode());
            system.AddListener(owner, typeof(VSListenerOtherGlobalEvent), MakeGlobalNode());

            system.ExecuteEvent(new VSListenerTestGlobalEvent());

            Assert.That(_fakeBaseEngine.GlobalRuns.Count, Is.EqualTo(1));
        }

        [Test]
        public void GlobalEvent_MultipleOwners_AllFired()
        {
            system.AddListener(new EntId(1), typeof(VSListenerTestGlobalEvent), MakeGlobalNode());
            system.AddListener(new EntId(2), typeof(VSListenerTestGlobalEvent), MakeGlobalNode());
            system.AddListener(new EntId(3), typeof(VSListenerTestGlobalEvent), MakeGlobalNode());

            system.ExecuteEvent(new VSListenerTestGlobalEvent());

            Assert.That(_fakeBaseEngine.GlobalRuns.Count, Is.EqualTo(3));
        }

        [Test]
        public void GlobalEvent_InactiveNode_NotFired()
        {
            var owner = new EntId(1);
            var node = MakeGlobalNode();
            ActiveField.SetValue(node, false);
            system.AddListener(owner, typeof(VSListenerTestGlobalEvent), node);

            system.ExecuteEvent(new VSListenerTestGlobalEvent());

            Assert.That(_fakeBaseEngine.GlobalRuns.Count, Is.EqualTo(0));
        }

        // ═════════════════════════════════════════════════════════════════════
        // ENTITY EVENTS — Owner mode
        // ═════════════════════════════════════════════════════════════════════

        // NOTE: new EntityEvent<T>() produces EntityID == EntId.Invalid.
        // Registering a node with owner == EntId.Invalid exercises the Owner lookup path
        // without needing to set EntityID externally (it is internal to the Core assembly).

        [Test]
        public void EntityEvent_Owner_DefaultTiming_FiresForMatchingEntity()
        {
            var owner = EntId.Invalid; // matches ev.EntityID (default)
            var node = MakeEntityNode(VSEventTiming.Default, VSEntityListenTarget.Owner);
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), node);

            system.ExecuteEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(1));
        }

        [Test]
        public void EntityEvent_Owner_PreTiming_FiredByExecutePreEntityEvent()
        {
            var owner = EntId.Invalid;
            var node = MakeEntityNode(VSEventTiming.Pre, VSEntityListenTarget.Owner);
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), node);

            system.ExecutePreEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(1));
        }

        [Test]
        public void EntityEvent_Owner_PostTiming_FiredByExecutePostEntityEvent()
        {
            var owner = EntId.Invalid;
            var node = MakeEntityNode(VSEventTiming.Post, VSEntityListenTarget.Owner);
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), node);

            system.ExecutePostEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(1));
        }

        [Test]
        public void EntityEvent_Owner_DoesNotFireForDifferentEntity()
        {
            // Register for EntId(42); ev.EntityID = EntId.Invalid (0) ≠ EntId(42)
            var node = MakeEntityNode(VSEventTiming.Default, VSEntityListenTarget.Owner);
            system.AddListener(new EntId(42), typeof(VSListenerTestEntityEvent), node);

            system.ExecuteEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(0));
        }

        [Test]
        public void EntityEvent_Owner_DefaultTiming_IsolatedFromPreAndPost()
        {
            var owner = EntId.Invalid;
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), MakeEntityNode(VSEventTiming.Pre, VSEntityListenTarget.Owner));
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), MakeEntityNode(VSEventTiming.Post, VSEntityListenTarget.Owner));

            system.ExecuteEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(0));
        }

        [Test]
        public void EntityEvent_Owner_PreTiming_IsolatedFromDefaultAndPost()
        {
            var owner = EntId.Invalid;
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), MakeEntityNode(VSEventTiming.Default, VSEntityListenTarget.Owner));
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), MakeEntityNode(VSEventTiming.Post, VSEntityListenTarget.Owner));

            system.ExecutePreEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(0));
        }

        [Test]
        public void EntityEvent_Owner_PostTiming_IsolatedFromDefaultAndPre()
        {
            var owner = EntId.Invalid;
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), MakeEntityNode(VSEventTiming.Default, VSEntityListenTarget.Owner));
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), MakeEntityNode(VSEventTiming.Pre, VSEntityListenTarget.Owner));

            system.ExecutePostEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(0));
        }

        // ═════════════════════════════════════════════════════════════════════
        // ENTITY EVENTS — All mode
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void EntityEvent_All_DefaultTiming_FiresRegardlessOfEntity()
        {
            // All mode fires even when owner EntId differs from ev.EntityID
            var owner = new EntId(99);
            var node = MakeEntityNode(VSEventTiming.Default, VSEntityListenTarget.All);
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), node);

            system.ExecuteEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(1));
        }

        [Test]
        public void EntityEvent_All_PreTiming_FiredByExecutePreEntityEvent()
        {
            var owner = new EntId(99);
            var node = MakeEntityNode(VSEventTiming.Pre, VSEntityListenTarget.All);
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), node);

            system.ExecutePreEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(1));
        }

        [Test]
        public void EntityEvent_All_PostTiming_FiredByExecutePostEntityEvent()
        {
            var owner = new EntId(99);
            var node = MakeEntityNode(VSEventTiming.Post, VSEntityListenTarget.All);
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), node);

            system.ExecutePostEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(1));
        }

        [Test]
        public void EntityEvent_All_DefaultTiming_IsolatedFromPreAndPost()
        {
            var owner = new EntId(99);
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), MakeEntityNode(VSEventTiming.Pre, VSEntityListenTarget.All));
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), MakeEntityNode(VSEventTiming.Post, VSEntityListenTarget.All));

            system.ExecuteEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(0));
        }

        [Test]
        public void EntityEvent_All_MultipleOwners_AllFired()
        {
            system.AddListener(new EntId(1), typeof(VSListenerTestEntityEvent), MakeEntityNode(target: VSEntityListenTarget.All));
            system.AddListener(new EntId(2), typeof(VSListenerTestEntityEvent), MakeEntityNode(target: VSEntityListenTarget.All));

            system.ExecuteEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(2));
        }

        // ═════════════════════════════════════════════════════════════════════
        // ENTITY EVENTS — Dynamic mode
        // ═════════════════════════════════════════════════════════════════════

        // NOTE: GetDynamicTargetEntity() returns EntId.Invalid when no "Target" port is connected.
        // new EntityEvent<T>() also has EntityID == EntId.Invalid, so the Dynamic filter passes.

        [Test]
        public void EntityEvent_Dynamic_DefaultTiming_FiresWhenTargetEntityMatches()
        {
            var owner = new EntId(1);
            var node = MakeEntityNode(VSEventTiming.Default, VSEntityListenTarget.Dynamic);
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), node);

            system.ExecuteEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(1));
        }

        [Test]
        public void EntityEvent_Dynamic_PreTiming_FiredByExecutePreEntityEvent()
        {
            var owner = new EntId(1);
            var node = MakeEntityNode(VSEventTiming.Pre, VSEntityListenTarget.Dynamic);
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), node);

            system.ExecutePreEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(1));
        }

        [Test]
        public void EntityEvent_Dynamic_PostTiming_FiredByExecutePostEntityEvent()
        {
            var owner = new EntId(1);
            var node = MakeEntityNode(VSEventTiming.Post, VSEntityListenTarget.Dynamic);
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), node);

            system.ExecutePostEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(1));
        }

        [Test]
        public void EntityEvent_Dynamic_DefaultTiming_IsolatedFromPreAndPost()
        {
            var owner = new EntId(1);
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), MakeEntityNode(VSEventTiming.Pre, VSEntityListenTarget.Dynamic));
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), MakeEntityNode(VSEventTiming.Post, VSEntityListenTarget.Dynamic));

            system.ExecuteEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(0));
        }

        // ═════════════════════════════════════════════════════════════════════
        // ENTITY EVENTS — Removal & type isolation
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void EntityEvent_RemoveListener_NodeNotFiredAfterRemoval()
        {
            var owner = EntId.Invalid;
            var node = MakeEntityNode(VSEventTiming.Default, VSEntityListenTarget.Owner);
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), node);
            system.RemoveListener(owner, typeof(VSListenerTestEntityEvent), node);

            system.ExecuteEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(0));
        }

        [Test]
        public void EntityEvent_OnlyMatchingEventType_IsFired()
        {
            var owner = new EntId(1);
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), MakeEntityNode(target: VSEntityListenTarget.All));
            system.AddListener(owner, typeof(VSListenerOtherEntityEvent), MakeEntityNode(target: VSEntityListenTarget.All));

            system.ExecuteEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(1));
        }

        // ═════════════════════════════════════════════════════════════════════
        // OnDestroyEntity
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void OnDestroyEntity_RemovesAllListeners_AcrossAllTimingsAndMaps()
        {
            var owner = EntId.Invalid;
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), MakeEntityNode(VSEventTiming.Pre, VSEntityListenTarget.Owner));
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), MakeEntityNode(VSEventTiming.Default, VSEntityListenTarget.Owner));
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), MakeEntityNode(VSEventTiming.Post, VSEntityListenTarget.Owner));
            system.AddListener(owner, typeof(VSListenerTestEntityEvent), MakeEntityNode(VSEventTiming.Default, VSEntityListenTarget.All));
            system.AddListener(owner, typeof(VSListenerTestGlobalEvent), MakeGlobalNode(VSEventTiming.Default));

            system.OnDestroyEntity(owner);

            system.ExecutePreEntityEvent(new VSListenerTestEntityEvent());
            system.ExecuteEntityEvent(new VSListenerTestEntityEvent());
            system.ExecutePostEntityEvent(new VSListenerTestEntityEvent());
            system.ExecuteEvent(new VSListenerTestGlobalEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(0));
            Assert.That(_fakeBaseEngine.GlobalRuns.Count, Is.EqualTo(0));
        }

        [Test]
        public void OnDestroyEntity_DoesNotRemoveListenersOfOtherOwners()
        {
            // owner2 == EntId.Invalid so ev.EntityID matches the Owner map lookup
            var owner1 = new EntId(99);
            var owner2 = EntId.Invalid;
            system.AddListener(owner1, typeof(VSListenerTestEntityEvent), MakeEntityNode(target: VSEntityListenTarget.Owner));
            system.AddListener(owner2, typeof(VSListenerTestEntityEvent), MakeEntityNode(target: VSEntityListenTarget.Owner));

            system.OnDestroyEntity(owner1);

            system.ExecuteEntityEvent(new VSListenerTestEntityEvent());

            Assert.That(_fakeBaseEngine.EntityRuns.Count, Is.EqualTo(1));
        }

        [Test]
        public void OnDestroyEntity_CalledTwice_DoesNotThrow()
        {
            var owner = new EntId(1);
            system.AddListener(owner, typeof(VSListenerTestGlobalEvent), MakeGlobalNode());

            system.OnDestroyEntity(owner);

            Assert.DoesNotThrow(() => system.OnDestroyEntity(owner));
        }
    }
}
