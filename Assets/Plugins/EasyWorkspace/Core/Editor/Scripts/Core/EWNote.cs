using UnityEditor;
using UnityEngine;

namespace EasyWorkspace
{
    [EWCustom("Note", 200f, 200f)]
    [EWResizable(100f, 100f, 700f, 700f)]
    public class EWNote : EWCustom
    {
        [SerializeField] private string _text;
        [SerializeField] private int _fontSize = 16;
    
        protected override void DrawGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.textArea) { wordWrap = true, fontSize = _fontSize };
            string newText = EditorGUILayout.TextArea(_text, style, GUILayout.ExpandHeight(true));
            if (newText != _text)
            {
                _text = newText;
                
                Undo.RecordObject(this, "Change Note Text");
            }
        }

        protected override void DrawInspectorGUI()
        {
            EditorGUILayout.LabelField("Font Size");
            _fontSize = EditorGUILayout.IntSlider("", _fontSize, 8, 48);
        }
        
        protected override string GetCollapseInfo()
        {
            return _text;
        }
    }
}