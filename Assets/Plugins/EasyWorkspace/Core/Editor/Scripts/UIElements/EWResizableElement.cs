using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace EasyWorkspace
{
    public class EWResizableElement : ResizableElement
    {
        public new class UxmlFactory : UxmlFactory<EWResizableElement, UxmlTraits> { }
    }
}