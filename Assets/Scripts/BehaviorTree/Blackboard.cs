using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree
{
    public class Blackboard
    {
        private Dictionary<string, object> data = new Dictionary<string, object>();

        //写入数据
        public void Set(string key, object value)
        {
            if (data.ContainsKey(key))
                data[key] = value;
            else
                data.Add(key, value);
        }

        //读取数据（泛型）
        public T Get<T>(string key)
        {
            if (data.TryGetValue(key, out object value))
            {
                try
                {
                    return (T)value;
                }
                catch
                {
                    Debug.LogError($"黑板键 '{key}' 的类型不是 {typeof(T)}");
                    return default;
                }
            }
            Debug.LogWarning($"黑板键 '{key}' 不存在");
            return default;
        }

        //检查键是否存在
        public bool Has(string key)
        {
            return data.ContainsKey(key);
        }

        //删除键
        public void Remove(string key)
        {
            if (data.ContainsKey(key))
                data.Remove(key);
        }

        //清空所有数据
        public void Clear()
        {
            data.Clear();
        }

        //调试：打印所有键
        public void DebugPrint()
        {
            Debug.Log("=== 黑板内容 ===");
            foreach (var kvp in data)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value}");
            }
        }
    }
}