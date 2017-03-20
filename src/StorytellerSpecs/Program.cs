﻿using System;
using StoryTeller;

namespace StorytellerSpecs
{
    internal class Program
    {
        public static void Debug()
        {
            using (var runner = StorytellerRunner.Basic())
            {
                // worry about the 2nd message here
                var counts = runner.Run("Error Handling / Fallback from Chain to Global Error Handling").Counts;

                // Error Handling / Retry on Exceptions

                Console.WriteLine(counts);
            }
        }

        public static void Main(string[] args)
        {
            StorytellerAgent.Run(args);
        }
    }
}