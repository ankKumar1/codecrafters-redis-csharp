using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src
{
    public class InputParser
    {
        public static string[] ParseInput(string input)
        {
            var result = new List<string>();

            var parts = input.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

            int count = int.Parse(parts[0].Substring(1));

            int index = 1;

            for (int i = 0; i < count; i++)
            {
                index++;
                result.Add(parts[index]);
                index++;
            }

            return result.ToArray();
        }
    }
}
