using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src
{
    public class HandleLists
    {
        static Dictionary<string, List<string>> listStore = new Dictionary<string, List<string>>();
        public static string RPush(string[] command)
        {
            if (command.Length < 3)
            {
                return "-ERR wrong number of arguments\r\n";
            }

            string key = command[1];
            if (!listStore.TryGetValue(key, out var list))
            {
                list = new List<string>();
                listStore[key] = list;
            }

            for (int i = 2; i < command.Length; i++)
            {
                list.Add(command[i]);
            }

            return $":{list.Count}\r\n";
        }

        public static string LRange(string[] command)
        {
            if (command.Length < 4)
            {
                return "-ERR wrong number of arguments\r\n";
            }

            string key = command[1];
            
            if (!listStore.TryGetValue(key, out var list))
            {
                return "*0\r\n";
            }
            int n = list.Count;
            int start = int.Parse(command[2]);
            int stop = int.Parse(command[3]);

            if(start>=n || start > stop)
            {
                return "*0\r\n";
            }

            var result = new StringBuilder();

            int count = stop - start + 1;
            result.Append($"*{count}\r\n");

            for (int i = start; i <= stop; i++)
            {
                string value = list[i];

                result.Append($"${value.Length}\r\n");
                result.Append($"{value}\r\n");
            }

            return result.ToString();
        }
    }
}
