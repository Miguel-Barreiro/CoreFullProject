using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyWorkspace
{
    public class EWAssetView : EWFileView<EWAsset>
    {
        private EWAssetContainer _asset;
        private int _styleIndex;

        public EWAssetView(EWAsset file) : base(file)
        {
            _asset.UpdateContainer(File.Asset);
        }

        protected override void InitializeLayout(GeometryChangedEvent evt)
        {
            base.InitializeLayout(evt);
        
            GraphView.ObjectPickerUpdated += OnObjectPickerUpdated;
        }

        protected override void LoadUXML()
        {
            _styleIndex = File.StyleIndex;
            LoadUXML(File.Style);
        }

        protected override void InitializeComponents()
        {
            base.InitializeComponents();

            _asset = this.Q<EWAssetContainer>("Asset");
        }

        protected override void RegisterCallbacks()
        {
            base.RegisterCallbacks();
        
            _asset.HoverUpdated += OnUpdateAssetHover;
            _asset.Clicked += AssetClicked;
        }
    
        protected override void UnregisterCallbacks()
        {
            base.UnregisterCallbacks();
        
            GraphView.ObjectPickerUpdated -= OnObjectPickerUpdated;
            _asset.Clicked -= AssetClicked;
        }

        public override void UpdateFile()
        {
            File.Size = File.DefaultSize;
            UpdateStyle();
            
            base.UpdateFile();
        
            UpdateAsset();
        }

        private void UpdateAsset()
        {
            _asset.UpdateContainer(File.Asset);
        }
    
        protected override void UpdateHover(bool panelHover)
        {
            base.UpdateHover(panelHover && !_asset.Hover);
        }
    
        private void OnUpdateAssetHover(bool hover)
        {
            UpdateHover(PanelHover);
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            for (int i = 0; i < EWContainer.Instance.AssetUXML.Length; i++)
            {
                VisualTreeAsset assetStyle = EWContainer.Instance.AssetUXML[i];
                int styleIndex = i;
                evt.menu.AppendAction("View/" + EWContainer.Instance.AssetStyleNames[i], _ => UpdateStyle(styleIndex),
                    File.Style == assetStyle ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }

            base.BuildContextualMenu(evt);
        }

        protected override void OnDragUpdated(DragUpdatedEvent evt)
        {
            base.OnDragUpdated(evt);

            if (DragAndDrop.objectReferences.Length != 1) return;
        
            Object asset = EWSystem.ConvertDropObject(DragAndDrop.objectReferences[0]);
            if (asset != null)
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }
    
        protected override void OnDragPerform(DragPerformEvent evt)
        {
            base.OnDragPerform(evt);
        
            Object asset = EWSystem.ConvertDropObject(DragAndDrop.objectReferences[0]);
            if (asset != null)
            {
                File.Asset = asset;
                UpdateFile();
            }
        }
    
        private void OnObjectPickerUpdated()
        {
            if (EditorGUIUtility.GetObjectPickerControlID() != GetHashCode()) return;
        
            File.Asset = EditorGUIUtility.GetObjectPickerObject();
            UpdateFile();
        }
    
        private void AssetClicked()
        {
            if (File.Asset == null)
                ShowAssetPicker();
        }

        private void UpdateStyle(int index)
        {
            File.StyleIndex = index;
            UpdateStyle();
        }
        
        private void UpdateStyle()
        {
            if (_styleIndex == File.StyleIndex) return;
            _styleIndex = File.StyleIndex;

            UnregisterCallbacks();
            Clear();
            LoadUXML(EWContainer.Instance.AssetUXML[File.StyleIndex]);
            InitializeComponents();
            RegisterCallbacks();
            UpdateFile();
        }

        protected override void UpdateTitleColor(Color color)
        {
            if (_styleIndex == 1)
                return;
            
            base.UpdateTitleColor(color);
        }

        public void ShowAssetPicker()
        {
            EditorGUIUtility.ShowObjectPicker<Object>(File.Asset, false, "", GetHashCode());
        }
    
        public override bool IsSelectable()
        {
            Event evt = Event.current;
            if (_asset != null && evt != null && _asset.Hover)
                return false;
        
            return base.IsSelectable();
        }
    }
}