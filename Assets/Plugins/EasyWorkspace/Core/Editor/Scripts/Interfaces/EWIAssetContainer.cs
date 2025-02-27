using UnityEngine;

namespace EasyWorkspace
{
    public interface EWIAssetContainer
    {
        Object Asset { get; }
        EWFileView FileView { get; }
        string DragTitle { get; }
        void PressAsset();
        void UnpressAsset();
    }
}