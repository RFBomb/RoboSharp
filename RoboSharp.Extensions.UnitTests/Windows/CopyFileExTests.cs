using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboSharp.Extensions.Tests;
using RoboSharp.Extensions.Windows;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static RoboSharp.Extensions.Tests.AssertExtensions;

namespace RoboSharp.Extensions.Windows.UnitTests
{
    [TestClass()]
    public class CopyFileExTests
    {
        private static string GetRandomPath(bool inSubFolder = false) => TestPrep.GetRandomPath(inSubFolder);

        [TestMethod]
        public void TestCancellationTokens()
        {
            // Cancelling the linked source does not cancel the input tokens
            var cts_1 = new CancellationTokenSource();
            var cts_2 = new CancellationTokenSource();
            var cts_linked = CancellationTokenSource.CreateLinkedTokenSource(cts_1.Token, cts_2.Token);
            Assert.IsFalse(cts_linked.IsCancellationRequested);
            cts_linked.Cancel();
            Assert.IsTrue(cts_linked.IsCancellationRequested);
            Assert.IsFalse(cts_1.IsCancellationRequested);
            Assert.IsFalse(cts_2.IsCancellationRequested);

            // canceling a token that was an input causes linke to report as cancelled
            cts_linked = CancellationTokenSource.CreateLinkedTokenSource(cts_1.Token, cts_2.Token);
            Assert.IsFalse(cts_linked.IsCancellationRequested);
            cts_1.Cancel();
            Assert.IsTrue(cts_1.IsCancellationRequested);
            Assert.IsFalse(cts_2.IsCancellationRequested);
            Assert.IsTrue(cts_linked.IsCancellationRequested);
        }

        [TestMethod()]
        public async Task RunIFileCopierTests()
        {
            IFileCopierFactory factory = new CopyFileExFactory() { Options = CopyFileExOptions.NONE };
            if (VersionManager.IsPlatformWindows)
            {
                _ = await IFileCopierTests.RunTest(factory);
            }
            else
                await Assert.ThrowsExceptionAsync<PlatformNotSupportedException>(() => IFileCopierTests.RunTest(factory));
        }

        [TestMethod]
        public void CopyFileEx_ToDirectory()
        {
            string sourceFile = GetRandomPath();
            string destFile = GetRandomPath(true);
            string destFolder = Path.GetDirectoryName(destFile);

            try
            {
                Console.WriteLine(string.Format("Source: {0}\nDestination: {1}", sourceFile, destFile));
                File.WriteAllText(sourceFile, "Test Contents");
                // Verify test prep
                Assert.IsTrue(File.Exists(sourceFile), "Source File not created!");
                Assert.IsFalse(Directory.Exists(destFolder), "Destination folder already Exists!");
                Assert.IsFalse(File.Exists(destFile), "Destination File Already Exists!");
                //Test the function
                Assert.IsTrue(CopyFileEx.CopyFile(sourceFile, destFile, default), "Function returned False (copy failed)");
                Assert.IsTrue(File.Exists(destFile), "File does not exist at destination");
            }
            finally
            {
                if (File.Exists(sourceFile)) File.Delete(sourceFile);
                if (File.Exists(destFile)) File.Delete(destFile);
                if (Directory.Exists(destFolder)) Directory.Delete(destFolder, false);
            }
        }

