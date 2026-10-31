// SecondWave.Vectors — dumps conformance/vectors.json FROM the C#
// core for a stateless scripted input pattern. The JS realization is
// checked against these traces exactly (tools/conformance.mjs).
// The script pattern is a pure function of the step index and is
// re-implemented identically in the JS checker.
using System;
using System.IO;
using System.Text;
using NTRanch.SecondWave;

internal static class Program
{
    // Stateless script: confirm out of the gate and title, then a
    // repeating hold/switch/release pattern; confirm pulses land inside
    // peak windows and take the continuation path. STOP is never sent.
    public static int ScriptBits(int step)
    {
        if (step == 0 || step == 1) return Input.Confirm;
        int cycle = step % 240;
        int bits = 0;
        if (cycle < 150) bits |= Input.Tap;
        if (cycle == 150) bits |= Input.Down;
        if (step % 50 == 0) bits |= Input.Confirm;
        return bits;
    }

    private static int Main(string[] args)
    {
        string outPath = args.Length > 0 ? args[0] : "conformance/vectors.json";
        var sb = new StringBuilder();
        sb.Append("{\n  \"seed\": 42,\n  \"levels\": [");
        for (int level = 0; level < 3; level++)
        {
            var core = Core.Create(42, level, false);
            var trace = new StringBuilder();
            const int totalSteps = 6000;
            for (int step = 0; step < totalSteps; step++)
            {
                core.Step(ScriptBits(step));
                if (step % 20 == 0 || step == totalSteps - 1)
                {
                    if (trace.Length > 0) trace.Append(",\n");
                    trace.Append(string.Create(System.Globalization.CultureInfo.InvariantCulture,
                        $"[{step},{core.Screen},{core.Scene},{core.Phase},{core.Depth}," +
                        $"{core.Channel(0)},{core.Channel(1)},{core.Channel(2)}," +
                        $"{core.Sensitivity},{core.ContinuationsTotal},{core.HoldMs},{core.Score}]"));
                }
            }
            if (level > 0) sb.Append(",");
            sb.Append($"\n    {{\"level\": {level}, \"trace\": [\n{trace}\n    ]}}");
        }
        sb.Append("\n  ]\n}\n");
        File.WriteAllText(outPath, sb.ToString());
        Console.WriteLine($"vectors written to {outPath}");
        return 0;
    }
}
