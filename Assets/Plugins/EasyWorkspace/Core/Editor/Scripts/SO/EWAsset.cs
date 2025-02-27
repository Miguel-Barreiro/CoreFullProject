using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace EasyWorkspace
{
    public class EWAsset : EWFile
    {
        [SerializeField] private Object _asset;
        [SerializeField] private int _styleIndex;

        public Object Asset { get => _asset; set => SetAndSave(ref _asset, value); }
        public int StyleIndex { get => _styleIndex; set => SetAndSave(ref _styleIndex, value); }
        public VisualTreeAsset Style => EWContainer.Instance.AssetUXML[_styleIndex];
        
        protected override string DefaultTitle => Asset != null ? Asset.name : "None";

        public override Vector2 DefaultSize
        {
            get
            {
                switch (_styleIndex)
                {
                    case 0:
                        return new Vector2(90f, 115);
                    case 1:
                        return new Vector2(180, 40f);
                    default:
                        return new Vector2(100f, 100f);
                }
            }
        }

        public override EWFileView CreateView()
        {
            return new EWAssetView(this);
        }

        public void Initialize(Object asset)
        {
            Asset = asset;
        }

        protected override void DrawInspectorGUI()
        {
            _asset = EditorGUILayout.ObjectField(_asset, typeof(Object), false, GUILayout.Height(30f));
            EditorGUILayout.Space(10f);
            _styleIndex = EditorGUILayout.Popup("Style", _styleIndex, EWContainer.Instance.AssetStyleNames);
        }
    }
}
