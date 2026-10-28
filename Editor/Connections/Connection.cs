using System;
using System.Collections.Generic;
using JsonRawNode;
using NodeLib.Editor.Nodes;
using NodeLib.Editor.Serialization;
using NodeLib.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace NodeLib.Editor.Connections
{
    public class Connection : ISerialize
    {
        private const int Size = 5; 
        
        public string Id { get; }
        
        public ConnectionPort OutPort { get; set; }
        public ConnectionPort InPort { get; set; }
        
        public Vector2 ContentOffset { get; set; }
        
        public bool PendingDestroy { get; set; }
        public Rect DestroyButtonRect { get; private set; }

        private bool _lastMouseButton0;
        
        public Connection(ConnectionPort outPort, ConnectionPort inPort)
        {
            Id = NodeLibUtil.GetUniqueId();
            
            OutPort = outPort;
            InPort = inPort;
        }

        public Connection(List<NodeBase> nodes, RawNode data)
        {
            Id = data.GetString("id");
            
            string outPortId = data.GetString("out");
            string inPortId = data.GetString("in");
            
            foreach (var node in nodes)
            {
                foreach (var port in node.Ports)
                {
                    if (OutPort == null && port.PortDirection == PortDirection.Out && port.Id == outPortId)
                    {
                        OutPort = port;
                    }
                    
                    if (InPort == null && port.PortDirection == PortDirection.In && port.Id == inPortId)
                    {
                        InPort = port;
                    }

                    if (InPort != null && OutPort != null)
                    {
                        return;
                    }
                }
            }
        }

        private Vector2 CalcTangent(ConnectionPort port)
        {
            return port.PortSide switch
            {
                PortSide.Right => Vector2.right,
                PortSide.Left => Vector2.left,
                PortSide.Up => Vector2.down,
                PortSide.Down => Vector2.up,
                _ => throw new Exception("CalcTangent")
            };
        }

        public void Draw()
        {
            if (OutPort == null || InPort == null)
            {
                Debug.LogWarning("Port is null, Connection ID: " + Id);
                return;
            }
            
            var outPortPos = OutPort.Rect.center + ContentOffset;
            var inPortPos = InPort.Rect.center + ContentOffset;
            
            var distance = Vector2.Distance(outPortPos, inPortPos);
            distance = Mathf.Lerp(25, 200, distance / 500f);

            Handles.color = OutPort.Color;
            
            var startTangent = outPortPos + CalcTangent(OutPort) * distance;
            var endTangent = inPortPos + CalcTangent(InPort) * distance;
            
            Handles.DrawBezier(outPortPos, inPortPos, startTangent, endTangent, Color.white, null, 2);
            var p = Handles.MakeBezierPoints(outPortPos, inPortPos, startTangent, endTangent, 3);
            Handles.color = Color.red;

            DestroyButtonRect = new Rect(p[1] - Vector3.one * Size, Vector2.one * (Size * 2));

            var e = Event.current;
            _lastMouseButton0 = e.button == 0;
            
            if (Handles.Button(DestroyButtonRect.center, Quaternion.identity, DestroyButtonRect.width / 2, DestroyButtonRect.width, Handles.DotHandleCap))
            {
                if (_lastMouseButton0)
                {
                    PendingDestroy = true;
                }
            }
        }

        public object Serialize()
        {
            return SerializeUtil.Dict().SetArgs("id", Id, "out", OutPort.Id, "in", InPort.Id);
        }
    }
}