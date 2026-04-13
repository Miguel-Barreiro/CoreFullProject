using System.Linq;
using Core.Editor;
using Core.Model;
using Core.Model.Data;
using Core.Model.ModelSystems;
using Core.Utils.CachedDataStructures;
using Core.VSEngine;
using Core.VSEngine.Nodes.TestNodes;
using NUnit.Framework;
using UnityEngine;
using Zenject;

namespace Testing_Core.Editor.UnitTests.VSNodes.Lists
{
	public sealed class VSListNodesTest : UnitTest
	{
		[SerializeField] private ActionGraph ListNodes;

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
		public void ListNodesTest()
		{
			ActionGraph actionGraph = ListNodes;
			ExecuteTestNodes(actionGraph, VSEngineCore);

		}
	}
}
