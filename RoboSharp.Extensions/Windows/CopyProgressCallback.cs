using RoboSharp.Extensions.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32.Storage.FileSystem;
using Win32 = Windows.Win32;

namespace RoboSharp.Extensions.Windows
{
    /// <summary>
    /// Handle the CallBack requested by CopyFileEx
    /// </summary>
    /// <param name="totalFileSize">Total File Size to be copied (bytes)</param>
    /// <param name="totalBytesTransferred">Total number of bytes transfered</param>
    /// <param name="streamSize">The total size of the current file stream, in bytes.</param>
    /// <param name="streamBytesTransferred">The total number of bytes in the current stream that have been transferred from the source file to the destination file since the copy operation began.</param>
    /// <param name="streamID">A handle to the current stream. The first time CopyProgressRoutine is called, the stream number is 1.</param>
    /// <param name="reason"><inheritdoc cref="CopyProgressCallbackReason" path="*"/>
    /// User-Defined data that will be passed into the callback.
    /// <para/> Example : CopyProgressData progressData = GCHandle.FromIntPtr(lpData).Target as CopyProgressData;
    /// </param>
    /// <returns><inheritdoc cref="CopyProgressCallbackResult"/></returns>
    /// <remarks>
    /// Signature : Func{long, long, long ,long, uint, CopyProgressCallbackReason, IntPtr, IntPtr, IntPtr, CopyProgressCallbackResult}
    /// <br/>
    /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/winbase/nc-winbase-lpprogress_routine"/>
    /// </remarks>
    /// <seealso cref="Win32.Storage.FileSystem.LPPROGRESS_ROUTINE"/>
    public delegate CopyProgressCallbackResult CopyProgressCallback(
        long totalFileSize,
        long totalBytesTransferred,
        long streamSize,
        long streamBytesTransferred,
        uint streamID,
        CopyProgressCallbackReason reason
        //,IntPtr hSourceFile
        //,IntPtr hDestinationFile
        //,IntPtr data
        );

    /// <summary>Method signature for function to pass into <see cref="CopyFileEx.CreateCallback(BytesTransferredCallback)"/></summary>
    /// <remarks>
    /// Signature : Func&lt;<see langword="long"/> totalFileSize, <see langword="long"/> bytesTransfered, <see cref="CopyProgressCallbackResult"/>&gt;
    /// </remarks>
    /// <inheritdoc cref="CopyProgressCallback"/>
    public delegate CopyProgressCallbackResult BytesTransferredCallback(long totalFileSize, long totalTransferred);

    /// <summary>
    /// Event description from CopyFileEx (why its performing the callback)
    /// </summary>
    /// <seealso cref="Win32.Storage.FileSystem.LPPROGRESS_ROUTINE_CALLBACK_REASON"/>
    public enum CopyProgressCallbackReason : uint
    {
        /// <summary>
        /// Copy Progress Updated
        /// </summary>
        CALLBACK_CHUNK_FINISHED = LPPROGRESS_ROUTINE_CALLBACK_REASON.CALLBACK_CHUNK_FINISHED,

        /// <summary>
        /// A stream was created and is about to be copied. This is the callback reason given when the callback routine is first invoked. 
        /// </summary>
        CALLBACK_STREAM_SWITCH = LPPROGRESS_ROUTINE_CALLBACK_REASON.CALLBACK_STREAM_SWITCH
    }

    /// <summary>
    /// The result of the callback to be evaluated by CopyFileEx
    /// </summary>
    public enum CopyProgressCallbackResult : uint
    {
        /// <summary>
        /// Continue the copy operation. 
        /// </summary>
        CONTINUE = 0U,

        /// <summary>
        /// Cancel the copy operation.
        /// The partially copied destination file is deleted.
        /// </summary>
        CANCEL = 1U,

        /// <summary>
        /// Stop the copy operation. It can be restarted at a later time.
        /// The partially copied destination file is left intact.
        /// </summary>
        STOP = 2U,

        /// <summary>
        /// Continue the copy operation, but prevent additional callbacks. 
        /// </summary>
        QUIET = 3U
    }

    public partial class CopyFileEx
    {
        /// <summary>
        /// Create a callback new callback that wraps the <paramref name="token"/> in order to detected a cancellation request and pass it back to CopyFileEx
        /// </summary>
        /// <param name="token">The token to wrap</param>
        /// <returns>A new <see cref="CopyProgressCallback"/></returns>
        public static CopyProgressCallback CreateCallback(CancellationToken token)
        {
            return GetResult;
            CopyProgressCallbackResult GetResult(long tfs, long bc, long _1, long _2, uint _3, CopyProgressCallbackReason reason)
            {
                return token.IsCancellationRequested ? CopyProgressCallbackResult.CANCEL : CopyProgressCallbackResult.CONTINUE;
            }
        }

