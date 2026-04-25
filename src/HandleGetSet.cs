using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.src
{
    public class HandleGetSet
    {
        static Dictionary<string, RedisValue> store = new();
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

            store[key] = new RedisValue
            {
                Value = value,
                Expiry = expiry
            };
            return OutputParser.SimpleString("OK");
        }

        public static string HandleGet(string key, Socket client)
        {
            string response = string.Empty;
            if (store.TryGetValue(key, out var entry))
            {
                if (entry.Expiry.HasValue && entry.Expiry.Value < DateTime.UtcNow)
                {
                    store.Remove(key);
                    response = OutputParser.NullBulk();
                }
                else
                {
                    string value = entry.Value;
                    response = OutputParser.BulkString(value);
                }
            }
            else
            {
                response = OutputParser.NullBulk();
            }
            return response;
        }
    }
}
