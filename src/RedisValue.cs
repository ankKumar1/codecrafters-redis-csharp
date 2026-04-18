using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src
{
    public class RedisValue
    {
        public string Value { get; set; }
        public DateTime? Expiry { get; set; }
    }
}
