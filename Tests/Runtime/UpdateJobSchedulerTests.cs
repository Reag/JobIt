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
            yield return new WaitForEndOfFrame();

            //Act
            yield return null;
        }

        [UnityTest]
        public IEnumerator Register_LateUpdateToLateUpdateJob_NoExceptions()
        {
            //Arrange
            var behaviour = GetMockMonoBehaviour();
            UpdateJobScheduler.Register<MockLateUpdateToLateUpdateJob, int>(behaviour, 1);
            yield return new WaitForEndOfFrame();

            //Act
            yield return null;
        }

        [UnityTest]
        public IEnumerator Register_InvokedUpdateJob_InJobDictionary()
        {
            //Arrange
            var behaviour = GetMockMonoBehaviour();
            UpdateJobScheduler.Register<MockUpdateToUpdateJob, int>(behaviour, 1);
            yield return new WaitForEndOfFrame();

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
            yield return new WaitForEndOfFrame();

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
            yield return new WaitForEndOfFrame();
            yield return null;
            var jobObj = UpdateJobScheduler.GetJobObject<MockUpdateToUpdateJob, int>();

            //Assert
            Assert.IsTrue(jobObj != null, "Failed to create job for valid elements");
            Assert.IsTrue(jobObj.JobSize == 2, $"Did not register all jobs correctly ({jobObj.JobSize} vs 2)");
        }

        [UnityTest]
        public IEnumerator Register_MultipleJobsOneWithException_ValidJobsRun()
        {
            //Arrange
            var b1 = GetMockMonoBehaviour();
            var b2 = GetMockMonoBehaviour();
            var b3 = GetMockMonoBehaviour();

            //Act
            UpdateJobScheduler.Register<MockUpdateToUpdateJob, int>(b1, 1);
            var jobObj1 = UpdateJobScheduler.GetJobObject<MockUpdateToUpdateJob, int>();
            UpdateJobScheduler.Register<MockExceptionUpdateToUpdateJob, int>(b2, 1);
            var jobObj2 = UpdateJobScheduler.GetJobObject<MockExceptionUpdateToUpdateJob, int>();
            UpdateJobScheduler.Register<MockLateUpdateToLateUpdateJob, int>(b3, 1);
            var jobObj3 = UpdateJobScheduler.GetJobObject<MockLateUpdateToLateUpdateJob, int>();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            //Assert
            Assert.IsTrue(jobObj1.TryReadItem(b1, out var jobData) && jobData != 1, "Valid mock job failed to run because of another invalid job");
            Assert.IsTrue(jobObj3.TryReadItem(b3, out jobData) && jobData != 1, "Valid mock job failed to run because of another invalid job");
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
            yield return new WaitForEndOfFrame();

            //Act
            yield return new WaitForEndOfFrame(); //Now the job has run once

            //Assert
            Assert.IsTrue(invoked, "Job Complete event never fired");
        }

        [UnityTest]
        public IEnumerator Withdraw_ValidJob_Removed()
        {
            //Arrange
            var behaviour = GetMockMonoBehaviour();
            UpdateJobScheduler.Register<MockUpdateToUpdateJob, int>(behaviour, 1);
            yield return new WaitForEndOfFrame();
            var jobObj = UpdateJobScheduler.GetJobObject<MockUpdateToUpdateJob, int>();

            //Act
            yield return new WaitForEndOfFrame(); //Now the job has run once
            UpdateJobScheduler.Withdraw<MockUpdateToUpdateJob, int>(behaviour);
            yield return new WaitForEndOfFrame();

            //Assert
            Assert.IsTrue(jobObj.JobSize == 0, "Job not removed via scheduler");
        }

        [UnityTest]
        public IEnumerator Withdraw_InvalidJob_Warning()
        {
            //Arrange
            var behaviour = GetMockMonoBehaviour();

            //Act
            UpdateJobScheduler.Withdraw<MockUpdateToUpdateJob, int>(behaviour);
            yield return null;

            //Assert
            LogAssert.Expect(LogType.Warning, "Attempted to Withdraw from a non existing job of type JobIt.Tests.MockClasses.MockUpdateToUpdateJob!");
        }

        [UnityTest]
        public IEnumerator UpdateJobData_ValidJob_DataUpdated()
        {
            //Arrange
            var behaviour = GetMockMonoBehaviour();
            UpdateJobScheduler.Register<MockUpdateToUpdateJob, int>(behaviour, 1);
            var jobObj = UpdateJobScheduler.GetJobObject<MockUpdateToUpdateJob, int>();
            yield return new WaitForEndOfFrame(); //Complete is called, but the job was never run, as tests start after the invoker would update

            //Act
            yield return null; //Now the job has run once
            yield return new WaitForEndOfFrame(); //Ensure the job is not running before reading values from it
            bool readData = jobObj.TryReadItem(behaviour, out int jobData);
            UpdateJobScheduler.UpdateJobData<MockUpdateToUpdateJob, int>(behaviour, 0);
            yield return new WaitForEndOfFrame();

            //Assert
            Assert.IsTrue(readData && jobData != 1, "Job failed to run");
            Assert.IsTrue(jobObj.TryReadItem(behaviour, out jobData) && jobData == 0, "UpdateJobData did not update the jobs data");
        }

        [UnityTest]
        public IEnumerator UpdateJobData_InvalidJob_Warning()
        {
            //Arrange
            var behaviour = GetMockMonoBehaviour();

            //Act
            UpdateJobScheduler.UpdateJobData<MockUpdateToUpdateJob, int>(behaviour, 1);
            yield return null;

            //Assert
            LogAssert.Expect(LogType.Warning, "Attempted to UpdateJobData for a non existing job of type JobIt.Tests.MockClasses.MockUpdateToUpdateJob!");
        }

        [UnityTest]
        public IEnumerator UpdateJobData_NullData_Nothing()
        {
            //Act
            UpdateJobScheduler.UpdateJobData<MockUpdateToUpdateJob, int>(null, 1);
            var jobObj = UpdateJobScheduler.GetJobObject<MockUpdateToUpdateJob, int>();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            //Assert
            LogAssert.NoUnexpectedReceived();
            Assert.IsTrue(jobObj == null, "Made a job object for an invalid element");
        }

        [UnityTest]
        public IEnumerator TryReadJobData_WhileRunning_Warning()
        {
            //Arrange
            var behaviour = GetMockMonoBehaviour();

            //Act
            UpdateJobScheduler.Register<MockUpdateToUpdateJob, int>(behaviour, 1);
            yield return new WaitForEndOfFrame();
            yield return null; //Job is currently running at this timing
            var readData = UpdateJobScheduler.TryReadJobData<MockUpdateToUpdateJob, int>(behaviour, out var data);

            //Assert
            Assert.IsFalse(readData, "Managed to read data on a running job, which is wrong");
            LogAssert.Expect(LogType.Warning, "Job is currently running, readback is disabled! Consider adjusting your timing");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TryReadJobData_NotRunning_GotData()
        {
            //Arrange
            var behaviour = GetMockMonoBehaviour();

            //Act
            UpdateJobScheduler.Register<MockUpdateToUpdateJob, int>(behaviour, 1);
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame(); //Job is not running
            var readData = UpdateJobScheduler.TryReadJobData<MockUpdateToUpdateJob, int>(behaviour, out var data);

            //Assert
            Assert.IsTrue(readData, "Could not read data on a non running job");
            Assert.IsTrue(data != 1, "Job did not run");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
