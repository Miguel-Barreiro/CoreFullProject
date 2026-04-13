# Plan: EventListenNode & EntityEventListenNode — Core Event Integration

## Context
The VSEngine was built around a custom `VSEventBase` hierarchy. The Core framework uses `Event<T>` (global, pooled, `BaseEvent`) and `EntityEvent<T>` (entity-scoped, `BaseEntityEvent`) as its actual event types. The current `EventListenNode<T : VSEventBase>` doesn't work with these. The goal is to replace it with two non-generic, runtime type-selector nodes — each with an inspector dropdown and retained field-bridge output ports.

---

## Files to Create
- `Assets/Core/Runtime/VSEngine/Core/EntityEventListenNode.cs`
- `Assets/Core/Editor/VSEngine/EventListenNodeEditor.cs`
- `Assets/Core/Editor/VSEngine/EntityEventListenNodeEditor.cs`

## Files to Modify
- `Assets/Core/Runtime/VSEngine/Core/EventListenNode.cs` — rewrite generic → non-generic
- `Assets/Core/Runtime/VSEngine/Core/EventNodeUtils.cs` — add non-generic helpers
- `Assets/Core/Runtime/VSEngine/Systems/VSEventListenersSystem.cs` — implement
- `Assets/Core/Runtime/VSEngine/Core/VSExecutionControl.cs` — add `CoreEvent` to interface + impl
- `Assets/Core/Runtime/VSEngine/Core/VSNodeBase.cs` — add `GetCoreEvent<T>()` helper
- `Assets/Core/Runtime/VSEngine/Core/VSEngineCore.cs` — add overload for core event execution

---

## Step 1 — `EventNodeUtils`: non-generic helpers

Add two new methods alongside the existing generic ones:

```csharp
// Port building from a runtime Type (no VSEvent<T> wrapper assumption)
internal static void CreateFieldPorts(Type eventType, List<VSFieldPort> vsFieldPorts, bool isInput)
// Scans eventType.GetFields() for [VSField]. All ports use FieldOrigin.Event.

// Reading a field value from an object (no generic constraint)
internal static OperationResult<object> ReadFromObject(
    string portName, Dictionary<string, VSFieldPort> cache, object eventObj)
// Uses FieldInfo.GetValue(eventObj) via the stored FieldName on VSFieldPort.
```

> `GetEventDataType<T>()` and the `VSEvent<T>` unwrapping are only used by the old generic paths — keep them, don't remove.

---

## Step 2 — Execution context: add `CoreEvent`

**`VSExecutionControl.cs`** — in `IVSExecutionControl`:
```csharp
public BaseEvent? CoreEvent { get; }
public BaseEntityEvent? CoreEvent { get; }
```
In `VSExecutionControl`:
```csharp
private BaseEvent? coreEventObject;
public BaseEvent? CoreEvent => coreEventObject;

private BaseEntityEvent? coreEntityEventObject;
public BaseEntityEvent? CoreEntityEvent => coreEntityEventObject;

private EntId ownerId;

// Overloaded Start for core events:
public void Start(VSEngineCore vsEngine, BaseEvent coreEvent, EntId ownerId, ExecutableNode? startNode)
{
    this.vsEngine = vsEngine;
    this.coreEventObject = coreEvent;
    this.vsEvent = null!;   // not used by core-event nodes
    this.ownerId = ownerId;
    ...
}

// Overloaded Start for core events:
public void Start(VSEngineCore vsEngine, BaseEntityEvent coreEntityEvent, EntId ownerId, ExecutableNode? startNode)
{
    this.vsEngine = vsEngine;
    this.coreEventObject = coreEvent;
    this.vsEvent = null!;   // not used by core-event nodes
    this.ownerId = ownerId;
    ...
}
```

```




**`VSEngineCore.cs`** — add a parallel `RunInternal` overload:
```csharp
protected virtual void RunInternal(NodeGraph nodeGraph,
    BaseEventListenNode eventListenNode,
    BaseEntityEvent coreEvent,
    EntId ownerId, 
    VSExecutionControl vsExecutionControl)
{
    vsExecutionControl.Start(this, coreEvent, ownerId, eventListenNode);
    // same execution loop, no IsPropagating check (core events have no propagation)
    ...
}

