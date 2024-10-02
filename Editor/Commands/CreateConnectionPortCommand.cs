using JsonRawNode;
using NodeLib.Editor.Connections;
using NodeLib.Editor.Nodes;
using NodeLib.Editor.Serialization;
using NodeLib.Editor.Undo._Base;

namespace NodeLib.Editor.Undo
{
    public class CreateConnectionPortCommand : CommandBase
    {
        public const string ConstType = "CreateConnectionPort";
        
        private readonly ConnectionPort _port;
        private NodeBase _parentNode;

        public CreateConnectionPortCommand(ConnectionPort port, NodeLibEditor editor) : base(editor)
        {
            Type = ConstType;
            _port = port;
            _parentNode = port.ParentNode;
        }

        public CreateConnectionPortCommand(NodeLibEditor editor, RawNode rawNode) : base(editor, rawNode)
        {
        } 
        
        public override void Do()
        {
            _parentNode.Ports.Add(_port);
        }

        protected override void InitFromRawNode()
        {
            _parentNode = FindNode(rawNode.GetString("parentNodeId"));
        }

        public override void Undo()
        {
            base.Undo();
            
            _parentNode.Ports.RemoveAt(_parentNode.Ports.Count - 1);
        }

        protected override object OnSerialize()
        {
            return SerializeUtil.Dict(base.OnSerialize()).SetArgs("parentNodeId", _parentNode.Id);
        }
    }
}