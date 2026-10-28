using System;
using System.Collections.Generic;
using System.IO;
using Jsonite;
using JsonRawNode;
using JsonRawNodeDataProvider;
using NodeLib.Editor.Connections;
using NodeLib.Editor.EditorResources;
using NodeLib.Editor.Navigations;
using NodeLib.Editor.Nodes;
using NodeLib.Editor.Serialization;
using NodeLib.Editor.Commands;
using NodeLib.Editor.Commands._Base;
using UnityEditor;
using UnityEngine;

namespace NodeLib.Editor
{
    public abstract class NodeLibEditor : EditorWindow, ISerialize
    {
        protected abstract string EditorTypeKey { get; }
        protected abstract string TempFileNameKey { get; }
        protected abstract string LastOpenedFileNameKey { get; }

        public List<NodeBase> Nodes { get; } = new();
        public List<Connection> Connections { get; } = new();

        public Color32 InspectorColor { get; set; } = new(60, 60, 60, 255);
        public float InspectorWidth { get; set; } = 460;
        public bool KeepUndoHistoryInDocument { get; set; }

        private bool _changed;

        public bool Changed
        {
            get => _changed;
            protected set
            {
                if (value && !titleContent.text.EndsWith('*'))
                {
                    titleContent.text += "*";
                }

                if (!value && titleContent.text.EndsWith('*'))
                {
                    titleContent.text = titleContent.text.TrimEnd('*');
                }

                _changed = value;
            }
        }

        private readonly GUIStyle _inspectorStyle = new();

        private ResourcePath _resourcePath;

        private Vector2 _contentOffset;

        public Vector2 ContentOffset
        {
            get => _contentOffset;
            set
            {
                _contentOffset = value;
                GUI.changed = true;
            }
        }

        private readonly List<Connection> _connectionsToDestroy = new();
        private readonly List<ICommand> _commands = new();
        private ICommand _commandAfterSaving;

        protected event Action<ICommand> CommandDone;
        protected event Action<ICommand> CommandUndone;

        private DragNodeCommand _dragNodeCommand;
        private ConnectionPort _startOutPort;
        private NodeBase _lastInspectorNode;

        private NodeLibNavigationWindow _navigationWindow; 

        private bool _unsafe;

        protected static T CreateWindow<T>(string winTitle) where T : NodeLibEditor
        {
            var window = GetWindow<T>();
            window.titleContent = new GUIContent(winTitle);
            return window;
        }

        #region Public API

        public void NewDoc()
        {
            if (Changed && !_unsafe)
            {
                WarningSave();
                Changed = false;
            }

            _commandAfterSaving = null;
            _dragNodeCommand = null;
            _startOutPort = null;
            _commands.Clear();
            Nodes.Clear();
            Connections.Clear();
        }

        public void OpenFile(string file)
        {
            var data = new RawNode(Json.Deserialize(File.ReadAllText(file)));
            LoadJson(data);
            EditorPrefs.SetString(LastOpenedFileNameKey, file);
        }

        public void LoadJson(RawNode data)
        {
            if (data.GetString("editorType") != EditorTypeKey)
            {
                throw new Exception("NodeLibEditor::LoadJson/Mismatch editorType");
            }
            
            NewDoc();

            foreach (var nodeData in data.GetRawNodeArray("nodes"))
            {
                var node = NodeFactory(nodeData);
                node.Editor = this;
                Nodes.Add(node);
            }

            foreach (var connData in data.GetRawNodeArray("connections"))
            {
                Connections.Add(new Connection(Nodes, connData));
            }

            foreach (var commData in data.GetRawNodeArray("commands"))
            {
                _commands.Add(CommandFactory(commData));
            }
        }

        public void SaveFile(string file)
        {
            var dict = (Dictionary<string, object>)Serialize();
            
            if (!KeepUndoHistoryInDocument)
            {
                dict.Remove("commands");
            }
            
            File.WriteAllText(file, Json.Serialize(dict));

            if (_commands.Count > 0)
            {
                _commandAfterSaving = _commands[_commands.Count - 1];
            }
            
            Changed = false;
            OnSaveFile(file);
        }
        
        public int CountConnections(ConnectionPort port)
        {
            int count = 0;

            foreach (var connection in Connections)
            {
                if (connection.OutPort == port || connection.InPort == port)
                {
                    count++;
                }
            }

            return count;
        }

        public Connection FindConnection(ConnectionPort port)
        {
            foreach (var connection in Connections)
            {
                if (connection.InPort == port || connection.OutPort == port)
                {
                    return connection;
                }
            }

            return null;
        }
        
