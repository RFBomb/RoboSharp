using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboSharp.UnitTests
{
    [TestClass()]
    public class StringBuilderExtensionsTests
    {
        [TestMethod()]
        public void AppendWrappedPathTest()
        {
            // Handled by RoboCommandParserTests
        }

        [TestMethod()]
        public void AsEnumerableTest()
        {
            Assert.AreEqual(5, new StringBuilder("13245").AsEnumerable().Count());
        }

        [TestMethod()]
        public void IndexOfTest()
        {
            var builder = new StringBuilder("XYZ");
            Assert.AreEqual(1, builder.IndexOf("Y", true));
            Assert.AreEqual(1, builder.IndexOf("Y",false));
            Assert.AreEqual(1, builder.IndexOf("y", false));
            Assert.AreEqual(-1, builder.IndexOf("y", true));
        }

        [TestMethod()]
        public void IndexOfTest1()
        {
            var builder = new StringBuilder("XYZ_XYZ");
            Assert.AreEqual(5, builder.IndexOf("Y", true, 3));
            Assert.AreEqual(5, builder.IndexOf("Y", false, 3));
            Assert.AreEqual(5, builder.IndexOf("y", false, 3));
            Assert.AreEqual(-1, builder.IndexOf("y", true, 3));
        }

        [TestMethod()]
        public void LastIndexOfTest()
        {
            var builder = new StringBuilder("ABC123XYZ");
            Assert.AreEqual(0, builder.LastIndexOf('A'));
            Assert.AreEqual(1, builder.LastIndexOf('B'));
            Assert.AreEqual(2, builder.LastIndexOf('C'));
            Assert.AreEqual(5, builder.LastIndexOf('3'));
            Assert.AreEqual(8, builder.LastIndexOf('Z'));
            Assert.AreEqual(-1, builder.LastIndexOf('7'));
        }

        [TestMethod()]
        public void SubStringTest()
        {
            string expected = "Wolf Pack";
            var builder = new StringBuilder("XYZ " + expected);
            Assert.AreEqual(expected, builder.SubString(4).ToString());
        }

        [TestMethod()]
        public void SubStringTest1()
        {
            var builder = new StringBuilder("XYZ Wolf Pack");
            Assert.AreEqual("Wolf", builder.SubString(4, 4).ToString());
        }

        [TestMethod()]
        public void StartsWithTest()
        {
            var builder = new StringBuilder("XYZ");
            Assert.IsTrue(builder.StartsWith('X'));
            Assert.IsFalse(builder.StartsWith('Z'));
        }

        [TestMethod()]
        public void StartsWithTest1()
        {
            var builder = new StringBuilder("XYZ");
            Assert.IsTrue(builder.StartsWith("XY"));
            Assert.IsFalse(builder.StartsWith("ZY"));
        }

        [TestMethod()]
        public void EndsWithTest()
        {
            var builder = new StringBuilder("XYZ");
            Assert.IsTrue(builder.EndsWith('Z'));
            Assert.IsFalse(builder.EndsWith('X'));
        }

        [TestMethod()]
        public void EndsWithTest1()
        {
            var builder = new StringBuilder("XYZ");
            Assert.IsTrue(builder.EndsWith("YZ"));
            Assert.IsFalse(builder.EndsWith("XY"));
        }

        [TestMethod()]
        public void TrimTest()
        {
            Assert.AreEqual("", new StringBuilder("   \t").Trim().ToString());
            Assert.AreEqual("T", new StringBuilder("   \r\n\tT\t\t \r\n  ").Trim().ToString());
        }

        [TestMethod()]
        public void TrimTest1()
        {
            Assert.AreEqual("", new StringBuilder("   \t").Trim(' ').ToString());
            Assert.AreEqual("T", new StringBuilder("   \t\tT\t\t   ").Trim(' ').ToString());
            Assert.AreEqual("Y", new StringBuilder("   \tXYZ\t   ").Trim(' ', 'X', 'Z').ToString());
        }

        [TestMethod()]
        public void TrimStartTest()
        {
            Assert.AreEqual("", new StringBuilder("   \t").TrimStart().ToString());
            Assert.AreEqual("T   ", new StringBuilder("\t\t   T   ").TrimStart().ToString());
        }

        [TestMethod()]
        public void TrimStartTest1()
        {
            Assert.AreEqual("", new StringBuilder("   \t").TrimStart(' ').ToString());
            Assert.AreEqual("T  ", new StringBuilder("   \t\tT  ").TrimStart(' ').ToString());
            Assert.AreEqual("YZ  ", new StringBuilder("   \tXYZ  ").TrimStart(' ', 'X', 'Z').ToString());
        }

        [TestMethod()]
        public void TrimEndTest()
        {
            Assert.AreEqual("", new StringBuilder("   \t").TrimEnd().ToString());
            Assert.AreEqual("   T", new StringBuilder("   T   \t").TrimEnd().ToString());
        }

        [TestMethod()]
        public void TrimEndTest1()
        {
            Assert.AreEqual("", new StringBuilder("   \t").TrimEnd(' ').ToString());
            Assert.AreEqual("   \t\tT", new StringBuilder("   \t\tT  \n\t").TrimEnd(' ').ToString());
            Assert.AreEqual("   \tXY", new StringBuilder("   \tXYZ  \t\r\n").TrimEnd(' ', 'X', 'Z').ToString());
        }

        [TestMethod()]
        public void TrimStartTest2()
        {
            Assert.AreEqual("Z", new StringBuilder("XYZ").TrimStart("XY").ToString());
            Assert.AreNotEqual("Z", new StringBuilder("XYZ").TrimStart("ZY").ToString());
        }
    }
}