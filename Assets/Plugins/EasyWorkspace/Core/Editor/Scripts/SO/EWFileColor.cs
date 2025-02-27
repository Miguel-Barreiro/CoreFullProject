using UnityEngine;

namespace EasyWorkspace
{
    [System.Serializable]
    public class EWFileColor
    {
        [SerializeField] private string _name;
        [Space(20f)]
        [SerializeField] private bool _blackText;
        [SerializeField] private Color _colorToolbar;
        
        public string Name => _name;
        public bool BlackText => _blackText;
        public Color ColorToolbar => _colorToolbar;
        
    }
}