        public List<Connection> FindConnections(ConnectionPort port)
        {
            List<Connection> tmp = new();
            
            foreach (var connection in Connections)
            {
                if (connection.InPort == port || connection.OutPort == port)
                {
                    tmp.Add(connection);
                }
            }

            return tmp;
        }
        
        public abstract NodeBase NodeFactory(RawNode nodeData);
        
        public string OpenedFileName()
        {
            if (EditorPrefs.HasKey(LastOpenedFileNameKey))
            {
                return EditorPrefs.GetString(LastOpenedFileNameKey);
            }

            return string.Empty;
        }
        
        #endregion

        #region Protected

        protected virtual void OnSaveFile(string file)
        {
        }
        
        protected virtual ICommand CommandFactory(RawNode commData)
        {
            var type = commData.GetString("type");
            switch (type)
            {
                case CreateConnectionCommand.ConstType:
                    return new CreateConnectionCommand(this, commData);
                case CreateConnectionPortCommand.ConstType:
                    return new CreateConnectionPortCommand(this, commData);
                case CreateNodeCommand.ConstType:
                    return new CreateNodeCommand(this, commData);
                case DestroyConnectionCommand.ConstType:
                    return new DestroyConnectionCommand(this, commData);
                case DestroyConnectionPortCommand.ConstType:
                    return new DestroyConnectionPortCommand(this, commData);
                case DestroyNodeCommand.ConstType:
                    return new DestroyNodeCommand(this, commData);
                case DragNodeCommand.ConstType:
                    return new DragNodeCommand(this, commData);
                case FocusNodeCommand.ConstType:
                    return new FocusNodeCommand(this, commData);
                case NodeContentChangedCommand.ConstType:
                    return new NodeContentChangedCommand(this, commData);
            }

            return null;
        }

        protected abstract void OnWindowEnable();
        protected abstract void OnWindowDisable();
        protected abstract void OnWindowDestroy();

        // protected void DestroyConnection(Connection connection)
        // {
        //     DoCommand(new DestroyConnectionCommand(connection, Connections));
        // }

        protected void CreateNode(NodeBase node)
        {
            node.Editor = this;
            DoCommand(new CreateNodeCommand(node, this));
        }

        protected NodeBase FindNodeUnderCursor()
        {
            var e = Event.current;

            foreach (var node in Nodes)
            {
                var pos = node.Position;
                node.Position += ContentOffset;

                if (node.Rect.Contains(e.mousePosition))
                {
                    node.Position = pos;
                    return node;
                }

                node.Position = pos;
            }

            return null;
        }

        protected NodeBase FindFocusedNode()
        {
            foreach (var node in Nodes)
            {
                if (node.IsFocused) return node;
            }

            return null;
        }

        public ICommand DoCommand(ICommand command)
        {
            _commands.Add(command);
            command.Do();
            CommandDone?.Invoke(command);
            GUI.changed = command.UpdateGUI;
            Changed |= command.ChangeDoc;
            return command;
        }

        protected virtual void DrawConnections()
        {
            if (_startOutPort != null)
            {
                Handles.color = _startOutPort.Color;
                Handles.DrawLine(_startOutPort.Position + ContentOffset, Event.current.mousePosition);
                GUI.changed = true;
            }

            foreach (var connection in Connections)
            {
                connection.ContentOffset = ContentOffset;
                connection.Draw();
            }
        }

        protected virtual void DrawNodes()
        {
            foreach (var node in Nodes)
            {
                var pos = node.Position;
                node.Position += ContentOffset;
                node.Draw();
                node.Position = pos;
            }
        }

        protected virtual void DrawInspector()
        {
            var inspectorRect = new Rect(position.width - InspectorWidth, 0, InspectorWidth, position.height);
            
            GUI.BeginGroup(inspectorRect);
            
            var bc = GUI.backgroundColor;
            GUI.backgroundColor = InspectorColor;
            GUI.Box(new Rect(0, 0, inspectorRect.width, inspectorRect.height), string.Empty, _inspectorStyle);
            GUI.backgroundColor = bc;

            EditorGUI.BeginChangeCheck();
            GUILayout.BeginVertical();

            var focused = FindFocusedNode();
            if (focused != null)
            {
                focused.DrawInspector(new Rect(position.width - InspectorWidth, 0, InspectorWidth - 18, position.height));
                if (_lastInspectorNode != focused)
                {
                    GUI.changed = true;
                    _lastInspectorNode = focused;
                }
            }
            
            GUILayout.EndVertical();
            EditorGUI.EndChangeCheck();

            GUI.EndGroup();
        }

        protected virtual void DrawNavWindow()
        {
            if (_navigationWindow.Enable)
            {
                _navigationWindow.Draw();
            }
        }

