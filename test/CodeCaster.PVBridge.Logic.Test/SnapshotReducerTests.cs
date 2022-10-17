using System;
using System.Collections.Generic;
using CodeCaster.PVBridge.Output;
using NUnit.Framework;

// ReSharper disable once CollectionNeverUpdated.Local
namespace CodeCaster.PVBridge.Logic.Test
{
    /// <summary>
    /// There are no tests. Will add once stabilized. Most exotic scenarios ought to be covered now.
    ///
    /// TODO: assume that each comment or log statement in application code was reached once, then add test for proof.
    /// </summary>
    [TestFixture]
    public class SnapshotReducerTests
    {
        [Test]
        public void EmptyInput_Gives_EmptyOutput()
        {
            // Arrange
            var snapshots = new List<Snapshot>();

            // Act
            var reducedSnapshots = SnapshotReducer.GetDataForResolution(snapshots, DateTime.Today, DateTime.Today.AddDays(1).AddMinutes(6), resolutionInMinutes: 42);

            // Assert
            Assert.That(reducedSnapshots, Is.Empty);
        }
    }
}
