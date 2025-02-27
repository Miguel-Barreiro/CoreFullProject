using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace EasyWorkspace
{
    public class EWGraph : GraphView
    {
        public new class UxmlFactory : UxmlFactory<EWGraph, UxmlTraits> { }

        private EWWorkspace _workspace;

        private ContentZoomer _contentZoomer;

        private Vector3 _lastPosition;
        private Vector3 _savedPosition;
        private bool _isFileMovement;
        private bool _isDragAndDropActive;

        public bool IsFileMovement => _isFileMovement;

        public readonly float MinZoom = 0.4f;
        public readonly float MaxZoom = 1.5f;

        public static UnityAction<MouseDownEvent> MouseDownEventGlobal;
        public static UnityAction<MouseUpEvent> MouseUpEventGlobal;
        public static UnityAction<MouseMoveEvent> MouseMoveEventGlobal;
        public UnityAction FileCountChanged;
        public UnityAction ObjectPickerUpdated;

        private readonly float _creationOffset = 30f;
    
        public EWGraph()
        {
            ResetEvents();
            AddManipulators();

            RegisterCallbacks();
    
            EWFileView.MouseDown += OnFileMouseDown;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public void UpdateWorkspace(EWWorkspace workspace)
        {
            _workspace = workspace;
    
            SetZoomActive(EWSettings.ZoomScrolling);
    
            PopulateView();
    
            viewTransform.position = _workspace.Position;
            viewTransform.scale = _workspace.Zoom;
        }

        private void ResetEvents()
        {
            EWFileView.MouseDown = null;
            EWFileView.Selected = null;
            EWFileView.Unselected = null;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void AddManipulators()
        {
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
        }

        private void RegisterCallbacks()
        {
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            
            RegisterCallback<MouseDownEvent>(OnMouseDownEvent, TrickleDown.TrickleDown);
            RegisterCallback<MouseUpEvent>(OnMouseUpEvent, TrickleDown.TrickleDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent, TrickleDown.TrickleDown);
        }

        public void Update()
        {
            TryUpdateWorkspace();
            FixOffset();
        }

        public void OnGUI()
        {
            FixDragAndDrop();
    
            if (Event.current.commandName == "ObjectSelectorUpdated")
            {
                ObjectPickerUpdated?.Invoke();
            }
        }

        private void TryUpdateWorkspace()
        {
            if (_workspace == null) return;
    
            if (_workspace.Position != viewTransform.position) 
                _workspace.Position = viewTransform.position;

            if (_workspace.Zoom != viewTransform.scale)
            {
                _workspace.Zoom = viewTransform.scale;

                _workspace.ZoomChanged?.Invoke(_workspace.Zoom.x);
            }
        }

        private void FixOffset()
        {
            if (viewTransform.position != _lastPosition && _isFileMovement)
            {
                _lastPosition = viewTransform.position;
        
                foreach (EWFileView element in graphElements)
                {
                    if (!element.selected) element.UpdatePosition();
                }
            }
        }

        private void FixDragAndDrop()
        {
            bool isDragAndDropActive = DragAndDrop.objectReferences.Length > 0;
            if (_isDragAndDropActive != isDragAndDropActive)
            {
                _isDragAndDropActive = isDragAndDropActive;
        
                foreach (EWFileView element in graphElements)
                {
                    element.SetDragAndDropActive(isDragAndDropActive);
                }
            }
        }
        
        private void OnFileMouseDown(EWFileView fileView, MouseDownEvent evt)
        {
            if (evt.button == (int) MouseButton.LeftMouse)
                StartFileMovement();
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            object data = DragAndDrop.GetGenericData("EW");
            if (data is EWAssetView) return;
            if (evt.target != this) return;
            if (DragAndDrop.objectReferences.Length == 0) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            object data = DragAndDrop.GetGenericData("EW");
            if (data is EWAssetView) return;
    
            if (DragAndDrop.objectReferences.Length > 0 && evt.target == this)
            {
                DropFiles(DragAndDrop.objectReferences, ConvertMousePositionToLocal(evt.localMousePosition));
    
                DragAndDrop.AcceptDrag();
            }
        }

        private void OnMouseDownEvent(MouseDownEvent evt)
        {
            if ((evt.pressedButtons & (1 << (int) MouseButton.LeftMouse)) != 0 && evt.button != (int) MouseButton.LeftMouse)
            {
                evt.StopImmediatePropagation();
            }

            MouseDownEventGlobal?.Invoke(evt);
        }

        private void OnMouseUpEvent(MouseUpEvent evt)
        {
            MouseUpEventGlobal?.Invoke(evt);

            if (evt.button == (int) MouseButton.LeftMouse)
            {
                EndFileMovement();
            }
        }

        private void OnMouseMoveEvent(MouseMoveEvent evt)
        {
            MouseMoveEventGlobal?.Invoke(evt);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.elementsToRemove != null)
            {
                RemoveFiles(graphViewChange.elementsToRemove.Select(e => e as EWFileView).ToArray(), false);
            }
    
            return graphViewChange;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            foreach (EWFileView fileView in graphElements)
            {
                fileView.UpdateFile();
            }
        }

        private void StartFileMovement()
        {
            _isFileMovement = true;
    
            foreach (EWFileView element in graphElements)
            {
                if (!element.selected) element.SaveOffset();
            }
    
            _savedPosition = viewTransform.position;
        }

        private void EndFileMovement()
        {
            if (!_isFileMovement) return;
    
            _isFileMovement = false;
    
            foreach (EWFileView element in graphElements)
            {
                Rect rect = element.GetPosition();
                rect.position += (Vector2) (viewTransform.position - _savedPosition) / viewTransform.scale.x;
                element.SetPosition(rect);
            }
    
            viewTransform.position = _savedPosition;
        }

        public void Frame()
        {
            float offset = 47f;
    
            Rect rect = layout;
            rect.size -= Vector2.up * offset;
    
            float zoom = _workspace.Zoom.x;

            CalculateFrameTransform(CalculateRectToFitAll(contentViewContainer), rect, 10, out Vector3 frameTranslation, out Vector3 frameScaling);
            UpdateViewTransform(frameTranslation + Vector3.up * offset, frameScaling);
    
            if (!EWSettings.ZoomFit)
                UpdateZoom(zoom);
        }

        private EWFileView CreateFile(Type type, Vector2 position, Action<EWFile> action = null)
        {
            EWFile file = _workspace.CreateFile(type);
            file.Position = position;
            
            action?.Invoke(file);
    
            return CreateFileView(file);
        }

        private EWFileView CreateFileView(EWFile file)
        {
            file.Validate();
        
            EWFileView fileView = file.CreateView();
            fileView.SetGraphView(this);
    
            AddElement(fileView);
        
            FileCountChanged?.Invoke();
    
            return fileView;
        }

        private void DropFiles(Object[] objects, Vector2 position)
        {
            for (int i = 0; i < objects.Length; i++)
                objects[i] = EWSystem.ConvertDropObject(objects[i]);

            objects = objects.Where(o => o != null).Distinct().ToArray();

            if (objects.Length > 1)
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Add folder"), false, () => { AddFiles(objects, position, true); });
                menu.AddItem(new GUIContent($"Add assets ({objects.Length})"), false, () => { AddFiles(objects, position); });
                menu.ShowAsContext();
            }
            else if (objects.Length == 1 && EWSystem.IsDirectory(objects[0]))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Add folder"), false, () => { AddFiles(objects, position, true, objects[0]); });
                menu.AddItem(new GUIContent("Add asset"), false, () => { AddFiles(objects, position); });
                menu.ShowAsContext();
            }
            else
            {
                AddFiles(objects, position);
            }
        }

        private void AddFiles(Object[] objects, Vector2 position, bool isFolder = false, Object directory = null)
        {
            if (isFolder)
            {
                CreateFileFolder(position, objects, directory);
            }
            else
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    CreateFileAsset(position + Vector2.one * _creationOffset * i, objects[i]);
                }
            }
        }

        private void CreateFileFolder(Vector2 position, Object[] objects = null, Object directory = null)
        {
            EWFileView fileView = CreateFile(typeof(EWFolder), position, file =>
            {
                EWFolder folder = file as EWFolder;
                if (directory) folder.Initialize(directory);
                else folder.Initialize(objects);
            });
    
            fileView.UpdateFile();
        }

        private void CreateFileAsset(Vector2 position, Object obj)
        {
            EWFileView fileView = CreateFile(typeof(EWAsset), position);
            EWAsset asset = fileView.File as EWAsset;
            asset.Initialize(obj);
            fileView.UpdateFile();
        }

        private void PopulateView()
        {
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements);
            graphViewChanged += OnGraphViewChanged;

            foreach (EWFile file in _workspace.CurrentFiles)
            {
                if (file is EWCustom && Attribute.GetCustomAttribute(file.GetType(), typeof(EWCustomAttribute)) == null)
                    continue;
            
                CreateFileView(file);
            }
        }

        public void RemoveFiles(EWFileView[] fileViews, bool removeView = true)
        {
            foreach (EWFileView fileView in fileViews)
            {
                _workspace.RemoveFile(fileView.File);
                if (removeView) RemoveElement(fileView);
            }
        
            FileCountChanged?.Invoke();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
    
            if (evt.target != this)
                return;
    
            Vector2 mousePosition = ConvertMousePositionToLocal(evt.localMousePosition);
    
            evt.menu.MenuItems().Clear();
            AddDefaultFiles(evt, mousePosition);
            AddCustomFiles(evt, mousePosition);
        }

        private void AddDefaultFiles(ContextualMenuPopulateEvent evt, Vector2 mousePosition)
        {
            evt.menu.AppendAction("Asset", _ =>
            {
                EWAssetView asset = CreateFile(typeof(EWAsset), mousePosition) as EWAssetView;
                asset.ShowAssetPicker();
            });
            evt.menu.AppendAction("Folder", _ => { CreateFileFolder(mousePosition); });
            evt.menu.AppendAction("Note", _ => { CreateFile(typeof(EWNote), mousePosition); });
        }

        private void AddCustomFiles(ContextualMenuPopulateEvent evt, Vector2 mousePosition)
        {
            List<Type> ignoredTypes = new List<Type>
            {
                typeof(EWAsset),
                typeof(EWFolder),
                typeof(EWNote)
            };
    
            Type[] types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(domainAssembly => domainAssembly.GetTypes())
                .Where(type => typeof(EWCustom).IsAssignableFrom(type) && !type.IsAbstract && !ignoredTypes.Contains(type))
                .ToArray();
    
            foreach (Type type in types)
            {
                EWCustomAttribute customAttribute = (EWCustomAttribute) Attribute.GetCustomAttribute(type, typeof(EWCustomAttribute));
            
                if (customAttribute == null)
                    continue;
            
                evt.menu.AppendAction("Custom/ " + customAttribute.EditorName, _ => { CreateFile(type, mousePosition); });
            }
        }

        private Vector2 ConvertMousePositionToLocal(Vector2 mousePosition)
        {
            return viewTransform.matrix.inverse.MultiplyPoint(mousePosition);
        }

        public void UpdateZoom(float zoom)
        {
            float currentScale = viewTransform.scale.x;
            Vector3 graphSize = contentRect.size;
        
            Vector3 scale = zoom * Vector3.one;
            Vector3 offset = graphSize / scale.x - graphSize / currentScale;
            Vector3 position = viewTransform.position / currentScale * scale.x + offset * scale.x / 2f;
        
            UpdateViewTransform(position, scale);
            _workspace.Position = position;
            _workspace.Zoom = scale;
        }

        public void SetZoomActive(bool active)
        {
            if (active)
            {
                if (_contentZoomer == null)
                {
                    _contentZoomer = new ContentZoomer();
                    _contentZoomer.minScale = MinZoom;
                    _contentZoomer.maxScale = MaxZoom;
                    _contentZoomer.scaleStep = 0.04f;
                }
                this.AddManipulator(_contentZoomer);
            }
            else
            {
                this.RemoveManipulator(_contentZoomer);
            }
        }

        public void SelectFiles(EWFile[] newFiles)
        {
            ClearSelection();
    
            foreach (EWFileView fileView in graphElements)
            {
                if (newFiles.Contains(fileView.File))
                    AddToSelection(fileView);
            }
        }
    }
}