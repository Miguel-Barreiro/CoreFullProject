using System;
using UnityEditor;
using UnityEngine;

namespace EasyWorkspace
{
    public abstract class EWFile : ScriptableObject
    {
        [SerializeField] private string _title;
        [SerializeField] private Vector3 _position;
        [SerializeField] private Vector3 _size;
        [SerializeField] private bool _collapsed;
        [SerializeField] private string _color = "Default";

        private SerializedObject _serializedObject;
        
        public Vector3 Position { get => _position; set => SetAndSave(ref _position, value); }
        public Vector3 Size { get => _size; set => SetAndSave(ref _size, value); }
        public bool Collapsed { get => _collapsed; set => SetAndSave(ref _collapsed, value); }
        public string Color { get => _color; set => SetAndSave(ref _color, value); }
        
        /// <summary>
        /// Set to true if the panel needs to be updated
        /// </summary>
        public bool NeedUpdate { get; protected set; }

        protected abstract string DefaultTitle { get; }
        public abstract Vector2 DefaultSize { get; }

        internal string Title => string.IsNullOrEmpty(_title) ? DefaultTitle : _title;

        protected SerializedObject SerializedObject => _serializedObject ??= new SerializedObject(this);

        public abstract EWFileView CreateView();
    
        /// <summary>
        /// Sets the value of a field and saves the file
        /// </summary>
        protected void SetAndSave<T>(ref T field, T value)
        {
            field = value;
            Save();
        }
        
        /// <summary>
        /// Saves the file
        /// </summary>
        public void Save()
        {
            EditorUtility.SetDirty(this);
        }
    
        private void DrawBaseInspectorGUI()
        {
            _title = EditorGUILayout.TextField("Title", _title);
            EditorGUILayout.Space(10f);
        }
        
        /// <summary>
        /// Draws the inspector GUI of the panel
        /// </summary>
        protected virtual void DrawInspectorGUI() { }

        public void DrawInspectorGUI(float labelWidth)
        {
            _serializedObject = new SerializedObject(this);
            
            float lastLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = labelWidth;
        
            DrawBaseInspectorGUI();
            DrawInspectorGUI();
        
            EditorGUIUtility.labelWidth = lastLabelWidth;
        
            SerializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Called when the inspector GUI is updated
        /// </summary>
        public virtual void Update()
        {
            
        }
        
        public void Validate()
        {
            if (_size == Vector3.zero)
            {
                _size = DefaultSize;
                _position -= _size / 2f;
            }
            
            EWResizableAttribute resizableAttribute = (EWResizableAttribute) Attribute.GetCustomAttribute(GetType(), typeof(EWResizableAttribute));
            if (resizableAttribute == null && (Vector2) _size != DefaultSize)
            {
                Size = DefaultSize;
            }
            
            if (EWFileColorContainer.GetFileColor(Color).Name == "Default" && Color != "Default")
                Color = "Default";
        }
    }
}