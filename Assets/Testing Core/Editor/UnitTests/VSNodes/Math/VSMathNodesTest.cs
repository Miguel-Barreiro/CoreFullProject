using System.Collections.Generic;
using System.Linq;
using Core.Editor;
using Core.Model;
using Core.Model.Data;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.Utils.CachedDataStructures;
using Core.VSEngine;
using Core.VSEngine.Nodes.TestNodes;
using NUnit.Framework;
using Testing_Core.Editor.UnitTests.AbilityTests;
using UnityEngine;
using Zenject;

namespace Testing_Core.Editor.UnitTests.VSNodes.Math
{
	public sealed class VSMathNodesTest : UnitTest
	{
		
		[SerializeField] private ActionGraph  MathNodes;
		
		[Inject] private readonly IEntityHierarchySystem hierarchySystem = null!;
		[Inject] private readonly EntitiesContainer entitiesContainer = null!;
		[Inject] private readonly VSEngineCore VSEngineCore = null!;

		
		protected override void InstallTestSystems(IUnitTestInstaller installer)
		{
			
		}
		protected override void ResetComponentContainers(DataContainersController dataController)
		{
		}
		
		
		[TearDown]
		public void TearDown()
		{
			var allEntities = entitiesContainer.GetAllEntitiesByType<Entity>().ToList();
			foreach (var entity in allEntities)
				EntitiesContainer.DestroyEntity(entity.ID);

			ExecuteFrame(0.1f);
			EntitiesContainer.Reset();
		}

		[Test]
		public void BasicMathNodesTest()
		{
			// foreach (ActionGraph actionGraph in MathNodes)
			// {
			ActionGraph actionGraph = MathNodes; 
			
				using CachedList<BaseTestAssertNode> assertNodes = ListCache<BaseTestAssertNode>.Get();
				VSBaseEngine.GetAssertNodes(actionGraph, assertNodes);

				foreach (BaseTestAssertNode assertNode in assertNodes)
				{
					Debug.Log($"Running assert node: {assertNode.name}");
					VSEngineCore.RunTestNode(assertNode);
				}

			// }
			// var owner = new OwnerEntity();
			// var ability = new VSAbility(owner.ID, TEST_ABILITY_CONFIG);
			//
			// var children = hierarchySystem.GetChildrenList(owner.ID);
			// Assert.That(children, Contains.Item(ability.ID));
			// Assert.That(children.Count, Is.EqualTo(1));
		}

		
	}
}