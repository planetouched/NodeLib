using JsonRawNode;
using NodeLib.Editor.Nodes;
using NodeLib.Editor.Serialization;

namespace NodeLib.Editor.Undo._Base
{
    public abstract class CommandBase : ICommand
    {
        public string Type { get; protected set; }
        
        public bool ChangeDoc { get; protected set; } = true;
        public bool UndoPrev { get; set;}
        public bool UpdateGUI { get; protected set;} = true;

        private bool _initFromNode;

        public abstract void Do();

        public virtual void Undo()
        {
            if (!_initFromNode && rawNode != null)
            {
                InitFromRawNode();
                _initFromNode = true;
            }
        }

        protected readonly NodeLibEditor editor;
        protected readonly RawNode rawNode;
        
        protected CommandBase(NodeLibEditor editor)
        {
            this.editor = editor;
        }

        protected CommandBase(NodeLibEditor editor, RawNode node)
        {
            this.editor = editor;
            Type = node.GetString("type");
            UndoPrev = node.GetBool("undoPrev");
            rawNode = node;
        }
        
        protected NodeBase FindNode(string id)
        {
            foreach (var node in editor.Nodes)
            {
                if (node.Id == id)
                {
                    return node;
                }
            }

            return null;
        }

        protected virtual void InitFromRawNode()
        {
        }
        
        public object Serialize()
        {
            if (rawNode != null)
            {
                return rawNode.RawData;
            }

            return OnSerialize();
        }

        protected virtual object OnSerialize()
        {
            return SerializeUtil.Dict().SetArgs("type", Type, "undoPrev", UndoPrev);
        }
    }
}