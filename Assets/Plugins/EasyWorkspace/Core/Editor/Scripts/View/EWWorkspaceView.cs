using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyWorkspace
{
    public class EWWorkspaceView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<EWWorkspaceView, UxmlTraits> { }

        private Slider _sliderZoom;

        private EWGraph _graph;
        private EWInspectorView _inspector;
        private VisualElement _tabContainer;
        private Button _buttonSettings;
        private Button _buttonRefresh;
        private Label _startLabel;

        private static EWWorkspace _needToRename;

        private EWWorkspaceTab[] _tabs;

        private EWWorkspace _workspace;

        public EWGraph Graph => _graph;
        public EWWorkspace Workspace => _workspace;

        public EWWorkspaceView()
        {
            RegisterCallbacks();
        }

        private void RegisterCallbacks()
        {
            RegisterCallback<GeometryChangedEvent>(Initialize);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void Initialize(GeometryChangedEvent evt)
        {
            if (_workspace == null)
                return;
    
            UnregisterCallback<GeometryChangedEvent>(Initialize);
    
            _graph = this.Q<EWGraph>();
            _inspector = this.Q<EWInspectorView>("Inspector");
            _tabContainer = this.Q("WorkspaceTabContainer");
            _buttonSettings = this.Q<Button>("Settings");
            _buttonRefresh = this.Q<Button>("Refresh");
            _startLabel = this.Q<Label>("StartLabel");
        
            SetUpTabs();
            UpdateTabs();
            SetUpToolbar();
            SetUpSettings();
            UpdateWorkspace(_workspace);
        
            OnFileCountChanged();
        
            _graph.FileCountChanged += OnFileCountChanged;
        }

        private void SetUpTabs()
        {
            Button buttonAddTab = _tabContainer.Q<Button>("Add");
            buttonAddTab.clickable.clicked += EWWorkspaceSystem.CreateWorkspace;
        
            Button buttonOpenWorkspace = _tabContainer.Q<Button>("OpenWorkspace");
            buttonOpenWorkspace.SetEnabled(EWWorkspaceSystem.GetClosedWorkspaces().Any());
            buttonOpenWorkspace.clickable.clicked += () =>
            {
                GenericMenu menu = new GenericMenu();
                EWWorkspace[] closedWorkspaces = EWWorkspaceSystem.GetClosedWorkspaces();
        
                foreach (EWWorkspace workspace in closedWorkspaces)
                    menu.AddItem(new GUIContent(workspace.name), false, () => EWWorkspaceSystem.AddWorkspace(workspace));
        
                menu.ShowAsContext();
            };
        }

        public void UpdateTabs()
        {
            EWWorkspace[] openedWorkspaces = EWWorkspaceSystem.GetAddedWorkspaces();
            EWWorkspaceTab[] currentTabs = _tabContainer.Query<EWWorkspaceTab>().ToList().ToArray();

            _tabs = new EWWorkspaceTab[openedWorkspaces.Length];
    
            for (int i = 0; i < Mathf.Max(openedWorkspaces.Length, currentTabs.Length); i++)
            {
                if (i < openedWorkspaces.Length)
                {
                    EWWorkspaceTab tab;
            
                    if (i >= currentTabs.Length)
                    {
                        VisualTreeAsset visualTreeAsset = EWContainer.Instance.WorkspaceTabUXML;
                        tab = visualTreeAsset.CloneTree().Q<EWWorkspaceTab>();
                        foreach (StyleSheet styleSheet in visualTreeAsset.stylesheets) 
                            tab.styleSheets.Add(styleSheet);

                        _tabContainer.Insert(_tabContainer.childCount - 3, tab);
                        _tabs[i] = tab;
                    }
                    else
                    {
                        tab = currentTabs[i];
                        _tabs[i] = currentTabs[i];
                    }

                    tab.Initialize(openedWorkspaces[i], openedWorkspaces[i].Opened);
            
                    if (_needToRename == tab.Workspace)
                    {
                        tab.StartRenaming();
                        _needToRename = null;
                    }
                }
                else
                {
                    _tabContainer.Remove(currentTabs[i]);
                }
            }
        }

        private void SetUpToolbar()
        {
            SetUpButtonInspect();
            SetUpSliderZoom();
            SetUpButtonFit();
            SetUpButtonRefresh();
        }

        private void SetUpSettings()
        {
            _buttonSettings.clicked += EWSettingsView.Open;
        }
    
        private void SetUpButtonRefresh()
        {
            _buttonRefresh.clickable.clicked += EWWindow.UpdateWorkspaceView;
        }

        private void SetUpButtonInspect()
        {
            ToolbarButton buttonInspect = this.Q<ToolbarButton>("Inspect");
            buttonInspect.clicked += () =>
            {
                _inspector.Show();
            };
        
            buttonInspect.SetEnabled(_inspector.CanShow);
            _inspector.CanShowChanged += () =>
            {
                buttonInspect.SetEnabled(_inspector.CanShow);
            };
        }

        private void SetUpSliderZoom()
        {
            _sliderZoom = this.Q<Slider>("Zoom");
            _sliderZoom.labelElement.style.minWidth = 0f;
            _sliderZoom.lowValue = _graph.MinZoom;
            _sliderZoom.highValue = _graph.MaxZoom;
            _sliderZoom.value = _workspace.Zoom.x;
            _sliderZoom.RegisterValueChangedCallback(evt =>
            {
                _graph.UpdateZoom(evt.newValue);
            });
            _workspace.ZoomChanged += zoom =>
            {
                _sliderZoom.value = zoom;
            };
        }

        private void SetUpButtonFit()
        {
            Button buttonFit = this.Q<Button>("Fit");
            buttonFit.clickable.clicked += () =>
            {
                _graph.Frame();
            };
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (EWWindow.KeyDown(evt.keyCode))
                evt.StopPropagation();
        }
    
        private void OnFileCountChanged()
        {
            _startLabel.style.display = EWWorkspaceSystem.GetCurrentWorkspace().CurrentFiles.Any() ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void Update()
        {
            _graph?.Update();
        }

        public void OnGUI()
        {
            _graph?.OnGUI();
        }

        public void UpdateWorkspace(EWWorkspace workspace)
        {
            _workspace = workspace;

            _inspector?.Initialize(_graph);
            _graph?.UpdateWorkspace(_workspace);
        }

        public void StartRenaming(EWWorkspace workspace)
        {
            _needToRename = workspace;
        }
    }
}