        protected virtual void Draw()
        {
            DrawNodes();
            DrawConnections();
            DrawInspector();
            DrawNavWindow();
        }

        #endregion

        #region Private

        private void CreateConnection(ConnectionPort outPort, ConnectionPort inPort)
        {
            if (inPort.ParentNode.ApproveConnection(inPort, outPort))
            {
                int opc = CountConnections(outPort);
                int ipc = CountConnections(inPort);

                if ((opc == 0 || outPort.MultipleConnections) && (ipc == 0 || inPort.MultipleConnections))
                {
                    DoCommand(new CreateConnectionCommand(outPort, inPort, this));
                }
            }
        }
        
        private void UndoCommand()
        {
            if (_commands.Count == 0) return;

            EditorGUI.FocusTextInControl(null);
            
            int last = _commands.Count - 1;
            var command = _commands[last];
            
            _commands.RemoveAt(last);
            
            Changed |= command.ChangeDoc;
            
            if (_commands.Count == 0 || (_commands.Count > 0 && _commandAfterSaving == _commands[_commands.Count - 1]))
            {
                Changed = false;
            }
            
            command.Undo();
            CommandUndone?.Invoke(command);
            GUI.changed |= command.UpdateGUI;

            if (command.UndoPrev)
            {
                UndoCommand();
            }
        }

        private void TryFocusing()
        {
            _lastInspectorNode = null;
            
            var e = Event.current;

            var focused = FindFocusedNode();

            for (int i = Nodes.Count - 1; i >= 0; i--)
            {
                var node = Nodes[i];

                var pos = node.Position;
                node.Position += ContentOffset;

                if (node.Rect.Contains(e.mousePosition))
                {
                    if (focused == node)
                    {
                        node.Position = pos;
                        return;
                    }

                    node.Position = pos;
                    DoCommand(new FocusNodeCommand(focused, node, this));
                    return;
                }

                node.Position = pos;
            }

            if (focused != null)
            {
                DoCommand(new FocusNodeCommand(focused, null, this));
            }
        }

        private ConnectionPort FindConnectionPortUnderCursor()
        {
            var e = Event.current;

            foreach (var node in Nodes)
            {
                var pos = node.Position;
                node.Position += ContentOffset;

                ConnectionPort p = null;

                foreach (var port in node.Ports)
                {
                    if (port.Rect.Contains(e.mousePosition))
                    {
                        p = port;
                        break;
                    }
                }

                node.Position = pos;

                if (p != null)
                {
                    return p;
                }
            }

            return null;
        }

        private bool FindDestroyConnectionButtonUnderCursor()
        {
            var e = Event.current;

            foreach (var connection in Connections)
            {
                if (connection.DestroyButtonRect.Contains(e.mousePosition))
                {
                    return true;
                }
            }

            return false;
        }

        private void ProcessEvents()
        {
            var e = Event.current;
            
            if (e.type == EventType.KeyUp)
            {
                if (e.keyCode == KeyCode.RightAlt || e.keyCode== KeyCode.F12)
                {
                    _navigationWindow.Enable = false;
                    GUI.changed = true;
                }
            }

            if (_navigationWindow.Enable)
            {
                return;
            }

            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.RightAlt || e.keyCode== KeyCode.F12)
                {
                    _navigationWindow.Enable = true;
                    GUI.changed = true;
                }
                
                if (e.control && e.keyCode == KeyCode.S)
                {
                    CtrlSave();
                    e.Use();
                    return;
                }
                
                if (e.control && e.keyCode == KeyCode.Z)
                {
                    UndoCommand();
                    e.Use();
                    return;
                }

                if (e.control && (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace) && _startOutPort == null)
                {
                    var focused = FindFocusedNode();
                    if (focused != null)
                    {
                        DoCommand(new DestroyNodeCommand(focused, this));
                        e.Use();
                        return;
                    }
                }
            }

            var inspectorRect = new Rect(position.width - InspectorWidth, 0, InspectorWidth, position.height);
            
            if (e.type == EventType.MouseDown)
            {
                if (e.button == 0)
                {
                    if (!e.control && !FindDestroyConnectionButtonUnderCursor() && !inspectorRect.Contains(e.mousePosition))
                    {
                        var focusedPrev = FindFocusedNode();
                        TryFocusing();
                        var focused = FindFocusedNode();

                        if (focused != null)
                        {
                            var pos = focused.Position;
                            focused.Position += ContentOffset;

                            if (focused.HeaderRect.Contains(e.mousePosition))
                            {
                                _dragNodeCommand = new DragNodeCommand(focused.Position - ContentOffset, focused, this);

                                if (focused != focusedPrev)
                                {
                                    _dragNodeCommand.UndoPrev = true;
                                }
                            }

                            focused.Position = pos;
                        }

                        return;
                    }

                    //start connection
                    var port = FindConnectionPortUnderCursor();

                    if (e.control && port != null && port.PortDirection == PortDirection.Out && !port.Disabled)
                    {
                        int opc = CountConnections(port);
                        if (opc == 0 || port.MultipleConnections)
                        {
                            _startOutPort = port;
                        }
                    }
                }
            }

