using UnityEditor;
using System.Diagnostics;
using EasyWorkspace;

public static class EWUpdate
{
    [InitializeOnLoadMethod]
    public static void Initialize()
    {
        if (!EWContainer.IsInitialized)
            return;
        
        GitCommand("rm --cached " + EWWorkspaceSystem.WorkspacePath + ".meta");
    }
    
    private static void GitCommand(string command)
    {
        try
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = command,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }
        catch
        {
            // ignored
        }
    }
}