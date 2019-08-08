using System;
using System.Threading.Tasks;
using Promptuarium;

namespace Dump
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: Dump.exe <promptuarium_container>");
                Environment.Exit(-1);
            }

            var container = Element.LoadAsync(args[0]).GetAwaiter().GetResult();

            Console.WriteLine(container.TreeToString());
            Console.WriteLine(container.Statistics);
        }
    }
}
