using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace EasyWorkspace
{
    public class EWWorkspace : ScriptableObject
    {
        [SerializeField] private List<EWFile> _files = new List<EWFile>();
        [SerializeField] private Vector3 _position;
        [SerializeField] private Vector3 _zoom = Vector2.one;
        [SerializeField] private bool _added;
        [SerializeField] private bool _opened;
        [SerializeField] private int _order;
    
        public Vector3 Position { get => _position; set => SetAndSave(ref _position, value); }
        public Vector3 Zoom { get => _zoom; set => SetAndSave(ref _zoom, value); }
        public bool Added { get => _added; set => SetAndSave(ref _added, value); }
        public bool Opened { get => _opened; set => SetAndSave(ref _opened, value); }
        public int Order { get => _order; set => SetAndSave(ref _order, value); }

        public List<EWFile> CurrentFiles
        {
            get
            {
                _files.RemoveAll(file => file == null);
                return _files;
            }
        }
    
        public UnityAction<float> ZoomChanged;
    
        private void SetAndSave<T>(ref T field, T value)
        {
            field = value;
            Save();
        }

        private void Save()
        {
            EditorUtility.SetDirty(this);
        }
        
        private void SaveAssets()
        {
            EWWindow.SetTrackingProjectChanges(false);
            AssetDatabase.SaveAssets();
            EWWindow.SetTrackingProjectChanges(true);
        }
    
        public void AddFile(EWFile file, bool undo = false)
        {
            _files.Add(file);

            AssetDatabase.AddObjectToAsset(file, this);
        
            if (undo)
                Undo.RegisterCreatedObjectUndo(file, "Create file");
        
            SaveAssets();
        }
    
        public EWFile CreateFile(System.Type type)
        {
            EWFile file = CreateInstance(type) as EWFile;
            file.name = type.Name;
            file.hideFlags = HideFlags.HideInHierarchy;
        
            Undo.RecordObject(this, "Create file");
        
            AddFile(file, true);
        
            return file;
        }

        public void RemoveFile(EWFile fileViewFile)
        {
            Undo.RecordObject(this, "Remove file");
        
            _files.Remove(fileViewFile);
        
            Undo.DestroyObjectImmediate(fileViewFile);
            
            SaveAssets();
        }
    }
}