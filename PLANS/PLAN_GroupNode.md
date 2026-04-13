# Plan: GroupNode for ActionGraph (XNode)

## Context
Designers need a way to visually group related nodes in the ActionGraph editor. The group acts as a movable container window: dragging it moves all enclosed nodes together. It also has an editable title and a free-text documentation area so designers can annotate the intent of each cluster of nodes without breaking the graph's execution.

---

## Files to Create

| File | Purpose |
|------|---------|
| `Assets/Core/Runtime/VSEngine/Nodes/GroupNode.cs` | Runtime data class – no ports, no execution |
| `Assets/Core/Editor/VSEngine/GroupNodeEditor.cs` | Custom `NodeEditor` for rendering the group |
| `Assets/Core/Editor/VSEngine/ActionGraphEditor.cs` | Custom `NodeGraphEditor` to ensure groups render behind other nodes |

---

## Step 1 – Runtime: `GroupNode.cs`

Extends **`XNode.Node`** directly (not `VSNodeBase`). No ports, no execution logic — purely a design-time annotation object stored inside the ActionGraph ScriptableObject.

```csharp
namespace Core.VSEngine.Nodes
{
    [CreateNodeMenu("Group", order = 0)]
    [NodeTint("#2A2A2A")]
    public class GroupNode : Node
    {
        [SerializeField] public string groupTitle = "Group";
        [SerializeField, TextArea(3, 10)] public string groupNotes = "";
        [SerializeField] public Vector2 groupSize = new Vector2(400, 300);

        public override object GetValue(NodePort port) => null;
    }
}
```

- `groupTitle` — editable short label shown in the header.
- `groupNotes` — multi-line documentation text shown in the body.
- `groupSize` — serialized width × height so the size persists across reloads.
- No `[Input]` / `[Output]` fields → no ports appear.

---

## Step 2 – Editor: `GroupNodeEditor.cs`

`[CustomNodeEditor(typeof(GroupNode))]` — auto-discovered by XNode's reflection system.

### 2a – Width
Override `GetWidth()` → return `(int)groupNode.groupSize.x`.

### 2b – Header: Editable Title
Override `OnHeaderGUI()` — replace the read-only name label with an inline `EditorGUILayout.PropertyField` on `groupTitle`.

### 2c – Body: Notes + Resize Handle
Override `OnBodyGUI()`:

1. **Notes** — `EditorGUILayout.TextArea` for `groupNotes`, height fills the group body.
2. **Resize handle** — 16×16 rect at the bottom-right corner:
   - `EditorGUIUtility.AddCursorRect` → `MouseCursor.ResizeUpLeft`.
   - Convert local rect to screen space via `window.GridToWindowPositionNoClipped(target.position)` (both `GridToWindowPositionNoClipped` and `zoom` are public on `NodeEditorWindow`).
   - `MouseDown` on handle → start resize, record start mouse pos + start size.
   - `MouseDrag` → `newSize = startSize + delta * zoom`, clamped to minimum (200 × 150), `EditorUtility.SetDirty(target)`.
   - `MouseUp` → stop resize.

### 2d – Group Movement: Move Contained Nodes
Fields on the editor class (not serialized): `_prevPosition`, `_initialized`.

At the **top** of `OnBodyGUI()`:
- First call: set `_prevPosition = target.position`, `_initialized = true` (no movement triggered).
- Subsequent calls: if `target.position != _prevPosition`, compute delta, build `Rect oldBounds = new Rect(_prevPosition, groupNode.groupSize)`, iterate `target.graph.nodes` — for every node whose `position` falls inside `oldBounds`, call `Undo.RecordObject` and add delta.
- Always update `_prevPosition = target.position`.

### 2e – Visual Style
Override `GetBodyStyle()` / `GetBodyHighlightStyle()` — return styles with a semi-transparent dark background so the group reads as a tinted panel behind other nodes.

---

## Step 3 – Editor: `ActionGraphEditor.cs`

`[CustomNodeGraphEditor(typeof(ActionGraph), "xNode.Settings")]`

XNode's `DrawNodes()` iterates `graph.nodes` in index order. Groups at index 0 are drawn first → rendered **behind** all other nodes.

```csharp
public override XNode.Node CreateNode(Type type, Vector2 position)
{
    XNode.Node node = base.CreateNode(type, position);
    MoveGroupToFront(node);
    return node;
}

public override XNode.Node CopyNode(XNode.Node original)
{
    XNode.Node node = base.CopyNode(original);
    MoveGroupToFront(node);
    return node;
}

private void MoveGroupToFront(XNode.Node node)
{
    if (node is GroupNode && target.nodes.Contains(node))
    {
        target.nodes.Remove(node);
        target.nodes.Insert(0, node);
        EditorUtility.SetDirty(target);
    }
}
```

---

## Draw Order

```
OnGUI():
  DrawGrid()
  DrawConnections()
  DrawNodes()          ← iterates graph.nodes in order
    [0] GroupNode      ← drawn first = rendered behind everything
    [1] IfNode
    [2] NumberNode
    ...
  graphEditor.OnGUI()
```

---

## Edge Cases

| Case | Handling |
|------|----------|
| Node outside group when group moves | `oldBounds.Contains(n.position)` — only enclosed nodes move |
| Resize below minimum | Clamp to (200, 150) |
| First-frame false-positive move | `_initialized` flag; no movement on first call |
| Paste / Duplicate of GroupNode | `CopyNode()` override also reorders to front |
| Multi-select drag (group + other nodes) | XNode moves all selected nodes; delta detection still works via `_prevPosition` from previous frame |
| Multiple groups | Each `GroupNodeEditor` instance tracks its own `_prevPosition` independently |

---

## Verification

1. **Compile** — no errors in Console after domain reload.
2. **Create** — right-click in an ActionGraph → `Group` in menu → wide semi-transparent node appears.
3. **Title** — click header, type new title → persists after domain reload.
4. **Notes** — type multi-line text in body → persists.
5. **Resize** — drag bottom-right handle → group expands/shrinks, size saved.
6. **Group drag** — place nodes inside group bounds, drag group → all inner nodes follow.
7. **Render order** — group renders behind other nodes (no overlap clipping).
8. **Undo** — Ctrl+Z after group drag → group and inner nodes revert together.
