using System;
using UnityEngine;

namespace EasyWorkspace
{
    /// <summary>
    /// Attribute to define a custom panel
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EWCustomAttribute : Attribute
    {
        private string _editorName;
        private string _defaultTitle;
        private Vector2 _defaultSize;
    
        public string EditorName => _editorName;
        public string DefaultTitle => _defaultTitle;
        public Vector2 DefaultSize => _defaultSize;

        public EWCustomAttribute(string editorName, string defaultTitle, float defaultWidth, float defaultHeight)
        {
            _editorName = editorName;
            _defaultTitle = defaultTitle;
            _defaultSize = new Vector2(defaultWidth, defaultHeight);
        }
    
        public EWCustomAttribute(string editorName, float defaultWidth, float defaultHeight)
        {
            _editorName = editorName;
            _defaultTitle = editorName;
            _defaultSize = new Vector2(defaultWidth, defaultHeight);
        }
    }
}