using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyWorkspace
{
    [InitializeOnLoad]
    public static class EWAssetContainerSystem
    {
        private static EWIAssetContainer _currentAssetContainer;
    
        private static bool _canClick;
        private static MouseButton _button;
    
        private static bool _canAssetDrag;
        private static float _assetDragTreshold = 10f;
        private static Vector2 _assetDragStartMousePosition;
        
        static EWAssetContainerSystem()
        {
            EWGraph.MouseDownEventGlobal -= OnMouseDownEventGlobal;
            EWGraph.MouseUpEventGlobal -= OnMouseUpEventGlobal;
            EWGraph.MouseMoveEventGlobal -= OnMouseMoveEventGlobal;
        
            EWGraph.MouseDownEventGlobal += OnMouseDownEventGlobal;
            EWGraph.MouseUpEventGlobal += OnMouseUpEventGlobal;
            EWGraph.MouseMoveEventGlobal += OnMouseMoveEventGlobal;
        }
    
        private static void OnMouseDownEventGlobal(MouseDownEvent evt)
        {
            if (evt.button != (int) MouseButton.LeftMouse)
                return;

            _canAssetDrag = false;
        }

        private static void OnMouseMoveEventGlobal(MouseMoveEvent evt)
        {
            if (evt.pressedButtons != 1)
                return;

            if (!_canAssetDrag)
                return;
        
            bool isAssetDrag = Vector2.Distance(_assetDragStartMousePosition, evt.mousePosition) > _assetDragTreshold;
        
            if (!isAssetDrag)
                return;
        
            _canClick = false;

            EWSystem.StartDrag(_currentAssetContainer);
            _currentAssetContainer.UnpressAsset();
        
            evt.StopPropagation();
        }
    
        private static void OnMouseUpEventGlobal(MouseUpEvent evt)
        {
            _currentAssetContainer?.UnpressAsset();
        }
        
        private static void Interact(Object asset, MouseButton button, bool shift, bool alt)
        {
            EWAssetActionType actionType = EWAssetActionType.Open;
            switch (button)
            {
                case MouseButton.LeftMouse:
                    if (shift)
                        actionType = EWSettings.AssetActionMmb;
                    else if (alt)
                        actionType = EWSettings.AssetActionRmb;
                    else
                        actionType = EWSettings.AssetActionLmb;
                    break;
                case MouseButton.RightMouse:
                    actionType = EWSettings.AssetActionRmb;
                    break;
                case MouseButton.MiddleMouse:
                    actionType = EWSettings.AssetActionMmb;
                    break;
            }
            
            switch (actionType)
            {
                case EWAssetActionType.Open:
                    EWSystem.OpenAsset(asset);
                    break;
                case EWAssetActionType.Select:
                    EWSystem.SelectAsset(asset);
                    break;
                case EWAssetActionType.Show:
                    EWSystem.ShowAsset(asset);
                    break;
            }
        }
    
        public static void AssetDown(this EWIAssetContainer assetContainer, MouseDownEvent evt)
        {
            _currentAssetContainer = assetContainer;
        
            if (assetContainer.Asset == null)
                return;
        
            _canAssetDrag = true;
            _canClick = true;
            _assetDragStartMousePosition = evt.mousePosition;
            _button = (MouseButton) evt.button;
        
            _currentAssetContainer.PressAsset();
        }
    
        public static void AssetUp(this EWIAssetContainer assetContainer, MouseUpEvent evt)
        {
            if (_currentAssetContainer == null)
                return;
        
            if (_currentAssetContainer.Asset == null)
                return;
        
            if (!_canClick)
                return;
        
            if (evt.button != (int) _button)
                return;
            
            Interact(_currentAssetContainer.Asset, _button, evt.shiftKey, evt.altKey);
        }
    }
}