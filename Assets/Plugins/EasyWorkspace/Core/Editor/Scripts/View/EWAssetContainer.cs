using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace EasyWorkspace
{
    public class EWAssetContainer : VisualElement, EWIAssetContainer
    {
        public new class UxmlFactory : UxmlFactory<EWAssetContainer, UxmlTraits> { }

        private EWFileView _fileView;
        private VisualElement _icon;
        private Label _title;

        private bool _hover;
        private bool _initialized;
        private float _titleSize;

        private Texture2D _texture;

        private float MinTitleSize = 10f;
        private float MaxTitleSize = 24f;

        public bool Hover => _hover;

        public Object Asset { get; set; }
        public EWFileView FileView => _fileView;
        public string DragTitle => Asset.name;
        public UnityAction Clicked { get; set; }

        public UnityAction<bool> HoverUpdated;

        public EWAssetContainer()
        {
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    
            RegisterCallback<MouseDownEvent>(OnMouseDownEventAsset);
            RegisterCallback<MouseUpEvent>(OnMouseUpEventAsset);
    
            RegisterCallback<PointerEnterEvent>(OnPointerEnterEventAsset);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeaveEventAsset);
    
            EditorApplication.hierarchyChanged += OnProjectChanged;
        }

        private void TryInitialize()
        {
            if (!_initialized)
            {
                _initialized = true;
        
                _fileView = GetFirstAncestorOfType<EWFileView>();
                _icon = parent.Q<VisualElement>("AssetIcon") ?? this;
                _title = this.Query<Label>("AssetTitle");

                if (_title != null)
                    _titleSize = _title.resolvedStyle.fontSize;
            }
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            EditorApplication.hierarchyChanged -= OnProjectChanged;
        }

        private void OnProjectChanged()
        {
            UpdateIcon();
        }

        private void OnPointerEnterEventAsset(PointerEnterEvent evt)
        {
            AddToClassList("asset-hover");

            _hover = true;
    
            HoverUpdated?.Invoke(true);
        }

        private void OnPointerLeaveEventAsset(PointerLeaveEvent evt)
        {
            RemoveFromClassList("asset-hover");

            _hover = false;
    
            HoverUpdated?.Invoke(false);
        }

        private void OnMouseDownEventAsset(MouseDownEvent evt)
        {
            evt.StopImmediatePropagation();
    
            this.AssetDown(evt);
        }

        private void OnMouseUpEventAsset(MouseUpEvent evt)
        {
            evt.StopImmediatePropagation();
    
            if (!_hover)
                return;
    
            Clicked?.Invoke();
    
            this.AssetUp(evt);
        }

        private Texture2D GetIcon()
        {
            if (!Asset) return null;
    
            Texture2D icon = AssetPreview.GetAssetPreview(Asset);
            if (!icon) icon = AssetPreview.GetMiniThumbnail(Asset);

            return icon;
        }

        private void UpdateIcon()
        {
            if (_icon == null)
                return;
    
            _icon.style.backgroundImage = GetIcon();
        }

        private void UpdateTitle()
        {
            if (_title != null)
            {
                _title.text = Asset.name;
                UpdateTitleSize();
            }
        }

        private void UpdateTitleSize()
        {
            if (_title != null)
            {
                float titleSize = _titleSize / parent.transform.scale.x;
                titleSize = Mathf.Clamp(titleSize, MinTitleSize, MaxTitleSize);
        
                _title.style.fontSize = titleSize;
            }
        }

        public void UpdateContainer()
        {
            TryInitialize();

            UpdateIcon();
            UpdateTitle();
            UpdateTitleSize();
        }

        public void UpdateContainer(Object asset)
        {
            Asset = asset;
    
            UpdateContainer();
        }

        public void UpdateColor(Color colorPanel, Color colorToolbar)
        {
            style.backgroundColor = colorPanel;
            if (_title == null)
                return;

            if (_title.resolvedStyle.backgroundColor != Color.clear)
                _title.style.backgroundColor = colorToolbar;
        }

        public void PressAsset()
        {
            AddToClassList("asset-pressed");
        }

        public void UnpressAsset()
        {
            RemoveFromClassList("asset-pressed");
        }
    }
}