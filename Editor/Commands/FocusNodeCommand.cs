using JsonRawNode;
using NodeLib.Editor.Nodes;
using NodeLib.Editor.Serialization;
using NodeLib.Editor.Undo._Base;

namespace NodeLib.Editor.Undo
{
    public class FocusNodeCommand : CommandBase
    {
        public const string ConstType = "FocusNode";
        
        private NodeBase _prev;
        private NodeBase _next;
        private int _index = -1;
        
        public FocusNodeCommand(NodeBase prev, NodeBase next, NodeLibEditor editor) : base(editor)
        {
            Type = ConstType;
            ChangeDoc = false;
            _prev = prev;
            _next = next;
        }

        public FocusNodeCommand(NodeLibEditor editor, RawNode rawNode): base(editor, rawNode)
        {
        }
        
        public override void Do()
        {
            if (_prev != null)
            {
                _prev.IsFocused = false;
            }

            if (_next != null)
            {
                _next.IsFocused = true;
                _index = editor.Nodes.IndexOf(_next);
                editor.Nodes.RemoveAt(_index);
                editor.Nodes.Add(_next);
            }
        }

        protected override void InitFromRawNode()
        {
            _index = rawNode.GetInt("index");
            
            if (rawNode.HasKey("prev"))
            {
                _prev = FindNode(rawNode.GetString("prev"));
            }

            if (rawNode.HasKey("next"))
            {
                _next = FindNode(rawNode.GetString("next"));
            }
        }

        public override void Undo()
        {
            base.Undo();
            
            if (_next != null)
            {
                editor.Nodes.RemoveAt(editor.Nodes.Count - 1);
                editor.Nodes.Insert(_index, _next);
                _next.IsFocused = false;
            }
            
            if (_prev != null)
            {
                _prev.IsFocused = true;
            }
        }

        protected override object OnSerialize()
        {
            return SerializeUtil.Dict(base.OnSerialize()).SetArgs("index", _index, "prev", _prev?.Id, "next", _next?.Id);
        }
    }
}