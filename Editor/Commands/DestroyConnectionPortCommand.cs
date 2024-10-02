using System.Collections.Generic;
using JsonRawNode;
using NodeLib.Editor.Connections;
using NodeLib.Editor.Nodes;
using NodeLib.Editor.Serialization;
using NodeLib.Editor.Undo._Base;

namespace NodeLib.Editor.Undo
{
    public class DestroyConnectionPortCommand : CommandBase
    {
        public const string ConstType = "DestroyConnectionPort";
        
        private ConnectionPort _port;
        private int _index;
        private readonly List<DestroyConnectionCommand> _commands = new();
        private NodeBase _parentNode;

        public DestroyConnectionPortCommand(ConnectionPort port, NodeLibEditor editor) : base(editor)
        {
            Type = ConstType;
            _port = port;
            _parentNode = port.ParentNode;
            
            foreach (var connection in editor.Connections)
            {
                if (connection.InPort == _port || connection.OutPort == _port)
                {
                    _commands.Add(new DestroyConnectionCommand(connection, editor));
                }
            }
        }

        public DestroyConnectionPortCommand(NodeLibEditor editor, RawNode rawNode) : base(editor, rawNode)
        {
        }
        
        public override void Do()
        {
            _index = _parentNode.Ports.IndexOf(_port);
            _parentNode.Ports.RemoveAt(_index);
            
            foreach (var connectionCommand in _commands)
            {
                connectionCommand.Do();
            }
        }

        protected override void InitFromRawNode()
        {
            _index = rawNode.GetInt("index");
            _parentNode = FindNode(rawNode.GetString("parentNodeId"));
            _port = new ConnectionPort(_parentNode, rawNode.GetNode("port"));

            foreach (var cNode in rawNode.GetRawNodeArray("commands"))
            {
                _commands.Add(new DestroyConnectionCommand(editor, cNode));
            }
        }

        public override void Undo()
        {
            base.Undo();
            
            _parentNode.Ports.Insert(_index, _port);

            for (int i = _commands.Count - 1; i >= 0; i--)
            {
                _commands[i].Undo();
            }
        }

        protected override object OnSerialize()
        {
            return SerializeUtil.Dict(base.OnSerialize()).SetArgs(
                "index", _index, 
                "port", _port.Serialize(), 
                "parentNodeId", _parentNode.Id,
                "commands", SerializeUtil.SerializeArray(_commands));
        }
    }
}