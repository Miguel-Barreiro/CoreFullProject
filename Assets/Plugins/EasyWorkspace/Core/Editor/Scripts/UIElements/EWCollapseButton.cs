using UnityEngine.Events;
using UnityEngine.UIElements;

namespace EasyWorkspace
{
    public class EWCollapseButton : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<EWCollapseButton, UxmlTraits> { }

        private bool _isCollapsed;

        public bool IsCollapsed
        {
            get => _isCollapsed;
            set
            {
                _isCollapsed = value;
                UpdateState();
        
                ValueChanged?.Invoke(_isCollapsed);
            }
        }

        public UnityAction<bool> ValueChanged;

        public EWCollapseButton()
        {
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            styleSheets.Add(EWContainer.Instance.CollapseButton);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            IsCollapsed = !IsCollapsed;
        }

        private void UpdateState()
        {
            if (_isCollapsed)
                AddToClassList("collapsed");
            else
                RemoveFromClassList("collapsed");
        }
    }
}