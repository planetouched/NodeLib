using JsonRawNode;
using NodeLib.Editor.Nodes;
using NodeLib.Editor.Serialization;
using NodeLib.Editor.Commands._Base;

namespace NodeLib.Editor.Commands
{
    public class NodeContentChangedCommand : CommandBase
    {
        public const string ConstType = "NodeContentChanged";
        
        private NodeBase _node;
        private object _serializedObject;

        public NodeContentChangedCommand(NodeBase node, NodeLibEditor editor) : base(editor)
        {
            Type = ConstType;
            _node = node;
        }

        public NodeContentChangedCommand(NodeLibEditor editor, RawNode rawNode) : base(editor, rawNode)
        {
        }
        
        public override void Do()
        {
            _serializedObject = _node.Serialize();
        }

        protected override void InitFromRawNode()
        {
            _node = FindNode(rawNode.GetString("nodeId"));
            _serializedObject = rawNode.GetNode("serializedObject").RawData;
        }

        public override void Undo()
        {
            base.Undo();
            
            _node.Initialize(new RawNode(_serializedObject));
            
            //так как порты пересоздаются при изменении контента, то в Connections указатели на них протухают
            //укажем на новые порты 
            foreach (var port in _node.Ports)
            {
                foreach (var connection in editor.Connections)
                {
                    if (connection.InPort.Id == port.Id)
                    {
                        connection.InPort = port;
                    }
                    else if (connection.OutPort.Id == port.Id)
                    {
                        connection.OutPort = port;
                    }
                }
            }            
        }

        protected override object OnSerialize()
        {
            return SerializeUtil.Dict(base.OnSerialize()).SetArgs("nodeId", _node.Id,  "serializedObject", _serializedObject);
        }
    }
}