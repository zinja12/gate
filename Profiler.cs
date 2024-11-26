using System;
using System.Collections.Generic;
using System.Diagnostics;

public class Profiler
{
    private Stopwatch stopwatch;
    private Dictionary<string, long> timings;
    private List<string> logOrder;

    private bool active;

    public Profiler(bool active) {
        this.active = active;

        stopwatch = new Stopwatch();
        timings = new Dictionary<string, long>();
        logOrder = new List<string>();
    }

    //starts timing a specific section
    public void start(string sectionName) {
        if (!active) return;

        if (timings.ContainsKey(sectionName)) {
            throw new InvalidOperationException($"Profiler section '{sectionName}' is already being tracked.");
        }
        stopwatch.Restart();
        logOrder.Add(sectionName);
    }

    //ends timing the specified section and logs the elapsed time
    public void end(string sectionName) {
        if (!active) return;

        if (!timings.ContainsKey(sectionName) && !logOrder.Contains(sectionName)) {
            throw new InvalidOperationException($"Profiler section '{sectionName}' was not started.");
        }
        stopwatch.Stop();
        timings[sectionName] = stopwatch.ElapsedTicks;
    }

    //logs the total time taken by all sections in the frame
    public void print_frame_summary() {
        if (!active) return;

        Console.WriteLine("---- Frame Profile Summary ----");
        double total_elapsed = 0;
        foreach (var section in logOrder) {
            if (timings.TryGetValue(section, out long elapsed)) {
                Console.WriteLine($"{section}: {elapsed_milliseconds(elapsed)} ms");
                total_elapsed += elapsed;
            }
        }
        Console.WriteLine($"TOTAL_ELAPSED:{elapsed_milliseconds((long)total_elapsed)} ms");
        total_elapsed = 0;
        Console.WriteLine("-------------------------------");
        clear();
    }

    //converts Stopwatch ticks to milliseconds
    private double elapsed_milliseconds(long ticks) {
        return ticks / (double)Stopwatch.Frequency * 1000.0;
    }

    //clears profiler for next frame
    private void clear() {
        if (!active) return;

        timings.Clear();
        logOrder.Clear();
    }
}