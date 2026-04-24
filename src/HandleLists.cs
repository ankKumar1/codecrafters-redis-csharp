using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.src
{
    public class HandleLists
    {
        static ConcurrentDictionary<string, List<string>> listStore = new();
        static ConcurrentDictionary<string, Queue<Socket>> waitingClients = new();
        static ConcurrentDictionary<string, object> keyLocks = new();

        public static string RPush(string[] command)
        {
            if (command.Length < 3)
                return "-ERR wrong number of arguments\r\n";

            string key = command[1];
            var list = listStore.GetOrAdd(key, _ => new List<string>());
            var queue = waitingClients.GetOrAdd(key, _ => new Queue<Socket>());

            int pushedToList = 0;

            foreach (var val in command.Skip(2))
            {
                Socket? client = null;

                lock (queue)
                {
                    if (queue.Count > 0)
                        client = queue.Dequeue();
                }

                if (client != null)
                {
                    string response = BuildArrayResponse(key, val);
                    client.Send(Encoding.UTF8.GetBytes(response));
                }
                else
                {
                    lock (list)
                    {
                        list.Add(val);
                        pushedToList++;
                    }
                }
            }

            int finalLength;

            lock (list)
            {
                finalLength = list.Count;
            }
            return $":{finalLength}\r\n";
        }


        public static string LRange(string[] command)
        {
            if (command.Length < 4)
                return "-ERR wrong number of arguments\r\n";

            string key = command[1];

            if (!listStore.TryGetValue(key, out var list))
                return "*0\r\n";

            lock (list)
            {
                int n = list.Count;

                int start = int.Parse(command[2]);
                int stop = int.Parse(command[3]);

                if (start < 0) start = n + start;
                if (stop < 0) stop = n + stop;

                if (start < 0) start = 0;
                if (stop >= n) stop = n - 1;

                if (start >= n || start > stop)
                    return "*0\r\n";

                var result = new StringBuilder();
                int count = stop - start + 1;

                result.Append($"*{count}\r\n");

                for (int i = start; i <= stop; i++)
                {
                    string value = list[i];
                    result.Append($"${value.Length}\r\n{value}\r\n");
                }

                return result.ToString();
            }
        }

        public static string LPush(string[] command)
        {
            if (command.Length < 3)
                return "-ERR wrong number of arguments\r\n";

            string key = command[1];
            var list = listStore.GetOrAdd(key, _ => new List<string>());

            lock (list)
            {
                foreach (var val in command.Skip(2))
                {
                    list.Insert(0, val);
                }
            }

            return $":{list.Count}\r\n";
        }

        public static string LLen(string[] command)
        {
            if (command.Length < 2)
                return "-ERR wrong number of arguments\r\n";

            string key = command[1];

            if (!listStore.TryGetValue(key, out var list))
                return ":0\r\n";

            return $":{list.Count}\r\n";
        }

        public static string LPop(string[] command)
        {
            if (command.Length < 2)
                return "-ERR wrong number of arguments\r\n";

            string key = command[1];

            if (!listStore.TryGetValue(key, out var list) || list.Count == 0)
                return "$-1\r\n";

            lock (list)
            {
                if (command.Length == 2)
                {
                    string val = list[0];
                    list.RemoveAt(0);
                    return $"${val.Length}\r\n{val}\r\n";
                }

                int count = int.Parse(command[2]);
                if (count > list.Count) count = list.Count;

                var result = new StringBuilder();
                result.Append($"*{count}\r\n");

                for (int i = 0; i < count; i++)
                {
                    string val = list[i];
                    result.Append($"${val.Length}\r\n{val}\r\n");
                }

                list.RemoveRange(0, count);
                return result.ToString();
            }
        }

        public static string BLPop(string[] command, Socket client)
        {
            if (command.Length < 3)
                return "-ERR wrong number of arguments\r\n";

            string key = command[1];

            var list = listStore.GetOrAdd(key, _ => new List<string>());
            var queue = waitingClients.GetOrAdd(key, _ => new Queue<Socket>());
            var keyLock = keyLocks.GetOrAdd(key, _ => new object());

            lock (keyLock) 
            {
                if (list.Count > 0)
                {
                    string value = list[0];
                    list.RemoveAt(0);
                    return BuildArrayResponse(key, value);
                }

                queue.Enqueue(client);
            }

            return null;
        }

        private static string BuildArrayResponse(string key, string value)
        {
            var sb = new StringBuilder();

            sb.Append("*2\r\n");
            sb.Append($"${key.Length}\r\n{key}\r\n");
            sb.Append($"${value.Length}\r\n{value}\r\n");

            return sb.ToString();
        }
    }
}