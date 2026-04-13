using System;
using System.Collections.Generic;
using System.Reflection;
using Core.Utils;
using Core.VSEngine;
using Core.VSEngine.Nodes;
using Core.VSEngine.Nodes.Lists;
using Core.VSEngine.Nodes.Lists.Utils;
using Core.VSEngine.Nodes.Math;
using Core.VSEngine.Nodes.TestNodes;
using UnityEditor;
using UnityEngine;
using XNode;

/// <summary>
/// Temporary editor utility — creates all VS test ActionGraph assets.
/// Run via Tools → Tests → Create VS Test Action Graphs, then delete this file.
/// </summary>
public static class CreateVSTestActionGraphs
{
    [MenuItem("Tools/Tests/Create VS Test Action Graphs")]
    public static void CreateAllGraphs()
    {
        CreateValueNodesAG();
        CreateFlowNodesAG();
        CreateLoopNodesAG();
        CreateListNodesAG();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CreateVSTestActionGraphs] All ActionGraph assets created successfully.");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Suite 4 — Value Nodes
    // ──────────────────────────────────────────────────────────────────────
    static void CreateValueNodesAG()
    {
        const string folder = "Assets/Testing Core/Editor/UnitTests/VSNodes/Values";
        EnsureFolder(folder);
        ActionGraph graph = MakeGraph(folder + "/ValueNodesAG.asset", "ValueNodesAG");

        // Scenario: NumberNode(42) → AssertNumberNode(expect=42)
        var n42   = MakeNode<NumberNode>(graph, "Number_42");
        SetF(n42, "ValueFloat", 42f);

        var a42   = MakeNode<AssertNumberNode>(graph, "NumberNode_ReturnsValue");
        SetF(a42, "ValueFloat", 42f); SetF(a42, "ExpectEqual", true); SetF(a42, "RepeatNumber", 1);

        // Scenario: NumberNode(-5) → AssertNumberNode(expect=-5)
        var nNeg  = MakeNode<NumberNode>(graph, "Number_Neg5");
        SetF(nNeg, "ValueFloat", -5f);

        var aNeg  = MakeNode<AssertNumberNode>(graph, "NumberNode_NegativeValue");
        SetF(aNeg, "ValueFloat", -5f); SetF(aNeg, "ExpectEqual", true); SetF(aNeg, "RepeatNumber", 1);

        // Scenario: NumberNode(0) → AssertNumberNode(expect=0)
        var n0    = MakeNode<NumberNode>(graph, "Number_0");
        SetF(n0, "ValueFloat", 0f);

        var a0    = MakeNode<AssertNumberNode>(graph, "NumberNode_Zero");
        SetF(a0, "ValueFloat", 0f); SetF(a0, "ExpectEqual", true); SetF(a0, "RepeatNumber", 1);

        Commit(graph, n42, a42, nNeg, aNeg, n0, a0);

        Link(n42,  "Value", a42,  "Input");
        Link(nNeg, "Value", aNeg, "Input");
        Link(n0,   "Value", a0,   "Input");

        EditorUtility.SetDirty(graph);
        Debug.Log("[CreateVSTestActionGraphs] ValueNodesAG created.");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Suite 1 — Flow Nodes
    // ──────────────────────────────────────────────────────────────────────
    static void CreateFlowNodesAG()
    {
        const string folder = "Assets/Testing Core/Editor/UnitTests/VSNodes/Flow";
        EnsureFolder(folder);
        ActionGraph graph = MakeGraph(folder + "/FlowNodesAG.asset", "FlowNodesAG");

        // ── IfNode_TrueBranch ──
        var ifT   = MakeNode<IfNode>(graph, "IfNode_TrueCondition");
        SetF(ifT, "Condition", true);
        var n10   = MakeNode<NumberNode>(graph, "Number_10");
        SetF(n10, "ValueFloat", 10f);
        var aTrB  = MakeNode<AssertNumberNode>(graph, "IfNode_TrueBranch");
        SetF(aTrB, "ValueFloat", 10f); SetF(aTrB, "ExpectEqual", true); SetF(aTrB, "RepeatNumber", 1);

        // ── IfNode_FalseBranch ──
        var ifF   = MakeNode<IfNode>(graph, "IfNode_FalseCondition");
        SetF(ifF, "Condition", false);
        var n7    = MakeNode<NumberNode>(graph, "Number_7");
        SetF(n7, "ValueFloat", 7f);
        var aFlB  = MakeNode<AssertNumberNode>(graph, "IfNode_FalseBranch");
        SetF(aFlB, "ValueFloat", 7f); SetF(aFlB, "ExpectEqual", true); SetF(aFlB, "RepeatNumber", 1);

        // ── IfNode_TrueDoesNotRunFalse ──
        var ifTo  = MakeNode<IfNode>(graph, "IfNode_TrueOnly");
        SetF(ifTo, "Condition", true);
        var n5    = MakeNode<NumberNode>(graph, "Number_5");
        SetF(n5, "ValueFloat", 5f);
        var aTo   = MakeNode<AssertNumberNode>(graph, "IfNode_TrueDoesNotRunFalse");
        SetF(aTo, "ValueFloat", 5f); SetF(aTo, "ExpectEqual", true); SetF(aTo, "RepeatNumber", 1);

        // ── MultipleBranchNode_AllBranchesRun ──
        var mbn   = MakeNode<MultipleBranchNode>(graph, "MultipleBranchNode");
        // Give the dynamic port list 2 elements so xNode creates "Continue 0" and "Continue 1"
        SetF(mbn, "Continue", new Control?[] { null, null });
        CallOnBeforeSerialize(mbn);

        var n1    = MakeNode<NumberNode>(graph, "Number_1");
        SetF(n1, "ValueFloat", 1f);
        var aBr1  = MakeNode<AssertNumberNode>(graph, "MultipleBranch_Branch1");
        SetF(aBr1, "ValueFloat", 1f); SetF(aBr1, "ExpectEqual", true); SetF(aBr1, "RepeatNumber", 1);

        var n2    = MakeNode<NumberNode>(graph, "Number_2");
        SetF(n2, "ValueFloat", 2f);
        var aBr2  = MakeNode<AssertNumberNode>(graph, "MultipleBranch_Branch2");
        SetF(aBr2, "ValueFloat", 2f); SetF(aBr2, "ExpectEqual", true); SetF(aBr2, "RepeatNumber", 1);

        Commit(graph, ifT, n10, aTrB, ifF, n7, aFlB, ifTo, n5, aTo, mbn, n1, aBr1, n2, aBr2);

        // Data connections
        Link(n10, "Value", aTrB, "Input");
        Link(n7,  "Value", aFlB, "Input");
        Link(n5,  "Value", aTo,  "Input");
        Link(n1,  "Value", aBr1, "Input");
        Link(n2,  "Value", aBr2, "Input");

        // Flow connections
        Link(ifT, "True",  aTrB, "Enter");
        Link(ifF, "False", aFlB, "Enter");
        Link(ifTo,"True",  aTo,  "Enter");

        // MultipleBranch: "Continue 0" → aBr1.Enter, "Continue 1" → aBr2.Enter
        TryLinkDynamic(mbn, "Continue 0", aBr1, "Enter");
        TryLinkDynamic(mbn, "Continue 1", aBr2, "Enter");

        EditorUtility.SetDirty(graph);
        Debug.Log("[CreateVSTestActionGraphs] FlowNodesAG created.");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Suite 2 — Loop Nodes
    // ──────────────────────────────────────────────────────────────────────
    static void CreateLoopNodesAG()
    {
        const string folder = "Assets/Testing Core/Editor/UnitTests/VSNodes/Flow";
        EnsureFolder(folder);
        ActionGraph graph = MakeGraph(folder + "/LoopNodesAG.asset", "LoopNodesAG");

        // ── ForeachLoop_IteratesElements: list[5,5,5] → loop → assert(5) ──
        var l555  = MakeListNumbersNode(graph, "List_5_5_5", 5f, 5f, 5f);
        var fe1   = MakeForeachNode(graph, "ForeachLoop_Numbers");
        var nIt   = MakeNode<NumberNode>(graph, "Number_5_Iter");
        SetF(nIt, "ValueFloat", 5f);
        var aIt   = MakeNode<AssertNumberNode>(graph, "ForeachLoop_IteratesElements");
        SetF(aIt, "ValueFloat", 5f); SetF(aIt, "ExpectEqual", true); SetF(aIt, "RepeatNumber", 1);

        // ── ForeachLoop_EmptyList: list[] → loop → continue → assert(0) ──
        var lEmp  = MakeListNumbersNode(graph, "List_Empty");
        var fe2   = MakeForeachNode(graph, "ForeachLoop_Empty");
        var nEmp  = MakeNode<NumberNode>(graph, "Number_0_Empty");
        SetF(nEmp, "ValueFloat", 0f);
        var aEmp  = MakeNode<AssertNumberNode>(graph, "ForeachLoop_EmptyList");
        SetF(aEmp, "ValueFloat", 0f); SetF(aEmp, "ExpectEqual", true); SetF(aEmp, "RepeatNumber", 1);

        // ── ForEachCondition_TrueBranch: list[10], cond=true → True → assert(10) ──
        var l10a  = MakeListNumbersNode(graph, "List_10a", 10f);
        var fec1  = MakeForEachCondNode(graph, "ForEachCond_True", condition: true);
        var nCT   = MakeNode<NumberNode>(graph, "Number_10_CondTrue");
        SetF(nCT, "ValueFloat", 10f);
        var aCT   = MakeNode<AssertNumberNode>(graph, "ForEachCondition_TrueBranch");
        SetF(aCT, "ValueFloat", 10f); SetF(aCT, "ExpectEqual", true); SetF(aCT, "RepeatNumber", 1);

        // ── ForEachCondition_FalseBranch: list[10], cond=false → False → assert(0) ──
        var l10b  = MakeListNumbersNode(graph, "List_10b", 10f);
        var fec2  = MakeForEachCondNode(graph, "ForEachCond_False", condition: false);
        var nCF   = MakeNode<NumberNode>(graph, "Number_0_CondFalse");
        SetF(nCF, "ValueFloat", 0f);
        var aCF   = MakeNode<AssertNumberNode>(graph, "ForEachCondition_FalseBranch");
        SetF(aCF, "ValueFloat", 0f); SetF(aCF, "ExpectEqual", true); SetF(aCF, "RepeatNumber", 1);

        Commit(graph,
            l555, fe1, nIt, aIt,
            lEmp, fe2, nEmp, aEmp,
            l10a, fec1, nCT, aCT,
            l10b, fec2, nCF, aCF);

        // Data connections (AssertNode.Input ← NumberNode, not from loop element)
        Link(nIt,  "Value", aIt,  "Input");
        Link(nEmp, "Value", aEmp, "Input");
        Link(nCT,  "Value", aCT,  "Input");
        Link(nCF,  "Value", aCF,  "Input");

        // Flow/structure connections
        TryLinkDynamic(l555, "Result", fe1, "List");
        Link(fe1, "LoopExecute", aIt, "Enter");

        TryLinkDynamic(lEmp, "Result", fe2, "List");
        Link(fe2, "Continue", aEmp, "Enter");

        TryLinkDynamic(l10a, "Result", fec1, "List");
        Link(fec1, "True", aCT, "Enter");

        TryLinkDynamic(l10b, "Result", fec2, "List");
        Link(fec2, "False", aCF, "Enter");

        EditorUtility.SetDirty(graph);
        Debug.Log("[CreateVSTestActionGraphs] LoopNodesAG created.");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Suite 3 — List Nodes
    // ──────────────────────────────────────────────────────────────────────
    static void CreateListNodesAG()
    {
        const string folder = "Assets/Testing Core/Editor/UnitTests/VSNodes/Lists";
        EnsureFolder(folder);
        ActionGraph graph = MakeGraph(folder + "/ListNodesAG.asset", "ListNodesAG");

        // ── ListNumbersConst_Values: list[3,6,9] → loop → assertLimits(3..9) ──
        var l369  = MakeListNumbersNode(graph, "List_3_6_9", 3f, 6f, 9f);
        var fe1   = MakeForeachNode(graph, "ForeachLoop_ListTest");
        var n6    = MakeNode<NumberNode>(graph, "Number_6_InRange");
        SetF(n6, "ValueFloat", 6f);
        var aLim  = MakeNode<AssertNumberLimitsNode>(graph, "ListNumbersConst_Values");
        SetF(aLim, "MinInclusive", 3f); SetF(aLim, "MaxInclusive", 9f);
        SetF(aLim, "ExpectTrue", true); SetF(aLim, "RepeatNumber", 1);

        // ── ListConstNode_WithNumbers: NumberNode(42) → ListConst(1) → loop → assert(42) ──
        var lc42  = MakeNode<ListConstNode>(graph, "ListConst_1_Number");
        SetF(lc42, "ElementType", NodeElementType.Numbers);
        SetF(lc42, "NumberElements", 1);
        CallOnBeforeSerialize(lc42);
        var n42   = MakeNode<NumberNode>(graph, "Number_42");
        SetF(n42, "ValueFloat", 42f);
        var fe2   = MakeForeachNode(graph, "ForeachLoop_ListConst");
        var a42   = MakeNode<AssertNumberNode>(graph, "ListConstNode_WithNumbers");
        SetF(a42, "ValueFloat", 42f); SetF(a42, "ExpectEqual", true); SetF(a42, "RepeatNumber", 1);

        // ── FilterList_KeepsMatchingElements: list[1,2,5,10] → filter(>3) → loop → assertLimits(4..10) ──
        var l1210 = MakeListNumbersNode(graph, "List_1_2_5_10", 1f, 2f, 5f, 10f);
        var filt  = MakeNode<FilterListByConditionNode>(graph, "Filter_GT3");
        SetF(filt, "Type", NodeElementType.Numbers);
        CallOnBeforeSerialize(filt);
        var cmp   = MakeNode<MathComparisonNode>(graph, "Compare_GT3");
        SetF(cmp, "comparison", MathComparisons.GreaterThan);
        var n3    = MakeNode<NumberNode>(graph, "Number_3_Threshold");
        SetF(n3, "ValueFloat", 3f);
        var fe3   = MakeForeachNode(graph, "ForeachLoop_FilterResult");
        var n7    = MakeNode<NumberNode>(graph, "Number_7_ForFilter");
        SetF(n7, "ValueFloat", 7f);
        var aFilt = MakeNode<AssertNumberLimitsNode>(graph, "FilterList_KeepsMatchingElements");
        SetF(aFilt, "MinInclusive", 4f); SetF(aFilt, "MaxInclusive", 10f);
        SetF(aFilt, "ExpectTrue", true); SetF(aFilt, "RepeatNumber", 1);

        Commit(graph,
            l369, fe1, n6, aLim,
            lc42, n42, fe2, a42,
            l1210, filt, cmp, n3, fe3, n7, aFilt);

        // ── Data connections ──
        Link(n6,  "Value", aLim,  "Input");   // 6 is in [3,9]
        Link(n42, "Value", a42,   "Input");   // 42 == 42
        Link(n7,  "Value", aFilt, "Input");   // 7 is in [4,10]

        // ── Structure connections ──
        // ListNumbersConst_Values
        TryLinkDynamic(l369, "Result", fe1, "List");
        Link(fe1, "LoopExecute", aLim, "Enter");

        // ListConstNode_WithNumbers
        TryLinkDynamic(n42, "Value", lc42, "Input0");   // NumberNode → ListConst input slot
        TryLinkDynamic(lc42, "Result", fe2, "List");
        Link(fe2, "LoopExecute", a42, "Enter");

        // FilterList
        TryLinkDynamic(l1210, "Result", filt, "List");
        TryLinkDynamic(filt, "CurrentElement", cmp, "a");
        Link(n3, "Value", cmp, "b");
        TryLinkDynamic(cmp, "Result", filt, "ShouldInclude");
        TryLinkDynamic(filt, "ResultList", fe3, "List");
        Link(fe3, "LoopExecute", aFilt, "Enter");

        EditorUtility.SetDirty(graph);
        Debug.Log("[CreateVSTestActionGraphs] ListNodesAG created.");
    }

    // ══════════════════════════════════════════════════════════════════════
    // Helper factories
    // ══════════════════════════════════════════════════════════════════════

    static ListNumbersConstNode MakeListNumbersNode(ActionGraph graph, string nodeName, params float[] values)
    {
        var node = MakeNode<ListNumbersConstNode>(graph, nodeName);
        SetF(node, "Values", new List<float>(values));
        CallOnBeforeSerialize(node);
        return node;
    }

    static ForeachLoopNode MakeForeachNode(ActionGraph graph, string nodeName)
    {
        var node = MakeNode<ForeachLoopNode>(graph, nodeName);
        SetF(node, "Type", NodeElementType.Numbers);
        CallOnBeforeSerialize(node);
        return node;
    }

    static ForEachConditionNode MakeForEachCondNode(ActionGraph graph, string nodeName, bool condition)
    {
        var node = MakeNode<ForEachConditionNode>(graph, nodeName);
        SetF(node, "Type", NodeElementType.Numbers);
        SetF(node, "Condition", condition);
        CallOnBeforeSerialize(node);
        return node;
    }

    // ══════════════════════════════════════════════════════════════════════
    // Low-level helpers
    // ══════════════════════════════════════════════════════════════════════

    static ActionGraph MakeGraph(string path, string assetName)
    {
        // Delete if already exists so we start fresh
        if (AssetDatabase.LoadAssetAtPath<ActionGraph>(path) != null)
            AssetDatabase.DeleteAsset(path);

        var graph = ScriptableObject.CreateInstance<ActionGraph>();
        graph.name = assetName;
        AssetDatabase.CreateAsset(graph, path);
        return graph;
    }

    static T MakeNode<T>(ActionGraph graph, string nodeName) where T : Node
    {
        T node = graph.AddNode<T>();
        node.name = nodeName;
        return node;
    }

    static void Commit(ActionGraph graph, params Node[] nodes)
    {
        foreach (Node n in nodes)
        {
            AssetDatabase.AddObjectToAsset(n, graph);
            EditorUtility.SetDirty(n);
        }
        EditorUtility.SetDirty(graph);
    }

    /// <summary>Connect output → input by field name; logs error if port missing.</summary>
    static void Link(Node from, string fromPort, Node to, string toPort)
    {
        NodePort op = from.GetOutputPort(fromPort);
        NodePort ip = to.GetInputPort(toPort);
        if (op == null) { Debug.LogError($"[Link] Output port '{fromPort}' not found on {from.name} ({from.GetType().Name})"); return; }
        if (ip == null) { Debug.LogError($"[Link] Input  port '{toPort}' not found on {to.name} ({to.GetType().Name})"); return; }
        if (!op.IsConnectedTo(ip)) op.Connect(ip);
    }

    /// <summary>Tries to connect; silently skips if either port is missing (dynamic ports may not exist).</summary>
    static void TryLinkDynamic(Node from, string fromPort, Node to, string toPort)
    {
        NodePort op = from.GetOutputPort(fromPort) ?? from.GetPort(fromPort);
        NodePort ip = to.GetInputPort(toPort)      ?? to.GetPort(toPort);
        if (op == null || ip == null)
        {
            Debug.LogWarning($"[TryLinkDynamic] Skipped {from.name}.{fromPort} → {to.name}.{toPort} (port not found)");
            return;
        }
        if (!op.IsConnectedTo(ip)) op.Connect(ip);
    }

    static void SetF(object obj, string fieldName, object value)
    {
        Type t = obj.GetType();
        FieldInfo fi = null;
        while (t != null && fi == null)
        {
            fi = t.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            t  = t.BaseType;
        }
        if (fi != null) fi.SetValue(obj, value);
        else Debug.LogError($"[SetF] Field '{fieldName}' not found on {obj.GetType().Name}");
    }

    static void CallOnBeforeSerialize(Node node)
    {
        if (node is ISerializationCallbackReceiver r) r.OnBeforeSerialize();
    }

    static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Replace("\\", "/").Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
