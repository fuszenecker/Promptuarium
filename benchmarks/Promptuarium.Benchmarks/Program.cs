using BenchmarkDotNet.Running;

// dotnet run -c Release --project benchmarks/Promptuarium.Benchmarks -- --filter '*'
// (or without arguments, to choose the benchmarks interactively)
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
