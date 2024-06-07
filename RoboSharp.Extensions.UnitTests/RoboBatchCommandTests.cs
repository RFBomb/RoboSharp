using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp;
using RoboSharp.Extensions.Helpers;
using RoboSharp.Interfaces;
using RoboSharp.UnitTests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RoboSharp.Extensions.Tests
{
    [TestClass]
    public class RoboBatchCommandTests
    {
        [TestMethod]
        public async Task TestCopyOperation()
        {
            Test_Setup.ClearOutTestDestination();
            var source = Test_Setup.GenerateCommand(true, false);
            var root = new DirectoryPair(source.CopyOptions.Source, TestPrep.AppDataFolder);
            var files = root.EnumerateSourceFilePairs(FilePair.CreatePair);
            var cmd = new RoboBatchCommand(new StreamedCopierFactory());
            cmd.Configuration.EnableFileLogging = true;
            cmd.AddCopiers(files);
            var results = await Test_Setup.RunTest(cmd);
            Test_Setup.WriteLogLines(results.Results);
            Assert.AreEqual(files.Count(), results.Results.FilesStatistic.Copied); // expect 4
        }
        
        [TestMethod]
        public async Task TestCancellation()
        {
            CancellationTokenSource cToken = new CancellationTokenSource();

            Test_Setup.ClearOutTestDestination();
            var source = Test_Setup.GenerateCommand(true, false);
            var root = new DirectoryPair(source.CopyOptions.Source, source.CopyOptions.Destination);
            var files = root.EnumerateSourceFilePairs(FilePair.CreatePair);
            var cmd = new RoboBatchCommand(new StreamedCopierFactory());
            cmd.Configuration.EnableFileLogging = true;
            cmd.AddCopiers(files);

            cmd.OnFileProcessed += (o, e) => cmd.Stop(); // Simulates cancellating via a UI by cancelling AFTER it was started
            var results = await Test_Setup.RunTest(cmd);
            Assert.IsTrue(results.Results.Status.WasCancelled);
            var numCopied = results.Results.FilesStatistic.Copied;
            Assert.IsTrue(numCopied < 4 && numCopied > 0);
        }
    }
}
