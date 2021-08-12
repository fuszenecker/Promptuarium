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
                Console.WriteLine("Usage: dump.exe <promptuarium_container>");
                Environment.Exit(-1);
            }

            try
            {
                var container = await Element.LoadAsync(args[0]).ConfigureAwait(false);

                Console.WriteLine(container.TreeToString("-> "));
                Console.WriteLine(container.Statistics);
            }
            catch (Exception ex)
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exception has been thrown during execution:\n{ex.Message}\n{ex.InnerException?.Message}");
                Console.ForegroundColor = originalColor;
            }
        }
    }
}
