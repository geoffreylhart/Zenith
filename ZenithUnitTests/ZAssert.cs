using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithUnitTests
{
    public class ZAssert
    {
        public static void AssertIsClose(double a, double b)
        {
            Assert.IsTrue(Math.Abs(a - b) < 0.001);
        }
    }
}
