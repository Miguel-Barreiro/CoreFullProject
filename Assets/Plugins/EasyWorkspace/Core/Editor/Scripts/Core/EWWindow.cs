using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyWorkspace
{
    public class EWWindow : EditorWindow
    {
        private static EWWindow _window;
    
        private bool _initialized;
        private bool _needToUpdate;
    
        public static EWWorkspaceView WorkspaceView;
        
        public static bool TrackingProjectChanges { get; private set; }
        
        private void Initialize()
        {
            if (!EWContainer.IsInitialized)
                return;
            
            if (_initialized)
                return;
            
            _initialized = true;
            
            EWWorkspaceSystem.Initialize();
            
            _window = this;
            
            Undo.undoRedoPerformed += OnUndoRedo;
            SetTrackingProjectChanges(true);
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            _initialized = false;
            
            Undo.undoRedoPerformed -= OnUndoRedo;
            SetTrackingProjectChanges(false);
        }
        
        public static void SetTrackingProjectChanges(bool value)
        {
            if (TrackingProjectChanges == value)
                return;
            
            TrackingProjectChanges = value;
            
            if (value)
                EditorApplication.projectChanged += OnProjectChanged;
            else
                EditorApplication.projectChanged -= OnProjectChanged;
        }

        [MenuItem("Window/Easy Workspace")]
        public static void OpenWindow()
        {
            _window = GetWindow<EWWindow>();
            _window.titleContent = new GUIContent("Easy Workspace");
            _window.minSize = new Vector2(400f, 300f);
        }

        private void Update()
        {
            WorkspaceView?.Update();
        }
    
        private void OnGUI()
        {
            WorkspaceView?.OnGUI();
        
            if (_needToUpdate)
            {
                _needToUpdate = false;
                UpdateWorkspaceView();
            }
            else if (Event.current.type == EventType.KeyDown)
            {
                KeyDown(Event.current.keyCode);
            }
        }
        
        private void OnUndoRedo()
        {
            _needToUpdate = true;
            AssetDatabase.SaveAssets();
        }
    
        private void ShowInitializationScreen()
        {
            if (EWWorkspaceSystem.GetClosedWorkspaces().Length == 0)
            {
                if (EWBackup.NeedRestore() && EWBackup.HasBackup())
                    EWBackup.TryRestore();
                else
                    EWWorkspaceSystem.CreateWorkspace();
                return;
            }

            EWContainer.Instance.InitializationScreenUXML.CloneTree(rootVisualElement);
        }
    
        private void ShowWorkspaceScreen(EWWorkspace workspace)
        {
            EWContainer.Instance.WorkspaceUXML.CloneTree(rootVisualElement);
        
            WorkspaceView = rootVisualElement.Q<EWWorkspaceView>();
            WorkspaceView.UpdateWorkspace(workspace);
        }

        private void ShowSettings()
        {
            EWContainer.Instance.SettingsUXML.CloneTree(rootVisualElement);
        }

        private void CreateGUI()
        {
            Initialize();
            
            UpdateWorkspaceView();
        }
        
        private static void OnProjectChanged()
        {
            if (!EWContainer.IsInitialized)
                return;
            
            EWWorkspaceSystem.UpdateWorkspaces();
            UpdateWorkspaceView();
        }
    
        public static void UpdateWorkspaceView()
        {
            _window.rootVisualElement.Clear();
            int count = _window.rootVisualElement.styleSheets.count;
            for (int i = 1; i < count; i++)
                _window.rootVisualElement.styleSheets.Remove(_window.rootVisualElement.styleSheets[1]);

            if (EWSettings.SettingsIsOpen)
            {
                _window.ShowSettings();
                return;
            }
        
            EWWorkspace workspace = EWWorkspaceSystem.GetCurrentWorkspace();
            if (workspace == null)
                _window.ShowInitializationScreen();
            else
                _window.ShowWorkspaceScreen(workspace);
        }
        
        public static bool KeyDown(KeyCode keyCode)
        {
            if (keyCode == KeyCode.R)
            {
                UpdateWorkspaceView();
                return true;
            }

            if (keyCode == KeyCode.F)
            {
                WorkspaceView?.Graph?.Frame();
                return true;
            }
            
            return false;
        }
    
        public static void FocusWindow()
        {
            _window.Focus();
        }
    }
}