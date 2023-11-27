using JobIt.Runtime.Abstract;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Serialization;

namespace JobIt.Tests.MockClasses
{
    public class MockUpdateJob : UpdateJob<int>
    {
        public NativeList<int> ValueList;
        public JobHandle Handle;

        public override void Awake()
        {
            base.Awake();
            ValueList = new NativeList<int>(Allocator.Persistent);
        }
        protected override int JobPriority => 0;

        protected override void DisposeLogic()
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

        protected override void UpdateJobData(int index, int data)
        {
            ValueList[index] = data;
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
            AddCompleter<MockJobScheduleCompleter>();
            mockCompleter = GetComponent<MockJobScheduleCompleter>();
        }

        public void DeactivateCompleter()
        {
            DestroyImmediate(mockCompleter);
        }
    }
}