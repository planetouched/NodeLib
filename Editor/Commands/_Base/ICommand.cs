using NodeLib.Editor.Serialization;

namespace NodeLib.Editor.Commands._Base
{
    public interface ICommand : ISerialize
    {
        bool ChangeDoc { get; }
        bool UndoPrev { get; set; }
        bool UpdateGUI { get; } 
        void Do();
        void Undo();
    }
}