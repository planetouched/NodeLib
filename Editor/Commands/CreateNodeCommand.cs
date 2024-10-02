using JsonRawNode;
using NodeLib.Editor.Nodes;
using NodeLib.Editor.Undo._Base;

namespace NodeLib.Editor.Undo
{
    public class CreateNodeCommand : CommandBase
    {
        public const string ConstType = "CreateNode";

        private readonly NodeBase _node;

        public CreateNodeCommand(NodeBase node, NodeLibEditor editor) : base(editor)
        {
            Type = ConstType;
            _node = node;
        }

        public CreateNodeCommand(NodeLibEditor editor, RawNode rawNode) : base(editor, rawNode)
        {
        }
        
        public override void Do()
        {
            editor.Nodes.Add(_node);
        }

        public override void Undo()
        {
            base.Undo();
            
            editor.Nodes.RemoveAt(editor.Nodes.Count - 1);
        }
    }
}