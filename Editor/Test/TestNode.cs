using JsonRawNode;
using NodeLib.Editor.Connections;
using NodeLib.Editor.Nodes;
using UnityEngine;

namespace NodeLib.Editor.Test
{
    public class TestNode : NodeBase
    {
        public const string Type = "test";
        protected override string NodeType => Type;

        public TestNode(string title, Vector2 position) : base(title, position)
        {
            Ports.Add(new ConnectionPort(
                this, 
                "Test", 
                new Vector2(0, 50), 
                PortSide.Left, 
                PortIcon.Right, 
                PortDirection.In, 
                true, 
                Color.green, 
                "test"));
            
            Ports.Add(new ConnectionPort(
                this, 
                "Test", 
                new Vector2(0, 50), 
                PortSide.Right, 
                PortIcon.Right, 
                PortDirection.Out, 
                true, 
                Color.green, 
                "test"));
        }

        public TestNode(RawNode data) : base(data)
        {
        }

        protected override void OnInitialize(RawNode rawData)
        {
        }
    }
}