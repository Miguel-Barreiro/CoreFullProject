using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyWorkspace
{
    public static class EWSystem
    {
        public static T[] GetAssetsAtPath<T>(string path, bool deepSearch = false) where T : Object
        {
            if (!Directory.Exists(path))
                return Array.Empty<T>();
            
            List<T> assets = new();

            string[] files = Directory.GetFileSystemEntries(path, "*", deepSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            foreach (string assetPath in files)
            {
                if (assetPath.EndsWith(".meta"))
                    continue;
                
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset) 
                    assets.Add(asset);
            }

            return assets.ToArray();
        }
    
        public static T[] GetAssetsAtPath<T>(string path, bool deepSearch, EWFilterList whiteList) where T : Object
        {
            List<T> assets = new List<T>();
            T[] allAssets = GetAssetsAtPath<T>(path, deepSearch);
        
            if ((int) whiteList == 0) 
                return assets.ToArray();
        
            foreach (T asset in allAssets)
            {
                if (asset.name.Contains(".meta"))
                    continue;
            
                if (asset == null)
                    continue;

                if (CheckMatchAsset(asset, whiteList))
                {
                    assets.Add(asset);
                }
            }

            return assets.ToArray();
        }
    
        private static bool CheckMatchAsset(Object asset, EWFilterList filterList)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string assetExtension = Path.GetExtension(path);
        
            if (filterList.HasFlag(EWFilterList.Folder) && IsDirectory(asset)) return true;
            if (filterList.HasFlag(EWFilterList.Scene) && assetExtension.Contains(".unity")) return true;
            if (filterList.HasFlag(EWFilterList.Prefab) && asset is GameObject) return true;
            if (filterList.HasFlag(EWFilterList.ScriptableObject) && assetExtension.Contains(".asset")) return true;
            if (filterList.HasFlag(EWFilterList.Material) && asset is Material) return true;
            if (filterList.HasFlag(EWFilterList.Texture) && asset is Texture) return true;
            if (filterList.HasFlag(EWFilterList.Script) && assetExtension.Contains(".cs")) return true;
            if (filterList.HasFlag(EWFilterList.Animation) && asset is AnimationClip) return true;
            if (filterList.HasFlag(EWFilterList.Animator) && asset is RuntimeAnimatorController) return true;
            if (filterList.HasFlag(EWFilterList.Audio) && asset is AudioClip) return true;

            return false;
        }
    
        public static bool IsDirectory(Object asset)
        {
            return AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(asset));
        }
    
        private static void OpenFolder(int instanceId)
        {
            Type pt = Type.GetType("UnityEditor.ProjectBrowser,UnityEditor");
            MethodInfo method = pt.GetMethod("ShowFolderContents", BindingFlags.NonPublic | BindingFlags.Instance);
        
            Object[] projectBrowserInstances = Resources.FindObjectsOfTypeAll(pt);
            foreach (Object t in projectBrowserInstances)
            {
                try
                {
                    method.Invoke(t, new object[] {instanceId, true});
                }
                catch
                {
                    // ignored
                }
            }
        }
        
        private static void OpenScene(Object scene)
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                EditorApplication.playModeStateChanged += OpenSceneAfterPlayMode;
            }
            else
            {
                Open();
            }
            
            void Open()
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(scene));
            }
                
            void OpenSceneAfterPlayMode(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    Open();
                    EditorApplication.playModeStateChanged -= OpenSceneAfterPlayMode;
                }
            }
        }
    
        public static void StartDrag(EWIAssetContainer assetContainer)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new[] { assetContainer.Asset };
            DragAndDrop.StartDrag(assetContainer.DragTitle);
            DragAndDrop.SetGenericData("EW", assetContainer.FileView);
        }
    
        public static void SelectAsset(Object asset)
        {
            EditorGUIUtility.PingObject(asset);
        }
    
        public static void OpenAsset(Object asset)
        {
            if (IsDirectory(asset))
                OpenFolder(asset.GetInstanceID());
            else if (asset is GameObject)
                AssetDatabase.OpenAsset(asset);
            else if (asset is SceneAsset)
                OpenScene(asset);
            else
                OpenOther();
            
            return;

            void OpenOther()
            {
                EditorWindow focusedWindow = EditorWindow.focusedWindow;
                int numberOfWindows = Resources.FindObjectsOfTypeAll<EditorWindow>().Length;
                Object[] objects = Selection.objects;
                
                AssetDatabase.OpenAsset(asset);
                
                int newNumberOfWindows = Resources.FindObjectsOfTypeAll<EditorWindow>().Length;
                EditorWindow focusedWindowAfter = EditorWindow.focusedWindow;
                if (Selection.activeObject == asset && focusedWindow == focusedWindowAfter && numberOfWindows == newNumberOfWindows) 
                {
                    Selection.objects = objects;
                    ShowAsset(asset);
                }
            }
        }
    
        public static void ShowAsset(Object asset)
        {
            EditorUtility.OpenPropertyEditor(asset);
        }
    
        public static Object ConvertDropObject(Object obj)
        {
            if (obj is GameObject gameObject)
            {
                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
                if (string.IsNullOrEmpty(path))
                    return null;
            
                return AssetDatabase.LoadAssetAtPath<Object>(path);
            }
            
            if (!AssetDatabase.Contains(obj))
                return null;
        
            return obj;
        }
    }
}