using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Jobs;
using Debug = UnityEngine.Debug;

static class JobExtensions
{
    static readonly HashSet<Type> JobsRun = new();

    public static void RunTimed<T>(this T job, string msg = null, bool silent = false) where T : struct, IJob
    {
        var sw = new Stopwatch();
        sw.Start();
        job.Run();

        var time = sw.Elapsed.TotalMilliseconds;
        var type = job.GetType();

        if (silent)
        {
            JobsRun.Add(type);
            return;
        }

        if (string.IsNullOrWhiteSpace(msg))
            msg = type.Name;
        if (time <= 1000)
        {
            Debug.Log(JobsRun.Add(type)
                ? $"FIRSTRUN: {msg} took {time:N4} ms"
                : $"{msg} took {time:N4} ms");
        }
        else
        {
            time /= 1000;
            Debug.Log(JobsRun.Add(type)
                ? $"FIRSTRUN: {msg} took {time:N2} s"
                : $"{msg} took {time:N2} s");
        }
    }
}
