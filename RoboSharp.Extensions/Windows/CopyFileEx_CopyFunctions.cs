﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Win32 = Windows.Win32;
using Windows.Win32.Storage.FileSystem;

namespace RoboSharp.Extensions.Windows
{
    public partial class CopyFileEx
    {
        /// <summary>
        /// Copies an existing file to a new file, notifying the application of its progress through a callback function
        /// </summary>
        /// <param name="source">Source FilePath </param>
        /// <param name="destination">Destination File Path</param>
        /// <param name="progressCallback">Progress Reporter Call-Back</param>
        /// <param name="options">Copy Flags</param>
        /// <returns>TRUE if the copy operation completed</returns>
        /// <inheritdoc cref="Win32.PInvoke.CopyFileEx(string, string, LPPROGRESS_ROUTINE, void*, Win32.Foundation.BOOL*, uint)"/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "ThrowIfNotWindows")]
        internal static unsafe bool InvokeCopyFileEx(
            string source,
            string destination,
            LPPROGRESS_ROUTINE progressCallback,
            CopyFileExOptions options)
        {
            VersionManager.ThrowIfNotWindowsPlatform("P/Invoke.CopyFileEx is only available on Windows");
            bool returnValue = Win32.PInvoke.CopyFileEx(source, destination, progressCallback, lpData: null, pbCancel: null, dwCopyFlags: (uint)options);
            if (!returnValue) Win32Error.ThrowLastError(source, destination);
            return returnValue;
        }


