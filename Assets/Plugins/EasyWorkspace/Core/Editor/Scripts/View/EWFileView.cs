using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace EasyWorkspace
{
    public abstract class EWFileView<T> : EWFileView where T : EWFile
    {
        protected new T File => base.File as T;

        protected EWFileView(T file) : base(file)
        {
        
        }
    }

    public abstract class EWFileView : GraphElement, ICollectibleElement
    {
        public EWFile File { get; protected set; }

        protected EWGraph GraphView;
    
        protected VisualElement Panel;
        private VisualElement _toolbar;
        private Label _title;
        private ResizableElement _resizableElement;
        private EWCollapseButton _collapseButton;
        private Label _collapsedInfo;
        private VisualElement _dropIndicator;

        protected bool PanelHover;
    
        private Vector3 _offset;
        private IVisualElementScheduledItem _collapseAnimation;
        private Action _collapseAnimationAction;
    
        public static UnityAction<EWFileView, MouseDownEvent> MouseDown;
    
        public static UnityAction<EWFileView> Selected;
        public static UnityAction<EWFileView> Unselected;

        protected EWFileView(EWFile file)
        {
            File = file;
        
            LoadUXML();
            InitializeComponents();
            AddManipulators();
            InitializeCapabilities();
            RegisterCallbacks();
            TrySetUpResizableElement();
            OnCollapseValueChanged(File.Collapsed);
        }
    
        protected void LoadUXML(VisualTreeAsset uxml)
        {
            uxml.CloneTree(this);
        }

        protected abstract void LoadUXML();
    
        protected virtual void InitializeComponents()
        {
            Panel = this.Q("Panel");
            _toolbar = this.Q("Toolbar");
            _title = this.Q<Label>("Title");
        
            _resizableElement = this.Q<ResizableElement>();
        
            _collapseButton = this.Q<EWCollapseButton>("Collapse");
            if (_collapseButton != null)
            {
                _collapseButton.IsCollapsed = File.Collapsed;
                if (File is not EWICollapsible)
                    _collapseButton.style.display = DisplayStyle.None;
            }
        
            _dropIndicator = this.Q("DropIndicator");
            UpdateDragAndDropIndicator(false);
        
            _collapsedInfo = this.Q<Label>("CollapsedInfo");
        }

        private void InitializeCapabilities()
        {
            capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable |
                            Capabilities.Ascendable;
        
            if (EWSettings.Snapping)
                capabilities |= Capabilities.Snappable;
        }

        protected virtual void AddManipulators()
        {
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }
    
        protected virtual void RegisterCallbacks()
        {
            RegisterCallback<GeometryChangedEvent>(InitializeLayout);
            RegisterCallback<DetachFromPanelEvent>(_ => UnregisterCallbacks());
        
            RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
        
            RegisterCallback<MouseEnterEvent>(OnMouseEnterEvent);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeaveEvent);
        
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            RegisterCallback<DragExitedEvent>(OnDragExited);
        
            Panel.RegisterCallback<GeometryChangedEvent>(OnPanelGeometryChanged);

            if (_resizableElement != null)
            {
                _resizableElement.RegisterCallback<GeometryChangedEvent>(_ => OnSizeChanged());
                foreach (VisualElement child in _resizableElement.Query().ToList())
                    child.RegisterCallback<MouseUpEvent>(_ => ResizeComplete());
            }
        
            if (_collapseButton != null)
                _collapseButton.ValueChanged += OnCollapseValueChanged;
        }
    
        protected virtual void UnregisterCallbacks()
        {
        
        }
    
        protected virtual void InitializeLayout(GeometryChangedEvent evt)
        {
            UnregisterCallback<GeometryChangedEvent>(InitializeLayout);
        
            Initialize();
        }

        protected virtual void Initialize()
        {
            UpdateColor();
            UpdateFile();
        }

        protected virtual void OnMouseDownEvent(MouseDownEvent evt)
        {
            MouseDown?.Invoke(this, evt);
        }
    
        protected virtual void OnMouseUpEvent(MouseUpEvent evt)
        {
        
        }

        protected virtual void OnMouseEnterEvent(MouseEnterEvent evt)
        {
            PanelHover = true;
            UpdateHover();

            UpdateDragAndDropIndicator(true);
        }
    
        protected virtual void OnMouseLeaveEvent(MouseLeaveEvent evt)
        {
            PanelHover = false;
            UpdateHover();
        
            UpdateDragAndDropIndicator(false);
        }
    
        protected virtual void OnDragUpdated(DragUpdatedEvent evt)
        {
        
        }
    
        protected virtual void OnDragPerform(DragPerformEvent evt)
        {
            UpdateDragAndDropIndicator(false);
        }
    
        protected virtual void OnDragExited(DragExitedEvent evt)
        {
            UpdateDragAndDropIndicator(false);
        }
    
        private void UpdateDragAndDropIndicator(bool active)
        {
            if (_dropIndicator != null)
                _dropIndicator.style.display = active && CanAddDragAndDropAssets() ? DisplayStyle.Flex : DisplayStyle.None;
        }
    
        protected virtual bool CanAddDragAndDropAssets()
        {
            return false;
        }
    
        private void UpdateLayoutByPanel()
        {
            if (!File.Collapsed) return;

            Rect rect = new Rect(layout.position, Panel.layout.size);
            base.SetPosition(rect);
            _resizableElement.parent.style.width = rect.width;
            _resizableElement.parent.style.height = rect.height;
        
        }
    
        private void OnPanelGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateLayoutByPanel();
        }
    
        protected virtual void OnSizeChanged()
        {
            if (File.Collapsed) return;
        
            File.Size = new Vector2(_resizableElement.parent.layout.width, _resizableElement.parent.layout.height);
        }
    
        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            EWFileView[] filesView;
            if (GraphView.selection.Count > 0)
                filesView = selected ? GraphView.selection.Select(selectable => (selectable as EWFileView)).ToArray() : Array.Empty<EWFileView>();
            else
                filesView = new[] {this};
            EWFile[] files = filesView.Select(fileView => fileView.File).ToArray();

            if (files.Length > 0)
            {
                string move = files.Length > 1 ? $"Move ({files.Length}) to/" : "Move to/";
                string copy = files.Length > 1 ? $"Copy ({files.Length}) to/" : "Copy to/";
                foreach (EWWorkspace workspace in EWWorkspaceSystem.GetAllWorkspaces())
                {
                    if (workspace == EWWorkspaceSystem.GetCurrentWorkspace()) continue;
                
                    evt.menu.AppendAction(move + workspace.name, _ => EWWorkspaceSystem.MoveFiles(files, workspace), DropdownMenuAction.AlwaysEnabled);
                    evt.menu.AppendAction(copy + workspace.name, _ => EWWorkspaceSystem.CopyFiles(files, workspace), DropdownMenuAction.AlwaysEnabled);
                }
            }
        
            string delete = files.Length > 1 ? $"Delete ({files.Length})" : "Delete";
            evt.menu.AppendAction(delete, _ => GraphView.RemoveFiles(filesView), DropdownMenuAction.AlwaysEnabled);
        }

        private void EnableResizableElement()
        {
            if (_resizableElement == null) return;
            if (_resizableElement.style.display == DisplayStyle.Flex) return;

            _resizableElement.style.display = DisplayStyle.Flex;
        }

        private void DisableResizableElement()
        {
            if (_resizableElement == null) return;
            if (_resizableElement.style.display == DisplayStyle.None) return;
        
            _resizableElement.style.display = DisplayStyle.None;
        }
    
        public virtual void UpdateFile()
        {
            UpdateTitle();
            UpdateLayout();
            UpdateCollapsedInfo();
            TrySetUpResizableElement();
        }

        private void UpdateTitle()
        {
            _title.text = File.Title;
        }

        private void UpdateLayout()
        {
            Rect rect = new Rect(File.Position, File.Size);
            if (File.Collapsed)
            {
                rect.size = Panel.layout.size;
            }
        
            base.SetPosition(rect);
        }
    
        private void UpdateCollapsedInfo()
        {
            if (_collapsedInfo != null && File is EWICollapsible collapsible)
            {
                _collapsedInfo.text = collapsible.CollapseInfo;
                _collapsedInfo.pickingMode = File.Collapsed ? PickingMode.Position : PickingMode.Ignore;
            }
        }
    
        private void UpdateResizableElementLayout()
        {
            if (_resizableElement == null) return;

            _resizableElement.parent.style.width = File.Size.x;
            _resizableElement.parent.style.height = File.Size.y;
        }

        protected void UpdateHover()
        {
            UpdateHover(PanelHover);
        }
    
        protected virtual void UpdateHover(bool panelHover)
        {
            if (panelHover) Panel.AddToClassList("panel-hover");
            else Panel.RemoveFromClassList("panel-hover");
        }
    
        private void TrySetUpResizableElement()
        {
            if (_resizableElement == null) return;

            _resizableElement.parent.style.width = File.Size.x;
            _resizableElement.parent.style.height = File.Size.y;
        
            EWResizableAttribute resizable = (EWResizableAttribute) Attribute.GetCustomAttribute(File.GetType(), typeof(EWResizableAttribute));
            if (resizable != null)
            {
                _resizableElement.parent.style.minWidth = resizable.MinSize.x;
                _resizableElement.parent.style.maxWidth = resizable.MaxSize.x;
                
                _resizableElement.parent.style.minHeight = resizable.MinSize.y;
                _resizableElement.parent.style.maxHeight = resizable.MaxSize.y;
            }
            else
            {
                _resizableElement.style.display = DisplayStyle.None;
            }
        }
    
        private void ResizeComplete()
        {
            if (_resizableElement == null) return;

            SetPosition(new Rect(layout.position + _resizableElement.parent.layout.position - Vector2.one, _resizableElement.parent.layout.size));
            _resizableElement.parent.style.left = 0f;
            _resizableElement.parent.style.top = 0f;
        }
    
        protected virtual void OnCollapseValueChanged(bool collapsed)
        {
            bool isSwitching = File.Collapsed != collapsed;
            File.Collapsed = collapsed;

            if (File.Collapsed)
            {
                styleSheets.Add(EWContainer.Instance.FolderCollapse);
                if (isSwitching)
                    StartAnimation();
            
                DisableResizableElement();
            
                if (!isSwitching)
                    Panel.RegisterCallback<GeometryChangedEvent>(UpdateLayoutByPanelEvent);
            }
            else
            {
                UpdateLayout();
                UpdateResizableElementLayout();
                EnableResizableElement();
            
                if (isSwitching) RegisterCallback<GeometryChangedEvent>(RemoveStyleCollapseEvent);
                else styleSheets.Remove(EWContainer.Instance.FolderCollapse);
            }
        
            if (isSwitching && collapsed)
                UpdateCollapsedInfo();

            return;

            void StartAnimation()
            {
                if (_collapseAnimation != null && _collapseAnimation.isActive)
                {
                    _collapseAnimation.Pause();
                    _collapseAnimationAction?.Invoke();
                }
            
                StyleList<TimeValue> transitionDuration = Panel.style.transitionDuration;
                Panel.style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue> {new TimeValue(0.15f)});
                _collapseAnimationAction = () =>
                {
                    Panel.style.transitionDuration = transitionDuration;
                    UpdateFile();
                };
                _collapseAnimation = schedule
                    .Execute(_collapseAnimationAction)
                    .StartingIn((long) (Panel.style.transitionDuration.value[0].value * 1000));
            }
        
            void UpdateLayoutByPanelEvent(GeometryChangedEvent evt)
            {
                RegisterCallback<GeometryChangedEvent>(UpdateLayoutByFileEvent);
                UnregisterCallback<GeometryChangedEvent>(UpdateLayoutByPanelEvent);
            }
        
            void UpdateLayoutByFileEvent(GeometryChangedEvent evt)
            {
                UpdateLayoutByPanel();
                UnregisterCallback<GeometryChangedEvent>(UpdateLayoutByFileEvent);
            }

            void RemoveStyleCollapseEvent(GeometryChangedEvent evt)
            {
                RemoveStyleCollapse();
                UnregisterCallback<GeometryChangedEvent>(RemoveStyleCollapseEvent);
            }
        
            void RemoveStyleCollapse()
            {
                styleSheets.Remove(EWContainer.Instance.FolderCollapse);
                StartAnimation();
            }
        }

        protected virtual void UpdateTitleColor(Color color)
        {
            _title.style.color = color;
        }
        
        protected virtual void UpdateToolbarColor(Color color)
        {
            _toolbar.style.backgroundColor = color;
        }
    
        public void SaveOffset()
        {
            _offset = GraphView.viewTransform.position / GraphView.viewTransform.scale.x + File.Position;
        }

        public void UpdatePosition()
        {
            SetPosition(GetPosition());
        }
    
        public override Rect GetPosition()
        {
            Rect position = base.GetPosition();
            if (GraphView.IsFileMovement && !selected)
            {
                position.position = -GraphView.viewTransform.position / GraphView.viewTransform.scale.x + _offset;
            }
            return position;
        }

        public override void SetPosition(Rect newPos)
        {
            Undo.RecordObject(File, "Change position");

            base.SetPosition(newPos);

            File.Position = new Vector2(newPos.x, newPos.y);
        
            if (!File.Collapsed)
                File.Size = new Vector2(newPos.width, newPos.height);
        }

        public override void OnSelected()
        {
            base.OnSelected();
        
            Panel.AddToClassList("panel-selected");
        
            Selected?.Invoke(this);
        }

        public override void OnUnselected()
        {
            base.OnUnselected();

            Panel.RemoveFromClassList("panel-selected");
        
            Unselected?.Invoke(this);
        }
    
        public void SetDragAndDropActive(bool active)
        {
            if (active) DisableResizableElement();
            else EnableResizableElement();
        }

        public void UpdateColor()
        {
            EWFileColor color = EWFileColorContainer.GetFileColor(File.Color);
            UpdateTitleColor(EWFileColorContainer.GetTitleColor(color));
            UpdateToolbarColor(color.ColorToolbar);
        }
    
        public void CollectElements(HashSet<GraphElement> collectedElementSet, Func<GraphElement, bool> conditionFunc)
        {
            
        }
    
        public void SetGraphView(EWGraph graphView)
        {
            GraphView = graphView;
        }
    }
}