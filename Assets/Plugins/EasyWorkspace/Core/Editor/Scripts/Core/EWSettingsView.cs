using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace EasyWorkspace
{
    public class EWSettingsView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<EWSettingsView, UxmlTraits> { }
        
        private Toggle _workspacesAll;
        private ListView _list;
        private Button _import;
        private Button _export;
        private Button _delete;
        private List<bool> _toggleStates;
        
        public EWSettingsView()
        {
            RegisterCallbacks();
        }

        private void RegisterCallbacks()
        {
            RegisterCallback<GeometryChangedEvent>(Initialize);
        }

        private void Initialize(GeometryChangedEvent evt)
        {
            UnregisterCallback<GeometryChangedEvent>(Initialize);
            
            _workspacesAll = this.Q<Toggle>("WorkspacesAll");
            _list = this.Q<ListView>("List");
            _import = this.Q<Button>("Import");
            _export = this.Q<Button>("Export");
            _delete = this.Q<Button>("Delete");
            
            SetUpButtonClose(this.Q<Button>("Close"));
            SetUpWorkspacesAll();
            SetUpList();
            SetUpBackupTime(this.Q<Label>("LastBackupTime"));
            SetUpBackupButtons(this.Q<Button>("Backup"), this.Q<Button>("Restore"));
            SetUpAssetActions(this.Q<EnumField>("AssetActionLMB"), this.Q<EnumField>("AssetActionRMB"), this.Q<EnumField>("AssetActionMMB"));
            SetUpWorkspaceButtons(this.Q<Toggle>("Snapping"), this.Q<Toggle>("ZoomScrolling"), this.Q<Toggle>("ZoomFit"));
            
            UpdateWorkspaceListButtons();
        }
        
        private void SetUpButtonClose(Button button)
        {
            button.clicked += Close;
        }
        
        private void SetUpWorkspacesAll()
        {
            EWWorkspace[] workspaces = EWWorkspaceSystem.GetAllWorkspaces();
            _workspacesAll.text = $"All ({workspaces.Length})";
            _workspacesAll.RegisterValueChangedCallback(evt =>
            {
                for (int i = 0; i < _toggleStates.Count; i++)
                    _toggleStates[i] = evt.newValue;

                _list.RefreshItems();
                
                UpdateWorkspaceListButtons();
            });
        }

        private void SetUpList()
        {
            EWWorkspace[] workspaces = EWWorkspaceSystem.GetAllWorkspaces();
            _toggleStates = new List<bool>(new bool[workspaces.Length]);

            Func<VisualElement> makeItem = () =>
            {
                Toggle toggle = new Toggle();
                return toggle;
            };

            Action<VisualElement, int> bindItem = (e, i) =>
            {
                Toggle toggle = e as Toggle;
                toggle.text = workspaces[i].name;
                toggle.SetValueWithoutNotify(_toggleStates[i]);

                if (toggle.userData is EventCallback<ChangeEvent<bool>> existingCallback)
                    toggle.UnregisterValueChangedCallback(existingCallback);

                EventCallback<ChangeEvent<bool>> callback = evt =>
                {
                    _toggleStates[i] = evt.newValue;
                    _workspacesAll.SetValueWithoutNotify(_toggleStates.All(x => x));
                    
                    UpdateWorkspaceListButtons();
                };
                toggle.userData = callback;
                toggle.RegisterValueChangedCallback(callback);
            };

            _list.makeItem = makeItem;
            _list.bindItem = bindItem;
            _list.itemsSource = workspaces;
            _list.fixedItemHeight = 20f;
            
            _list.RefreshItems();

            _import.clicked += EWWorkspaceSystem.ImportWorkspaces;
            _export.clicked += () => EWWorkspaceSystem.ExportWorkspaces(workspaces.Where((_, i) => _toggleStates[i]).ToArray());
            _delete.clicked += () => EWWorkspaceSystem.DeleteWorkspaces(workspaces.Where((_, i) => _toggleStates[i]).ToArray());
        }
        
        private void SetUpAssetActions(EnumField lmb, EnumField rmb, EnumField mmb)
        {
            lmb.Init(EWSettings.AssetActionLmb);
            rmb.Init(EWSettings.AssetActionRmb);
            mmb.Init(EWSettings.AssetActionMmb);
            
            lmb.RegisterValueChangedCallback(evt => EWSettings.AssetActionLmb = (EWAssetActionType) evt.newValue);
            rmb.RegisterValueChangedCallback(evt => EWSettings.AssetActionRmb = (EWAssetActionType) evt.newValue);
            mmb.RegisterValueChangedCallback(evt => EWSettings.AssetActionMmb = (EWAssetActionType) evt.newValue);
        }
        
        private void SetUpWorkspaceButtons(Toggle snapping, Toggle zoomScrolling, Toggle zoomFit)
        {
            snapping.SetValueWithoutNotify(EWSettings.Snapping);
            zoomScrolling.SetValueWithoutNotify(EWSettings.ZoomScrolling);
            zoomFit.SetValueWithoutNotify(EWSettings.ZoomFit);
            
            snapping.RegisterValueChangedCallback(evt => EWSettings.Snapping = evt.newValue);
            zoomScrolling.RegisterValueChangedCallback(evt => EWSettings.ZoomScrolling = evt.newValue);
            zoomFit.RegisterValueChangedCallback(evt => EWSettings.ZoomFit = evt.newValue);
        }
        
        private void SetUpBackupTime(Label lastBackupTime)
        {
            lastBackupTime.text = "Last backup:\n" + EWSettings.LastBackupTime;
        }
        
        private void SetUpBackupButtons(Button create, Button restore)
        {
            create.clicked += () =>
            {
                EWBackup.TryBackup();
                EWWindow.UpdateWorkspaceView();
            };
            restore.clicked += () =>
            {
                EWBackup.ForceRestore();
                EWWindow.UpdateWorkspaceView();
            };
            
            if (!EWBackup.HasBackup())
                restore.SetEnabled(false);
        }

        private void UpdateWorkspaceListButtons()
        {
            bool anySelected = _toggleStates.Any(x => x);
                    
            _export.SetEnabled(anySelected);
            _delete.SetEnabled(anySelected);

            _export.text = _export.text.Replace(_export.text.Split('(', ')')[1], _toggleStates.Count(x => x).ToString());
            _delete.text = _delete.text.Replace(_delete.text.Split('(', ')')[1], _toggleStates.Count(x => x).ToString());
        }

        public static void Open()
        {
            EWSettings.SettingsIsOpen = true;
            EWWindow.UpdateWorkspaceView();
        }

        public static void Close()
        {
            EWSettings.SettingsIsOpen = false;
            EWWindow.UpdateWorkspaceView();
        }
    }
}