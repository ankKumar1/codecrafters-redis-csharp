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
    }
}
