using System;
using JobIt.Tests.MockClasses;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace JobIt.Tests.Editor
{
    public class UpdateJobTests
    {
        private MockUpdateJob _job;
        private GameObject _mockObject;

        [SetUp]
        public void Setup()
        {
            _mockObject = new GameObject();
            _job = _mockObject.AddComponent<MockUpdateJob>();
            _job.Awake();
        }

        [TearDown]
        public void TearDown()
        {
            _job.Dispose();
            Object.DestroyImmediate(_mockObject);
        }

        [Test]
        public void Awake_CreateAndDestroy_Correct()
        {
            //Assert
            Assert.IsTrue(_job != null, "Failed to create job!");
        }

        [Test]
        public void GetGameObject_NotNull()
        {
            //Assert
            Assert.IsTrue(_job.GetGameObject() != null, "Game object associated with job is null!");
        }

        [Test]
        public void RegisterItem_IsValidData_Registered()
        {
            //Arrange
            var owner = _mockObject.AddComponent<MockMonoBehaviour>();

            //Act
            _job.RegisterItem(owner, 1);
            _job.StartJob();
            _job.EndJob();

            //Assert
            Assert.IsTrue(_job.JobSize == 1);
        }

        [Test]
        public void RegisterItem_ItemAlreadyRegistered_Updated()
        {
            //Arrange
            var owner = _mockObject.AddComponent<MockMonoBehaviour>();

            //Act
            _job.RegisterItem(owner, 0);
            _job.RegisterItem(owner, 5);
            _job.StartJob();
            _job.EndJob();

            //Assert
            Assert.IsTrue(_job.JobSize == 1);
            Assert.IsTrue(_job.ValueList[0] != 0, "RegisterItem on an already added item did not update its value");
        }

        [Test]
        public void RegisterItem_IsInvalidData_Exception()
        {
            //Assert
            Assert.Throws<NullReferenceException>(() => {
                //Act
                _job.RegisterItem(null, 1);
                _job.StartJob();
                _job.EndJob();
            });
        }

        [Test]
        public void RegisterItem_ItemDestroyedBeforeJob_RemovedFromJob()
        {
            //Arrange
            var owner = _mockObject.AddComponent<MockMonoBehaviour>();

            //Act
            _job.RegisterItem(owner, 1);
            Object.DestroyImmediate(owner);
            _job.StartJob();
            _job.EndJob();

            //Assert
            Assert.IsTrue(_job.JobSize == 0);
        }

        [Test]
        public void WithdrawItem_IsValidData_NotInJob()
        {
            //Arrange
            var owner = _mockObject.AddComponent<MockMonoBehaviour>();

            //Act
            _job.RegisterItem(owner, 1);
            _job.StartJob();
            _job.EndJob();
            _job.WithdrawItem(owner);
            _job.StartJob();
            _job.EndJob();

            //Assert
            Assert.IsTrue(_job.JobSize == 0, "Failed to remove element from job");
        }

        [Test]
        public void WithdrawItem_InvalidData_InJob()
        {
            //Arrange
            var owner = _mockObject.AddComponent<MockMonoBehaviour>();
            var notTheOwner = _mockObject.AddComponent<MockMonoBehaviour>();

            //Act
            _job.RegisterItem(owner, 1);
            _job.StartJob();
            _job.EndJob();
            _job.WithdrawItem(notTheOwner);
            _job.StartJob();
            _job.EndJob();

            //Assert
            LogAssert.Expect(LogType.Warning, @"Attempted to remove an non existing job element from JobIt.Tests.MockClasses.MockUpdateJob");
            Assert.IsTrue(_job.JobSize == 1, "Wrong element removed from job");
        }

        [Test]
        public void TryReadItem_ValidData_CorrectData()
        {
            //Arrange
            var owner = _mockObject.AddComponent<MockMonoBehaviour>();

            //Act
            _job.RegisterItem(owner, 1);
            _job.StartJob();
            _job.EndJob();
            var success = _job.TryReadItem(owner, out var data);

            //Assert
            LogAssert.NoUnexpectedReceived(); //No warnings
            Assert.IsTrue(success, "TryReadItem failed to read data");
            Assert.IsTrue(data != default, "Default data returned!");
        }

        [Test]
        public void TryReadItem_InvalidData_Default()
        {
            //Arrange
            var owner = _mockObject.AddComponent<MockMonoBehaviour>();
            var notTheOwner = _mockObject.AddComponent<MockMonoBehaviour>();

            //Act
            _job.RegisterItem(owner, 1);
            _job.StartJob();
            _job.EndJob();
            var success = _job.TryReadItem(notTheOwner, out var data);

            //Assert
            LogAssert.NoUnexpectedReceived(); //No warnings
            Assert.IsFalse(success, "TryReadItem failed to read data");
            Assert.IsTrue(data == default, "TryReadItem leaked valid data for an invalid request!");
        }

        [Test]
        public void TryReadItem_CalledDuringRun_False()
        {
            //Arrange
            var owner = _mockObject.AddComponent<MockMonoBehaviour>();

            //Act
            _job.RegisterItem(owner, 1);
            _job.StartJob();
            var success = _job.TryReadItem(owner, out var data);
            _job.EndJob();

            //Assert
            Assert.IsFalse(success, "TryReadItem read data on a running job");
            Assert.IsTrue(data == default, "TryReadItem leaked valid data for an invalid request!");
        }

        [Test]
        public void UpdateItem_ValidData_DataUpdated()
        {
            //Arrange
            var owner = _mockObject.AddComponent<MockMonoBehaviour>();

            //Act
            _job.RegisterItem(owner, 1);
            _job.StartJob();
            _job.EndJob();
            _job.UpdateItem(owner, -1);
            _job.StartJob();
            _job.EndJob();

            //Assert
            Assert.IsTrue(_job.ValueList[0] == -2, "Job Data was not updated");
        }

        [Test]
        public void UpdateItem_InvalidData_DataNotUpdated()
        {
            //Arrange
            var owner = _mockObject.AddComponent<MockMonoBehaviour>();
            var notTheOwner = _mockObject.AddComponent<MockMonoBehaviour>();

            //Act
            _job.RegisterItem(owner, 1);
            _job.StartJob();
            _job.EndJob();
            _job.UpdateItem(notTheOwner, -1);
            _job.StartJob();
            _job.EndJob();

            //Assert
            Assert.IsTrue(_job.ValueList[0] != -2, "UpdateItem allowed a non owner to update a value");
        }

        [Test]
        public void ProcessQueue_DestroyedItem_ItemRemoved()
        {
            //Arrange
            var owner = _mockObject.AddComponent<MockMonoBehaviour>();

            //Act
            _job.RegisterItem(owner, 1);
            _job.StartJob();
            _job.EndJob();
            Object.DestroyImmediate(owner);
            _job.UpdateItem(owner, -1);
            _job.StartJob();
            _job.EndJob();

            //Assert
            Assert.IsTrue(_job.JobSize == 0, "Failed to remove a destroyed object when an update was requested");
        }

        [Test]
        public void WithdrawItem_LotsOfItemsAddedAndRemove_OrderPreserved()
        {
            //Arrange
            var ownerA = _mockObject.AddComponent<MockMonoBehaviour>();
            var ownerB = _mockObject.AddComponent<MockMonoBehaviour>();
            var ownerC = _mockObject.AddComponent<MockMonoBehaviour>();
            var ownerD = _mockObject.AddComponent<MockMonoBehaviour>();
            var ownerE = _mockObject.AddComponent<MockMonoBehaviour>();

            //Act
            //i => Index held during this operation
            //o => owner of said index
            _job.RegisterItem(ownerA, 1); //o=A i=0
            _job.RegisterItem(ownerB, 2); //o=B i=1
            _job.StartJob();
            _job.EndJob();
            _job.RegisterItem(ownerC, 3); //o=C i=2
            _job.WithdrawItem(ownerA); //o=C i=0, list size--
            _job.WithdrawItem(ownerB); //list size --
            _job.RegisterItem(ownerD,4); //o=D i=1
            _job.RegisterItem(ownerE, 5); //o=E i=2
            _job.StartJob();
            _job.EndJob();

            //Assert
            LogAssert.NoUnexpectedReceived();
            Assert.IsTrue(_job.JobSize == 3, "Incorrect number of jobs tracked");
            Assert.IsTrue(_job.TryReadItem(ownerC, out var cVal), "ownerC was missing from the ownerList!");
            Assert.IsTrue(_job.TryReadItem(ownerD, out var dVal), "ownerD was missing from the ownerList!");
            Assert.IsTrue(_job.TryReadItem(ownerE, out var eVal), "ownerE was missing from the ownerList!");
            Assert.IsTrue(_job.ValueList[0] == cVal, "Job at index 0 was not owned by ownerC");
            Assert.IsTrue(_job.ValueList[1] == dVal, "Job at index 1 was not owned by ownerD");
            Assert.IsTrue(_job.ValueList[2] == eVal, "Job at index 2 was not owned by ownerE");
        }
    }
}
