using System;
using UnityEngine;

namespace EasyWorkspace
{
    /// <summary>
    /// Attribute to define a resizable panel. The parameters are the minimum and maximum sizes of the panel
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EWResizableAttribute : Attribute
    {
        private Vector2 _minSize;
        private Vector2 _maxSize;
    
        public Vector2 MinSize => _minSize;
        public Vector2 MaxSize => _maxSize;

        public EWResizableAttribute(float minSizeX, float minSizeY, float maxSizeX, float maxSizeY)
        {
            _minSize = new Vector2(minSizeX, minSizeY);
            _maxSize = new Vector2(maxSizeX, maxSizeY);
        }
    }
}