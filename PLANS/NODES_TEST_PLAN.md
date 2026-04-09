# XNode Custom Node Unit Tests — Implementation Plan

## Current State

- **Tested (6):** All math nodes via `VSMathNodesTest` + `BasicMathNodesAG.asset`
- **Already covered at system level (not node-level):** `EventListenNode`, `EntityEventListenNode`, `EventWriteNode` → `VSEventListenersSystemTests.cs`
- **Disabled/structural (skip):** `ScriptNode`, `InputsNode`, `OutputsNode`

---

## Test Pattern

Each test suite = **1 C# class** (mirrors `VSMathNodesTest`) + **1 ActionGraph `.asset`** built in the Unity Editor.

The C# class always follows this shape:

```csharp
[SerializeField] private ActionGraph MyNodes;

[Test]
public void MyNodesTest()
{
    using CachedList<BaseTestAssertNode> assertNodes = ListCache<BaseTestAssertNode>.Get();
    VSBaseEngine.GetAssertNodes(MyNodes, assertNodes);
    foreach (BaseTestAssertNode assertNode in assertNodes)
        VSEngineCore.RunTestNode(assertNode);
}
```

All assertion logic lives inside the ActionGraph nodes (`AssertNumberNode`, `AssertNumberLimitsNode`, `AssertStatNode`).

---

## Test Suites to Create

### Suite 1 — Flow Nodes

**Files:**
- `Assets/Testing Core/Editor/UnitTests/VSNodes/Flow/VSFlowNodesTest.cs`
- `Assets/Testing Core/Editor/UnitTests/VSNodes/Flow/FlowNodesAG.asset`

**Nodes covered:** `IfNode`, `MultipleBranchNode`

**ActionGraph scenarios:**

| Scenario | Graph shape |
|---|---|
| `IfNode_TrueBranch` | `NumberNode(10)` → `IfNode(Condition=true)` → **True** → `AssertNumberNode(expected=10)` |
| `IfNode_FalseBranch` | `NumberNode(7)` → `IfNode(Condition=false)` → **False** → `AssertNumberNode(expected=7)` |
| `IfNode_TrueDoesNotRunFalse` | `IfNode(Condition=true)` → **True** → `AssertNumber(5)`, **False** branch left disconnected |
| `MultipleBranchNode_AllBranchesRun` | `MultipleBranchNode` with 2 branches → each branch leads to its own `AssertNumberNode` |

---

### Suite 2 — Loop Nodes

**Files:**
- `Assets/Testing Core/Editor/UnitTests/VSNodes/Flow/VSLoopNodesTest.cs`
- `Assets/Testing Core/Editor/UnitTests/VSNodes/Flow/LoopNodesAG.asset`

**Nodes covered:** `ForeachLoopNode`, `ForEachConditionNode`

**ActionGraph scenarios:**

| Scenario | Graph shape |
|---|---|
| `ForeachLoop_IteratesElements` | `ListNumbersConstNode([5,5,5])` → `ForeachLoopNode` → **LoopExecute** → `AssertNumberNode(expected=5)` → **Continue** |
| `ForeachLoop_EmptyList` | `ListNumbersConstNode([])` → `ForeachLoopNode` → **Continue** → `AssertNumberNode(expected=0)` (asserts a constant to prove Continue ran) |
| `ForEachConditionNode_TrueBranch` | `ListNumbersConstNode([10])` → `ForEachConditionNode(Condition=true)` → **True** → `AssertNumberLimitsNode(min=9, max=11)` |
| `ForEachConditionNode_FalseBranch` | `ListNumbersConstNode([10])` → `ForEachConditionNode(Condition=false)` → **False** → `AssertNumberNode(0)` (constant check to prove False ran) |

---

### Suite 3 — List Nodes

**Files:**
- `Assets/Testing Core/Editor/UnitTests/VSNodes/Lists/VSListNodesTest.cs`
- `Assets/Testing Core/Editor/UnitTests/VSNodes/Lists/ListNodesAG.asset`

**Nodes covered:** `ListNumbersConstNode`, `ListConstNode`, `FilterListByConditionNode`

**ActionGraph scenarios:**

| Scenario | Graph shape |
|---|---|
| `ListNumbersConst_Values` | `ListNumbersConstNode([3,6,9])` → `ForeachLoopNode` → **LoopExecute** → `AssertNumberLimitsNode(min=3, max=9)` |
| `ListConstNode_WithNumbers` | `NumberNode(42)` → `ListConstNode(1 element)` → `ForeachLoopNode` → `AssertNumberNode(expected=42)` |
| `FilterList_KeepsMatchingElements` | `ListNumbersConstNode([1,2,5,10])` → `FilterListByConditionNode(ShouldInclude = input > 3 via MathComparisonNode)` → `ForeachLoopNode` → `AssertNumberLimitsNode(min=4, max=10)` |

---

### Suite 4 — Value Nodes

**Files:**
- `Assets/Testing Core/Editor/UnitTests/VSNodes/Values/VSValueNodesTest.cs`
- `Assets/Testing Core/Editor/UnitTests/VSNodes/Values/ValueNodesAG.asset`

**Nodes covered:** `NumberNode`

> `StringNode` — no `AssertStringNode` exists; skip until a string assert node is added.

**ActionGraph scenarios:**

| Scenario | Graph shape |
|---|---|
| `NumberNode_ReturnsValue` | `NumberNode(42)` → `AssertNumberNode(expected=42)` |
| `NumberNode_NegativeValue` | `NumberNode(-5)` → `AssertNumberNode(expected=-5)` |
| `NumberNode_Zero` | `NumberNode(0)` → `AssertNumberNode(expected=0)` |

---

## Folder Structure After Implementation

```
Assets/Testing Core/Editor/UnitTests/VSNodes/
├── Math/
│   ├── VSMathNodesTest.cs          ✓ exists
│   └── BasicMathNodesAG.asset      ✓ exists
├── Flow/
│   ├── VSFlowNodesTest.cs          ← new
│   ├── FlowNodesAG.asset           ← new (Unity Editor)
│   ├── VSLoopNodesTest.cs          ← new
│   └── LoopNodesAG.asset           ← new (Unity Editor)
├── Lists/
│   ├── VSListNodesTest.cs          ← new
│   └── ListNodesAG.asset           ← new (Unity Editor)
└── Values/
    ├── VSValueNodesTest.cs         ← new
    └── ValueNodesAG.asset          ← new (Unity Editor)
```

---

## Implementation Order

1. **Suite 4 (Value Nodes)** — Simplest graphs; validates `NumberNode` which is used as input in all other suites
2. **Suite 1 (Flow Nodes)** — `IfNode` and `MultipleBranchNode` are critical control flow primitives
3. **Suite 2 (Loop Nodes)** — Builds on list nodes; slightly more complex graphs
4. **Suite 3 (List Nodes)** — Most complex; `FilterListByConditionNode` requires a nested condition sub-graph

---

## Work Split: Code vs. Unity Editor

- **Code only (straightforward):** All 4 C# test class files — same boilerplate as `VSMathNodesTest`, just different `ActionGraph` reference and test method name
- **Unity Editor required:** All 4 `.asset` ActionGraph files must be built by hand in the XNode graph editor (place nodes, set values, connect ports, assign the asset to the serialized field on the test object)
