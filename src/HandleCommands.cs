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

            switch (command[0].ToLower())
            {
                case "ping":
                    response = "+PONG\r\n";
                    break;

                case "echo":
                    string message = command[1];
                    response = $"${message.Length}\r\n{message}\r\n";
                    break;

                case "set":                   
                    response = HandleSet(command); 
                    break;

                case "get":
                    string key = command[1];
                    response = HandleGet(key, client);
                    break;

                case "rpush":
                    response = HandleLists.RPush(command);
                    break;

                case "lrange":
                    response = HandleLists.LRange(command);
                    break;

                case "lpush":
                    response = HandleLists.LRange(command);
                    break;

                default:
                    response = "-ERR unknown command\r\n";
                    break;
            }
            client.Send(Encoding.UTF8.GetBytes(response));
        }

        private static string HandleSet(string[] command)
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
            return "+OK\r\n";
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
