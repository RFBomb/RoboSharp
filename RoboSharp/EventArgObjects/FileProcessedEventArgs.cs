using RoboSharp.Interfaces;
using System;
using System.IO;

// Do Not change NameSpace here! -> Must be RoboSharp due to prior releases
namespace RoboSharp
{
    /// <summary>
    /// <inheritdoc cref="ProcessedFileInfo"/>
    /// </summary>
    public class FileProcessedEventArgs : EventArgs
    {
        
        public static readonly FileProcessedEventArgs WhiteSpaceLogLine = new FileProcessedEventArgs(string.Empty);

        /// <inheritdoc cref="ProcessedFileInfo"/>
        public ProcessedFileInfo ProcessedFile { get; }

        /// <inheritdoc cref="EventArgs.EventArgs"/>
        public FileProcessedEventArgs(ProcessedFileInfo file)
        {
            ProcessedFile = file;
        }

        public FileProcessedEventArgs(string systemMessage)
        {
            ProcessedFile = new ProcessedFileInfo(systemMessage);
        }

        public FileProcessedEventArgs(FileInfo file, IRoboCommand command, ProcessedFileFlag status = ProcessedFileFlag.None)
        {
            ProcessedFile = new ProcessedFileInfo(file, command, status);
        }

        public FileProcessedEventArgs(DirectoryInfo dir, IRoboCommand command, long size = 0, ProcessedDirectoryFlag status = ProcessedDirectoryFlag.None)
        {
            ProcessedFile = new ProcessedFileInfo(dir, command, status, size);
        }
    }
}
