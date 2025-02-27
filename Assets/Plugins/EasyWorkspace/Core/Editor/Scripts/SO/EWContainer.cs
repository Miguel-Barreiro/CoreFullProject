using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace EasyWorkspace
{
    public class EWContainer : ScriptableObject
    {
        [SerializeField] private Object _root;
        [Space(20f)]
        [SerializeField] private VisualTreeAsset _workspaceUXML;
        [SerializeField] private VisualTreeAsset _initializationScreenUXML;
        [SerializeField] private VisualTreeAsset _workspaceTabUXML;
        [SerializeField] private VisualTreeAsset _customUXML;
        [SerializeField] private VisualTreeAsset _folderUXML;
        [SerializeField] private VisualTreeAsset _folderAssetContainerUXML;
        [SerializeField] private VisualTreeAsset _folderAssetContainerLineUXML;
        [SerializeField] private VisualTreeAsset _settingUXML;
        [Space(20f)]
        [SerializeField] private VisualTreeAsset[] _assetUXML;
        [SerializeField] private string[] _assetStyleNames;
        [Space(20f)]
        [SerializeField] private StyleSheet _folderCollapse;
        [SerializeField] private StyleSheet _collapseButton;
        [SerializeField] private StyleSheet _folderModeSwitchLine;
        [SerializeField] private StyleSheet _folderModeSwitchGrid;
        [Space(20f)]
        [SerializeField] private EWFileColorContainer _fileColorContainer;
        
        public static bool IsInitialized => Instance && Instance.Root;
        
        public Object Root => _root;
        public VisualTreeAsset InitializationScreenUXML => _initializationScreenUXML;
        public VisualTreeAsset WorkspaceUXML => _workspaceUXML;
        public VisualTreeAsset WorkspaceTabUXML => _workspaceTabUXML;
        public VisualTreeAsset CustomUXML => _customUXML;
        public VisualTreeAsset FolderUXML => _folderUXML;
        public VisualTreeAsset FolderAssetContainerUXML => _folderAssetContainerUXML;
        public VisualTreeAsset FolderAssetContainerLineUXML => _folderAssetContainerLineUXML;
        public VisualTreeAsset SettingsUXML => _settingUXML;
        public VisualTreeAsset[] AssetUXML => _assetUXML;
        public string[] AssetStyleNames => _assetStyleNames;
        public StyleSheet FolderCollapse => _folderCollapse;
        public StyleSheet CollapseButton => _collapseButton;
        public StyleSheet FolderModeSwitchLine => _folderModeSwitchLine;
        public StyleSheet FolderModeSwitchGrid => _folderModeSwitchGrid;
        public EWFileColorContainer FileColorContainer => _fileColorContainer;

        private static EWContainer _instance;

        public static EWContainer Instance
        {
            get
            {
                if (!_instance)
                {
                    string[] guids = AssetDatabase.FindAssets("t:" + typeof(EWContainer));
                    
                    if (guids.Length == 0)
                        return null;

                    _instance = AssetDatabase.LoadAssetAtPath<EWContainer>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }

                if (!_instance._root || !_instance._workspaceUXML)
                {
                    EditorUtility.CopySerialized(_instance, _instance);
                    
                    if (!_instance._root || !_instance._workspaceUXML)
                        return null;
                }

                return _instance;
            }
        }
    }
}