protected virtual void RunInternal(NodeGraph nodeGraph,
    BaseEventListenNode eventListenNode,
    BaseEvent coreEvent,
    EntId ownerId, 
    VSExecutionControl vsExecutionControl)
{
    vsExecutionControl.Start(this, coreEvent, ownerId, eventListenNode);
    // same execution loop, no IsPropagating check (core events have no propagation)
    ...
}

```

**`VSNodeBase.cs`** — add helper alongside `GetEvent<T>()`:
```csharp
protected BaseEvent? GetCoreEvent() => ExecutionControl.CoreEvent;

protected BaseEntityEvent? GetEntityCoreEvent<TEvent>() => ExecutionControl.CoreEntityEvent;
```

---

## Step 3 — Rewrite `EventListenNode` (for `BaseEvent`)

Replace the generic class entirely. Keep the same file path.

Also define this enum (e.g. in `EventNodeUtils.cs` or its own file):

```csharp
public enum VSEventTiming { Pre, Default, Post }
```

```csharp
[Node.NodeTint("#194d33")]
[Node.CreateNodeMenu("Miguel/events/EventListenNode")]
[Serializable]
public class EventListenNode : BaseEventListenNode, IValueNode
{
    [SerializeField, HideInInspector] private SerializedType selectedEventType;
    [SerializeField, HideInInspector] private List<VSFieldPort> vsFieldPorts = new();
    [SerializeField] public VSEventTiming Timing = VSEventTiming.Default;
    
    private Dictionary<string, VSFieldPort>? fieldCache;

    public Type? EventType => SerializedTypeUtils.GetParsedType(selectedEventType);

    public OperationResult<object> GetValue(string portName)
    {
        BaseEvent? ev = GetCoreEvent();
        if (ev == null) return OperationResult<object>.Failure($"No BaseEvent found in {name}");
        if(fieldCache == null)
        {
            fieldCache = new(); 
            EventNodeUtils.BuildFieldCache(fieldCache, vsFieldPorts);
        }
        return EventNodeUtils.ReadFromObject(portName, fieldCache, ev);
    }

    public override void Register(Type _, EntId ownerId)   // ignore passed type, use stored
        => base.Register(EventType!, ownerId);
    public override void DeRegister(Type _, EntId ownerId)
        => base.DeRegister(EventType!, ownerId);

#if UNITY_EDITOR
    private void BuildDynamicPorts()
    {
        Type? t = EventType; if (t == null) return;
        EventNodeUtils.CreateFieldPorts(t, vsFieldPorts, false);
        EventNodeUtils.AddDynamicPorts(this, vsFieldPorts);
    }
    public override void OnBeforeSerialize() { BuildDynamicPorts(); base.OnBeforeSerialize(); }
#endif
}
```

> **Note:** Remove `OnTestingEventNode` (the old generic concrete subclass) once this is done — it relied on `EventListenNode<TestingEvent>`.

---

## Step 4 — Create `EntityEventListenNode` (for `BaseEntityEvent`)

Also define this enum (e.g. alongside `VSEventTiming`):

```csharp
public enum VSEntityListenTarget
{
    Owner,    // fires only when ev.EntityID == owner (default)
    Parent,   // not implemented yet — reserved
    All,      // fires for any entity's event
    Dynamic,  // fires when ev.EntityID matches the value of the "Target" input port
}
```

```csharp
[Node.NodeTint("#1a3352")]
[Node.CreateNodeMenu("Miguel/events/EntityEventListenNode")]
[Serializable]
public class EntityEventListenNode : BaseEventListenNode, IValueNode
{
    [SerializeField, HideInInspector] private SerializedType selectedEventType;
    [SerializeField, HideInInspector] private List<VSFieldPort> vsFieldPorts = new();
    [SerializeField] public VSEventTiming Timing = VSEventTiming.Default;
    [SerializeField] public VSEntityListenTarget ListenTarget = VSEntityListenTarget.Owner;
    private Dictionary<string, VSFieldPort>? fieldCache;

    public Type? EventType => SerializedTypeUtils.GetParsedType(selectedEventType);

    // For Dynamic mode: read the EntId from the connected "Target" input port.
    public EntId GetDynamicTargetEntity() => GetInputValue<EntId>("Target");

    public OperationResult<object> GetValue(string portName)
    {
        BaseEntityEvent? ev = GetCoreEntityEvent();
        if (ev == null) return OperationResult<object>.Failure($"No BaseEntityEvent in {name}");
        if(fieldCache == null)
        {
            fieldCache = new();
            EventNodeUtils.BuildFieldCache(fieldCache, vsFieldPorts);
        }
        return EventNodeUtils.ReadFromObject(portName, fieldCache, ev);
    }

