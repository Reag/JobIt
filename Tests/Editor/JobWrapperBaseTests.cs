using JobIt.Tests.MockClasses;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JobIt.Tests.Editor
{
    public class JobWrapperBaseTests
    {
        private GameObject _mockObject;
        private MockMonoBehaviour _owner;

        [SetUp]
        public void Setup()
        {
            _mockObject = new GameObject();
            _owner = _mockObject.AddComponent<MockMonoBehaviour>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_mockObject);
        }

        [Test]
        public void Constructor_RegistersAndStoresData()
        {
            //Act
            var wrapper = new MockIsolatedJobWrapper(_owner, 5);

            //Assert
            Assert.IsTrue(wrapper.IsRegisteredPublic, "Wrapper did not register on construction");
            Assert.IsTrue(wrapper.RegisterCount == 1, "RegisterToJobInternal not called exactly once");
            Assert.IsTrue(wrapper.CurrentDataPublic == 5, "Constructor did not store initial data");
        }

        [Test]
        public void RegisterToJob_WhenAlreadyRegistered_NoOp()
        {
            //Arrange
            var wrapper = new MockIsolatedJobWrapper(_owner, 5); // RegisterCount == 1

            //Act
            wrapper.RegisterToJob(); // parameterless overload, uses currentData

            //Assert
            Assert.IsTrue(wrapper.RegisterCount == 1, "RegisterToJob ran again while already registered");
        }

        [Test]
        public void UpdateJobData_WhenNotDisposed_UpdatesDataAndCallsInternal()
        {
            //Arrange
            var wrapper = new MockIsolatedJobWrapper(_owner, 5);

            //Act
            wrapper.UpdateJobData(9);

            //Assert
            Assert.IsTrue(wrapper.CurrentDataPublic == 9, "UpdateJobData did not store new data");
            Assert.IsTrue(wrapper.UpdateCount == 1, "UpdateJobInternal not called");
        }

        [Test]
        public void WithdrawFromJob_WhenRegistered_WithdrawsAndClearsFlag()
        {
            //Arrange
            var wrapper = new MockIsolatedJobWrapper(_owner, 5);

            //Act
            wrapper.WithdrawFromJob();

            //Assert
            Assert.IsFalse(wrapper.IsRegisteredPublic, "Wrapper still marked registered after withdraw");
            Assert.IsTrue(wrapper.WithdrawCount == 1, "WithdrawFromJobInternal not called");
        }

        [Test]
        public void WithdrawFromJob_WhenNotRegistered_NoOp()
        {
            //Arrange
            var wrapper = new MockIsolatedJobWrapper(_owner, 5);
            wrapper.WithdrawFromJob(); // now not registered

            //Act
            wrapper.WithdrawFromJob(); // second call should be a no-op

            //Assert
            Assert.IsTrue(wrapper.WithdrawCount == 1, "WithdrawFromJobInternal ran while not registered");
        }

        [Test]
        public void Dispose_WithdrawsAndMarksDisposed()
        {
            //Arrange
            var wrapper = new MockIsolatedJobWrapper(_owner, 5);

            //Act
            wrapper.Dispose();

            //Assert
            Assert.IsTrue(wrapper.DisposedPublic, "Wrapper not marked disposed");
            Assert.IsFalse(wrapper.IsRegisteredPublic, "Wrapper not withdrawn on dispose");
            Assert.IsTrue(wrapper.WithdrawCount == 1, "Dispose did not withdraw from job");
        }

        [Test]
        public void UpdateJobData_AfterDispose_NoOp()
        {
            //Arrange
            var wrapper = new MockIsolatedJobWrapper(_owner, 5);
            wrapper.Dispose();

            //Act
            wrapper.UpdateJobData(42);

            //Assert
            Assert.IsTrue(wrapper.CurrentDataPublic == 5, "UpdateJobData mutated data after dispose");
            Assert.IsTrue(wrapper.UpdateCount == 0, "UpdateJobInternal ran after dispose");
        }

        [Test]
        public void RegisterToJob_AfterDispose_NoOp()
        {
            //Arrange
            var wrapper = new MockIsolatedJobWrapper(_owner, 5); // RegisterCount == 1
            wrapper.Dispose(); // withdraws

            //Act
            wrapper.RegisterToJob();

            //Assert
            Assert.IsTrue(wrapper.RegisterCount == 1, "RegisterToJob ran after dispose");
            Assert.IsFalse(wrapper.IsRegisteredPublic, "Wrapper re-registered after dispose");
        }
    }
}
