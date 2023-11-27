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
    }
}
