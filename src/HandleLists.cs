using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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
                return OutputParser.Error("ERR wrong number of arguments");

            string key = command[1];
            var list = listStore.GetOrAdd(key, _ => new List<string>());
            var queue = waitingClients.GetOrAdd(key, _ => new Queue<Socket>());

            int currentLength;

            lock (list)
            {
                currentLength = list.Count;
            }

            int valuesToPush = command.Length - 2;

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
                    string response = OutputParser.Array(key, val); 
                    client.Send(Encoding.UTF8.GetBytes(response));
                }
                else
                {
                    lock (list)
                    {
                        list.Add(val);
                    }
                }
            }

            return OutputParser.Integer(currentLength + valuesToPush); 
        }


        public static string LRange(string[] command)
        {
            if (command.Length < 4)
                return OutputParser.Error("ERR wrong number of arguments");

            string key = command[1];

            if (!listStore.TryGetValue(key, out var list))
                return OutputParser.Array(new List<string>()); 

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
                    return OutputParser.Array(new List<string>());

                var resultList = new List<string>();

                for (int i = start; i <= stop; i++)
                {
                    resultList.Add(list[i]);
                }

                return OutputParser.Array(resultList); 
            }
        }

        public static string LPush(string[] command)
        {
            if (command.Length < 3)
                return OutputParser.Error("ERR wrong number of arguments");

            string key = command[1];
            var list = listStore.GetOrAdd(key, _ => new List<string>());

            lock (list)
            {
                foreach (var val in command.Skip(2))
                {
                    list.Insert(0, val);
                }
            }

            return OutputParser.Integer(list.Count); 
        }

        public static string LLen(string[] command)
        {
            if (command.Length < 2)
                return OutputParser.Error("ERR wrong number of arguments");

            string key = command[1];

            if (!listStore.TryGetValue(key, out var list))
                return OutputParser.Integer(0);

            return OutputParser.Integer(list.Count); 
        }

        public static string LPop(string[] command)
        {
            if (command.Length < 2)
                return OutputParser.Error("ERR wrong number of arguments");

            string key = command[1];

            if (!listStore.TryGetValue(key, out var list) || list.Count == 0)
                return OutputParser.NullBulk();

            lock (list)
            {
                if (command.Length == 2)
                {
                    string val = list[0];
                    list.RemoveAt(0);
                    return OutputParser.BulkString(val);
                }

                int count = int.Parse(command[2]);
                if (count > list.Count) count = list.Count;

                var resultList = new List<string>();

                for (int i = 0; i < count; i++)
                {
                    resultList.Add(list[i]);
                }

                list.RemoveRange(0, count);

                return OutputParser.Array(resultList); 
            }
        }

        public static string BLPop(string[] command, Socket client)
        {
            if (command.Length < 3)
                return OutputParser.Error("ERR wrong number of arguments");

            string key = command[1];
            double timeout = double.Parse(command[^1], CultureInfo.InvariantCulture);

            var list = listStore.GetOrAdd(key, _ => new List<string>());
            var queue = waitingClients.GetOrAdd(key, _ => new Queue<Socket>());
            var keyLock = keyLocks.GetOrAdd(key, _ => new object());

            lock (keyLock)
            {
                if (list.Count > 0)
                {
                    string value = list[0];
                    list.RemoveAt(0);
                    return OutputParser.Array(key, value);
                }

                queue.Enqueue(client);
            }

            if (timeout > 0)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(timeout));

                    lock (keyLock)
                    {
                        if (queue.Contains(client))
                        {
                            var newQueue = new Queue<Socket>();

                            while (queue.Count > 0)
                            {
                                var c = queue.Dequeue();
                                if (c != client)
                                    newQueue.Enqueue(c);
                            }

                            while (newQueue.Count > 0)
                                queue.Enqueue(newQueue.Dequeue());

                            client.Send(Encoding.UTF8.GetBytes(OutputParser.NullArray()));
                        }
                    }
                });
            }

            return null;
        }
    }
}