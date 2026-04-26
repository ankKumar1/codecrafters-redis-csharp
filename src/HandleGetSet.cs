using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.src
{
    public class HandleGetSet
    {
        public static string HandleSet(string[] command)
        {
            string key = command[1];
            string value = command[2];

            DateTime? expiry = null;

            if (command.Length >= 5 && command[3].Equals("PX", StringComparison.OrdinalIgnoreCase))
            {
                int milliseconds = int.Parse(command[4]);
                expiry = DateTime.UtcNow.AddMilliseconds(milliseconds);
            }

            DataStore.KeyValueStore[key] = new RedisValue
            {
                Value = value,
                Expiry = expiry
            };

            return OutputParser.SimpleString("OK");
        }

        public static string HandleGet(string key)
        {
            if (DataStore.KeyValueStore.TryGetValue(key, out var entry))
            {
                if (entry.Expiry.HasValue && entry.Expiry.Value < DateTime.UtcNow)
                {
                    DataStore.KeyValueStore.TryRemove(key, out _);
                    return OutputParser.NullBulk();
                }

                return OutputParser.BulkString(entry.Value);
            }

            return OutputParser.NullBulk();
        }
    }
}
