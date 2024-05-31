using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp;
using RoboSharp.Results;
using RoboSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboSharp.UnitTests
{
    [TestClass]
    public class RegexTests
    {
        [TestMethod]
        public void RoboCommand_ProgressData()
        {
            var regex = RoboCommand.Process_OutputProgressDataRegex();
            Assert.IsTrue(regex.IsMatch("100%"));
            Assert.IsTrue(regex.IsMatch("1.05%"));

            Assert.IsFalse(regex.IsMatch("    8"));
            Assert.IsFalse(regex.IsMatch("New File  \t\t    1024\t1024_Bytes.txt"));
            Assert.IsFalse(regex.IsMatch("New Dir          0\tC:\\Repos\\RoboSharp\\"));
        }

        [TestMethod]
        public void RoboCommand_DirectoryData()
        {
            var regex = RoboCommand.Process_OutputDirectoryDataRegex();
            Assert.IsTrue(regex.IsMatch("\t                   4\tC:\\Repos\\RoboSharp\\".Trim()), "\n-->Failed 'Existing Directory' check");
            
            // New Directory
            string newDir = "New Dir          4\tC:\\Repos\\RoboSharp\\";
            var dirMatch = RoboCommand.Process_OutputDirectoryDataRegex().Match(newDir);
            Assert.IsTrue(dirMatch.Success, "\n-->Failed 'New Directory' check");
            Assert.AreEqual("New Dir", dirMatch.Groups["Type"].Value);
            Assert.AreEqual("4", dirMatch.Groups["FileCount"].Value);
            Assert.AreEqual("C:\\Repos\\RoboSharp\\", dirMatch.Groups["Path"].Value);
        }

        [TestMethod]
        public void RoboSharpConfiguration_ErrorToken()
        {
            const string _error = "5\\25\\2024 {0} 1234 (0x01) Some Error C:\\MyFile.txt";
            const string _dir  = "New Dir          4\tC:\\Repos\\RoboSharp\\";
            const string _file = "New File  \t\t    1024\t1024_Bytes.txt";
            const string _message = "JunkData  " + _error;

            foreach(var config in RoboSharpConfiguration.defaultConfigurations)
            {
                Assert.IsTrue(config.Value.ErrorTokenRegex.IsMatch(string.Format(_error, config.Value.ErrorToken)), $"\n{config.Key} Error Regex Failed");

                Assert.IsFalse(config.Value.ErrorTokenRegex.IsMatch(_dir), "\n--> Unexpectedly matched a Directory line");
                Assert.IsFalse(config.Value.ErrorTokenRegex.IsMatch(_file), "\n--> Unexpectedly matched a File line");
                Assert.IsFalse(config.Value.ErrorTokenRegex.IsMatch(string.Format(_message, config.Value.ErrorToken)), "\n--> Unexpectedly matched a Message line");
            }
        }
    }
}
