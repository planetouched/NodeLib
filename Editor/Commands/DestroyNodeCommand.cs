using System.Collections.Generic;
using JsonRawNode;
using NodeLib.Editor.Nodes;
using NodeLib.Editor.Serialization;
using NodeLib.Editor.Undo._Base;

namespace NodeLib.Editor.Undo
{
    public class DestroyNodeCommand : CommandBase
    {
        public const string ConstType = "DestroyNode";
        
        private NodeBase _node;
        private int _index;

        private readonly List<DestroyConnectionPortCommand> _commands = new();

        public DestroyNodeCommand(NodeBase node, NodeLibEditor editor) : base(editor)
        {
            Type = ConstType;
            _node = node;
            
            foreach (var port in _node.Ports)
            {
                _commands.Add(new DestroyConnectionPortCommand(port, editor));
            }
        }

        public DestroyNodeCommand(NodeLibEditor editor, RawNode rawNode) : base(editor, rawNode)
        {
        }
        
        public override void Do()
        {
            _index = editor.Nodes.IndexOf(_node);
            editor.Nodes.RemoveAt(_index);

            foreach (var command in _commands)
            {
                command.Do();
            }
        }

        protected override void InitFromRawNode()
        {
            _index = rawNode.GetInt("index");
            _node = editor.NodeFactory(rawNode.GetNode("node"));

            foreach (var cNode in rawNode.GetRawNodeArray("commands"))
            {
                _commands.Add(new DestroyConnectionPortCommand(editor, cNode));
            }
        }

        public override void Undo()
        {
            base.Undo();
            
            editor.Nodes.Insert(_index, _node);
                
            for (int i = _commands.Count - 1; i >= 0; i--)
            {
                _commands[i].Undo();
            }
        }

        protected override object OnSerialize()
        {
            return SerializeUtil.Dict(base.OnSerialize()).SetArgs("node", _node.Serialize(), "index", _index, "commands", SerializeUtil.SerializeArray(_commands));
        }
    }
}