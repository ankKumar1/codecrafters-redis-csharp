using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src
{
    public class HandleStreams
    {
        const string ERR_ID_ORDER = "ERR The ID specified in XADD is equal or smaller than the target stream top item";
        const string ERR_ZERO_ID = "ERR The ID specified in XADD must be greater than 0-0";

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

            if (id.EndsWith("-*"))
            {
                var parts = id.Split('-');
                long ms = long.Parse(parts[0]);

                long seq = (ms == 0) ? 1 : 0;

                if (stream.Count > 0)
                {
                    var (lastMs, lastSeq) = ParseId(stream[^1].Id);

                    if (ms < lastMs)
                        return OutputParser.Error(ERR_ID_ORDER);

                    if (ms == lastMs)
                        seq = lastSeq + 1;
                }

                id = $"{ms}-{seq}";
            }

            if (id == "0-0")
                return OutputParser.Error(ERR_ZERO_ID);

            lock (stream)
            {
                if (stream.Count > 0)
                {
                    var lastId = stream[^1].Id;

                    if (!IsValidNewId(id, lastId))
                        return OutputParser.Error(ERR_ID_ORDER);
                }

                var fields = new Dictionary<string, string>();

                for (int i = 3; i < command.Length; i += 2)
                {
                    fields[command[i]] = command[i + 1];
                }

                stream.Add(new StreamValue
                {
                    Id = id,
                    Fields = fields
                });
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
