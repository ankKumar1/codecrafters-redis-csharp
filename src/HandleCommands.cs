using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.src
{
    public class HandleCommands
    {
        static Dictionary<string, string> valuePairs = new Dictionary<string, string>();
        public static void ExecuteCommands(string[] command, Socket client)
        {
            string response = string.Empty;
            if (command[0].Equals("echo", StringComparison.CurrentCultureIgnoreCase))
            {
                string message = command[1];
                response = $"${message.Length}\r\n{message}\r\n";
            }
            else if (command[0].Equals("set", StringComparison.CurrentCultureIgnoreCase))
            {
                valuePairs[command[1]] = command[2];
                response = $"+OK\r\n";
            }
            else if (command[0].Equals("get", StringComparison.CurrentCultureIgnoreCase))
            {
                if (valuePairs.TryGetValue(command[1], out string? value))
                {
                    response = $"${value.Length}\r\n{value}\r\n";
                }
                else
                {
                    response = "$-1\r\n";
                }
            }
            else
            {
                response = "+PONG\r\n";
                
            }
            client.Send(Encoding.UTF8.GetBytes(response));
        }
    }
}
