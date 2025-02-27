using System;

namespace EasyWorkspace
{
    [Flags]
    public enum EWFilterList
    {
        Folder = 1 << 0,
        Scene = 1 << 1,
        Prefab = 1 << 2,
        ScriptableObject = 1 << 3,
        Material = 1 << 4,
        Texture = 1 << 5,
        Script = 1 << 6,
        Animation = 1 << 7,
        Animator = 1 << 8,
        Audio = 1 << 9
    }
}