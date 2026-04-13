# Plan: Local Variable Declarations in ActionGraph

## Goal
Allow designers to declare named local variables directly on an `ActionGraph` asset, and surface those declarations in a persistent UI panel in the corner of the xNode editor window.

---

## Context

### Current state
- `ScriptExecution` stores runtime local variables in `Dictionary<string, object?>` — variables are created ad-hoc when a `SetLocalVariableNode` runs; there is no declared schema.
- `GetLocalVariableNode` and `SetLocalVariableNode` each have a raw `string VariableName` field — no connection to a declared list.
- `ActionGraph` has no local variable declarations.
- The xNode editor exposes `NodeGraphEditor.OnGUI()` (called after all node drawing) — the correct hook for a persistent overlay.
- There is no custom `NodeGraphEditor` for `ActionGraph` yet.

### Files to create / modify
| Action | File |
|--------|------|
| Modify | `Assets/Core/Runtime/VSEngine/ActionGraph.cs` |
| Create | `Assets/Core/Runtime/VSEngine/LocalVariableDefinition.cs` |
| Create | `Assets/Core/Editor/VSEngine/ActionGraphEditor.cs` |
| Modify | `Assets/Core/Runtime/VSEngine/Nodes/LocalVariables/GetLocalVariableNode.cs` |
| Modify | `Assets/Core/Runtime/VSEngine/Nodes/LocalVariables/SetLocalVariableNode.cs` |

---

## Steps

### Step 1 — Add `LocalVariableDefinition` data class (Runtime)

Create `Assets/Core/Runtime/VSEngine/LocalVariableDefinition.cs`.

```csharp
using System;
using UnityEngine;

namespace Core.VSEngine
{
    [Serializable]
    public class LocalVariableDefinition
    {
        [SerializeField] public string Name = "";
        [SerializeField] public NodeElementType Type = NodeElementType.Numbers;
    }
}
```

This is a plain serializable class so Unity serializes the list on the asset.

---

### Step 2 — Add declared variable list to `ActionGraph` (Runtime)

In `ActionGraph.cs`, add:

```csharp
[SerializeField]
private List<LocalVariableDefinition> localVariables = new();

public IReadOnlyList<LocalVariableDefinition> LocalVariables => localVariables;

// Editor-only helper to mutate the list
#if UNITY_EDITOR
public List<LocalVariableDefinition> LocalVariablesMutable => localVariables;
#endif
```

No runtime behaviour changes. `ScriptExecution` keeps working as-is (ad-hoc dict).

---

### Step 3 — Create `ActionGraphEditor` with corner overlay (Editor)

Create `Assets/Core/Editor/VSEngine/ActionGraphEditor.cs`.

Key points:
- Decorate with `[CustomNodeGraphEditor(typeof(ActionGraph))]`.
- Override `OnGUI()`.
- Draw the panel using **immediate-mode GUI** (not IMGUI Scroll/Layout inside zoomed space). Call `GUI.BeginGroup` / `GUI.EndGroup` with a fixed `Rect` anchored to the **bottom-left of the window** to stay immune to pan/zoom. Use `window.position` for sizing.
- The panel shows:
  - Title "Local Variables"
  - Scrollable list: each row has a name `TextField` + `EnumPopup` for `NodeElementType` + a remove `[-]` button
  - An `[+ Add Variable]` button at the bottom
- All mutations go through `Undo.RecordObject(target, ...)` + `EditorUtility.SetDirty(target)` so changes are saved and undoable. `target` is the `ActionGraph` cast from `NodeGraphEditor.target`.

Panel positioning (bottom-left, immune to zoom):

```csharp
private const float PanelWidth  = 260f;
private const float PanelHeight = 300f;
private const float Margin      = 10f;

private Rect GetPanelRect()
{
    Rect wp = window.position;
    // window.position is in screen space; OnGUI receives local coords → use (0,0) origin
    return new Rect(Margin,
                    wp.height - PanelHeight - Margin,
                    PanelWidth,
                    PanelHeight);
}
```

Because `OnGUI` is called after `GUI.matrix` zoom transforms are reset (the zoom transform is applied and un-applied around `DrawNodes`), the GUI matrix is back to identity here — so screen-space fixed layout works correctly.

Scroll state: keep a `Vector2 _scrollPos` field on the editor instance.

---

### Step 4 — Dropdown helper in `GetLocalVariableNode` and `SetLocalVariableNode` (Editor, optional but recommended)

Add a custom `NodeEditor` for both node types that replaces the raw `VariableName` string field with a **dropdown populated from the graph's declared variables**.

Create `Assets/Core/Editor/VSEngine/LocalVariableNodeEditor.cs`:

```csharp
[CustomNodeEditor(typeof(GetLocalVariableNode))]
[CustomNodeEditor(typeof(SetLocalVariableNode))]
public class LocalVariableNodeEditor : NodeEditor
{
    public override void OnBodyGUI()
    {
        // Draw the Type enum normally
        // Replace VariableName with a dropdown if the graph has declarations
        ActionGraph graph = target.graph as ActionGraph;
        if (graph != null && graph.LocalVariables.Count > 0)
        {
            string[] names = graph.LocalVariables.Select(v => v.Name).ToArray();
            int current = Array.IndexOf(names, serializedObject.FindProperty("VariableName").stringValue);
            int selected = EditorGUILayout.Popup("Variable", Mathf.Max(current, 0), names);
            serializedObject.FindProperty("VariableName").stringValue = names[selected];
        }
        else
        {
            // Fallback: raw text field (original behaviour)
            NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("VariableName"));
        }
        NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("Type"));
        serializedObject.ApplyModifiedProperties();
        base.OnBodyGUI(); // draws ports
    }
}
```

**Note:** `[CustomNodeEditor]` does not support multiple attributes on one class — create two separate classes (`GetLocalVariableNodeEditor` and `SetLocalVariableNodeEditor`) both inheriting the same base.

---

### Step 5 — Validation (optional quality-of-life)

Add a warning in `GetLocalVariableNode.GetValue` and `SetLocalVariableNode.Action` when `VariableName` is not found in `graph.LocalVariables`. This is a lightweight runtime check — no structural changes needed.

---

## What this does NOT change

- `ScriptExecution` runtime dict logic — untouched. Variables still work ad-hoc even without declarations; declarations are purely a design-time contract.
- The xNode package itself — no package files are modified.
- Existing nodes — `GetLocalVariableNode` and `SetLocalVariableNode` keep working exactly as before for graphs that have no declarations (the dropdown degrades to a text field).

---

## Implementation order

1. `LocalVariableDefinition.cs` (no dependencies)
2. `ActionGraph.cs` change (depends on step 1)
3. `ActionGraphEditor.cs` corner panel (depends on step 2)
4. `LocalVariableNodeEditor.cs` dropdowns (depends on steps 1–2)
5. Optional validation in node `Action()`/`GetValue()`
