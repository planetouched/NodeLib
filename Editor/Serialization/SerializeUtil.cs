using System;
using System.Collections.Generic;

namespace NodeLib.Editor.Serialization
{
    public static class SerializeUtil
    {
        public static Dictionary<string, object> SetArgs(this Dictionary<string, object> dict, params object[] args)
        {
            for (int i = 0; i < args.Length / 2; i++)
            {
                var value = args[i * 2 + 1];
                
                if (value != null)
                {
                    if (value is float f)
                    {
                        value = Math.Round(f, 4);
                    }
                    
                    if (value is double d)
                    {
                        value = Math.Round(d, 4);
                    }
                    
                    dict[(string) args[i * 2]] = value;
                }
            }

            return dict;
        }
        
        public static List<Dictionary<string, object>> SetArgs(this List<Dictionary<string, object>> dictArray, params object[] args)
        {
            var dict = new Dictionary<string, object>();
            dictArray.Add(dict);
            
            for (int i = 0; i < args.Length / 2; i++)
            {
                var value = args[i * 2 + 1];
                
                if (value is float f)
                {
                    value = Math.Round(f, 4);
                }
                    
                if (value is double d)
                {
                    value = Math.Round(d, 4);
                }
                
                dict.Add((string) args[i * 2], value);
            }

            return dictArray;
        }

        public static Dictionary<string, object> Dict(object fromDictionary = null)
        {
            if (fromDictionary != null)
            {
                return (Dictionary<string, object>) fromDictionary;
            }

            return new Dictionary<string, object>();
        }
        
        public static IList<Dictionary<string, object>> DictArray(object fromDictArray = null)
        {
            if (fromDictArray != null)
            {
                return (IList<Dictionary<string, object>>) fromDictArray;
            }

            return new List<Dictionary<string, object>>();
        }

        public static IList<object> SerializeArray<T>(IEnumerable<T> collection) where T : ISerialize
        {
            var list = new List<object>();

            foreach (var item in collection)
            {
                list.Add(item.Serialize());
            }

            return list;
        }
    }
}
