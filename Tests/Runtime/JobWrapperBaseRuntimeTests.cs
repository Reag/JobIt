using System.Collections;
using JobIt.Runtime.Impl.JobScheduler;
using JobIt.Tests.MockClasses;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JobIt.Tests.Runtime
{
    public class JobWrapperBaseRuntimeTests
    {
        private MockMonoBehaviour GetMockMonoBehaviour()
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
        public IEnumerator Register_ThroughWrapper_JobCreatedAndRegistered()
        {
            //Arrange
            var owner = GetMockMonoBehaviour();

            //Act
            var wrapper = new MockIntegrationJobWrapper(owner, 1); // RegisterToJobInternal -> scheduler
            yield return new WaitForEndOfFrame();
            yield return null; // let the invoker's Update process the queued Add
            var jobObj = UpdateJobScheduler.GetJobObject<MockUpdateToUpdateJob, int>();

            //Assert
            Assert.IsTrue(jobObj != null, "Wrapper did not register a job through the scheduler");
            Assert.IsTrue(jobObj.JobSize == 1, "Owner not registered to the job");

            wrapper.Dispose();
        }

        [UnityTest]
        public IEnumerator UpdateJobData_ThroughWrapper_DataUpdated()
        {
            //Arrange
            var owner = GetMockMonoBehaviour();
            var wrapper = new MockIntegrationJobWrapper(owner, 1);
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame(); // job has run; safe to read

            //Act
            wrapper.UpdateJobData(0); // UpdateJobInternal -> scheduler
            yield return new WaitForEndOfFrame();

            //Assert
            var jobObj = UpdateJobScheduler.GetJobObject<MockUpdateToUpdateJob, int>();
            Assert.IsTrue(jobObj != null, "Job missing after update");
            Assert.IsTrue(jobObj.TryReadItem(owner, out int data) && data == 0,
                "UpdateJobData through wrapper did not update the job data");

            wrapper.Dispose();
        }

        [UnityTest]
        public IEnumerator Dispose_ThroughWrapper_WithdrawsFromJob()
        {
            //Arrange
            var owner = GetMockMonoBehaviour();
            var wrapper = new MockIntegrationJobWrapper(owner, 1);
            yield return new WaitForEndOfFrame();
            yield return null; // let the invoker register the owner
            var jobObj = UpdateJobScheduler.GetJobObject<MockUpdateToUpdateJob, int>();
            Assert.IsTrue(jobObj != null && jobObj.JobSize == 1, "Setup failed: owner was not registered before dispose");

            //Act
            wrapper.Dispose(); // WithdrawFromJobInternal -> scheduler
            yield return new WaitForEndOfFrame();
            yield return null; // let the invoker process the queued Remove

            //Assert
            Assert.IsTrue(jobObj.JobSize == 0, "Wrapper Dispose did not withdraw the owner from the job");
        }
    }
}
