using JsonRawNode;
using NodeLib.Editor.Connections;
using NodeLib.Editor.Serialization;
using NodeLib.Editor.Commands._Base;

namespace NodeLib.Editor.Commands
{
    public class DestroyConnectionCommand : CommandBase
    {
        public const string ConstType = "DestroyConnection";
        
        private Connection _connection;
        private int _index;

        public DestroyConnectionCommand(Connection connection, NodeLibEditor editor) : base(editor)
        {
            Type = ConstType;
            _connection = connection;
        }

        public DestroyConnectionCommand(NodeLibEditor editor, RawNode rawNode) : base(editor, rawNode)
        {
        }
        
        public override void Do()
        {
            _index = editor.Connections.IndexOf(_connection);
            editor.Connections.RemoveAt(_index);

            _connection.OutPort.ParentNode.OnConnectionDestroy(_connection.OutPort, _connection.InPort);
            _connection.InPort.ParentNode.OnConnectionDestroy(_connection.InPort, _connection.OutPort);
        }

        protected override void InitFromRawNode()
        {
            _index = rawNode.GetInt("index");
            _connection = new Connection(editor.Nodes, rawNode.GetNode("connection"));
        }

        public override void Undo()
        {
            base.Undo();
            
            _connection.OutPort.ParentNode.OnConnectionCreate(_connection.OutPort, _connection.InPort);
            _connection.InPort.ParentNode.OnConnectionCreate(_connection.InPort, _connection.OutPort);

            editor.Connections.Insert(_index, _connection);
        }

        protected override object OnSerialize()
        {
            return SerializeUtil.Dict(base.OnSerialize()).SetArgs("index", _index, "connection", _connection.Serialize());
        }
    }
}