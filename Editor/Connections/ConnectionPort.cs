using System;
using JsonRawNode;
using NodeLib.Editor.EditorResources;
using NodeLib.Editor.Nodes;
using NodeLib.Editor.Serialization;
using NodeLib.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace NodeLib.Editor.Connections
{
    public class ConnectionPort : ISerialize
    {
        public string Id { get; }
        public string Name { get; set; }
        public Vector2 Position => ParentNode.Position + GetPortOffset();
        public Rect Rect => new(new Rect(Position - _size / 2, _size));
        public Vector2 SideOffset { get; set; }
        public string PortType { get; set; }
        public bool MultipleConnections { get; set; }
        public PortIcon PortIcon { get; set; }
        public NodeBase ParentNode { get; }
        public bool Disabled { get; set; }
        public PortSide PortSide { get; set; }
        public Color32 Color { get; set; }
        public Color32 TextColor { get; set; } = UnityEngine.Color.black;
        public PortDirection PortDirection { get; set; }
        public bool DisplayName { get; set; } = true;

        private Vector2 _size;
        public Texture2D IconTexture { get; private set; }
        private GUIStyle _textStyle;
        
        private static Texture2D _disabledTexture;

        public ConnectionPort(NodeBase parent, string name, Vector2 sideOffset, PortSide portSide, PortIcon icon, PortDirection portDirection, bool multipleConnections, Color32 color, string portType)
        {
            Id = NodeLibUtil.GetUniqueId();
            
            Name = name;
            MultipleConnections = multipleConnections;
            PortType = portType;
            PortSide = portSide;
            SideOffset = sideOffset;
            PortIcon = icon;

            ParentNode = parent;
            Color = color;
            PortDirection = portDirection;
            
            InitStyle();
        }

        public ConnectionPort(NodeBase parent, RawNode data)
        {
            Id = data.GetString("id");
            Name = data.GetString("name");
            Disabled = data.GetBool("disabled");
            MultipleConnections = data.GetBool("mConn");
            PortType = data.GetString("type");
            
            SideOffset = NodeLibUtil.ToVector(data.GetFloatArray("offset"));
            PortSide = Enum.Parse<PortSide>(data.GetString("side"));
            PortIcon = Enum.Parse<PortIcon>(data.GetString("icon"));

            ParentNode = parent;
            Color = NodeLibUtil.ToColor32(data.GetByteArray("color"));
            PortDirection = Enum.Parse<PortDirection>(data.GetString("direction"));
            
            InitStyle();
        }

        private Vector2 GetPortOffset()
        {
            if (PortSide == PortSide.Right)
            {
                return new Vector2(ParentNode.Rect.width, SideOffset.y + ParentNode.HeaderHeight);
            }
            
            if (PortSide == PortSide.Left)
            {
                return new Vector2(0, SideOffset.y + ParentNode.HeaderHeight);
            }
            
            if (PortSide == PortSide.Up)
            {
                return new Vector2(SideOffset.x, 0);
            }
            
            if (PortSide == PortSide.Down)
            {
                return new Vector2(SideOffset.x, ParentNode.Rect.height);
            }

            throw new Exception("GetPortOffset");
        }
        
        private void InitStyle()
        {
            if (_disabledTexture == null)
            {
                _disabledTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ResourcePath.Path + $"/Icons/{PortIcon.Disabled}.png");
            }
            
            IconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ResourcePath.Path + $"/Icons/{PortIcon}.png");
            _size = new Vector2(IconTexture.width, IconTexture.height);

            _textStyle = new GUIStyle();
            _textStyle.normal.textColor = TextColor;
        }
        
        public virtual void Draw()
        {
            var bc = GUI.color;
            GUI.color = Color;
            GUI.DrawTexture(Rect, IconTexture);
            GUI.color = bc;

            if (DisplayName)
            {
                var size = _textStyle.CalcSize(new GUIContent(Name));
                
                if (PortSide == PortSide.Right)
                {
                    var x = Position.x - size.x - 8;
                    GUI.Label(new Rect(new Vector2(x, Position.y - size.y / 2) , size), Name, _textStyle);
                }
                
                if (PortSide == PortSide.Left)
                {
                    var x = Position.x + 8;
                    GUI.Label(new Rect(new Vector2(x, Position.y - size.y / 2) , size), Name, _textStyle);
                }
            }
            
            if (Disabled)
            {
                GUI.DrawTexture(Rect, _disabledTexture);
            }
        }

        public object Serialize()
        {
            return SerializeUtil.Dict().SetArgs(
                "id", Id,
                "name", Name,
                "disabled", Disabled,
                "offset", NodeLibUtil.FromVector(SideOffset),
                "side", PortSide.ToString(),
                "icon", PortIcon.ToString(),
                "direction", PortDirection.ToString(),
                "mConn", MultipleConnections,
                "color", NodeLibUtil.FromColor32(Color),
                "type", PortType
            );
        }
    }
}