using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyWorkspace
{
    public class EWFolderView : EWFileView<EWFolder>
    {
        private ScrollView _scrollView;
        private ToolbarSearchField _searchField;
        private VisualElement _modeSwitchButton;
    
        private EWAssetContainer[] _assetContainers = Array.Empty<EWAssetContainer>();
    
        private bool _assetHover;
        private bool _isAssetsInitialized;
        private bool _lastScrollerEnabled;
        private VisualTreeAsset _currentAssetContainerUXML;
    
        private float TargetAssetSize => 90f;
        private float AssetMarginHorizontal => 2f;
        private float AssetMarginVertical => 3f;
        private float AssetPaddingTop => 5f;
        private float AssetMinSize => 0.5f;
        private float ContainerPadding => 3f;
        private float ContainerPaddingLeft => ContainerPadding + ScrollerSize / 2f - CurrentScrollerSize / 2f;
        private float ContainerPaddingRight => ContainerPadding + ScrollerSize / 2f + CurrentScrollerSize / 2f;
        private float ScrollerOffset => 5f;
        private float ScrollerSize => 13f + ScrollerOffset;
        private float CurrentScrollerSize => IsScrollerEnabled ? ScrollerSize : 0f;
        private bool IsScrollerEnabled => _scrollView.verticalScroller.enabledSelf;
        private bool LineMode => File.AssetScale < AssetMinSize;
    
        public EWFolderView(EWFolder file) : base(file)
        {
            File.UpdateAssets();
            
            UpdateModeSwitch(true);
        }

        protected override void LoadUXML()
        {
            LoadUXML(EWContainer.Instance.FolderUXML);
        }
    
        protected override void InitializeComponents()
        {
            base.InitializeComponents();
        
            _scrollView = this.Q<ScrollView>();
            _searchField = this.Q<ToolbarSearchField>();
            _modeSwitchButton = this.Q("ModeSwitch");

            _scrollView.verticalScroller.style.right = ScrollerOffset;
            _searchField.value = File.SearchFilter;
            _modeSwitchButton.AddToClassList("Grid");
        }

        protected override void RegisterCallbacks()
        {
            base.RegisterCallbacks();
        
            _scrollView.verticalScroller.valueChanged += OnVerticalScrollerValueChanged;
            _scrollView.contentContainer.RegisterCallback<GeometryChangedEvent>(OnScrollViewInitialized);
            _scrollView.contentContainer.RegisterCallback<GeometryChangedEvent>(OnScrollViewGeometryChanged);
        
            _searchField.RegisterValueChangedCallback(OnSearchValueChanged);
        
            _modeSwitchButton.RegisterCallback<MouseDownEvent>(OnModeSwitchClicked);
        }
    
        protected override void OnDragUpdated(DragUpdatedEvent evt)
        {
            base.OnDragUpdated(evt);
        
            if (!CanAddDragAndDropAssets()) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }
    
        protected override void OnDragPerform(DragPerformEvent evt)
        {
            base.OnDragPerform(evt);
        
            if (!CanAddDragAndDropAssets()) return;
        
            File.AddAssets(DragAndDrop.objectReferences);
        
            UpdateAssets();

            DragAndDrop.AcceptDrag();
            DragAndDrop.objectReferences = Array.Empty<UnityEngine.Object>();
        }

        public override void UpdateFile()
        {
            base.UpdateFile();
        
            _searchField.value = File.SearchFilter;
            UpdateAssets();
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
        
            UpdateAssets();
        }

        private void UpdateAssets()
        {
            InitializeAssetContainers();

            _scrollView.contentContainer.style.flexDirection = LineMode ? FlexDirection.Column : FlexDirection.Row;
            _scrollView.contentContainer.style.flexWrap = LineMode ? Wrap.NoWrap : Wrap.Wrap;
        
            float containerSize = _scrollView.layout.width - (ContainerPaddingLeft + ContainerPaddingRight);
            float scale = Mathf.Min(File.AssetScale, containerSize / TargetAssetSize);
            float assetSize = TargetAssetSize * scale;
            int columnCount = Mathf.Max(Mathf.FloorToInt(containerSize / (assetSize + AssetMarginHorizontal * 2f)), 1);
            float margin = (containerSize - columnCount * assetSize) / (columnCount * 2f);

            for (int i = 0; i < _assetContainers.Length; i++)
            {
                VisualElement cell = _assetContainers[i].parent.parent;
                VisualElement container = _assetContainers[i].parent;

                if (!LineMode)
                {
                    cell.style.width = assetSize;
                    cell.style.height = assetSize;
                    cell.style.minWidth = assetSize;
                    cell.style.minHeight = assetSize;
                    container.transform.scale = Vector3.one * scale;
                    container.style.width = new StyleLength(new Length(100f / scale, LengthUnit.Percent));
                    container.style.height = new StyleLength(new Length(100f / scale, LengthUnit.Percent));
                
                    cell.style.marginLeft = margin + (i % columnCount == 0 ? ContainerPaddingLeft : 0f);
                    cell.style.marginRight = margin;
                    cell.style.marginTop = AssetMarginVertical + (i < columnCount ? AssetPaddingTop : 0f);
                    cell.style.marginBottom = AssetMarginVertical;
                }
                else
                {
                    if (i == 0)
                        cell.style.marginTop = ContainerPadding;
                    else if (i == _assetContainers.Length - 1)
                        cell.style.marginBottom = ContainerPadding;
                }
            
                _assetContainers[i].UpdateContainer(File.GetFilteredAsset(i));
            }
        
            ForceUpdateScrollView();
        
            _lastScrollerEnabled = IsScrollerEnabled;
            _isAssetsInitialized = true;
        }
    
        protected override bool CanAddDragAndDropAssets()
        {
            if (DragAndDrop.objectReferences.Length <= 0) return false;
        
            object data = DragAndDrop.GetGenericData("EW");
            if (data == this) return false;
        
            if (!File.CanAddAssets) return false;
        
            return true;
        }
    
        private void OnScrollViewInitialized(GeometryChangedEvent evt)
        {
            if (_isAssetsInitialized)
                _scrollView.contentContainer.UnregisterCallback<GeometryChangedEvent>(OnScrollViewInitialized);
            else
                return;
        
            _scrollView.verticalScroller.value = File.ScrollPosition.y;
        
            UpdateAssets();
        }
    
        private void OnScrollViewGeometryChanged(GeometryChangedEvent evt)
        {
            if (_lastScrollerEnabled != IsScrollerEnabled)
                UpdateAssets();
        }
    
        private void OnSearchValueChanged(ChangeEvent<string> evt)
        {
            File.SearchFilter = evt.newValue;
        
            File.UpdateFilteredAssets();
            UpdateAssets();
        }
    
        private void ForceUpdateScrollView()
        {
            _scrollView.schedule.Execute(() =>
            {
                var fakeOldRect = Rect.zero;
                var fakeNewRect = _scrollView.layout;

                using GeometryChangedEvent evt = GeometryChangedEvent.GetPooled(fakeOldRect, fakeNewRect);
                evt.target = _scrollView.contentContainer;
                _scrollView.contentContainer.SendEvent(evt);
            });
        }
    
        private void InitializeAssetContainers()
        {
            bool needUpdateColor = false;
        
            VisualTreeAsset uxml = LineMode ? EWContainer.Instance.FolderAssetContainerLineUXML : EWContainer.Instance.FolderAssetContainerUXML;
            if (_currentAssetContainerUXML != uxml)
            {
                _currentAssetContainerUXML = uxml;
            
                foreach (EWAssetContainer assetContainer in _assetContainers)
                {
                    assetContainer.HoverUpdated -= OnUpdateAssetHover;
                    assetContainer.parent.parent.RemoveFromHierarchy();
                }
            
                _assetContainers = Array.Empty<EWAssetContainer>();
            
                needUpdateColor = true;
            }
        
            for (int i = _assetContainers.Length; i < File.FilteredAssetCount; i++)
            {
                VisualElement assetContainer = _currentAssetContainerUXML.CloneTree();
                _scrollView.Add(assetContainer);
            }
        
            for (int i = File.FilteredAssetCount; i < _assetContainers.Length; i++)
            {
                _assetContainers[i].HoverUpdated -= OnUpdateAssetHover;
                _assetContainers[i].parent.parent.RemoveFromHierarchy();
            }

            _assetContainers = _scrollView.Query<EWAssetContainer>().ToList().ToArray();

            foreach (EWAssetContainer assetContainer in _assetContainers)
            {
                assetContainer.HoverUpdated -= OnUpdateAssetHover;
                assetContainer.HoverUpdated += OnUpdateAssetHover;
            }
        
            if (needUpdateColor)
                UpdateColor();
        }
    
        private void OnUpdateAssetHover(bool hover)
        {
            _assetHover = hover;
        
            UpdateHover();
        }
    
        protected override void UpdateHover(bool panelHover)
        {
            base.UpdateHover(panelHover && !_assetHover);
        }
    
        private void OnVerticalScrollerValueChanged(float value)
        {
            File.ScrollPosition = new Vector2(File.ScrollPosition.x, value);
        }
        
        private void OnModeSwitchClicked(MouseDownEvent evt)
        {
            File.AssetScale = LineMode ? 0.9f : 0f;
            
            UpdateModeSwitch();
            UpdateAssets();
            _scrollView.schedule.Execute(UpdateAssets);
        }
        
        private void UpdateModeSwitch(bool force = false)
        {
            if (force)
            {
                List<VisualElement> children = new List<VisualElement>();
                List<float> transitionDurations = new List<float>();
                foreach (VisualElement visualElement in _modeSwitchButton.Query().ToList())
                {
                    if (!visualElement.resolvedStyle.transitionDuration.Any())
                        continue;
                    
                    children.Add(visualElement);
                    transitionDurations.Add(visualElement.resolvedStyle.transitionDuration.First().value);
                    visualElement.style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue> {new TimeValue(0f)});
                }

                RegisterCallback<GeometryChangedEvent>(_ => ReturnModeSwitchTransitionDuration(children, transitionDurations));
            }
            
            styleSheets.Remove(LineMode ? EWContainer.Instance.FolderModeSwitchGrid : EWContainer.Instance.FolderModeSwitchLine);
            styleSheets.Add(LineMode ? EWContainer.Instance.FolderModeSwitchLine : EWContainer.Instance.FolderModeSwitchGrid);
        }

        private void ReturnModeSwitchTransitionDuration(List<VisualElement> children, List<float> transitionDurations)
        {
            for (int i = 0; i < children.Count; i++)
            {
                children[i].style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue> {new TimeValue(transitionDurations[i])});
            }
            
            UnregisterCallback<GeometryChangedEvent>(_ => ReturnModeSwitchTransitionDuration(children, transitionDurations));
        }
    
        public override bool IsSelectable()
        {
            Event evt = Event.current;
            if (evt != null && _assetHover)
                return false;
        
            return base.IsSelectable();
        }
    }
}