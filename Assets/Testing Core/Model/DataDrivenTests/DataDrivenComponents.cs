using System;
using System.Collections.Generic;
using Core.Model;
using Core.Model.ModelSystems;
using Core.Systems;
using Core.Utils;
using UnityEngine;

namespace Testing_Core.Model.DataDrivenTests
{
	
	public sealed class EntityTestDD : Entity, TestComponentOwnerDD2, TestComponentOwnerDD1
	{ }
	
	
	public struct TestDD2ComponentData : IComponentData
	{
		public EntId ID { get; set; }
		public int Value2;
		public void Init() { Value2 = 0; }
	}
	public interface TestComponentOwnerDD2 : Component<TestDD2ComponentData> { }
	

	[ComponentData(Priority = SystemPriority.Default, ContainerType = typeof(CustomComponentContainerDD1))]
	public struct TestDD1ComponentData : IComponentData
	{
		public EntId ID { get; set; }
		public int Value;
		public void Init() { Value = 0; }
	}
	public interface TestComponentOwnerDD1 : Component<TestDD1ComponentData> { }


	public sealed class CustomComponentContainerDD1 : ComponentContainer<TestDD1ComponentData>
	{
		// public TestDD1ComponentData[] Components = null;
		
		public PushBackArray<TestDD1ComponentData>[] Components;
		
		private readonly Dictionary<EntId, DD1Attributes> ComponentIndexByOwner = new Dictionary<EntId, DD1Attributes>();

		private class DD1Attributes
		{
			public uint Index;
			public uint ArrayType;
			public DD1Attributes(uint index, uint arrayType)
			{
				Index = index;
				ArrayType = arrayType;
			}
		}
		
		/// This is a dummy component to return when the requested component is not found
		/// do not override it
		public TestDD1ComponentData Invalid = new TestDD1ComponentData()
		{
			ID = EntId.Invalid
		};
		
		public CustomComponentContainerDD1(uint maxNumber)
		{
			RebuildWithMax(maxNumber);
		}
		
		public void SetupComponent(EntId owner)
		{
			if (ComponentIndexByOwner.ContainsKey(owner))
			{
				Debug.LogError($"Component(TestDD1ComponentData) already exists for owner {owner}");
				return;
			}

			PushBackArray<TestDD1ComponentData> pushBackArray = Components[0];
			TestDD1ComponentData[] componentDatas = pushBackArray.Items;
			if (!pushBackArray.SetupNew(out uint newIndex))
			{
				Debug.LogError($"No available space for new component(TestDD1ComponentData), FILLED[{pushBackArray.Count}] ");
				return;
			}

			ref TestDD1ComponentData dd1ComponentData = ref componentDatas[newIndex];
			
			dd1ComponentData.ID = owner;
			ComponentIndexByOwner[owner] = new DD1Attributes(newIndex, 0);
			dd1ComponentData.Init();
		}

		public void RemoveComponent(EntId owner)
		{
			if (!ComponentIndexByOwner.TryGetValue(owner, out DD1Attributes attributes))
				return;
			
			uint arrayType = attributes.ArrayType;
			uint index = attributes.Index;
			
			PushBackArray<TestDD1ComponentData> pushBackArray = Components[arrayType];
			pushBackArray.Remove(index, out uint changedIndex);
			
			ref TestDD1ComponentData dd1ComponentData = ref pushBackArray.Items[changedIndex];
			EntId changedEntityID = dd1ComponentData.ID;

			ComponentIndexByOwner[changedEntityID].Index = changedIndex;
			ComponentIndexByOwner.Remove(owner);
		}
		
		public ref TestDD1ComponentData GetComponent(EntId owner)
		{
			if (!ComponentIndexByOwner.TryGetValue(owner, out DD1Attributes attributes))
			{
				Debug.LogError($"Component({typeof(TestDD1ComponentData)}) not found for owner {owner}");
				return ref Invalid;
			}
			uint arrayType = attributes.ArrayType;
			uint index = attributes.Index;
			
			return ref Components[arrayType].Items[index];
		}
		
		public void RebuildWithMax(uint maxNumber)
		{
			
			ComponentIndexByOwner.Clear();
			Components = new PushBackArray<TestDD1ComponentData>[3] 
			{
				new PushBackArray<TestDD1ComponentData>(maxNumber), 
				new PushBackArray<TestDD1ComponentData>(maxNumber), 
				new PushBackArray<TestDD1ComponentData>(maxNumber)
			};
		}
		
		public uint Count => (uint) Math.Max(Components[0].Count,Mathf.Max(  Components[1].Count,  Components[2].Count));
		public uint MaxCount => Components[0].MaxCount;

	}


}