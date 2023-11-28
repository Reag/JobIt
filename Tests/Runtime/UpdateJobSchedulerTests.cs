using System.Collections;
using JobIt.Runtime.Impl.JobScheduler;
using JobIt.Tests.MockClasses;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JobIt.Tests.Runtime
{
    public class UpdateJobSchedulerTests
    {
        public MockMonoBehaviour GetMockMonoBehaviour()
        {
            var go = new GameObject();
            return go.AddComponent<MockMonoBehaviour>();
        }

        [TearDown]
        public void TearDown()
        {
            UpdateJobScheduler.CleanJobs();
        }

        [UnityTest]
        public IEnumerator Register_UpdateToUpdateJob_NoExceptions()
        {
            //Arrange
            var behaviour = GetMockMonoBehaviour();
            UpdateJobScheduler.Register<MockUpdateToUpdateJob, int>(behaviour, 1);

            //Act
            yield return null;
        }

        [UnityTest]
        public IEnumerator Register_LateUpdateToLateUpdateJob_NoExceptions()
        {
            //Arrange
            var behaviour = GetMockMonoBehaviour();
            UpdateJobScheduler.Register<MockLateUpdateToLateUpdateJob, int>(behaviour, 1);

            //Act
            yield return null;
        }

        [UnityTest]
        public IEnumerator Register_InvokedUpdateJob_InJobDictionary()
        {
            //Arrange
            var behaviour = GetMockMonoBehaviour();
            UpdateJobScheduler.Register<MockUpdateToUpdateJob, int>(behaviour, 1);

            //Act
            var jobObj = UpdateJobScheduler.GetJobObject<MockUpdateToUpdateJob, int>();
            yield return null;

            //Assert
            Assert.IsTrue(jobObj != null, "Could not find job in internal dictionary");
        }

        [UnityTest]
        public IEnumerator Register_NullObj_NoExceptionAndNoJob()
        {
            //Arrange
            UpdateJobScheduler.Register<MockUpdateToUpdateJob, int>(null, 1);

            //Act
            var jobObj = UpdateJobScheduler.GetJobObject<MockUpdateToUpdateJob, int>();
            yield return null;

            //Assert
            Assert.IsTrue(jobObj == null, "Created a job for an invalid element!");
        }

        [UnityTest]
        public IEnumerator Register_DestroyedObj_NoExceptionAndNoJob()
        {
            //Arrange
            var behaviour = GetMockMonoBehaviour();
            Object.Destroy(behaviour);
            yield return null;

            //Act
            UpdateJobScheduler.Register<MockUpdateToUpdateJob, int>(behaviour, 1);
            var jobObj = UpdateJobScheduler.GetJobObject<MockUpdateToUpdateJob, int>();

            //Assert
            Assert.IsTrue(jobObj == null, "Created a job for an invalid element!");
        }

        [UnityTest]
        public IEnumerator Register_MultipleJobs_AllRegistered()
        {
            //Arrange
            var b1 = GetMockMonoBehaviour();
            var b2 = GetMockMonoBehaviour();

            //Act
            UpdateJobScheduler.Register<MockUpdateToUpdateJob, int>(b1, 1);
            UpdateJobScheduler.Register<MockUpdateToUpdateJob, int>(b2, 2);
            yield return null;
            var jobObj = UpdateJobScheduler.GetJobObject<MockUpdateToUpdateJob, int>();

            //Assert
            Assert.IsTrue(jobObj != null, "Failed to create job for valid elements");
            Assert.IsTrue(jobObj.JobSize == 2, $"Did not register all jobs correctly ({jobObj.JobSize} vs 2)");
        }

        [Test]
        public void GetJobObject_InactiveJob_Null()
        {
            //Assert
            Assert.IsTrue(UpdateJobScheduler.GetJobObject<MockUpdateToUpdateJob,int>() == null, "Jobs are leaking from other tests");
        }

        [UnityTest]
        public IEnumerator GetJobCompleteEvent_ValidJob_Invoked()
        {
            //Arrange
            var behaviour = GetMockMonoBehaviour();
            UpdateJobScheduler.Register<MockUpdateToUpdateJob, int>(behaviour, 1);
            bool invoked = false;
            UpdateJobScheduler.GetJobObject<MockUpdateToUpdateJob, int>().OnJobComplete += () => { invoked = true; };

            //Act
            yield return null; //Complete is called, but the job was never run, as we started late
            yield return null; //Now the job has run once

            //Assert
            Assert.IsTrue(invoked, "Job Complete event never fired");
        }
    }
}
