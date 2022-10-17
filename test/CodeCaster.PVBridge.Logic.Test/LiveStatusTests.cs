using System;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Logic.Status;
using CodeCaster.PVBridge.Output;
using CodeCaster.PVBridge.Utils;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CodeCaster.PVBridge.Logic.Test
{
    [TestFixture]
    internal class LiveStatusTests
    {
#pragma warning disable CS8618 // Setup() for each call
        private LiveStatus _classUnderTest;
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
            _now = new DateTime(2022, 10, 16, 10, 21, 42, DateTimeKind.Local);
            _clockMock.SetupGet(c => c.Now).Returns(_now);

            _classUnderTest = new LiveStatus(loggerMock.Object, _clockMock.Object, _snapshotResolution);
        }

        [Test]
        public void GetState_Initial_RunsImmediately()
        {
            // Act
            var state = _classUnderTest.GetState();

            // Assert
            Assert.That(state, Is.EqualTo(State.Success));
        }

        [Test]
        public void HandleSnapshotRead_WithRecentSnapshot_WaitsTillNextInterval()
        {
            // Arrange
            // Age it somewhat, but not enough to fall into grace period.
            var snapshotTaken = _now.Add(_snapshotResolution / 3);

            var snapshot = new Snapshot
            {
                TimeTaken = snapshotTaken,
                ActualPower = 42,
            };

            // Act
            var handleState = _classUnderTest.HandleSnapshotReadResponse(_configMock.Object, snapshot);
            // TODO: this shouldn't be necessary anymore, Handle() returns states
            var state = _classUnderTest.GetState();

            // Assert
            Assert.That(handleState, Is.EqualTo(State.Success));

            // :21 with resolution of 5 becomes :25
            var expected = _now.Truncate(_snapshotResolution).Add(_snapshotResolution);
            
            Assert.Multiple(() =>
            {
                Assert.That(state.ShouldRetry, Is.True);
                Assert.That(state.ContinueAt, Is.EqualTo(expected));
            });
        }

        [Test]
        public void HandleSnapshotRead_WithOldSnapshot_RunsImmediately()
        {
            // Act
            // Age it somewhat, but not enough to fall into grace period.
            var snapshotTaken = _now.Add(-_snapshotResolution / 3);

            var snapshot = new Snapshot
            {
                TimeTaken = snapshotTaken,
                ActualPower = 42,
            };

            // Act
            var handleState = _classUnderTest.HandleSnapshotReadResponse(_configMock.Object, snapshot);

            // Let some time pass.
            var now = _now + _snapshotResolution;
            _clockMock.SetupGet(c => c.Now).Returns(now);

            // Act
            var state = _classUnderTest.GetState();

            // Assert
            // :26 with resolution of 5 becomes :30
            var expected = now.Truncate(_snapshotResolution).Add(_snapshotResolution);
            
            Assert.Multiple(() =>
            {
                // Reading went well the first time.
                Assert.That(handleState, Is.EqualTo(State.Success));

                // And we should read again right now.
                Assert.That(state, Is.EqualTo(State.Success));
            });
        }

        [Test]
        public void HandleSnapshotRead_WithStaleSnapshot_WaitsForRetry()
        {
            // Arrange
            // Age it somewhat, but not enough to fall into grace period.
            var snapshotTaken = _now.Add(-_snapshotResolution * 2);

            var snapshot = new Snapshot
            {
                TimeTaken = snapshotTaken,
                ActualPower = 42,
            };

            // Act
            var handleState = _classUnderTest.HandleSnapshotReadResponse(_configMock.Object, snapshot);
            // TODO: this shouldn't be necessary anymore, Handle() returns states
            var state = _classUnderTest.GetState();

            // Assert
            Assert.That(handleState, Is.EqualTo(state));
            
            // TODO: 10 is an implementation detail
            var expected = _now.AddMinutes(10);
            
            Assert.Multiple(() =>
            {
                Assert.That(state.ShouldRetry, Is.True);
                Assert.That(state.ContinueAt, Is.EqualTo(expected));
            });
        }
    }
}
