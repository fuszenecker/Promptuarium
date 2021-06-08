using System;
using System.Threading.Tasks;
using Promptuarium;

namespace Dump
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: Dump.exe <promptuarium_container>");
                Environment.Exit(-1);
            }

            var container = await Element.LoadAsync(args[0]).ConfigureAwait(false);

            Console.WriteLine(container.TreeToString());
            Console.WriteLine(container.Statistics);
        }
    }
}
