using System;
using System.Linq;
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
    internal class BacklogStatusTests
    {
        private const int BacklogDays = 14;

#pragma warning disable CS8618 // Setup() for each call
        private BacklogStatus _classUnderTest;
        private Mock<DataProviderConfiguration> _configMock;
        private TimeSpan _snapshotResolution;
        private Mock<IClock> _clockMock;
        private DateTime _now;
        private DateTime _syncStart;
#pragma warning restore CS8618

        [SetUp]
        public void SetUp()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>(MockBehavior.Loose);
            
            // TODO: unused
            _configMock = new Mock<DataProviderConfiguration>(MockBehavior.Strict);

            // Take a resolution of PVOutput non-donation mode: 5 minutes.
            _snapshotResolution = TimeSpan.FromMinutes(5);
            
            _clockMock = new Mock<IClock>(MockBehavior.Strict);
            _now = new DateTime(2022, 10, 17, 09, 59, 42, DateTimeKind.Local);
            _clockMock.SetupGet(c => c.Now).Returns(_now);
            
            // Take 14 days (including today, so -1).
            _syncStart = _now.Date.AddDays(-(BacklogDays - 1));
            
            _classUnderTest = new BacklogStatus(loggerMock.Object, _clockMock.Object, _syncStart, _snapshotResolution);
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
        public void GetBacklog_Initial_Returns_DaysSinceStart()
        {
            // Act
            var (backlogState, daysToSync) = _classUnderTest.GetBacklog();
            
            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(backlogState, Is.EqualTo(State.Success));

                Assert.That(daysToSync, Has.Count.EqualTo(BacklogDays));
            });
        }

        [Test]
        public void GetBacklog_HalfSynced_ReturnsNeedingSyncNow()
        {
            // Arrange
            var backlogDates = _syncStart.GetDaysUntil(_now).ToList();
            
            // Only mark half as synced to keep a backlog needing syncing.
            var daysSynced = backlogDates.Take((int)Math.Ceiling(backlogDates.Count / 2f)).ToList();
            
            Assert.That(daysSynced, Has.Count.AtLeast(1));

            State? lastState = null;

            foreach (var dayTime in daysSynced)
            {
                lastState = MarkBacklogDaySynced(dayTime);
            }

            // Act
            var (backlogState, daysToSync) = _classUnderTest.GetBacklog();

            // Assert
            Assert.That(lastState, Is.EqualTo(State.Success));
            
            Assert.That(lastState, Is.EqualTo(backlogState));
            
            Assert.That(daysToSync, Has.Count.EqualTo(backlogDates.Count - daysSynced.Count));
        }

        [Test]
        public void GetBacklog_FullySynced_Waits()
        {
            // Arrange
            DateTime now = _now;
            State? lastState = null;

            // Mark all days as synced.
            foreach (var dayTime in _syncStart.GetDaysUntil(now))
            {
                lastState = MarkBacklogDaySynced(dayTime);

                if (dayTime.Date == now.Date)
                {
                    // TODO: 
                }
            }

            // Act
            var (backlogState, daysToSync) = _classUnderTest.GetBacklog();
            
            // Assert
            // TODO: refactor GetState() away
            Assert.That(lastState, Is.EqualTo(backlogState));

            // TODO: 1 & 2 are an implementation detail
            var expectedContinue = _now.Truncate(TimeSpan.FromHours(1)).Add(TimeSpan.FromHours(2));

            Assert.Multiple(() =>
            {
                Assert.That(backlogState.ShouldRetry, Is.True);
                Assert.That(backlogState.ContinueAt, Is.EqualTo(expectedContinue));
                Assert.That(daysToSync, Is.Empty);
            });
        }

        [Test]
        public void GetBacklog_WithRetries_WaitsTillFirstRetry()
        {
            // Arrange
            var beforeYesterday = DateOnly.FromDateTime(_now.Date.AddDays(-2));
            var yesterday = DateOnly.FromDateTime(_now.Date.AddDays(-1));

            // Mark all days as synced.
            foreach (var dayTime in _syncStart.GetDaysUntil(_now))
            {
                MarkBacklogDaySynced(dayTime);
            }

            // Overwrite (!) yesterday's and the day before's success status with an error or two.
            // Fail twice, 20 minutes.
            var beforeYesterdayState1 = MarkBacklogDayFailed(beforeYesterday);
            var beforeYesterdayState2 = MarkBacklogDayFailed(beforeYesterday);
            
            // Fail once, 10 minutes, should come first.
            var yesterdayState = MarkBacklogDayFailed(yesterday);

            // Act
            var (backlogState, days) = _classUnderTest.GetBacklog();

            // Assert
            // TODO: 10 & 20 are an implementation detail
            Assert.That(beforeYesterdayState1.ContinueAt, Is.EqualTo(_now.AddMinutes(10)));
            Assert.That(beforeYesterdayState2.ContinueAt, Is.EqualTo(_now.AddMinutes(20)));

            // TODO: refactor GetState() away
            Assert.That(yesterdayState, Is.EqualTo(backlogState));

            // Retry in 10 minutes for yesterday
            var expectedContinue = _now.AddMinutes(10);

            Assert.Multiple(() =>
            {
                Assert.That(backlogState.ShouldRetry, Is.True);
                Assert.That(backlogState.ContinueAt, Is.EqualTo(expectedContinue));
                Assert.That(days, Is.Empty);
            });
        }

        private State MarkBacklogDayFailed(DateOnly day)
        {
            return _classUnderTest.HandleDayWrittenResponse(_configMock.Object, day, ApiResponse<DaySummary>.Failed());
        }

        private State MarkBacklogDaySynced(DateTime dayTime)
        {
            var day = DateOnly.FromDateTime(dayTime);

            var summary = new DaySummary
            {
                Day = dayTime,
                SyncedAt = _now.Add(-_snapshotResolution * 2),
                DailyGeneration = 42,
            };

            // Act
            return _classUnderTest.HandleDayWrittenResponse(_configMock.Object, day, summary);
        }
    }
}
