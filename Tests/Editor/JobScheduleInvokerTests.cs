using System.Collections;
using System.Collections.Generic;
using JobIt.Runtime.Abstract;
using JobIt.Tests.MockClasses;
using NUnit.Framework;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;

namespace JobIt.Tests.Editor
{
    public class JobScheduleInvokerTests
    {
        private GameObject _mockObject;
        private MockJobScheduleInvoker _invoker;
        private readonly List<IUpdateJob> _mockJobs = new();

        [SetUp]
        public void Setup()
        {
            _mockObject = new GameObject();
            _invoker = _mockObject.AddComponent<MockJobScheduleInvoker>();
            _invoker.SetupCompleter();
        }

        public MockUpdateJob CreateMockJob()
        {
            var newJob = _mockObject.AddComponent<MockUpdateJob>();
            newJob.Awake();
            _mockJobs.Add(newJob);
            return newJob;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_mockObject);
            Object.DestroyImmediate(_invoker);
            foreach (var job in _mockJobs)
            {
                job.Dispose();
            }
            _mockJobs.Clear();
        }

        [Test]
        public void Awake_CreateAndDestroy_Correct()
        {
            //Assert
            Assert.IsTrue(_invoker != null, "Failed to create Invoker!");
        }

        [Test]
        public void AddCompleter_Invoked_Correct()
        {
            //Assert
            Assert.IsTrue(_invoker.mockCompleter != null, "Failed to attach completer");
        }

        [Test]
        public void RegisterJob_ValidJob_Added()
        {
            //Arrange
            var job = CreateMockJob();

            //Act
            _invoker.RegisterJob(job);

            //Assert
            Assert.IsTrue(_invoker.MockJobListCount == 1, "Job not added to the Invoker!");
        }

        [Test]
        public void RegisterJob_OrderedJobs_Sorted()
        {
            //Arrange
            var job1 = CreateMockJob();
            var job2 = CreateMockJob();
            var job3 = CreateMockJob();

            //Act
            _invoker.RegisterJob(job1, 0);
            _invoker.RegisterJob(job2, 100);
            _invoker.RegisterJob(job3, 1);

            //Assert
            Assert.IsTrue(_invoker.MockJobListCount == 3, "Not all jobs added to the Invoker!");
            var jobs = _invoker.GetJobList();
            Assert.IsTrue((MockUpdateJob)jobs[0] == job1, "Jobs not ordered by priority correctly");
            Assert.IsTrue((MockUpdateJob)jobs[1] == job3, "Jobs not ordered by priority correctly");
            Assert.IsTrue((MockUpdateJob)jobs[2] == job2, "Jobs not ordered by priority correctly");
        }

        [Test]
        public void WithdrawJob_ValidJob_Removed()
        {
            //Arrange
            var job1 = CreateMockJob();
            var job2 = CreateMockJob();
            var job3 = CreateMockJob();
            _invoker.RegisterJob(job1);
            _invoker.RegisterJob(job2);
            _invoker.RegisterJob(job3);

            //Act
            _invoker.WithdrawJob(job1);
            _invoker.WithdrawJob(job2);
            _invoker.WithdrawJob(job3);

            //Assert
            Assert.IsTrue(_invoker.MockJobListCount == 0, "Failed to withdraw jobs");
        }

        [Test]
        public void RunJobs_ValidJobs_Executed()
        {
            //Arrange
            var job1 = CreateMockJob();
            job1.RegisterItem(_invoker, 0);
            var job2 = CreateMockJob();
            job2.RegisterItem(_invoker, 1);
            var job3 = CreateMockJob();
            job3.RegisterItem(_invoker, 3);

            //Act
            _invoker.RegisterJob(job1, 0);
            _invoker.RegisterJob(job2, 100);
            _invoker.RegisterJob(job3, 1);
            _invoker.MockStartJob();
            _invoker.mockCompleter.MockCompleteJob();

            //Assert
            Assert.IsTrue(_invoker.MockJobListCount == 3, "Not all jobs added to the Invoker!");
            var jobs = _invoker.GetJobList();
            Assert.IsTrue(job1.TryReadItem(_invoker, out int v) && v == 0, "Job1 did not execute correctly");
            Assert.IsTrue(job2.TryReadItem(_invoker, out v) && v == 2, "Job1 did not execute correctly");
            Assert.IsTrue(job3.TryReadItem(_invoker, out v) && v == 6, "Job1 did not execute correctly");
        }

        [Test]
        public void Instance_GetSingleton_Ok()
        {
            //Assert
            Assert.IsTrue(MockJobScheduleInvoker.Instance != null);
        }

        [Test]
        public void Instance_CreateSingleton_Ok()
        {
            //Act
            Object.DestroyImmediate(_invoker);
            _invoker = MockJobScheduleInvoker.Instance;

            //Assert
            Assert.IsTrue(MockJobScheduleInvoker.Instance != null);
        }
    }
}
