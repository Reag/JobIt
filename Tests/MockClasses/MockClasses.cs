using System.Collections.Generic;
using System.Linq;
using JobIt.Runtime.Abstract;
using JobIt.Runtime.Impl.JobScheduler;
using JobIt.Runtime.Utils;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace JobIt.Tests.MockClasses
{
    public class MockUpdateJob : UpdateJob<int>
    {
        public NativeList<int> ValueList;
        public JobHandle Handle;

        protected override void BuildNativeContainers()
        {
            ValueList = new NativeList<int>(Allocator.Persistent);
        }

        /// <inheritdoc />
        protected override void DisposeNativeContainers()
        {
            ValueList.Dispose();
        }

        protected override void AddJobData(int data)
        {
            ValueList.Add(data);
        }

        protected override int ReadJobDataAtIndex(int index)
        {
            return ValueList[index];
        }

        protected override void RemoveJobDataAndSwapBack(int index)
        {
            ValueList.RemoveAtSwapBack(index);
        }

        protected override JobHandle ScheduleJob(JobHandle dependsOn = default)
        {
            var job = new DoubleValue
            {
                Values = ValueList.AsArray()
            };
            Handle = job.Schedule(ValueList.Length, 1, dependsOn);
            return Handle;
        }

        protected override void CompleteJob()
        {
            Handle.Complete();
        }

        public struct DoubleValue : IJobParallelFor
        {
            public NativeArray<int> Values;
            public void Execute(int index)
            {
                Values[index] *= 2;
            }
        }
    }

    public class MockInvokedJob<T> : InvokedUpdateJob<T ,int> where T : JobScheduleInvoker<T>, IJobInvoker
    {
        public NativeList<int> ValueList;

        protected override void BuildNativeContainers()
        {
            ValueList = new NativeList<int>(Allocator.Persistent);
        }

        /// <inheritdoc />
        protected override void DisposeNativeContainers()
        {
            ValueList.Dispose();
        }

        protected override int JobPriority => 0;

        protected override void AddJobData(int data)
        {
            ValueList.Add(data);
        }

        protected override int ReadJobDataAtIndex(int index)
        {
            return ValueList[index];
        }

        protected override void RemoveJobDataAndSwapBack(int index)
        {
            ValueList.RemoveAtSwapBack(index);
        }

        protected override JobHandle ScheduleJob(JobHandle dependsOn = default)
        {
            var job = new DoubleValue
            {
                Values = ValueList.AsArray()
            };
            return job.Schedule(ValueList.Length, 1, dependsOn);
        }

        public struct DoubleValue : IJobParallelFor
        {
            public NativeArray<int> Values;
            public void Execute(int index)
            {
                Values[index] *= 2;
            }
        }
    }

    public class MockUpdateToUpdateJob : MockInvokedJob<UpdateJobInvoker>
    {

    }

    public class MockExceptionUpdateToUpdateJob : UpdateToUpdateJob<int>
    {
        protected override void BuildNativeContainers()
        {

        }

        /// <inheritdoc />
        protected override void DisposeNativeContainers()
        {
            
        }

        protected override void AddJobData(int data)
        {
            
        }

        protected override int ReadJobDataAtIndex(int index)
        {
            return 0;
        }

        protected override void RemoveJobDataAndSwapBack(int index)
        {
            
        }

        protected override JobHandle ScheduleJob(JobHandle dependsOn = default)
        {
            throw new System.NotImplementedException();
        }
    }

    public class MockLateUpdateToLateUpdateJob : MockInvokedJob<LateUpdateJobInvoker>
    {

    }

    public class MockMonoBehaviour : MonoBehaviour
    {

    }

    public class MockJobScheduleCompleter : JobScheduleCompleter
    {
        public void MockCompleteJob()
        {
            CompleteJob();
        }
    }

    public class MockJobScheduleInvoker : JobScheduleInvoker<MockJobScheduleInvoker>, IJobInvoker
    {
        public int MockJobListCount => JobList.Count;
        public MockJobScheduleCompleter mockCompleter;

        public void MockStartJob()
        {
            RunJobs();
        }

        public void SetupCompleter()
        {
            base.Awake();
            mockCompleter = AddCompleter<MockJobScheduleCompleter>();
        }

        public void DeactivateCompleter()
        {
            DestroyImmediate(mockCompleter);
        }

        public List<IUpdateJob> GetJobList()
        {
            return JobList.Select(x => x.Job).ToList();
        }
    }

    /// <summary>
    /// No-op job whose DisposeNativeContainers throws, to exercise the Dispose catch block.
    /// Records whether DisposeLogic still ran afterward, so tests can verify Dispose continues
    /// past the exception (catch isolation).
    /// </summary>
    public class MockThrowOnDisposeContainersJob : UpdateJob<int>
    {
        public bool DisposeLogicRan;

        protected override void BuildNativeContainers() { }
        protected override void DisposeNativeContainers() =>
            throw new System.Exception("boom: dispose containers");
        protected override void DisposeLogic() => DisposeLogicRan = true;
        protected override void AddJobData(int data) { }
        protected override int ReadJobDataAtIndex(int index) => 0;
        protected override void RemoveJobDataAndSwapBack(int index) { }
        protected override JobHandle ScheduleJob(JobHandle dependsOn = default) => dependsOn;
        protected override void CompleteJob() { }
    }

    /// <summary>
    /// No-op job whose DisposeLogic throws, to exercise the Dispose catch block.
    /// Records whether DisposeNativeContainers ran first, so tests can verify the earlier
    /// dispose steps completed before the throwing one.
    /// </summary>
    public class MockThrowOnDisposeLogicJob : UpdateJob<int>
    {
        public bool DisposeContainersRan;

        protected override void BuildNativeContainers() { }
        protected override void DisposeNativeContainers() => DisposeContainersRan = true;
        protected override void DisposeLogic() =>
            throw new System.Exception("boom: dispose logic");
        protected override void AddJobData(int data) { }
        protected override int ReadJobDataAtIndex(int index) => 0;
        protected override void RemoveJobDataAndSwapBack(int index) { }
        protected override JobHandle ScheduleJob(JobHandle dependsOn = default) => dependsOn;
        protected override void CompleteJob() { }
    }

    /// <summary>
    /// JobWrapperBase subclass that overrides the internals to no-ops, for isolated state-machine tests.
    /// </summary>
    public class MockIsolatedJobWrapper : JobWrapperBase<MockUpdateJob, int>
    {
        public int RegisterCount;
        public int UpdateCount;
        public int WithdrawCount;

        public MockIsolatedJobWrapper(Component owner, int data) : base(owner, data) { }

        public bool IsRegisteredPublic => isRegistered;
        public bool DisposedPublic => disposed;
        public int CurrentDataPublic => currentData;

        protected override void RegisterToJobInternal(int data) => RegisterCount++;
        protected override void UpdateJobInternal(int data) => UpdateCount++;
        protected override void WithdrawFromJobInternal() => WithdrawCount++;
    }

    /// <summary>
    /// JobWrapperBase subclass that keeps the real scheduler-calling internals, for integration tests.
    /// </summary>
    public class MockIntegrationJobWrapper : JobWrapperBase<MockUpdateToUpdateJob, int>
    {
        public MockIntegrationJobWrapper(Component owner, int data) : base(owner, data) { }
        // Intentionally does NOT override the *Internal methods, so the real
        // UpdateJobScheduler calls execute (covering the base virtual bodies).
    }
}