using System;
using System.Linq;
using CodeCaster.PVBridge.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeCaster.PVBridge.Logic.Test
{
    [TestClass]
    public class DateTimeExtensionsTests
    {
        [TestMethod]
        public void GetDaysUntil_Gets_One_Day_Starting_From_Time()
        {
            // Arrange
            var d = new DateTime(2022, 07, 23, 09, 54, 00);

            // Act
            var days = d.GetDaysUntil(d).ToList();

            // Assert
            Assert.AreEqual(1, days.Count);
            Assert.AreEqual(d, days[0]);
        }

        [TestMethod]
        public void GetDaysUntil_Gets_Fourteen_Days()
        {
            // Arrange
            var until = DateTime.Now;
            var since = until.AddDays(-13);

            // Act
            var days = since.GetDaysUntil(until).ToList();

            // Assert
            Assert.AreEqual(14, days.Count);
            Assert.AreEqual(since, days[0]);
            Assert.AreEqual(until, days[13]);
        }
    }
}
