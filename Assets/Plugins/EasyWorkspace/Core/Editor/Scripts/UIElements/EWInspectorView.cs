using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace EasyWorkspace
{
    public class EWInspectorView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<EWInspectorView, UxmlTraits> { }

        private EWGraph _graph;

        private Label _title;
        private IMGUIContainer _container;
        private DropdownField _dropdownColor;
        private VisualElement _dropdownColorIcon;

        private EWFile _currentFile;
        private bool _active;
    
        private EWFileView _lastClickedFile;
        private float _lastClickTime;

        private const float DoubleClickTime = 0.3f;
    
        public UnityAction CanShowChanged;
        public bool CanShow => _graph != null && _graph.selection.Count == 1;

        public EWInspectorView()
        {
            EWFileView.MouseDown += OnFileMouseDown;
            EWFileView.Selected += OnFileSelected;
            EWFileView.Unselected += OnFileUnselected;
        }

        public void Initialize(EWGraph graphView)
        {
            _graph = graphView;
    
            _title = this.Q<Label>();
            _container = this.Q<IMGUIContainer>();
            _dropdownColor = this.Q<DropdownField>();
            _dropdownColorIcon = _dropdownColor.Q("Icon");
    
            _dropdownColor.Children().First().style.minWidth = new StyleLength(StyleKeyword.Auto);
            _dropdownColor.Children().First().Children().First().style.visibility = Visibility.Hidden;

            _dropdownColor.choices = EWFileColorContainer.GetColors().ToList();
            _dropdownColor.RegisterValueChangedCallback(OnColorChanged);
        
            CanShowChanged?.Invoke();
        }

        private void UpdateTitle()
        {
            _title.text = _currentFile.Title;
        }
    
        private void DrawInspectorGUI(EWFileView fileView)
        {
            if (fileView == null || fileView.File == null)
            {
                _container.onGUIHandler = null;
                return;
            }
    
            EditorGUI.BeginChangeCheck();
            fileView.File.DrawInspectorGUI(90f);
            EditorGUILayout.Space(10f);
            if (EditorGUI.EndChangeCheck() || fileView.File.NeedUpdate)
            {
                fileView.File.Update();
        
                fileView.UpdateFile();
                fileView.File.Save();
            
                UpdateTitle();
            }
        }

        private void DisableInspector()
        {
            _currentFile = null;
            _container.onGUIHandler = null;
        
            RemoveFromClassList("inspector-active");
        }

        private void OnFileMouseDown(EWFileView fileView, MouseDownEvent evt)
        {
            if (_lastClickedFile == fileView && Time.realtimeSinceStartup - _lastClickTime < DoubleClickTime)
            {
                _lastClickedFile = null;
                _lastClickTime = 0f;
            
                Show(fileView);
            }
            else
            {
                _lastClickedFile = fileView;
                _lastClickTime = Time.realtimeSinceStartup;
            }
        }

        private void OnFileSelected(EWFileView fileView)
        {
            DisableInspector();
        
            CanShowChanged?.Invoke();
        }

        private async void OnFileUnselected(EWFileView fileView)
        {
            await Task.Delay(10);
            if (_graph == null) return;
    
            DisableInspector();
        
            CanShowChanged?.Invoke();
        }

        private void OnColorChanged(ChangeEvent<string> evt)
        {
            if (_currentFile == null) return;
    
            _currentFile.Color = evt.newValue;
            (_graph.selection[0] as EWFileView)?.UpdateColor();
            _dropdownColorIcon.style.backgroundColor = EWFileColorContainer.GetFileColor(_currentFile.Color).ColorToolbar;
        }
    
        public void Show(EWFileView fileView)
        {
            EWFile file = fileView.File;
    
            if (_currentFile == file) return;
            _currentFile = file;
    
            _container.onGUIHandler = () => DrawInspectorGUI(fileView);
    
            _dropdownColor.value = EWFileColorContainer.ConvertToName(file.Color);
            _dropdownColorIcon.style.backgroundColor = EWFileColorContainer.GetFileColor(file.Color).ColorToolbar;
        
            UpdateTitle();
    
            AddToClassList("inspector-active");
        }
    
        public void Show()
        {
            Show(_graph.selection[0] as EWFileView);
        }
    }
}