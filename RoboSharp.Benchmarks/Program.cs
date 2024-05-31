using global::System;
using global::System.Collections.Generic;

using BenchmarkDotNet.Running;

namespace RoboSharp.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = new BenchmarkSwitcher(typeof(Program).Assembly).Run(args);
            Console.ReadLine();
        }
    }
}