        /// <summary>
        /// Wrap an existing callback method, combinining with the the token to detect if the copy operation should be cancelled.
        /// </summary>
        /// <returns>A new <see cref="CopyProgressCallback"/></returns>
        public static CopyProgressCallback CreateCallback(CopyProgressCallback callback, CancellationToken token)
        {
            if (callback is null) return CreateCallback(token);
            return GetResult;
            CopyProgressCallbackResult GetResult(long a, long b, long c, long d, uint e, CopyProgressCallbackReason f)
            {
                var result = callback?.Invoke(a, b, c, d, e, f);
                if (token.IsCancellationRequested)
                    result = CopyProgressCallbackResult.CANCEL;
                return result ?? CopyProgressCallbackResult.CONTINUE;
            }
        }

        /// <summary>
        /// Create a callback new callback from a function that takes no parameters and returns a CallBack result
        /// </summary>
        /// <param name="shouldContinueCallback">The callback to wrap</param>
        /// <returns>A new <see cref="CopyProgressCallback"/></returns>
        public static CopyProgressCallback CreateCallback(Func<CopyProgressCallbackResult> shouldContinueCallback)
        {
            return new CopyProgressCallback((tfs, bc, _1, _2, _3, reason) => shouldContinueCallback());
        }

        /// <summary>
        /// Create a callback new callback from a function that takes no parameters and returns a CallBack result. 
        /// <br/>The Cancellation token takes priority over the callback.
        /// </summary>
        /// <param name="shouldContinueCallback">The callback to wrap</param>
        /// <param name="token">The token. If cancelled, the <paramref name="shouldContinueCallback"/> will not be executed and <see cref="CopyProgressCallbackResult.CANCEL"/> will be returned.</param>
        /// <returns>A new <see cref="CopyProgressCallback"/></returns>
        public static CopyProgressCallback CreateCallback(Func<CopyProgressCallbackResult> shouldContinueCallback, CancellationToken token)
        {
            if (!token.CanBeCanceled) return CreateCallback(shouldContinueCallback);
            return GetResult;
            CopyProgressCallbackResult GetResult(long tfs, long bc, long _1, long _2, uint _3, CopyProgressCallbackReason reason)
            {
                return token.IsCancellationRequested ? CopyProgressCallbackResult.CANCEL : shouldContinueCallback();
            }
        }

        /// <inheritdoc cref="CreateCallback(BytesTransferredCallback, CancellationToken)"/>
        public static CopyProgressCallback CreateCallback(BytesTransferredCallback callback) => CreateCallback(callback, CancellationToken.None);

        /// <summary>
        /// Wrap a callback with the provided <paramref name="token"/> to determine if cancellation is required
        /// </summary>
        /// <param name="callback">The callback with a signature of : <br/><code>Func&lt;<see langword="long"/> totalFileSize, <see langword="long"/> bytesTransfered, <see cref="CopyProgressCallbackResult"/>&gt;</code></param>
        /// <param name="token">Token used to determine if the copy operation should be cancelled.</param>
        /// <returns>A new <see cref="CopyProgressCallback"/></returns>
        public static CopyProgressCallback CreateCallback(BytesTransferredCallback callback, CancellationToken token)
        {
            return GetResult;
            CopyProgressCallbackResult GetResult(long tfs, long bc, long _1, long _2, uint _3, CopyProgressCallbackReason reason)
            {
                var result = callback?.Invoke(tfs, bc);
                if (token.IsCancellationRequested)
                    result = CopyProgressCallbackResult.CANCEL;
                return result ?? CopyProgressCallbackResult.CONTINUE;
            }
        }

        /// <summary>
        /// Create a new CopyProgressCallback
        /// </summary>
        /// <param name="action">The action to perform. First parameter is total file size, second parameter is number of bytes copied.</param>
        /// <param name="token">Token used to determine if the copy operation should be cancelled.</param>
        /// <returns>A new <see cref="CopyProgressCallback"/></returns>
        public static CopyProgressCallback CreateCallback(Action<long, long> action, CancellationToken token)
        {
            if (action is null && !token.CanBeCanceled) return null;
            if (action is null) return token.CanBeCanceled ? CreateCallback(token) : null;
            return GetResult;
            CopyProgressCallbackResult GetResult(long tfs, long bc, long _1, long _2, uint _3, CopyProgressCallbackReason reason)
            {
                action(tfs, bc);
                return token.IsCancellationRequested ? CopyProgressCallbackResult.CANCEL : CopyProgressCallbackResult.CONTINUE;
            }
        }

        /// <summary>
        /// Create a new callback that will calculate the current progress and report it to the <paramref name="progress"/> object
        /// </summary>
        /// <param name="progress">The object to report progress to</param>
        /// <param name="token">Token used to determine if the copy operation should be cancelled.</param>
        /// <returns>A new <see cref="CopyProgressCallback"/></returns>
        public static CopyProgressCallback CreateCallback(IProgress<double> progress, CancellationToken token = default)
        {
            if (progress is null && !token.CanBeCanceled) return null;
            if (progress is null) return token.CanBeCanceled ? CreateCallback(token) : null;
            return report;

            CopyProgressCallbackResult report(long total, long processed, long _1, long _2, uint _3, CopyProgressCallbackReason reason)
            {
                progress.Report((double)100 * processed / total);
                return token.IsCancellationRequested ? CopyProgressCallbackResult.CANCEL : CopyProgressCallbackResult.CONTINUE;
            }
        }
    }
}
