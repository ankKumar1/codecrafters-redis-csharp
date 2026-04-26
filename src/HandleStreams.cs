using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src
{
    public class HandleStreams
    {
        public static string Type(string[] command)
        {
            if (command.Length < 2)
                return OutputParser.Error("ERR wrong number of arguments");

            string key = command[1];
            if (DataStore.KeyValueStore.TryGetValue(key, out var entry))
            {
                if (entry.Expiry.HasValue && entry.Expiry.Value < DateTime.UtcNow)
                {
                    DataStore.KeyValueStore.TryRemove(key, out _);
                }
                else
                {
                    return OutputParser.SimpleString("string");
                }
            }

            if (DataStore.HasList(key))
            {
                return OutputParser.SimpleString("list");
            }
            return OutputParser.SimpleString("none");
        }
    }
}
