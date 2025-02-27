using System.IO;
using System.Linq;
using EasyWorkspace;
using UnityEditor;
using UnityEngine;

public static class EWBackup
{
    private const string BackupFolder = "EasyWorkspace" + "/Backup";
    private const int BackupInterval = 300;
    private static string BackupPath => $"{Application.persistentDataPath}/{BackupFolder}";
    
    private static double? _lastBackupTime;
    private static double LastBackupTime
    {
        get
        {
            return _lastBackupTime ??= EditorPrefs.GetInt("EWBackup.LastBackupTime", 0);
        }
        set
        {
            _lastBackupTime = value;
            EditorPrefs.SetInt("EWBackup.LastBackupTime", (int) value);
        }
    }
    
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.projectChanged -= OnProjectChanged;
        EditorApplication.projectChanged += OnProjectChanged;
        
        EditorApplication.quitting -= OnQuitting;
        EditorApplication.quitting += OnQuitting;
        
        EditorApplication.update -= OnUpdate;
        EditorApplication.update += OnUpdate;

        if (LastBackupTime > EditorApplication.timeSinceStartup)
            LastBackupTime = EditorApplication.timeSinceStartup;

        TryRestore();
    }
    
    private static void OnProjectChanged()
    {
        TryRestore();
    }
    
    private static void OnQuitting()
    {
        TryBackup();
    }
    
    private static void OnUpdate()
    {
        if (EditorApplication.timeSinceStartup - LastBackupTime >= BackupInterval)
            TryBackup();
    }
    
    public static void TryRestore()
    {
        if (IsInitialized() && NeedRestore())
        {
            if (HasBackup())
                Restore();
            else
                EWWorkspaceSystem.UpdateWorkspaces();
        }
    }
    
    public static void TryBackup()
    {
        if (CanBackup())
            Backup();
    }
    
    private static void Restore()
    {
        EWWorkspaceSystem.ImportWorkspaces(BackupPath);
    }
    
    public static void ForceRestore()
    {
        bool restore = true;
        if (Directory.Exists(EWWorkspaceSystem.WorkspacePath))
            restore = EditorUtility.DisplayDialog("Restore Backup", "Are you sure you want to restore the backup?\nCurrent workspaces will be lost.", "Yes", "No");
        
        if (!restore)
            return;
        
        if (Directory.Exists(EWWorkspaceSystem.WorkspacePath))
            Directory.Delete(EWWorkspaceSystem.WorkspacePath, true);
        
        Restore();
    }

    private static void Backup()
    {
        LastBackupTime = EditorApplication.timeSinceStartup;
        
        AssetDatabase.SaveAssets();
        
        if (HasBackupDirectory())
            Directory.Delete(BackupPath, true);
        
        Directory.CreateDirectory(BackupPath);
        
        foreach (EWWorkspace workspace in EWWorkspaceSystem.GetAllWorkspaces())
            EWWorkspaceSystem.ExportWorkspace(workspace, BackupPath);
        
        EWSettings.LastBackupTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
    
    public static bool CanBackup()
    {
        return IsInitialized() && HasWorkspaceDirectory();
    }
    
    public static bool NeedRestore()
    {
        return !HasWorkspaceDirectory();
    }
    
    private static bool IsInitialized()
    {
        return EWContainer.IsInitialized && EWWorkspaceSystem.IsInitialized;
    }
    
    private static bool HasWorkspaceDirectory()
    {
        return AssetDatabase.IsValidFolder(EWWorkspaceSystem.WorkspacePath);
    }
    
    private static bool HasBackupDirectory()
    {
        return Directory.Exists(BackupPath);
    }
    
    public static bool HasBackup()
    {
        return HasBackupDirectory() && Directory.EnumerateFiles(BackupPath).Any();
    }
}