            if (e.type == EventType.MouseUp)
            {
                //end dragging
                if (_dragNodeCommand != null)
                {
                    if (_dragNodeCommand.Started)
                    {
                        DoCommand(_dragNodeCommand);
                    }

                    _dragNodeCommand = null;
                    return;
                }

                //end connection
                if (_startOutPort != null)
                {
                    var port = FindConnectionPortUnderCursor();

                    if (e.button == 0 && e.control && port != null)
                    {
                        CreateConnection(_startOutPort, port);
                    }

                    _startOutPort = null;
                    GUI.changed = true;
                }
            }

            if (e.type == EventType.MouseDrag)
            {
                if (e.button == 2 && !e.control)
                {
                    ContentOffset += e.delta;
                    GUI.changed = true;
                }

                if (e.button == 0 && e.alt) //OSX
                {
                    ContentOffset += e.delta;
                    GUI.changed = true;
                }

                if (_dragNodeCommand != null)
                {
                    _dragNodeCommand.DraggingNode.Position += e.delta;
                    _dragNodeCommand.Started = true;
                    GUI.changed = true;
                }
            }
        }
        
        private void CtrlSave()
        {
            if (!Changed) return;

            if (EditorPrefs.HasKey(LastOpenedFileNameKey))
            {
                SaveFile(EditorPrefs.GetString(LastOpenedFileNameKey));
            }
            else
            {
                var file = EditorUtility.SaveFilePanel("Create a new file", "", "file.json", "json");

                if (!string.IsNullOrEmpty(file))
                {
                    EditorPrefs.SetString(LastOpenedFileNameKey, file);
                    SaveFile(file);
                }
            }
        }

        private void WarningSave()
        {
            if (!Changed) return;

            if (EditorUtility.DisplayDialog("Warning!", "Save file?", "Yes", "No"))
            {
                CtrlSave();
            }
        }

        #endregion

        #region Unity Events

        private void OnGUI()
        {
            ProcessEvents();

            Draw();

            foreach (var connection in Connections)
            {
                if (connection.PendingDestroy)
                {
                    _connectionsToDestroy.Add(connection);
                }
            }

            if (_connectionsToDestroy.Count > 0)
            {
                foreach (var connection in _connectionsToDestroy)
                {
                    connection.PendingDestroy = false;
                    DoCommand(new DestroyConnectionCommand(connection, this));
                }

                _connectionsToDestroy.Clear();
            }

            if (GUI.changed)
            {
                Repaint();
            }
        }

        private void OnEnable()
        {
            CreateInstance<ResourcePath>();
            
            RawNode.DataProvider = new RawNodeDataProvider();

            _navigationWindow = new NodeLibNavigationWindow(this);

            _inspectorStyle.normal.background = Texture2D.whiteTexture;
            _inspectorStyle.normal.textColor = Color.white;

            _unsafe = true;

            if (EditorPrefs.HasKey(TempFileNameKey))
            {
                var data = new RawNode(Json.Deserialize(EditorPrefs.GetString(TempFileNameKey)));
                LoadJson(data);
            }
            else
            {
                NewDoc();
            }

            _unsafe = false;

            OnWindowEnable();
        }

        private void OnDisable()
        {
            var text = Json.Serialize(Serialize());
            EditorPrefs.SetString(TempFileNameKey, text);

            OnWindowDisable();
        }

        private void OnDestroy()
        {
            WarningSave();

            CommandDone = null;
            CommandUndone = null;

            OnWindowDestroy();

            EditorPrefs.DeleteKey(LastOpenedFileNameKey);
            EditorPrefs.DeleteKey(TempFileNameKey);
            //EditorUtility.UnloadUnusedAssetsImmediate(true);
        }

        #endregion

        public object Serialize()
        {
            return SerializeUtil.Dict().SetArgs(
                "editorType", EditorTypeKey, 
                "nodes", SerializeUtil.SerializeArray(Nodes),
                "connections", SerializeUtil.SerializeArray(Connections),
                "commands", SerializeUtil.SerializeArray(_commands)
                );
        }
    }
}