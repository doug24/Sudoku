using BenchmarkDotNet.Attributes;
using System.Threading;
using QQWingLib;
using Microsoft.VSDiagnostics;

namespace QQWingBenchmarks;
[CPUUsageDiagnoser]
public class SolveBenchmarks
{
    private static readonly int[] PuzzleData = [5, 0, 0, 7, 0, 6, 0, 0, 4, 9, 0, 0, 0, 5, 0, 0, 0, 2, 0, 0, 7, 0, 0, 0, 1, 0, 0, 0, 3, 0, 0, 0, 0, 0, 1, 0, 0, 5, 0, 4, 0, 1, 0, 6, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 3, 0, 0, 9, 0, 4, 0, 0, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 8, 0, 0, 5, 0, ];
    private QQWing _solver = null !;
    [GlobalSetup]
    public void Setup()
    {
        QQWing.SectionLayout = new RegularLayout();
        _solver = new QQWing();
        _solver.SetRecordHistory(true);
    }

    [Benchmark]
    public bool SolveWithHistory()
    {
        _solver.SetPuzzle(PuzzleData);
        _solver.SetRecordHistory(true);
        return _solver.Solve(CancellationToken.None);
    }

    [Benchmark]
    public bool SolveWithoutHistory()
    {
        _solver.SetPuzzle(PuzzleData);
        _solver.SetRecordHistory(false);
        return _solver.Solve(CancellationToken.None);
    }
}