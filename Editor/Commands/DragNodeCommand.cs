using JsonRawNode;
using NodeLib.Editor.Nodes;
using NodeLib.Editor.Serialization;
using NodeLib.Editor.Commands._Base;
using UnityEngine;

namespace NodeLib.Editor.Commands
{
    public class DragNodeCommand : CommandBase
    {
        public const string ConstType = "DragNode";
        
        private Vector2 _oldPos;
        private NodeBase _node;

        public NodeBase DraggingNode => _node;
        public bool Started { get; set; }
        
        public DragNodeCommand(Vector2 oldPos, NodeBase node, NodeLibEditor editor) : base(editor)
        {
            Type = ConstType;
            _oldPos = oldPos;
            _node = node;
        }

        public DragNodeCommand(NodeLibEditor editor, RawNode rawNode): base(editor, rawNode)
        {
        }
        
        public override void Do()
        {
        }

        protected override void InitFromRawNode()
        {
            _node = FindNode(rawNode.GetString("nodeId"));
            var arr = rawNode.GetFloatArray("pos");
            _oldPos = new Vector2(arr[0], arr[1]);
        }

        public override void Undo()
        {
            base.Undo();
            
            _node.Position = _oldPos;
        }

        protected override object OnSerialize()
        {
            return SerializeUtil.Dict(base.OnSerialize()).SetArgs("nodeId", _node.Id, "pos", new []{_oldPos.x, _oldPos.y});
        }
    }
}