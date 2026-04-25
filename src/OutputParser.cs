using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src
{
    public static class OutputParser
    {
        public static string SimpleString(string value)
        {
            return $"+{value}\r\n";
        }

        public static string Error(string message)
        {
            return $"-{message}\r\n";
        }

        public static string Integer(int value)
        {
            return $":{value}\r\n";
        }

        public static string BulkString(string value)
        {
            if (value == null)
                return "$-1\r\n";

            return $"${value.Length}\r\n{value}\r\n";
        }

        public static string NullBulk()
        {
            return "$-1\r\n";
        }

        public static string NullArray()
        {
            return "*-1\r\n";
        }

        public static string Array(List<string> values)
        {
            if (values == null)
                return "*-1\r\n";

            var sb = new StringBuilder();
            sb.Append($"*{values.Count}\r\n");

            foreach (var val in values)
            {
                sb.Append(BulkString(val));
            }

            return sb.ToString();
        }

        public static string Array(params string[] values)
        {
            return Array(values.ToList());
        }
    }
}
