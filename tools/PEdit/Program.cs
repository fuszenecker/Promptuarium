using System;
using System.Threading.Tasks;

namespace PEdit;

internal static class Program
{
    private static Task<int> Main(string[] args)
    {
        return Commands.RunAsync(args, Console.Out, Console.Error);
    }
}
