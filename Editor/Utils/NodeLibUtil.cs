using System;
using UnityEngine;

namespace NodeLib.Editor.Utils
{
    public static class NodeLibUtil
    {
        public static string GetUniqueId()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", "");
        }

        public static byte[] FromColor32(Color32 color)
        {
            return new [] {color.r, color.g, color.b, color.a};
        }
        
        public static Color32 ToColor32(byte[] arr)
        {
            return new Color32(arr[0], arr[1], arr[2], arr[3]);
        }

        public static float[] FromVector(Vector2 v)
        {
            return new[] { v.x, v.y };
        }
        
        public static Vector2 ToVector(float[] arr)
        {
            return new Vector2(arr[0], arr[1]);
        }
        
        public static void DrawWithColor(Color color, Action m)
        {
            var tmp = GUI.color;
            GUI.color = color;
            m();
            GUI.color = tmp;
        }
        
        public static void DrawWithBackColor(Color color, Action m)
        {
            var tmp = GUI.backgroundColor;
            GUI.backgroundColor = color;
            m();
            GUI.backgroundColor = tmp;
        }        
    }
}