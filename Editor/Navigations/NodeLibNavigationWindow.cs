using UnityEngine;

namespace NodeLib.Editor.Navigations
{
    public class NodeLibNavigationWindow
    {
        public bool Enable { get; set; }
        private readonly NodeLibEditor _editor;

        public Vector2 Size { get; set; } = new Vector3(800, 600);

        private readonly GUIStyle _bodyStyle;
        private readonly GUIStyle _textStyle;
        private float _scale = 15;

        public NodeLibNavigationWindow(NodeLibEditor editor)
        {
            _editor = editor;
            
            _bodyStyle = new();
            _bodyStyle.normal.background = Texture2D.whiteTexture;

            _textStyle = new GUIStyle();
            _textStyle.normal.textColor = Color.white;
        }

        private void ProcessEvents(Rect winRect)
        {
            var e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (winRect.Contains(e.mousePosition))
                {
                    _editor.ContentOffset += -(e.mousePosition - winRect.center) * _scale;
                    GUI.changed = true;
                }
            }

            if (e.type == EventType.MouseDrag && e.button == 2)
            {
                _editor.ContentOffset += e.delta * _scale;
                GUI.changed = true;
            }
            
            if (e.isScrollWheel)
            {
                if (e.delta.y < 0)
                {
                    _scale -= 1;
                    if (_scale < 10) _scale = 10;
                    GUI.changed = true;
                }
                
                if (e.delta.y > 0)
                {
                    _scale += 1;
                    if (_scale > 20) _scale = 20;
                    GUI.changed = true;
                }
            }
        }
        
        public void Draw()
        {
            var winPos = new Vector2(_editor.position.width / 2, _editor.position.height / 2) - Size / 2;
            var winRect = new Rect(winPos, Size);

            ProcessEvents(winRect);

            var bc = GUI.backgroundColor;

            GUI.backgroundColor = Color.black;
            GUI.Box(new Rect(winPos - Vector2.one * 2, Size + Vector2.one * 4), string.Empty, _bodyStyle);

            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            GUI.Box(winRect, string.Empty, _bodyStyle);

            var contentOffset = _editor.ContentOffset / _scale;
            
            //screen
            var screenSize = _editor.position.size / _scale;
            var screenRect = new Rect(winRect.center - screenSize / 2, screenSize); 
            GUI.backgroundColor = new Color(1, 1, 0, 0.1f);
            GUI.Box(screenRect, string.Empty, _bodyStyle);
            
            foreach (var node in _editor.Nodes)
            {
                var nodePos = winRect.center + contentOffset + node.Position / _scale - _editor.position.size / _scale / 2;
                var nodeSize = node.Size / _scale;
                
                if (!winRect.Overlaps(new Rect(nodePos, nodeSize))) continue;
                
                var color = node.HeaderColor;

                GUI.backgroundColor = color;
                GUI.Box(new Rect(nodePos, nodeSize), string.Empty, _bodyStyle);
            }

            GUI.backgroundColor = bc;
            
            var labelRect = new Rect(winPos + Vector2.one * 10, new Vector2(Size.x - 20, 20));
            var fileName = _editor.OpenedFileName();
            GUI.Label(labelRect, fileName, _textStyle);
        }
    }
}