using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyWorkspace
{
    public static class EWWorkspaceSystem
    {
        private static List<EWWorkspace> _workspaces;
        private static List<EWWorkspace> _addedWorkspaces;
        private static EWWorkspace _currentWorkspace;

        public static bool IsInitialized => _workspaces != null;
        private static string WorkspaceRelativePath => "/Workspaces";
        public static string WorkspacePath => AssetDatabase.GetAssetPath(EWContainer.Instance.Root) + WorkspaceRelativePath;
        
        private static string[] _gitignoreLines = new[]
        {
            "**/Core/Workspaces",
            "**/Core/Workspaces.meta",
        };
        
        public static void Initialize()
        {
            UpdateWorkspaces();
            TryClearGitignore();
            TryAddGitignore();
        }

        public static void UpdateWorkspaces()
        {
            _workspaces = EWSystem.GetAssetsAtPath<EWWorkspace>(WorkspacePath).ToList();
            _addedWorkspaces = _workspaces.Where(workspace => workspace.Added).OrderBy(workspace => workspace.Order).ToList();
            _currentWorkspace = _addedWorkspaces.FirstOrDefault(w => w.Opened) ?? _addedWorkspaces.FirstOrDefault();
            
            foreach (EWWorkspace workspace in _workspaces)
                workspace.Opened = _currentWorkspace == workspace;
        }
    
        private static void TryClearGitignore()
        {
            string gitignorePath = ".gitignore";
            if (!File.Exists(gitignorePath))
                File.Create(gitignorePath).Close();
            
            try
            {
                string[] lines = File.ReadAllLines(gitignorePath).Where(line => !line.Contains("EasyWorkspace") || !line.Contains("Workspaces")).ToArray();
                File.WriteAllLines(gitignorePath, lines);
            }
            catch (Exception)
            {
                // ignored
            }
        }
        
        private static void TryAddGitignore()
        {
            string gitignorePath = AssetDatabase.GetAssetPath(EWContainer.Instance.Root)[..^4] + ".gitignore";
            if (!File.Exists(gitignorePath))
                File.Create(gitignorePath).Close();
            
            try
            {
                string[] lines = File.ReadAllLines(gitignorePath);
                
                if (lines.Length != _gitignoreLines.Length)
                {
                    File.WriteAllLines(gitignorePath, _gitignoreLines);
                    return;
                }
                
                if (lines.Where((t, i) => t != _gitignoreLines[i]).Any())
                    File.WriteAllLines(gitignorePath, _gitignoreLines);
            }
            catch (Exception)
            {
                // ignored
            }
        }
        
        private static void TryCreateWorkspaceDirectory()
        {
            if (!Directory.Exists(WorkspacePath))
                Directory.CreateDirectory(WorkspacePath);
        }
    
        private static EWWorkspace CreateWorkspaceFile()
        {
            TryCreateWorkspaceDirectory();
        
            EWWorkspace workspace = ScriptableObject.CreateInstance<EWWorkspace>();

            _workspaces.Add(workspace);
            
            bool trackChanges = EWWindow.TrackingProjectChanges;
            EWWindow.SetTrackingProjectChanges(false);
        
            AssetDatabase.CreateAsset(workspace, WorkspacePath + "/" + GetNewWorkspaceName() + ".asset");
            AssetDatabase.SaveAssets();
            
            EWWindow.SetTrackingProjectChanges(trackChanges);
        
            return workspace;
        }
    
        private static string GetNewWorkspaceName()
        {
            string workspaceName = "Workspace_";
            int number = 1;
            while (_workspaces.Any(w => w.name == workspaceName + number))
                number++;
            return workspaceName + number;
        }
    
        public static void CreateWorkspace()
        {
            EWWorkspace workspace = CreateWorkspaceFile();
            AddWorkspace(workspace);
        
            EWWindow.WorkspaceView.StartRenaming(workspace);
        }
        
        public static void DeleteWorkspaces(EWWorkspace[] workspaces)
        {
            bool delete = EditorUtility.DisplayDialog("Delete workspaces", "Are you sure you want to delete these workspaces?", "Delete", "Cancel");
            if (!delete) return;
            
            bool trackChanges = EWWindow.TrackingProjectChanges;
            EWWindow.SetTrackingProjectChanges(false);
        
            foreach (EWWorkspace workspace in workspaces)
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(workspace));
            
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            
            EWWindow.SetTrackingProjectChanges(trackChanges);
            
            UpdateWorkspaces();
            EWWindow.UpdateWorkspaceView();
        }
    
        public static void DeleteWorkspace(EWWorkspace workspace)
        {
            DeleteWorkspaces(new[] {workspace});
        }
        
        public static void AddWorkspace(EWWorkspace workspace)
        {
            _addedWorkspaces.Add(workspace);

            workspace.Added = true;
        
            OpenWorkspace(workspace);
        }
    
        public static void RemoveWorkspace(EWWorkspace workspace)
        {
            _addedWorkspaces.Remove(workspace);
        
            workspace.Added = false;

            if (workspace.Opened)
            {
                if (_addedWorkspaces.Count > 0)
                {
                    OpenWorkspace(_addedWorkspaces[0]);
                }
                else
                {
                    _currentWorkspace = null;
                
                    EWWindow.UpdateWorkspaceView();
                }
            }
            else
            {
                EWWindow.UpdateWorkspaceView();
            }
        }
    
        public static void OpenWorkspace(EWWorkspace workspace)
        {
            if (_currentWorkspace == workspace) return;
            if (!workspace.Added) AddWorkspace(workspace);
        
            if (_currentWorkspace != null)
                _currentWorkspace.Opened = false;
        
            workspace.Opened = true;
            workspace.Order = _addedWorkspaces.IndexOf(workspace);
        
            _currentWorkspace = workspace;
        
            EWWindow.UpdateWorkspaceView();
        }

        public static EWWorkspace GetCurrentWorkspace()
        {
            return _currentWorkspace;
        }
    
        public static EWWorkspace[] GetAddedWorkspaces()
        {
            return _addedWorkspaces.ToArray();
        }
    
        public static EWWorkspace[] GetClosedWorkspaces()
        {
            return _workspaces.Where(workspace => !_addedWorkspaces.Contains(workspace)).ToArray();
        }

        public static EWWorkspace[] GetAllWorkspaces()
        {
            return _workspaces.ToArray();
        }
    
        private static async void SelectFiles(EWFile[] files)
        {
            await Task.Delay(1);

            EWWindow.WorkspaceView.Graph.SelectFiles(files);
        }
    
        private static void MoveFiles(EWFile[] files, EWWorkspace to, bool copy)
        {
            EWFile[] newFiles = new EWFile[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                newFiles[i] = Object.Instantiate(files[i]);
                to.AddFile(newFiles[i]);

                if (!copy)
                    EWWindow.WorkspaceView.Workspace.RemoveFile(files[i]);
            }

            OpenWorkspace(to);
        
            EWWindow.UpdateWorkspaceView();
        
            SelectFiles(newFiles);
        }

        public static void MoveFiles(EWFile[] files, EWWorkspace to)
        {
            MoveFiles(files, to, false);
        }
    
        public static void CopyFiles(EWFile[] files, EWWorkspace to)
        {
            MoveFiles(files, to, true);
        }
    
        public static void ShiftWorkspace(EWWorkspace workspace, bool right)
        {
            int index = _addedWorkspaces.IndexOf(workspace);
            if (index == -1) return;
        
            int newIndex = index + (right ? 1 : -1);
            if (newIndex < 0 || newIndex >= _addedWorkspaces.Count) return;
        
            _addedWorkspaces[index] = _addedWorkspaces[newIndex];
            _addedWorkspaces[newIndex] = workspace;
        
            workspace.Order = newIndex;
            _addedWorkspaces[index].Order = index;
        
            EWWindow.WorkspaceView.UpdateTabs();
        }
    
        private static string GetFreeName(string name, string directory)
        {
            string extension = Path.GetExtension(name);
            name = Path.GetFileNameWithoutExtension(name);
        
            bool isDirectory = extension == "";
        
            string currentName = name + extension;
            int number = 1;
            while (isDirectory ? Directory.Exists(directory + "/" + currentName) : File.Exists(directory + "/" + currentName))
            {
                currentName = name + " (" + number + ")" + extension;
            
                number++;
            }
        
            return currentName;
        }
    
        public static void ExportWorkspace(EWWorkspace workspace, string directoryPath)
        {
            string workspacePath = directoryPath + "/" + GetFreeName(workspace.name + ".asset", directoryPath);
            File.Copy(AssetDatabase.GetAssetPath(workspace), workspacePath, true);
        }
    
        public static void ExportWorkspaces(EWWorkspace[] workspaces)
        {
            string directoryPath = EditorUtility.OpenFolderPanel("Export workspace", "", "");
            
            if (string.IsNullOrEmpty(directoryPath)) return;
        
            directoryPath += "/" + GetFreeName("Workspaces", directoryPath);
        
            Directory.CreateDirectory(directoryPath);

            foreach (EWWorkspace workspace in workspaces)
                ExportWorkspace(workspace, directoryPath);
        }
    
        private static void ImportWorkspace(string workspacePath)
        {
            string workspaceName = GetFreeName(Path.GetFileName(workspacePath), WorkspacePath);
            string path = WorkspacePath + "/" + workspaceName;
            
            TryCreateWorkspaceDirectory();
            
            File.Copy(workspacePath, path, true);

            AssetDatabase.ImportAsset(path);

            EWWorkspace asset = AssetDatabase.LoadAssetAtPath<EWWorkspace>(path);
            if (asset != null)
            {
                asset.name = workspaceName.Replace(".asset", "");
                EditorUtility.SetDirty(asset);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        
        public static void ImportWorkspaces(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath)) return;
        
            string[] workspacePaths = Directory.GetFiles(directoryPath, "*.asset", SearchOption.TopDirectoryOnly);
            
            bool trackChanges = EWWindow.TrackingProjectChanges;
            EWWindow.SetTrackingProjectChanges(false);
            
            foreach (string workspacePath in workspacePaths)
                ImportWorkspace(workspacePath);
            
            EWWindow.SetTrackingProjectChanges(trackChanges);
        
            UpdateWorkspaces();
            EWWindow.UpdateWorkspaceView();
        } 
    
        public static void ImportWorkspaces()
        {
            string directoryPath = EditorUtility.OpenFolderPanel("Load workspace", "", "");
            ImportWorkspaces(directoryPath);
        }
    }
}