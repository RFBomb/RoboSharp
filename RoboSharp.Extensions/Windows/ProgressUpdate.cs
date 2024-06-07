using System;
using System.Collections.Generic;
using System.Text;

namespace RoboSharp.Extensions.Windows
{
    /// <summary>
    /// Provides details about a copy operation's progress. For use with <see cref="IProgress{T}"/>
    /// </summary>
    public readonly struct ProgressUpdate
    {
        /// <summary>
        /// The default constructor - Calculates the Progress
        /// </summary>
        /// <param name="fileSize"></param>
        /// <param name="bytesCopied"></param>
        public ProgressUpdate(long fileSize, long bytesCopied)
        {
            TotalBytes = fileSize;
            BytesCopied = bytesCopied;
            if (TotalBytes == bytesCopied)
                Progress = 100;
            else if (fileSize > bytesCopied)
                Progress = (double)100 * bytesCopied / fileSize;
            else
                Progress = 0;
        }

        /// <summary>
        /// The current progress expressed as a percentage
        /// </summary>
        public double Progress { get; }
        
        /// <summary>
        /// The file size being copied
        /// </summary>
        public long TotalBytes { get; }

        /// <summary>
        /// The number of bytes copied
        /// </summary>
        public long BytesCopied { get; }
    }
}
