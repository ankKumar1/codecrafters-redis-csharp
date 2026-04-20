using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.src
{
    public class HandleCommands
    {
        static Dictionary<string, RedisValue> store = new();
        public static void ExecuteCommands(string[] command, Socket client)
        {
            string response;
            if (command[0].Equals("echo", StringComparison.OrdinalIgnoreCase))
            {
                string message = command[1];
                response = $"${message.Length}\r\n{message}\r\n";
            }
            else if (command[0].Equals("set", StringComparison.OrdinalIgnoreCase))
            {
                HandleSet(command);
                response = "+OK\r\n";
            }
            else if (command[0].Equals("get", StringComparison.OrdinalIgnoreCase))
            {
                string key = command[1];
                response = HandleGet(key, client);           
            }
            else if (command[0].Equals("rpush", StringComparison.OrdinalIgnoreCase))
            {
                response = HandleLists.RPush(command);
            }
            else
            {
                response = "+PONG\r\n";
                
            }
            client.Send(Encoding.UTF8.GetBytes(response));
        }

        private static void HandleSet(string[] command)
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
        }

        private static string HandleGet(string key, Socket client)
        {
            string response = string.Empty;
            if (store.TryGetValue(key, out var entry))
            {
                if (entry.Expiry.HasValue && entry.Expiry.Value < DateTime.UtcNow)
                {
                    store.Remove(key);
                    response = "$-1\r\n";
                }
                else
                {
                    string value = entry.Value;
                    response = $"${value.Length}\r\n{value}\r\n";
                }
            }
            else
            {
                response = "$-1\r\n";
            }
            return response;
        }
    }
}
