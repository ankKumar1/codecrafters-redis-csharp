using System;
using System.Collections;
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

            if (start < 0) start = n + start;
            if (stop < 0) stop = n + stop;

            if (start < 0) start = 0;
            if (stop >= n) stop = n - 1;

            if (start >= n || start > stop)
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

        public static string LPush(string[] commands)
        {
            if (commands.Length < 3)
            {
                return "-ERR wrong number of arguments\r\n";
            }

            string key = commands[1];

            if (!listStore.TryGetValue(key, out var list))
            {
                list = new List<string>();
                listStore[key] = list;
            }

            for (int i = 2; i < commands.Length; i++)
            {
                list.Insert(0, commands[i]);
            }

            return $":{list.Count}\r\n";
        }

        public static string LLen(string[] commands)
        {
            if (commands.Length < 2)
            {
                return "-ERR wrong number of arguments\r\n";
            }

            string key = commands[1];

            if (!listStore.TryGetValue(key, out var list))
            {
                return $":0\r\n";
            }
            return $":{list.Count}\r\n";
        }

        public static string LPop(string[] commands)
        {
            if (commands.Length < 2)
            {
                return "-ERR wrong number of arguments\r\n";
            }

            string key = commands[1];

            if (!listStore.TryGetValue(key, out var list) || list.Count == 0)
            {
                return $"$-1\r\n";
            }

            if (commands.Length == 2)
            {
                string val = list[0];
                list.RemoveAt(0);
                return $"${val.Length}\r\n{val}\r\n";
            }

            int count = int.Parse(commands[2]);

            if (count > list.Count)
                count = list.Count;

            var result = new StringBuilder();

            result.Append($"*{count}\r\n");

            for (int i = 0; i < count; i++)
            {
                string value = list[i];
                result.Append($"${value.Length}\r\n");
                result.Append($"{value}\r\n");
            }

            list.RemoveRange(0, count);
            return result.ToString();
        }

        public static string BLPop(string[] commands)
        {
            if (commands.Length < 3)
            {
                return "-ERR wrong number of arguments\r\n";
            }

            string key = commands[1];
            int timeout = int.Parse(commands[2]);

            DateTime endTime = DateTime.UtcNow.AddSeconds(timeout);

            while (DateTime.UtcNow < endTime)
            {
                if (listStore.TryGetValue(key, out var list) && list.Count > 0)
                {
                        string value = list[0];
                        list.RemoveAt(0);

                        var result = new StringBuilder();

                        result.Append("*2\r\n");
                        result.Append($"${key.Length}\r\n{key}\r\n");
                        result.Append($"${value.Length}\r\n{value}\r\n");

                        return result.ToString();    
                }

                Thread.Sleep(100);
            }

            return "$-1\r\n";
        }
    }
}
