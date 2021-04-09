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
        public void TestMethod1()
        {
            AssertAreApproximatelyEqual(PointCollectionTracker.Distance(new Vector3(17, 1, 0), new Vector3(0, 0, 0), new Vector3(1, 0, 0)), 1);
        }

        private void AssertAreApproximatelyEqual(double v1, double v2)
        {
            Assert.IsTrue(Math.Abs(v1 - v2) < 0.01);
        }
    }
}