    // Entity filtering is handled by the system via separate maps — no filter needed here.
    public override bool CanExecute(VSEventBase _, EntId ownerId) => IsActive;

    public override void Register(Type _, EntId ownerId)
        => base.Register(EventType!, ownerId);
    public override void DeRegister(Type _, EntId ownerId)
        => base.DeRegister(EventType!, ownerId);

#if UNITY_EDITOR
    private void BuildDynamicPorts()
    {
        Type? t = EventType; if (t == null) return;
        EventNodeUtils.CreateFieldPorts(t, vsFieldPorts, false);
        // Add or remove the "Target" input port based on ListenTarget
        if (ListenTarget == VSEntityListenTarget.Dynamic)
            EventNodeUtils.EnsureInputPort<EntId>(this, "Target");
        else
            EventNodeUtils.RemoveInputPort(this, "Target");
        EventNodeUtils.AddDynamicPorts(this, vsFieldPorts);
    }
    public override void OnBeforeSerialize() { BuildDynamicPorts(); base.OnBeforeSerialize(); }
#endif
}
```

---

## Step 5 — Implement `VSEventListenersSystem`

The hooks into Core are already wired — no changes to `SystemsController` or `EntityEventQueueImplementation` are needed.

**Global events** — `SystemsController.ProcessGlobalEvents()` calls in order:
1. `VSEventListenersSystem.ExecutePreEvent(currentEvent)`
2. `currentEvent.CallPreListenerSystemsInternal()`
3. `VSEventListenersSystem.ExecuteEvent(currentEvent)` ← **trigger VS graphs here**
4. `currentEvent.Execute()`
5. `VSEventListenersSystem.ExecutePostEvent(currentEvent)`
6. `currentEvent.CallPostListenerSystemsInternal()`
7. `currentEvent.Dispose()`

**Entity events** — `EntityEventQueueImplementation<T>.ExecuteAllEvents()` calls in order:
1. `VSEventListenersSystem.ExecutePreEntityEvent(entityEvent)`
2. Entity-specific and all-entity callbacks
3. `VSEventListenersSystem.ExecuteEntityEvent(entityEvent)` ← **trigger VS graphs here**
4. `VSEventListenersSystem.ExecutePostEntityEvent(entityEvent)`
5. `entityEvent.Dispose()`

Both `SystemsController` and `EntityEventQueueImplementation<T>` already inject `VSEventListenersSystem` directly via Zenject.

```csharp
// Alias for readability
using ListenerMap = Dictionary<Type, Dictionary<EntId, List<BaseEventListenNode>>>;

public sealed class VSEventListenersSystem : IVSEventListenersSystem, IOnDestroyEntitySystem
{
    [Inject] private VSEngineCore VSEngineCore;

    // Global event maps (BaseEvent) — one per timing, keyed by owner
    private readonly ListenerMap preGlobal     = new();
    private readonly ListenerMap defaultGlobal = new();
    private readonly ListenerMap postGlobal    = new();

    // Entity event maps — three strategies, each with one map per timing:
    //   Owner:   keyed by target entity (== owner), O(1) lookup via ev.EntityID
    //   All:     keyed by owner, iterate all on trigger
    //   Dynamic: keyed by owner, iterate all and check ev.EntityID == node.GetDynamicTargetEntity()
    private readonly ListenerMap preOwner     = new(), defaultOwner    = new(), postOwner    = new();
    private readonly ListenerMap preAll       = new(), defaultAll      = new(), postAll      = new();
    private readonly ListenerMap preDynamic   = new(), defaultDynamic  = new(), postDynamic  = new();

    // Fast removal by owner — stores (map, eventType) for every registered node
    private readonly Dictionary<EntId, List<OwnerEntry>> ownerIndex = new();

    private readonly struct OwnerEntry
    {
        public readonly ListenerMap Map;
        public readonly Type EventType;
        public readonly BaseEventListenNode Node;
        public OwnerEntry(ListenerMap map, Type eventType, BaseEventListenNode node)
            { Map = map; EventType = eventType; Node = node; }
    }

