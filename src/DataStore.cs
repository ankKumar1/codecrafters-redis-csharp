using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src
{
    public static class DataStore
    {
        public static ConcurrentDictionary<string, RedisValue> KeyValueStore = new();
        public static ConcurrentDictionary<string, List<string>> listStore = new();

        public static bool HasList(string key)
        {
            return listStore.TryGetValue(key, out _);
        }
    }
}
