using System;
using System.Collections.Generic;
using UnityEngine;

namespace FusionContinent.Framework
{
    /// <summary>
    /// 可序列化的字典
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> _keys = new List<TKey>();

        [SerializeField]
        private List<TValue> _values = new List<TValue>();

        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();

            foreach (var kvp in this)
            {
                _keys.Add(kvp.Key);
                _values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();

            for (int i = 0; i < Mathf.Min(_keys.Count, _values.Count); i++)
            {
                this[_keys[i]] = _values[i];
            }
        }
    }
}
