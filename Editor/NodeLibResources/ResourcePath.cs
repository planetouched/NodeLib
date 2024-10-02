using System.IO;
using UnityEditor;
using UnityEngine;

namespace NodeLib.Editor.EditorResources
{
    public class ResourcePath : UnityEditor.Editor
    {
        public static string Path { get; private set; }
        private void Awake()
        {
            var ms = MonoScript.FromScriptableObject(this);
            var scriptFilePath = AssetDatabase.GetAssetPath(ms);
            var fi = new FileInfo(scriptFilePath);

            var path = fi.Directory.ToString().Replace('\\', '/');
            Path = path.Replace(Application.dataPath, "Assets");
        }
    }
}