        [TestMethod]
        public void CopyFileEx_CopyFile()
        {
            bool callbackHit = false;
            int callbackHitCount = 0;
            var sourceFile = GetRandomPath();
            var destFile = GetRandomPath();
            try
            {
                Console.WriteLine(string.Format("Source: {0}\nDestination: {1}", sourceFile, destFile));

                // Source Missing Test
                if (File.Exists(sourceFile)) File.Delete(sourceFile);
                Assert.ThrowsException<FileNotFoundException>(() => CopyFileEx.CopyFile(sourceFile, destFile, CopyFileExOptions.FAIL_IF_EXISTS));

                // Prep for Fail_If_Exists Test
                File.WriteAllText(sourceFile, "Test Contents");
                File.WriteAllText(destFile, "Content to replace\n\nMoreContent");
                Assert.IsTrue(File.Exists(sourceFile));
                Assert.IsTrue(File.Exists(destFile));

                // Fail_If_Exists -- Overwrite
                Assert.ThrowsException<IOException>(() => CopyFileEx.CopyFile(sourceFile, destFile, CopyFileExOptions.FAIL_IF_EXISTS), "\nCopy Operation Succeeded when CopyFileExOptions.FAIL_IF_EXISTS was set");
                Assert.IsTrue(CopyFileEx.CopyFile(sourceFile, destFile, CopyFileExOptions.NONE), "\n Copy Operation Failed when CopyFileExOptions.NONE was set");

                // Cancellation
                var cancelCallback = FileFunctions.CreateCallback((ProgressUpdate b) =>
                {
                    callbackHit = true;
                    callbackHitCount++;
                    return CopyProgressCallbackResult.CANCEL;
                });
                Assert.IsFalse(callbackHit);
                Assert.ThrowsException<OperationCanceledException>(() => CopyFileEx.CopyFile(sourceFile, destFile, default, cancelCallback), "\nOperation was not cancelled");
                Assert.IsTrue(callbackHit, "\nCallback was not hit");
                Assert.AreEqual(1, callbackHitCount, "\nCallback count incorrect");
                callbackHit = false;
                callbackHitCount = 0;

                // Quiet
                var quietCallback = FileFunctions.CreateCallback((ProgressUpdate b) =>
                {
                    callbackHit = true;
                    callbackHitCount++;
                    return CopyProgressCallbackResult.QUIET;
                });
                Assert.IsFalse(callbackHit);
                Assert.IsTrue(CopyFileEx.CopyFile(sourceFile, destFile, default, quietCallback));
                Assert.IsTrue(callbackHit, "\nCallback was not hit");
                Assert.AreEqual(1, callbackHitCount, "\nCallback count incorrect");
                callbackHit = false;
                callbackHitCount = 0;

                // Continue
                var continueCallback = FileFunctions.CreateCallback((ProgressUpdate b) =>
                {
                    callbackHit = true;
                    callbackHitCount++;
                    return CopyProgressCallbackResult.CONTINUE;
                });
                Assert.IsFalse(callbackHit);
                Assert.IsTrue(CopyFileEx.CopyFile(sourceFile, destFile, default, continueCallback));
                Assert.IsTrue(callbackHit, "\nCallback was not hit");
                Assert.IsTrue(callbackHitCount >= 2, "\nCallback count incorrect");
                callbackHit = false;
                callbackHitCount = 0;
            }
            finally
            {
                if (File.Exists(sourceFile)) File.Delete(sourceFile);
                if (File.Exists(destFile)) File.Delete(destFile);
            }
        }

        [TestMethod]
        public async Task CopyFileEx_CopyFileAsync()
        {
            bool callbackHit = false;
            int callbackHitCount = 0;
            string sourceFile = GetRandomPath();
            string destFile = GetRandomPath();
            try
            {
                // Source Missing Test
                Console.WriteLine(string.Format("Source: {0}\nDestination: {1}", sourceFile, destFile));
                if (File.Exists(sourceFile)) File.Delete(sourceFile);
                await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () => await CopyFileEx.CopyFileAsync(sourceFile, destFile, CopyFileExOptions.NONE));

                // Prep for Fail_If_Exists Test
                File.WriteAllText(sourceFile, "Test Contents");
                File.WriteAllText(destFile, "Content to replace");
                Assert.IsTrue(File.Exists(sourceFile));
                Assert.IsTrue(File.Exists(destFile));

                // Fail_If_Exists -- Overwrite
                await Assert.ThrowsExceptionAsync<IOException>(async () => await CopyFileEx.CopyFileAsync(sourceFile, destFile, CopyFileExOptions.FAIL_IF_EXISTS), "\nCopy Operation Succeeded when CopyFileExOptions.FAIL_IF_EXISTS was set");
                Assert.IsTrue(await CopyFileEx.CopyFileAsync(sourceFile, destFile, CopyFileExOptions.NONE), "\n Copy Operation Failed when CopyFileExOptions.NONE was set");

                // Cancellation
                var cancelCallback = FileFunctions.CreateCallback((ProgressUpdate b) =>
                {
                    callbackHit = true;
                    callbackHitCount++;
                    return CopyProgressCallbackResult.CANCEL;
                });
                Assert.IsFalse(callbackHit);
                await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () => await CopyFileEx.CopyFileAsync(sourceFile, destFile, CopyFileExOptions.NONE, cancelCallback), "\nOperation was not cancelled");
                Assert.IsTrue(callbackHit, "\nCallback was not hit");
                Assert.AreEqual(1, callbackHitCount, "\nCallback count incorrect");
                callbackHit = false;
                callbackHitCount = 0;