    // --- Timing → map helpers ---
    private ListenerMap GlobalMap(VSEventTiming t) => t switch
        { VSEventTiming.Pre => preGlobal, VSEventTiming.Post => postGlobal, _ => defaultGlobal };
    private ListenerMap OwnerMap(VSEventTiming t) => t switch
        { VSEventTiming.Pre => preOwner,  VSEventTiming.Post => postOwner,  _ => defaultOwner };
    private ListenerMap AllMap(VSEventTiming t) => t switch
        { VSEventTiming.Pre => preAll,    VSEventTiming.Post => postAll,    _ => defaultAll };
    private ListenerMap DynamicMap(VSEventTiming t) => t switch
        { VSEventTiming.Pre => preDynamic, VSEventTiming.Post => postDynamic, _ => defaultDynamic };

    // --- Global event hooks ---
    public void ExecutePreEvent(BaseEvent ev)  => TriggerGlobal(ev, preGlobal);
    public void ExecuteEvent(BaseEvent ev)     => TriggerGlobal(ev, defaultGlobal);
    public void ExecutePostEvent(BaseEvent ev) => TriggerGlobal(ev, postGlobal);

    private void TriggerGlobal(BaseEvent ev, ListenerMap map)
    {
        if (!map.TryGetValue(ev.GetType(), out var byOwner)) return;
        foreach (var (ownerId, nodes) in byOwner)
            foreach (var node in nodes)
                if (node.CanExecute(null!, ownerId))
                    VSEngineCore.Run(node, ev, ownerId);
    }

    // --- Entity event hooks ---
    public void ExecutePreEntityEvent<T>(T ev)    where T : EntityEvent<T>, new()
        => TriggerEntity(ev, preOwner, preAll, preDynamic);
    public void ExecuteEntityEvent<T>(T ev)       where T : EntityEvent<T>, new()
        => TriggerEntity(ev, defaultOwner, defaultAll, defaultDynamic);
    public void ExecutePostEntityEvent<T>(T ev)   where T : EntityEvent<T>, new()
        => TriggerEntity(ev, postOwner, postAll, postDynamic);

    private void TriggerEntity<T>(T ev, ListenerMap ownerMap, ListenerMap allMap, ListenerMap dynamicMap)
        where T : EntityEvent<T>, new()
    {
        Type t = typeof(T);

        // Owner: O(1) lookup — only fire nodes whose owner entity == ev.EntityID
        if (ownerMap.TryGetValue(t, out var byTarget) &&
            byTarget.TryGetValue(ev.EntityID, out var ownerNodes))
            foreach (var node in ownerNodes)
                if (node.CanExecute(null!, ev.EntityID))
                    VSEngineCore.Run(node, ev);

        // All: fire for every registered owner
        if (allMap.TryGetValue(t, out var allByOwner))
            foreach (var (ownerId, nodes) in allByOwner)
                foreach (var node in nodes)
                    if (node.CanExecute(null!, ownerId))
                        VSEngineCore.Run(node, ev);

        // Dynamic: fire if ev.EntityID matches the node's runtime input port value
        if (dynamicMap.TryGetValue(t, out var dynByOwner))
            foreach (var (ownerId, nodes) in dynByOwner)
                foreach (var node in nodes)
                    if (node is EntityEventListenNode eeln &&
                        eeln.GetDynamicTargetEntity() == ev.EntityID &&
                        node.CanExecute(null!, ownerId))
                        VSEngineCore.Run(node, ev);
    }

    // --- Listener registration ---
    public void AddListener(EntId owner, Type eventType, BaseEventListenNode node)
    {
        ListenerMap map = node is EntityEventListenNode eeln
            ? eeln.ListenTarget switch
            {
                VSEntityListenTarget.All     => AllMap(node.Timing),
                VSEntityListenTarget.Dynamic => DynamicMap(node.Timing),
                _                            => OwnerMap(node.Timing),   // Owner + Parent (future)
            }
            : GlobalMap(node.Timing);

        // For Owner mode, key by owner so ev.EntityID lookup works directly
        EntId key = (node is EntityEventListenNode e2 &&
                     e2.ListenTarget == VSEntityListenTarget.Owner) ? owner : owner;
        // (key is always owner; Owner map uses it as the target entity since owner == target)

        Insert(map, eventType, key, node);

        // Record in owner index for fast bulk removal
        if (!ownerIndex.TryGetValue(owner, out var entries))
            ownerIndex[owner] = entries = new List<OwnerEntry>();
        entries.Add(new OwnerEntry(map, eventType, node));
    }

