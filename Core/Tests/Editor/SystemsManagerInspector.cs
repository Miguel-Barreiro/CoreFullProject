using System.Collections.Generic;
using Core.Systems;
using Core.Utils.CachedDataStructures;
using UnityEngine;
using UnityEngine.UIElements;

namespace Core.Tests.Editor
{
    [UnityEditor.CustomEditor(typeof(Systems.SystemsManager))]
    public class SystemsManagerInspector : UnityEditor.Editor
    {
        
        private const string OFF_LABEL = "OFF";
        private const string ON_LABEL = "ON";
        
        
        private readonly Color COLOR_RED = new Color(0.5f, 0.15f, 0.15f);
        private readonly Color COLOR_GREEN = new Color(0.15f, 0.4f, 0.15f);
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
        }
        public override VisualElement CreateInspectorGUI()
        {
            // Create a new VisualElement to be the root of our Inspector UI.
            VisualElement myInspector = new VisualElement();

            // Add a simple label.
            myInspector.Add(new Label("This is a custom Inspector"));

            Systems.SystemsManager systemsManager = (Systems.SystemsManager) target;
            DrawEntitySystems(systemsManager, myInspector);
            
            // Return the finished Inspector UI.
            return myInspector;
        }
        
        private void DrawEntitySystems(SystemsManager systemsManager, VisualElement parent)
        {
            SystemsContainer systemsContainer = systemsManager.SystemsContainer;


            if (systemsContainer != null)
            {
                IEnumerable<ISystem> allComponentSystems = systemsContainer.GetAllSystemsByInterface<ISystem>();
                using CachedList<ISystem> systems  = ListCache<ISystem>.Get();
                systems.AddRange(allComponentSystems);

                parent.Add(new Label($"Systems Count {systems.Count}"));
                
                foreach (ISystem system in systems)
                {
                    VisualElement systemElement = new VisualElement();
                    systemElement.style.flexDirection = FlexDirection.Row;
                    parent.Add(systemElement);
                    
                    Button button = new Button();
                    button.clicked += ()=>
                    {
                        system.Active = !system.Active;
                    };
                    button.style.backgroundColor = system.Active ? COLOR_GREEN : COLOR_RED;
                    button.style.color = Color.white;
                    button.text = system.Active? ON_LABEL : OFF_LABEL;

                    Label label = new Label($"  + {system.GetType().Name}");
                    systemElement.Add(label);
                    systemElement.Add(button);
                    systemElement.style.alignContent = Align.Center;
                    
                }

            }
        }
    }
}