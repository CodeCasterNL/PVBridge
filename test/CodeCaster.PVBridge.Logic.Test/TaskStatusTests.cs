using System;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Logic.Status;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace CodeCaster.PVBridge.Logic.Test
{
    [TestFixture]
    internal class TaskStatusTests
    {
#pragma warning disable CS8618 // Setup() for each call
        private Mock<TaskStatus> _classUnderTest;
        private Mock<DataProviderConfiguration> _configMock;
        private TimeSpan _snapshotResolution;
        private Mock<IClock> _clockMock;
        private DateTime _now;
#pragma warning restore CS8618

        [SetUp]
        public void SetUp()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>(MockBehavior.Loose);
            
            _configMock = new Mock<DataProviderConfiguration>(MockBehavior.Strict);

            _snapshotResolution = TimeSpan.FromMinutes(5);

            _clockMock = new Mock<IClock>(MockBehavior.Strict);
            _now = new DateTime(2022, 10, 17, 10, 14, 42, DateTimeKind.Local);
            _clockMock.SetupGet(c => c.Now).Returns(_now);

            _classUnderTest = new Mock<TaskStatus>(MockBehavior.Strict, loggerMock.Object, _clockMock.Object, _snapshotResolution);

            _classUnderTest.Setup(s => s.ToString()).Returns("testing");
            _classUnderTest.Protected().Setup<State>("UpdateState").Returns(State.Success);
        }

        [Test]
        public void GetState_Initial_RunsImmediately()
        {
            // Act
            var state = _classUnderTest.Object.GetState();

            // Assert
            Assert.That(state, Is.EqualTo(State.Success));
        }

        [Test]
        public void HandleApiResponse_Failed_Waits()
        {
            // Arrange
            var errorResponse = new ApiResponse(ApiResponseStatus.Failed);

            // Act
            var state = _classUnderTest.Object.HandleApiResponse(_configMock.Object, errorResponse);

            // Assert
            // TODO: 10 is an implementation detail
            var expected = _now.AddMinutes(10);

            Assert.Multiple(() =>
            {
                Assert.That(state.ShouldRetry, Is.True);
                Assert.That(state.ContinueAt, Is.EqualTo(expected));
            });
        }

        [Test]
        public void HandleApiResponse_Failed_WaitsIncrementally()
        {
            // Arrange first error
            var errorResponse = new ApiResponse(ApiResponseStatus.Failed);

            // Act
            var firstErrorState = _classUnderTest.Object.HandleApiResponse(_configMock.Object, errorResponse);

            // TODO: 10 is an implementation detail.
            var now = _now + TimeSpan.FromMinutes(10);

            // Assert
            Assert.That(firstErrorState.ShouldRetry, Is.True);
            // Ten minutes have passed, the retry should be now
            Assert.That(firstErrorState.ContinueAt, Is.EqualTo(now));

            // Arrange second error
            _clockMock.SetupGet(c => c.Now).Returns(now);

            // Act
            var secondErrorState = _classUnderTest.Object.HandleApiResponse(_configMock.Object, errorResponse);

            // Assert
            Assert.That(secondErrorState.ShouldRetry, Is.True);
            // TODO: 20 is an implementation detail.
            Assert.That(secondErrorState.ContinueAt, Is.EqualTo(now.AddMinutes(20)));
        }

        [Test]
        public void HandleApiResponse_RateLimited_Waits()
        {
            // Arrange
            var retryAt = _now.AddMinutes(42);
            var errorResponse = ApiResponse.RateLimited(retryAt);

            // Act
            var state = _classUnderTest.Object.HandleApiResponse(_configMock.Object, errorResponse);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(state.ShouldRetry, Is.True);
                Assert.That(state.ContinueAt, Is.EqualTo(retryAt));
            });
        }
    }
}