    public void RemoveListener(EntId owner, Type eventType, BaseEventListenNode node)
    {
        // Single-node removal still works via the map directly
        ListenerMap map = node is EntityEventListenNode eeln
            ? eeln.ListenTarget switch
            {
                VSEntityListenTarget.All     => AllMap(node.Timing),
                VSEntityListenTarget.Dynamic => DynamicMap(node.Timing),
                _                            => OwnerMap(node.Timing),
            }
            : GlobalMap(node.Timing);

        if (map.TryGetValue(eventType, out var byKey) &&
            byKey.TryGetValue(owner, out var nodes))
            nodes.Remove(node);
    }

    // Fast bulk removal when the owner entity is destroyed
    public void OnDestroyEntity(EntId destroyedId)
    {
        if (!ownerIndex.TryGetValue(destroyedId, out var entries)) return;
        foreach (var entry in entries)
            if (entry.Map.TryGetValue(entry.EventType, out var byKey) &&
                byKey.TryGetValue(destroyedId, out var nodes))
                nodes.Remove(entry.Node);
        ownerIndex.Remove(destroyedId);
    }

    private static void Insert(ListenerMap map, Type eventType, EntId key, BaseEventListenNode node)
    {
        if (!map.TryGetValue(eventType, out var byKey))
            map[eventType] = byKey = new Dictionary<EntId, List<BaseEventListenNode>>();
        if (!byKey.TryGetValue(key, out var nodes))
            byKey[key] = nodes = new List<BaseEventListenNode>();
        nodes.Add(node);
    }
}
```

> `VSEngineCore.Run(node, eventObject)` overloads accepting `BaseEvent` and `BaseEntityEvent` are added in Step 2.

---

## Step 6 — Custom Node Editors

Both editors follow the same pattern. Create inside `Assets/Core/Editor/VSEngine/`.

```csharp
// EventListenNodeEditor.cs
[CustomNodeEditor(typeof(EventListenNode))]
public class EventListenNodeEditor : XNodeEditor.NodeEditor
{
    public override void OnBodyGUI()
    {
        serializedObject.Update();

        // --- Type dropdown ---
        var node = (EventListenNode)target;
        var allTypes = TypeCache.Get().GetAllEventTypes().ToArray();
        string[] names = allTypes.Select(t => t.Name).ToArray();
        int current = Array.IndexOf(allTypes, node.EventType);
        int selected = EditorGUILayout.Popup("Event Type", current, names);
        if (selected >= 0 && selected != current)
        {
            Undo.RecordObject(node, "Change Event Type");
            // write into selectedEventType via SerializedProperty
            var prop = serializedObject.FindProperty("selectedEventType");
            prop.FindPropertyRelative("TypeName").stringValue = allTypes[selected].Name;
            prop.FindPropertyRelative("AssemblyQualifiedName").stringValue =
                allTypes[selected].AssemblyQualifiedName;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(node);
        }

        // --- Draw rest (ports, IsActive) ---
        base.OnBodyGUI();
    }
}
```

`EntityEventListenNodeEditor` is identical except it uses `GetAllEntityEventTypes()`.

---

## Requiring `[VSField]` on Core Events

For field bridge ports to appear, concrete `Event<T>` and `EntityEvent<T>` subclasses **must** annotate their fields with `[VSField]`:

```csharp
public sealed class MyGameEvent : Event<MyGameEvent>
{
    [VSField] public int Damage;
    [VSField(IsWritable = true)] public EntId Target;
}
```

This is a **developer convention** — document in CLAUDE.md.

---

## Verification

1. **In Unity Editor**: Open a graph → right-click → verify "Miguel/events/EventListenNode" and "Miguel/events/EntityEventListenNode" appear in the node menu.
2. **Inspector dropdown**: Select an event type from the dropdown → output ports matching `[VSField]` fields appear on the node.
3. **Changing type**: Switching event type in the dropdown → old ports removed, new ports added.
4. **Runtime (manual test)**: Fire a `BaseEvent` subclass → the connected VS graph executes → `GetValue()` returns correct field values.
5. **Entity filtering**: `EntityEventListenNode` only fires for the entity whose `EntId` registered it.
6. **Existing tests**: Run EditMode test suite — no regressions in Stats, EntityHierarchy, Timer, EntityEvents tests.
