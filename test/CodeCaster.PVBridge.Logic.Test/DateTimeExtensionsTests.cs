using System;
using System.Linq;
using CodeCaster.PVBridge.Utils;
using NUnit.Framework;

namespace CodeCaster.PVBridge.Logic.Test
{
    [TestFixture]
    public class DateTimeExtensionsTests
    {
        [Test]
        public void GetDaysUntil_Gets_One_Day_Starting_From_Time()
        {
            // Arrange
            var d = new DateTime(2022, 07, 23, 09, 54, 00, DateTimeKind.Local);

            // Act
            var days = d.GetDaysUntil(d).ToList();

            // Assert
            Assert.That(days, Has.Count.EqualTo(1));
            Assert.That(days[0], Is.EqualTo(d));
        }

        [Test]
        public void GetDaysUntil_Gets_Fourteen_Days()
        {
            // Arrange
            var until = DateTime.Now;
            var since = until.AddDays(-13);

            // Act
            var days = since.GetDaysUntil(until).ToList();

            // Assert
            Assert.That(days, Has.Count.EqualTo(14));
            
            Assert.Multiple(() =>
            {
                Assert.That(days[0], Is.EqualTo(since));
                Assert.That(days[13], Is.EqualTo(until));
            });
        }
    }
}
