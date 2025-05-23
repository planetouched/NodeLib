using JsonRawNode;
using NodeLib.Editor.Connections;
using NodeLib.Editor.Serialization;
using NodeLib.Editor.Commands._Base;

namespace NodeLib.Editor.Commands
{
    public class CreateConnectionCommand : CommandBase
    {
        public const string ConstType = "CreateConnection";
        
        private ConnectionPort _outPort;
        private ConnectionPort _inPort;

        
        public CreateConnectionCommand(ConnectionPort outPort, ConnectionPort inPort, NodeLibEditor editor) : base(editor)
        {
            Type = ConstType;
            
            _outPort = outPort;
            _inPort = inPort;
        }

        public CreateConnectionCommand(NodeLibEditor editor, RawNode rawNode) : base(editor, rawNode)
        {
        }
        
        public override void Do()
        {
            editor.Connections.Add(new Connection(_outPort, _inPort));
            
            _outPort.ParentNode.OnConnectionCreate(_outPort, _inPort);
            _inPort.ParentNode.OnConnectionCreate(_inPort, _outPort);
        }

        protected override void InitFromRawNode()
        {
            var outNode = FindNode(rawNode.GetString("outNodeId"));
            var intNode = FindNode(rawNode.GetString("inNodeId"));
                
            _outPort = outNode.FindPortById(rawNode.GetString("outPortId"));
            _inPort = intNode.FindPortById(rawNode.GetString("inPortId"));
        }

        public override void Undo()
        {
            base.Undo();
            
            editor.Connections.RemoveAt(editor.Connections.Count - 1);
            _outPort.ParentNode.OnConnectionDestroy(_outPort, _inPort);
            _inPort.ParentNode.OnConnectionDestroy(_inPort, _outPort);
        }

        protected override object OnSerialize()
        {
            return SerializeUtil.Dict(base.OnSerialize()).SetArgs(
                "outPortId", _outPort.Id, 
                "inPortId", _inPort.Id, 
                "outNodeId", _outPort.ParentNode.Id, 
                "inNodeId", _inPort.ParentNode.Id
                );
        }
    }
}