        /// <summary>
        /// Copies a file using the CopyFileEx function directly.
        /// </summary>
        /// <param name="source">The path to the source file.</param>
        /// <param name="destination">The path to the destination file.</param>
        /// <param name="progressCallback">Callback function for progress notifications during the copy operation.</param>
        /// <param name="flags">Flags specifying how the file should be copied.</param>
        /// <returns>True if the file copy operation is successful; otherwise, false.</returns>
        /// <exception cref="OperationCanceledException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="FileNotFoundException"/>
        /// <exception cref="Exception"/>
        public static bool CopyFile(
            string source,
            string destination,
            CopyFileExOptions flags,
            CopyProgressCallback progressCallback
            )
        {
            if (!File.Exists(source)) throw new FileNotFoundException("Source File Not Found.", source);
            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination));
            return InvokeCopyFileEx(source, destination, FileFunctions.CreateCallbackInternal(progressCallback, default), flags);
        }

        ///<inheritdoc cref="CopyFile(string,string,CopyFileExOptions, CopyProgressCallback)"/>
        public static bool CopyFile(string source, string destination, CopyFileExOptions flags)
            => CopyFile(source, destination, flags, null);

        /// <summary>
        /// Executes the CopyFileEx function via <see cref="Task.Run(Action, CancellationToken)"/>
        /// </summary>
        ///<inheritdoc cref="CopyFile(string,string,CopyFileExOptions, CopyProgressCallback)"/>
        public static Task<bool> CopyFileAsync(
            string source,
            string destination,
            CopyFileExOptions flags,
            CopyProgressCallback progressCallback = null,
            CancellationToken token = default
            )
        {
            if (!File.Exists(source)) throw new FileNotFoundException("Source File Not Found.", source);
            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination));
            LPPROGRESS_ROUTINE callback = FileFunctions.CreateCallbackInternal(progressCallback, token);
            return Task.Run(() => InvokeCopyFileEx(source, destination, callback, flags), token);
        }

        /// <summary>
        /// Copy a file Asynchronously. Does not allow overwriting a file.
        /// </summary>
        /// <inheritdoc cref="CopyFileAsync(string, string, bool, CancellationToken)"/>
        public static Task<bool> CopyFileAsync(string source, string destination)
            => CopyFileAsync(source, destination, false, default);

        /// <summary>
        /// Copy a file Asynchronously. Does not allow overwriting a file.
        /// </summary>
        /// <inheritdoc cref="CopyFileAsync(string, string, bool, CancellationToken)"/>
        public static Task<bool> CopyFileAsync(string source, string destination, CancellationToken cancellationToken)
            => CopyFileAsync(source, destination, false, cancellationToken);

        /// <summary>
        /// Copy a file Asynchronously.
        /// </summary>
        /// <param name="source">The source file path</param>
        /// <param name="destination">The destination file path</param>
        /// <param name="overwrite">set TRUE to allow overwriting a file.</param>
        /// <param name="token">The CancellationToken used to cancel the copy task.</param>
        /// <returns>True if the operation completed successfully, otherwise False.</returns>
        /// <exception cref="IOException"/>
        /// <exception cref="FileNotFoundException"/>
        /// <exception cref="OperationCanceledException"/>
        public static async Task<bool> CopyFileAsync(string source, string destination, bool overwrite, CancellationToken token = default)
        {
            if (!File.Exists(source)) throw new FileNotFoundException("Source File Not Found.", source);
            bool destExists = File.Exists(destination);
            if (!overwrite && destExists) throw new IOException("The destination already file exists");
            token.ThrowIfCancellationRequested();
            return await CopyFileAsync(source, destination, flags: overwrite ? CopyFileExOptions.NONE : CopyFileExOptions.FAIL_IF_EXISTS, token: token).ConfigureAwait(false);
        }

        /// <inheritdoc cref="CopyFileProgressAsync"/>
        public static Task<bool> CopyFileAsync(string source, string destination, IProgress<double> progress, int updateInterval = 100, bool overwrite = false, CancellationToken token = default)
        {
            return CopyFileProgressAsync(source, destination, overwrite ? CopyFileExOptions.NONE : CopyFileExOptions.FAIL_IF_EXISTS, updateInterval, percentProgress: progress, token: token);
        }

        /// <inheritdoc cref="CopyFileProgressAsync"/>
        public static Task<bool> CopyFileAsync(string source, string destination, IProgress<ProgressUpdate> progress, int updateInterval = 100, bool overwrite = false, CancellationToken token = default)
        {
            return CopyFileProgressAsync(source, destination, overwrite ? CopyFileExOptions.NONE : CopyFileExOptions.FAIL_IF_EXISTS, updateInterval, progress, null, null, token);
        }

        /// <inheritdoc cref="CopyFileProgressAsync"/>
        public static Task<bool> CopyFileAsync(string source, string destination, IProgress<long> progress, int updateInterval = 100, bool overwrite = false, CancellationToken token = default)
        {
            return CopyFileProgressAsync(source, destination, overwrite ? CopyFileExOptions.NONE : CopyFileExOptions.FAIL_IF_EXISTS, updateInterval, sizeProgress: progress, token: token);
        }

        /// <inheritdoc cref="CopyFileProgressAsync"/>
        public static Task<bool> CopyFileAsync(string source, string destination, IProgress<double> progress, CopyFileExOptions options, int updateInterval = 100, CancellationToken token = default)
        {
            return CopyFileProgressAsync(source, destination, options, updateInterval, null, null, progress, token);
        }

        /// <inheritdoc cref="CopyFileProgressAsync"/>
        public static Task<bool> CopyFileAsync(string source, string destination, IProgress<long> progress, CopyFileExOptions options, int updateInterval = 100, CancellationToken token = default)
        {
            return CopyFileProgressAsync(source, destination, options, updateInterval, null, progress, null, token);
        }

        /// <inheritdoc cref="CopyFileProgressAsync"/>
        public static Task<bool> CopyFileAsync(string source, string destination, IProgress<ProgressUpdate> progress, CopyFileExOptions options, int updateInterval = 100, CancellationToken token = default)
        {
            return CopyFileProgressAsync(source, destination, options, updateInterval, progress, null, null, token);
        }


        /// <summary>
        /// CopyFileAsync with a progress reporter
        /// </summary>
        /// <param name="progress">An IProgress object that will accept a progress notification</param>
        /// <param name="updateInterval">Time interval in milliseconds to update the <paramref name="progress"/> object</param>
        /// <param name="options">The CopyFileEx options to use</param>
        /// <returns>A task that completes when the copy operation has been completed or cancelled</returns>
        /// <inheritdoc cref="CopyFileAsync(string, string, bool, CancellationToken)"/>
        /// <param name="source"/><param name="destination"/><param name="token"/>
        /// <param name="percentProgress"/><param name="sizeProgress"/>
        private static Task<bool> CopyFileProgressAsync(
            string source, string destination, CopyFileExOptions options,
            int updateInterval = 100,
            IProgress<ProgressUpdate> progress = null,
            IProgress<long> sizeProgress = null,
            IProgress<double> percentProgress = null,
            CancellationToken token = default
            )
        {
            token.ThrowIfCancellationRequested();
            FileInfo sourceFile = new FileInfo(source);
            if (!sourceFile.Exists) throw new FileNotFoundException("Source file does not exist", source);
            if (options.HasFlag(CopyFileExOptions.FAIL_IF_EXISTS) && File.Exists(destination)) throw new IOException("The destination already file exists");
            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination));

            // Updater
            Task updateTask = null;
            long fileSize = 0;
            long totalBytesRead = 0;
            updateInterval = updateInterval > 25 ? updateInterval : 100;
            var updateToken = CancellationTokenSource.CreateLinkedTokenSource(token);
            if (sourceFile.Length > 0 && (progress != null | sizeProgress != null | percentProgress != null))
            {
                updateTask = Task.Run(async () =>
                {
                    while (totalBytesRead < sourceFile.Length)
                    {
                        updateToken.Token.ThrowIfCancellationRequested();
                        Report();
                        await Task.Delay(updateInterval, updateToken.Token);
                    }
                }, updateToken.Token);
            }

            //Writer
            var callback = FileFunctions.CreateCallbackInternal(progressRecorder, token);

            return Task.Run(() => InvokeCopyFileEx(source, destination, callback, options: default), token)
                .ContinueWith(async result =>
                {
                    updateToken.Cancel();
                    await updateTask.CatchCancellation(false);
                    updateToken.Dispose();
                    Report();
                    return await result;
                }, TaskContinuationOptions.None)
                .Unwrap();

            void progressRecorder(long size, long copied)
            {
                fileSize = size;
                totalBytesRead = copied;
            }

            void Report()
            {
                progress?.Report(new ProgressUpdate(fileSize, totalBytesRead, source, destination));
                sizeProgress?.Report(totalBytesRead);
                percentProgress?.Report((double)100 * totalBytesRead / fileSize);
            }
        }
    }
}
