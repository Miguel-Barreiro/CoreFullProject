using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyWorkspace
{
    public class EWFileColorContainer : ScriptableObject
    {
        [SerializeField] private Color _titleWhite;
        [SerializeField] private Color _titleBlack;
        [SerializeField] private EWFileColor[] _colors;
    
        private static EWFileColor DefaultColor => Instance._colors[0];
        private static EWFileColorContainer Instance => EWContainer.Instance.FileColorContainer;
    
        public static Color TitleWhite => Instance._titleWhite;
        public static Color TitleBlack => Instance._titleBlack;
    
        public static string[] GetColors()
        {
            return Instance._colors.Select(c => c.Name).ToArray();
        }

        public static EWFileColor GetFileColor(string name)
        {
            EWFileColor color = Instance._colors.FirstOrDefault(c => c.Name == name);
            return color ?? DefaultColor;
        }

        public static string ConvertToName(string name)
        {
            EWFileColor color = Instance._colors.FirstOrDefault(c => c.Name == name);
            return color?.Name ?? DefaultColor.Name;
        }
    
        public static Color GetTitleColor(EWFileColor color)
        {
            return color.BlackText ? TitleBlack : TitleWhite;
        }
    }
}