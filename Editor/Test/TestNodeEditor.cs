using System;
using JsonRawNode;
using NodeLib.Editor.Nodes;
using UnityEditor;
using UnityEngine;

namespace NodeLib.Editor.Test
{
    public class TestNodeEditor : NodeLibEditor
    {
        private const string TestEditorTempFileName = "TestEditorTempFileName";
        
        protected override string EditorTypeKey => "Test";
        protected override string TempFileNameKey => TestEditorTempFileName;
        protected override string LastOpenedFileNameKey => "TestEditorLastOpenedFileName";

        private static TestNodeEditor _editor;
        
        private Vector2 _lastMenuPosition;

        //[MenuItem("Test/New Editor")]
        public static void CreateWindow()
        {
            EditorPrefs.DeleteKey(TestEditorTempFileName);
            CreateWindow<TestNodeEditor>("Test");
        }

        public override NodeBase NodeFactory(RawNode nodeData)
        {
            var type = nodeData.GetString("type");
            if (type == TestNode.Type)
            {
                return new TestNode(nodeData);
            }

            throw new Exception("NodeFactory");
        }

        protected override void Draw()
        {
            ProcessMenuEvents();
            base.Draw();
        }
        
        private void ProcessMenuEvents()
        {
            var e = Event.current;
            
            var inspectorRect = new Rect(position.width - InspectorWidth, 0, InspectorWidth, position.height);
            
            if (e.type == EventType.MouseDown && e.button == 1 && !inspectorRect.Contains(e.mousePosition))
            {
                if (FindNodeUnderCursor() != null) return;

                _lastMenuPosition = e.mousePosition;
                
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Create Node"), false, OnCreateNode);
                menu.ShowAsContext();
            }
        }
        
        private void OnCreateNode()
        {
            CreateNode(new TestNode("Untitled", _lastMenuPosition - _editor.ContentOffset));
        }
        
        protected override void OnWindowDisable()
        {
        }

        protected override void OnWindowEnable()
        {
            _editor = this;
        }
        
        protected override void OnWindowDestroy()
        {
        }
    }
}