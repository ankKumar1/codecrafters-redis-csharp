using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src
{
    public class HandleStreams
    {
        public static string Type(string[] command)
        {
            if (command.Length < 2)
                return OutputParser.Error("ERR wrong number of arguments");

            string key = command[1];
            if (DataStore.KeyValueStore.TryGetValue(key, out var entry))
            {
                if (entry.Expiry.HasValue && entry.Expiry.Value < DateTime.UtcNow)
                {
                    DataStore.KeyValueStore.TryRemove(key, out _);
                }
                else
                {
                    return OutputParser.SimpleString("string");
                }
            }

            if (DataStore.HasList(key))
            {
                return OutputParser.SimpleString("list");
            }

            if (DataStore.HasStream(key))
            {
                return OutputParser.SimpleString("stream");
            }

            return OutputParser.SimpleString("none");
        }

        public static string XAdd(string[] command)
        {
            if (command.Length < 5 || (command.Length - 3) % 2 != 0)
                return OutputParser.Error("ERR wrong number of arguments");

            string key = command[1];
            string id = command[2];

            var stream = DataStore.streamStore.GetOrAdd(key, _ => new List<StreamValue>());

            var fields = new Dictionary<string, string>();

            for (int i = 3; i < command.Length; i += 2)
            {
                fields[command[i]] = command[i + 1];
            }

            var entry = new StreamValue
            {
                Id = id,
                Fields = fields
            };

            lock (stream)
            {
                if (stream.Count > 0)
                {
                    var lastEntry = stream[^1];

                    if (!IsValidNewId(id, lastEntry.Id))
                    {
                        return OutputParser.Error(
                            "ERR The ID specified in XADD is equal or smaller than the target stream top item"
                        );
                    }
                }

                stream.Add(entry);
            }

            return OutputParser.BulkString(id);
        }

        static (long ms, long seq) ParseId(string id)
        {
            var parts = id.Split('-');
            return (long.Parse(parts[0]), long.Parse(parts[1]));
        }

        static bool IsValidNewId(string newId, string lastId)
        {
            var (newMs, newSeq) = ParseId(newId);
            var (lastMs, lastSeq) = ParseId(lastId);

            if (newMs > lastMs)
                return true;

            if (newMs == lastMs && newSeq > lastSeq)
                return true;

            return false;
        }
    }
}
