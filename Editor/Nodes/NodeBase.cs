using System;
using System.Collections.Generic;
using JsonRawNode;
using NodeLib.Editor.Connections;
using NodeLib.Editor.Serialization;
using NodeLib.Editor.Commands;
using NodeLib.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace NodeLib.Editor.Nodes
{
    public abstract class NodeBase : ISerialize
    {
        public string Id { get; }
        protected abstract string NodeType { get; }

        public NodeLibEditor Editor { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }
        public virtual Vector2 MinSize { get; set; } = new(100, 64);
        public Vector2 HeaderSize => new(Size.x, HeaderHeight);
        public Rect Rect => new(Position, Size);
        public Rect HeaderRect => new(Position, HeaderSize);
        public bool ResizeWidthByTitle { get; set; } = true;
        public string Title { get; set; }
        public float HeaderHeight { get; set; } = 32;

        public Color32 HeaderTextColor { get; set; } = Color.white;
        public Color32 HeaderColor { get; set; } = new(38, 151, 255, 255);
        public Color32 BodyColor { get; set; } = new Color32(190, 190, 190, 255);
        public Color32 SelectColor { get; set; } = Color.yellow;
        public Color32 OutlineColor { get; set; } = new(60, 60, 60, 128);

        private bool _focused;
        private bool _contentSaved;
        private GUIStyle _headerStyle;
        private GUIStyle _bodyStyle;

        public bool IsFocused
        {
            get => _focused;
            set
            {
                if (!value)
                {
                    _contentSaved = false;
                }
                else
                {
                    EditorGUI.FocusTextInControl(null);
                }

                _focused = value;
            }
        }

        public List<ConnectionPort> Ports { get; } = new();

        private Texture2D _background;
        private Texture2D _headerBackground;

        private int _lastControl;

        protected RawNode data;

        protected NodeBase(string title, Vector2 position)
        {
            InitStyle();

            Id = NodeLibUtil.GetUniqueId();
            Title = title;
            Position = position;
        }

        protected NodeBase(RawNode data)
        {
            InitStyle();

            Id = data.GetString("id");
            Initialize(data);
        }

        protected abstract void OnInitialize(RawNode rawData);

        public void Initialize(RawNode rawData)
        {
            data = rawData;
            _contentSaved = false;

            Position = new Vector2(data.GetFloat("posX"), data.GetFloat("posY"));
            IsFocused = rawData.GetBool("focused");
            Title = data.GetString("title");

            if (data.HasKey("size"))
            {
                Size = NodeLibUtil.ToVector(data.GetFloatArray("size"));  
            }

            if (data.HasKey("hColor"))
            {
                HeaderColor = NodeLibUtil.ToColor32(data.GetByteArray("hColor"));
            }

            Ports.Clear();

            foreach (var node in data.GetRawNodeArray("ports"))
            {
                var port = ConnectionPortFactory(node);
                if (OnConnectionPortCreatePredicate(port))
                {
                    Ports.Add(port);
                }
            }

            UpdateSize();

            OnInitialize(rawData);
        }

        protected virtual ConnectionPort ConnectionPortFactory(RawNode node)
        {
            return new ConnectionPort(this, node);
        }
        
        protected virtual bool OnConnectionPortCreatePredicate(ConnectionPort port)
        {
            return true;
        }

        private void InitStyle()
        {
            Size = MinSize;

            _headerStyle = new();
            _headerStyle.normal.textColor = HeaderTextColor;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.alignment = TextAnchor.MiddleCenter;
            _headerStyle.normal.background = Texture2D.whiteTexture;

            _bodyStyle = new();
            _bodyStyle.fontStyle = FontStyle.Bold;
            _bodyStyle.normal.background = Texture2D.whiteTexture;
        }

        public virtual bool ApproveConnection(ConnectionPort ourPort, ConnectionPort otherPort)
        {
            return !ourPort.Disabled && ourPort.PortType == otherPort.PortType && ourPort.PortDirection == PortDirection.In && otherPort.PortDirection == PortDirection.Out;
        }

        public virtual void OnConnectionCreate(ConnectionPort ourPort, ConnectionPort otherPort)
        {
        }

        public virtual void OnConnectionDestroy(ConnectionPort ourPort, ConnectionPort otherPort)
        {
        }

        public ConnectionPort FindPortByName(string name)
        {
            foreach (var port in Ports)
            {
                if (port.Name == name) return port;
            }

            return null;
        }

        public ConnectionPort FindPortById(string id)
        {
            foreach (var port in Ports)
            {
                if (port.Id == id) return port;
            }

            return null;
        }

        public void SerializeContent()
        {
            Editor.DoCommand(new NodeContentChangedCommand(this, Editor));
        }

        private void InnerSerializeContent()
        {
            if (!_contentSaved)
            {
                SerializeContent();
                _contentSaved = true;
            }
        }

        public bool SerializeContentIfChanged(bool v1, bool v2)
        {
            if (v1 != v2)
            {
                InnerSerializeContent();
                return true;
            }

            return false;
        }
        
        public bool SerializeContentIfChanged(Vector2 v1, Vector2 v2)
        {
            if (v1 != v2)
            {
                InnerSerializeContent();
                return true;
            }

            return false;
        }

        public bool SerializeContentIfChanged(int v1, int v2)
        {
            if (v1 != v2)
            {
                InnerSerializeContent();
                return true;
            }

            return false;
        }

        public bool SerializeContentIfChanged(float v1, float v2)
        {
            if (Math.Abs(v1 - v2) > 0.0001f)
            {
                InnerSerializeContent();
                return true;
            }

            return false;
        }

        public bool SerializeContentIfChanged(double v1, double v2)
        {
            if (Math.Abs(v1 - v2) > 0.0001f)
            {
                InnerSerializeContent();
                return true;
            }

            return false;
        }

        public bool SerializeContentIfChanged(string v1, string v2)
        {
            if (v1 != v2)
            {
                InnerSerializeContent();
                return true;
            }

            return false;
        }

        private float CalcWidth()
        {
            var size = _bodyStyle.CalcSize(new GUIContent(Title));
            return Math.Max(MinSize.x, size.x + 40);
        }

        public void UpdateSize()
        {
            Size = Vector2.Max(MinSize, Size);

            if (ResizeWidthByTitle)
            {
                var width = CalcWidth();
                Size = new Vector2(width, Size.y);
            }

            OnSizeChange();
        }

        protected virtual void OnSizeChange()
        {
        }

        public virtual void Draw()
        {
            var bc = GUI.backgroundColor;

            GUI.backgroundColor = OutlineColor;
            GUI.Box(new Rect(Position - Vector2.one * 2, Size + Vector2.one * 4), string.Empty, _bodyStyle);

            GUI.backgroundColor = BodyColor;
            GUI.Box(Rect, string.Empty, _bodyStyle);

            if (IsFocused)
            {
                GUI.backgroundColor = SelectColor;
                GUI.Box(new Rect(Position - Vector2.one * 2, HeaderSize + Vector2.one * 4), string.Empty, _headerStyle);
            }

            GUI.backgroundColor = HeaderColor;
            GUI.Box(HeaderRect, Title, _headerStyle);

            GUI.backgroundColor = bc;

            foreach (var port in Ports)
            {
                port.Draw();
            }
        }

        public virtual void DrawInspector(Rect iRect)
        {
            if (_lastControl != GUIUtility.keyboardControl || Event.current.type == EventType.MouseUp)
            {
                _lastControl = GUIUtility.keyboardControl;
                _contentSaved = false;
            }

            var title = EditorGUILayout.TextField("Title", Title, GUILayout.Width(iRect.width));

            if (SerializeContentIfChanged(title, Title))
            {
                Title = title;
            }

            if (GUI.changed)
            {
                UpdateSize();
            }
        }

        public virtual object Serialize()
        {
            return SerializeUtil.Dict().SetArgs(
                "id", Id,
                "focused", IsFocused,
                "type", NodeType,
                "size", NodeLibUtil.FromVector(Size),
                "title", Title,
                "posX", Position.x,
                "posY", Position.y,
                "hColor", NodeLibUtil.FromColor32(HeaderColor),
                "ports", SerializeUtil.SerializeArray(Ports));
        }
    }
}