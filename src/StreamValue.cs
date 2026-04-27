using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src
{
    public class StreamValue
    {
        public string Id;
        public Dictionary<string, string> Fields;
    }
}
