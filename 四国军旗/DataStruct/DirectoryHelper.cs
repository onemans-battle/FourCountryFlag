using System;
using System.Collections.Generic;
using System.Linq;

namespace DataStruct
{
    //扩展类,
    public static class DirectoryHelper
    {
        //遍历字典中的每个键，对每个键值对调用委托
        public static void Map<TKey, TValue>(this Dictionary<TKey, TValue> directory,
            Action<TKey, Dictionary<TKey, TValue>> action)
        {
            TKey[] keys=directory.Keys.ToArray();
            foreach (TKey key in keys)
            {
                action(key, directory);
            }
        }
       
    }
}