                // Quiet
                var quietCallback = FileFunctions.CreateCallback((ProgressUpdate b) =>
                {
                    callbackHit = true;
                    callbackHitCount++;
                    return CopyProgressCallbackResult.QUIET;
                });
                Assert.IsFalse(callbackHit);
                Assert.IsTrue(await CopyFileEx.CopyFileAsync(sourceFile, destFile, CopyFileExOptions.NONE, quietCallback));
                Assert.IsTrue(callbackHit, "\nCallback was not hit");
                Assert.AreEqual(1, callbackHitCount, "\nCallback count incorrect");
                callbackHit = false;
                callbackHitCount = 0;

                // Continue
                var continueCallback = FileFunctions.CreateCallback((ProgressUpdate b) =>
                {
                    callbackHit = true;
                    callbackHitCount++;
                    return CopyProgressCallbackResult.CONTINUE;
                });
                Assert.IsFalse(callbackHit);
                Assert.IsTrue(await CopyFileEx.CopyFileAsync(sourceFile, destFile, CopyFileExOptions.NONE, continueCallback));
                Assert.IsTrue(callbackHit, "\nCallback was not hit");
                Assert.IsTrue(callbackHitCount >= 2, "\nCallback count incorrect");
                callbackHit = false;
                callbackHitCount = 0;
            }
            finally
            {
                if (File.Exists(sourceFile)) File.Delete(sourceFile);
                if (File.Exists(destFile)) File.Delete(destFile);
            }
        }

        [TestMethod()]
        public async Task CopyFileEx_AsyncOverloads()
        {
            string sourceFile = GetRandomPath();
            string destFile = GetRandomPath();

            try
            {
                Console.WriteLine(string.Format("Source: {0}\nDestination: {1}", sourceFile, destFile));
                if (File.Exists(sourceFile)) File.Delete(sourceFile);

                bool progFullUpdated = false;
                var progFull = new Progress<ProgressUpdate>();
                progFull.ProgressChanged += (o, e) => progFullUpdated = true;

                bool progPercentUpdated = false;
                var progPercent = new Progress<double>();
                progPercent.ProgressChanged += (o, e) => progPercentUpdated = true;

                bool progSizeUpdated = false;
                var progSize = new Progress<long>();
                progSize.ProgressChanged += (o, e) => progSizeUpdated = true;

                string assertMessage = "\n Source File Missing Test";
                await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () => await CopyFileEx.CopyFileAsync(sourceFile, destFile), assertMessage);
                await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () => await CopyFileEx.CopyFileAsync(sourceFile, destFile, false), assertMessage);
                await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () => await CopyFileEx.CopyFileAsync(sourceFile, destFile, true), assertMessage);
                await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () => await CopyFileEx.CopyFileAsync(sourceFile, destFile, progFull, 100, true), assertMessage);
                await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () => await CopyFileEx.CopyFileAsync(sourceFile, destFile, progPercent, 100, true), assertMessage);
                await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () => await CopyFileEx.CopyFileAsync(sourceFile, destFile, progSize, 100, true), assertMessage);
                Assert.IsFalse(progFullUpdated | progSizeUpdated | progPercentUpdated);

                File.WriteAllText(sourceFile, "Test Contents");
                File.WriteAllText(destFile, "Content to replace");
                Assert.IsTrue(File.Exists(sourceFile));
                Assert.IsTrue(File.Exists(destFile));

                // Prevent Overwrite
                assertMessage = "\n Overwrite Prevention Test";
                await Assert.ThrowsExceptionAsync<IOException>(async () => await CopyFileEx.CopyFileAsync(sourceFile, destFile), assertMessage);
                await Assert.ThrowsExceptionAsync<IOException>(async () => await CopyFileEx.CopyFileAsync(sourceFile, destFile, false), assertMessage);
                await Assert.ThrowsExceptionAsync<IOException>(async () => await CopyFileEx.CopyFileAsync(sourceFile, destFile, progFull, 100, false), assertMessage);
                await Assert.ThrowsExceptionAsync<IOException>(async () => await CopyFileEx.CopyFileAsync(sourceFile, destFile, progPercent, 100, false), assertMessage);
                await Assert.ThrowsExceptionAsync<IOException>(async () => await CopyFileEx.CopyFileAsync(sourceFile, destFile, progSize, 100, false), assertMessage);

                // Overwrite
                progPercentUpdated = false;
                progSizeUpdated = false;
                progFullUpdated = false;
                assertMessage = "\n Allow Overwrite Test";
                Assert.IsTrue(await CopyFileEx.CopyFileAsync(sourceFile, destFile, true), assertMessage);
                Assert.IsTrue(await CopyFileEx.CopyFileAsync(sourceFile, destFile, progFull, 100, true), assertMessage);
                Assert.IsTrue(await CopyFileEx.CopyFileAsync(sourceFile, destFile, progPercent, 100, true), assertMessage);
                Assert.IsTrue(await CopyFileEx.CopyFileAsync(sourceFile, destFile, progSize, 100, true), assertMessage);
                Assert.IsTrue(progFullUpdated, "Full Progress object never reported");
                Assert.IsTrue(progSizeUpdated, "Size Progress object never reported");
                Assert.IsTrue(progPercentUpdated, "Percentage Progress object never reported");

                // Cancellation Prior to write
                assertMessage = "\n Cancellation Test";
                var cdToken = new CancellationTokenSource();
                cdToken.Cancel();
                File.Delete(destFile);
                await AssertExtensions.AssertThrowsExceptionAsync<OperationCanceledException>(() => CopyFileEx.CopyFileAsync(sourceFile, destFile, cdToken.Token), assertMessage);
                await AssertExtensions.AssertThrowsExceptionAsync<OperationCanceledException>(() => CopyFileEx.CopyFileAsync(sourceFile, destFile, false, cdToken.Token), assertMessage);
                await AssertExtensions.AssertThrowsExceptionAsync<OperationCanceledException>(() => CopyFileEx.CopyFileAsync(sourceFile, destFile, progFull, 100, false, cdToken.Token), assertMessage);
                await AssertExtensions.AssertThrowsExceptionAsync<OperationCanceledException>(() => CopyFileEx.CopyFileAsync(sourceFile, destFile, progPercent, 100, false, cdToken.Token), assertMessage);
                await AssertExtensions.AssertThrowsExceptionAsync<OperationCanceledException>(() => CopyFileEx.CopyFileAsync(sourceFile, destFile, progSize, 100, false, cdToken.Token), assertMessage);
                Assert.IsFalse(File.Exists(destFile));

                // Cancellation Mid-Write
                assertMessage = "\n Cancellation Test";
                CancellationToken GetTimedToken() => new CancellationTokenSource(new TimeSpan(125)).Token;
                File.Delete(destFile);
                progPercentUpdated = false;
                progSizeUpdated = false;
                progFullUpdated = false;
                await AssertExtensions.AssertThrowsExceptionAsync<OperationCanceledException>(() => CopyFileEx.CopyFileAsync(sourceFile, destFile, GetTimedToken()), assertMessage);
                await AssertExtensions.AssertThrowsExceptionAsync<OperationCanceledException>(() => CopyFileEx.CopyFileAsync(sourceFile, destFile, false, GetTimedToken()), assertMessage);
                await AssertExtensions.AssertThrowsExceptionAsync<OperationCanceledException>(() => CopyFileEx.CopyFileAsync(sourceFile, destFile, progFull, 100, false, GetTimedToken()), assertMessage);
                await AssertExtensions.AssertThrowsExceptionAsync<OperationCanceledException>(() => CopyFileEx.CopyFileAsync(sourceFile, destFile, progPercent, 100, false, GetTimedToken()), assertMessage);
                await AssertExtensions.AssertThrowsExceptionAsync<OperationCanceledException>(() => CopyFileEx.CopyFileAsync(sourceFile, destFile, progSize, 100, false, GetTimedToken()), assertMessage);
                // These progress report assertions are to check that the operation STARTED but was cancelled prior to completion, causing deletion because Restartable mode was not used.
                Assert.IsTrue(progFullUpdated, "Full Progress object never reported");
                Assert.IsTrue(progSizeUpdated, "Size Progress object never reported");
                Assert.IsTrue(progPercentUpdated, "Percentage Progress object never reported");
                Assert.IsFalse(File.Exists(destFile));
            }
            finally
            {
                if (File.Exists(sourceFile)) File.Delete(sourceFile);
                if (File.Exists(destFile)) File.Delete(destFile);
            }
        }

    }
}
