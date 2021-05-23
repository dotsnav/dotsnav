using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace DotsNav
{
    [JobProducerType(typeof(JobParallelExtensions.JobTimeSliceStruct<>))]
    public interface IJobTimeSlice
    {
        int TotalItems { get; }
        int Chunks { get; }
        int ChunkBudget { get; }
        int Execute(int index, int budget, int jobIndex);
    }

    public static class JobParallelExtensions
    {
        public static unsafe JobHandle Schedule<T>(this T jobData, JobHandle dependency = default) where T : struct, IJobTimeSlice
        {
            var parameters = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), JobTimeSliceStruct<T>.Initialize(), dependency, ScheduleMode.Parallel);
            var amount = Amount(jobData);
            return JobsUtility.ScheduleParallelFor(ref parameters, amount, amount);
        }

        public static unsafe JobHandle ScheduleParallel<T>(this T jobData, int batchSize = 1, JobHandle dependency = default) where T : struct, IJobTimeSlice
        {
            var parameters = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), JobTimeSliceStruct<T>.Initialize(), dependency, ScheduleMode.Parallel);
            var amount = Amount(jobData);
            return JobsUtility.ScheduleParallelFor(ref parameters, amount, batchSize);
        }

        public static unsafe void Run<T>(this T jobData, JobHandle dependency = default) where T : struct, IJobTimeSlice
        {
            var parameters = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), JobTimeSliceStruct<T>.Initialize(), dependency, ScheduleMode.Run);
            var amount = Amount(jobData);
            JobsUtility.ScheduleParallelFor(ref parameters, amount, amount);
        }

        static int Amount<T>(T jobData) where T : struct, IJobTimeSlice => math.min(jobData.TotalItems, jobData.Chunks);

        [StructLayout(LayoutKind.Sequential, Size = 1)]
        internal struct JobTimeSliceStruct<T> where T : struct, IJobTimeSlice
        {
            static System.IntPtr jobReflectionData;

            public static System.IntPtr Initialize()
            {
                if (jobReflectionData == System.IntPtr.Zero)
                    { jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(T), new ExecuteJobFunction(Execute)); }
                return jobReflectionData;
            }

            public static unsafe void Execute(ref T jobData, System.IntPtr additionalPtr, System.IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out var beginIndex, out var endIndex))
                {
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), beginIndex, endIndex - beginIndex);

                    for (int i = beginIndex; i < endIndex; i++)
                    {
                        var index = i;
                        var budget = jobData.ChunkBudget;

                        while (budget > 0 && index < jobData.TotalItems)
                        {
                            budget -= jobData.Execute(index, budget, jobIndex);
                            index += jobData.Chunks;
                        }
                    }
                }
            }

            delegate void ExecuteJobFunction(ref T data, System.IntPtr additionalPtr, System.IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
        }
    }
}