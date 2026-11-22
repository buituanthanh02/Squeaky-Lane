using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Solver;
using PuzzleSolver = SqueakyLane.Core.Solver.Solver;

namespace SqueakyLane.Tests.EditMode
{
    public sealed class SolverPerformanceTests
    {
        [Test]
        public void Level16_ReportsTimeAllocationsAndSingleExpansionCost()
        {
            // Nạp JSON, tạo đồng hồ và JIT đường nhỏ trước vùng đo; không tính I/O/assert/report vào Solve.
            var level = SolverTestData.Load(16); var initial = GameState.Initial(level);
            var warmup = SolverTestData.Load(1);
            PuzzleSolver.Solve(warmup, GameState.Initial(warmup), 100);
            var warmWin = new WinSearch(warmup, GameState.Initial(warmup), 100); while (!warmWin.Step(1)) { }
            var watch = new Stopwatch(); var stepWatch = new Stopwatch();
            var times = new double[3];
            var report = new StringBuilder();
            report.AppendLine("Unity=" + Application.unityVersion + "; Editor/Mono; level=16; maxStates=300000; warmup=level1");
            report.AppendLine("CPU=" + SystemInfo.processorType + "; RAM_MB=" + SystemInfo.systemMemorySize);
            // GC.GetAllocatedBytesForCurrentThread có thể luôn trả 0 trên runtime này.
            // Hiệu chuẩn bằng cấp phát biết trước; Profiler counter là ước lượng mọi luồng trong vùng đồng bộ.
            using var allocation = new ProfilerRecorder(ProfilerCategory.Memory, "GC Allocated In Frame", 0);
            Assert.That(allocation.Valid, Is.True, "Không có Profiler counter cấp phát");
            Assert.That(allocation.UnitType, Is.EqualTo(ProfilerMarkerDataUnit.Bytes));
            long managedBefore = GC.GetAllocatedBytesForCurrentThread(), profilerBefore = allocation.CurrentValue;
            var probe = new byte[1024 * 1024]; probe[0] = 1;
            long profilerProbe = allocation.CurrentValue - profilerBefore;
            long managedProbe = GC.GetAllocatedBytesForCurrentThread() - managedBefore;
            GC.KeepAlive(probe);
            Assert.That(profilerProbe, Is.GreaterThanOrEqualTo(1024 * 1024), "Profiler không bắt được cấp phát mẫu 1 MiB");
            report.AppendLine("Calibration 1MiB: managedThreadCounterDelta=" + managedProbe + "; profilerDelta=" + profilerProbe);
            report.AppendLine("Allocation meter=ProfilerRecorder GC Allocated In Frame.CurrentValue delta; all threads; estimate, includes background noise.");
            for (int sample = 0; sample < times.Length; sample++)
            {
                // Chỉ dùng trong phép đo Editor, không đưa GC.Collect vào solver/runtime.
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2);
                long before = allocation.CurrentValue;
                watch.Restart(); var result = PuzzleSolver.Solve(level, initial, 300000); watch.Stop();
                long bytes = allocation.CurrentValue - before;
                Assert.That(bytes, Is.GreaterThan(0), "Không được nhận 0 byte khi bộ đếm không hoạt động");
                times[sample] = watch.Elapsed.TotalMilliseconds;
                Assert.That(result.Exhausted, Is.True); Assert.That(result.Moves, Is.Not.Null); Assert.That(result.Moves.Count, Is.EqualTo(9));
                report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "BFS sample={0}; ms={1:F3}; estimatedAllocatedBytes={2}; explored={3}; expanded={4}; moves={5}; GC0/1/2={6}/{7}/{8}",
                    sample + 1, times[sample], bytes, result.Explored, result.Expanded, result.Moves.Count,
                    GC.CollectionCount(0) - gc0, GC.CollectionCount(1) - gc1, GC.CollectionCount(2) - gc2));
            }
            var sorted = (double[])times.Clone(); Array.Sort(sorted);
            report.AppendLine(string.Format(CultureInfo.InvariantCulture, "BFS min/median/maxMs={0:F3}/{1:F3}/{2:F3}", sorted[0], sorted[1], sorted[2]));
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            long winBefore = allocation.CurrentValue;
            watch.Restart(); var win = new WinSearch(level, initial, 300000);
            double maxStep = 0; int calls = 0;
            while (!win.IsDone)
            {
                stepWatch.Restart(); win.Step(1); stepWatch.Stop();
                if (stepWatch.Elapsed.TotalMilliseconds > maxStep) maxStep = stepWatch.Elapsed.TotalMilliseconds;
                calls++;
            }
            watch.Stop(); long winBytes = allocation.CurrentValue - winBefore;
            Assert.That(win.Win && win.Exhausted, Is.True);
            report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "WinSearch ms={0:F3}; estimatedAllocatedBytes={1}; explored={2}; expanded={3}; Step(1)Calls={4}; maxStepMs={5:F3}",
                watch.Elapsed.TotalMilliseconds, winBytes, win.Explored, win.Expanded, calls, maxStep));
            if (maxStep > 2) report.AppendLine("NOTE: Step(1) >2ms; budget node chưa bảo đảm 2ms, cân nhắc chia tiếp theo từng lệnh sau khi đo trên máy đích.");
            report.AppendLine("EstimatedAllocatedBytes là tổng cấp phát ước lượng của vùng đo, không phải RAM giữ lại; số Editor không thay phép đo Android.");
            string output = report.ToString(); TestContext.WriteLine(output);
            string logs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
            Directory.CreateDirectory(logs);
            File.WriteAllText(Path.Combine(logs, "007-solver-performance.txt"), output, new UTF8Encoding(false));
        }
    }
}
