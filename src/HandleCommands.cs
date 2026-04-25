using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.src
{
    public class HandleCommands
    {
        public static void ExecuteCommands(string[] command, Socket client)
        {
            string response;

            switch (command[0].ToLower())
            {
                case "ping":
                    response = OutputParser.SimpleString("PONG");
                    break;

                case "echo":
                    string message = command[1];
                    response = $"${message.Length}\r\n{message}\r\n";
                    break;

                case "set":                   
                    response = HandleGetSet.HandleSet(command); 
                    break;

                case "get":
                    string key = command[1];
                    response = HandleGetSet.HandleGet(key, client);
                    break;

                case "rpush":
                    response = HandleLists.RPush(command);
                    break;

                case "lrange":
                    response = HandleLists.LRange(command);
                    break;

                case "lpush":
                    response = HandleLists.LPush(command);
                    break;

                case "llen":
                    response = HandleLists.LLen(command);
                    break;

                case "lpop":
                    response = HandleLists.LPop(command);
                    break;

                case "blpop":
                    response = HandleLists.BLPop(command, client);
                    break;

                default:
                    response = OutputParser.Error( "ERR unknown command");
                    break;
            }

            if (response != null)
            {
                client.Send(Encoding.UTF8.GetBytes(response));
            }
        }
    }
}
