using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using System;
using ZEditor.ZTemplates;

namespace ZEditorUnitTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void PointCollectionTests()
        {
            AssertAreApproximatelyEqual(1, PointCollectionTracker.Distance(new Vector3(17, 1, 0), new Vector3(0, 0, 0), new Vector3(1, 0, 0)));
        }

        [TestMethod]
        public void GraphicsSpeedTests()
        {
            using (var game = new TestGame())
                game.Run();
        }

        private void AssertAreApproximatelyEqual(double expected, double actual)
        {
            Assert.IsTrue(Math.Abs(expected - actual) < 0.01);
        }
    }
}
