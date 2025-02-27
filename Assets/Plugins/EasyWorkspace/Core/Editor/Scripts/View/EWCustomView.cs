using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyWorkspace
{
    public class EWCustomView<T> : EWFileView<T> where T : EWCustom
    {
        protected IMGUIContainer GUIContainer;

        public EWCustomView(T file) : base(file)
        {
            EditorApplication.update += OnEditorUpdate;
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);
        }

        private void OnDetachFromPanelEvent(DetachFromPanelEvent evt)
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            GUIContainer.MarkDirtyRepaint();
            Update();
        }

        protected virtual void Update()
        {
        
        }

        protected override void LoadUXML()
        {
            LoadUXML(EWContainer.Instance.CustomUXML);
        }
    
        protected override void InitializeComponents()
        {
            base.InitializeComponents();
        
            GUIContainer = this.Q<IMGUIContainer>();
            GUIContainer.onGUIHandler = File.DrawTotalGUI;
        }

        protected override void OnCollapseValueChanged(bool collapsed)
        {
            base.OnCollapseValueChanged(collapsed);

            GUIContainer.style.display = File.Collapsed ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    /// <summary>
    /// Base class for custom panels
    /// </summary>
    public abstract class EWCustom : EWFile, EWICollapsible
    {
        protected sealed override string DefaultTitle => GetAttribute().DefaultTitle;
        public sealed override Vector2 DefaultSize => GetAttribute().DefaultSize;
        public string CollapseInfo => GetCollapseInfo();

        protected float LabelWidth; 

        public sealed override EWFileView CreateView()
        {
            return new EWCustomView<EWCustom>(this);
        }
    
        /// <summary>
        /// Draws the GUI of the panel
        /// </summary>
        protected abstract void DrawGUI();
        
        /// <summary>
        /// Information to be displayed when the panel is collapsed
        /// </summary>
        protected virtual string GetCollapseInfo() => "";
        
        private EWCustomAttribute GetAttribute()
        {
            return (EWCustomAttribute) Attribute.GetCustomAttribute(GetType(), typeof(EWCustomAttribute));
        }

        public void DrawTotalGUI()
        {
            EditorGUI.BeginChangeCheck();
            float width = EditorGUIUtility.labelWidth;
            if (LabelWidth > 0f)
                EditorGUIUtility.labelWidth = LabelWidth;
            DrawGUI();
            EditorGUIUtility.labelWidth = width;
            if (EditorGUI.EndChangeCheck())
            {
                SerializedObject.ApplyModifiedProperties();
                Save();
            }
        }
    }
}