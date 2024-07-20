using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoboSharp.Interfaces;
using RoboSharp.Results;
using RoboSharp.EventArgObjects;
using static RoboSharp.Results.ProgressEstimator;
using RoboSharp.Extensions.Options;

namespace RoboSharp.Extensions.Helpers
{
    /// <summary>
    /// ResultsBuilder object for custom IRoboCommand implementations
    /// </summary>
    public class ResultsBuilder : IResultsBuilder, IDisposable
    {
        #region < Constructor >

        /// <inheritdoc cref="ResultsBuilder(IRoboCommand, ProgressEstimator, DateTime?)"/>
        public ResultsBuilder(IRoboCommand cmd) : this(cmd, new ProgressEstimator(cmd), DateTime.Now) { }

        /// <summary>
        /// Create a new  results builder
        /// </summary>
        /// <param name="calculator">a ProgressEstimator used to calculate the total number of files/directories</param>
        /// <param name="cmd">The associated IRoboCommand</param>
        /// <param name="startTime">The time the IRoboCommand was started. If not specified, uses DateTime.Now</param>
        public ResultsBuilder(IRoboCommand cmd, ProgressEstimator calculator, DateTime? startTime = null)
        {
            Command = cmd ?? throw new ArgumentNullException(nameof(cmd));
            ProgressEstimator = calculator ?? throw new ArgumentNullException(nameof(calculator)); ;
            StartTime = startTime ?? DateTime.Now;
            CreateHeader();
            Subscribe();
        }

        #endregion

        private bool _isLoggingHeaderOrSummary;
        private RoboCopyExitStatus _exitStatus;

        #region < Properties >

        /// <summary>
        /// The associated <see cref="IRoboCommand"/> object
        /// </summary>
        protected IRoboCommand Command { get; }

        /// <summary>
        /// The private collection of log lines
        /// </summary>
        protected List<string> LogLines { get; } = new List<string>();

        /// <summary>
        /// The collection of <see cref="ErrorEventArgs"/> generated by the command
        /// </summary>
        protected List<ErrorEventArgs> CommandErrors { get; } = new List<ErrorEventArgs>();

        /// <summary>
        /// Gets an array of all the log lines currently logged
        /// </summary>
        public string[] CurrentLogLines => LogLines.ToArray();

        /// <summary>
        /// Used to calculate the average speed, and is supplied to the results object when getting results.
        /// </summary>
        public AverageSpeedStatistic AverageSpeed { get; } = new AverageSpeedStatistic();

        /// <summary>
        /// The time the ResultsBuilder was instantiated
        /// </summary>
        public DateTime StartTime { get; }

        /// <summary>
        /// End Time is set when the summary is created.
        /// </summary>
        public DateTime EndTime { get; protected set; }

        /// <summary>
        /// Flag to prevent writing the summary to the log multiple times
        /// </summary>
        protected bool IsSummaryWritten { get; set; }

        /// <summary>
        /// The ProgressEstimator object that will be used to calculate the statistics objects
        /// </summary>
        public ProgressEstimator ProgressEstimator { get; }

        string IResultsBuilder.Source => Command.CopyOptions.Source;

        string IResultsBuilder.Destination => Command.CopyOptions.Destination;

        string IResultsBuilder.JobName => Command.Name;

        string IResultsBuilder.CommandOptions => Command.CommandOptions;

        IEnumerable<string> IResultsBuilder.LogLines => LogLines;

        IStatistic IResultsBuilder.BytesStatistic => ProgressEstimator.BytesStatistic;

        IStatistic IResultsBuilder.FilesStatistic => ProgressEstimator.FilesStatistic;

        IStatistic IResultsBuilder.DirectoriesStatistic => ProgressEstimator.DirectoriesStatistic;

        ISpeedStatistic IResultsBuilder.SpeedStatistic => AverageSpeed;

        RoboCopyExitStatus IResultsBuilder.ExitStatus => _exitStatus ?? new RoboCopyExitStatus(ProgressEstimator.GetExitCode());

        IEnumerable<ErrorEventArgs> IResultsBuilder.CommandErrors => CommandErrors;

        #endregion

        private void Subscribe()
        {
            Command.OnError += Command_OnError;
        }

        /// <summary>
        /// Unsubscribe from the associated IRoboCommand
        /// </summary>
        /// <remarks>Performed automatically when calling <see cref="Dispose"/></remarks>
        public void Unsubscribe()
        {
            Command.OnError -= Command_OnError;
        }

        private void Command_OnError(IRoboCommand sender, ErrorEventArgs e)
        {
            CommandErrors.Add(e);
        }

        /// <summary>
        /// Explicitly set the ExitStatus to report as part of the results
        /// </summary>
        public void SetExitStatus(RoboCopyExitStatus status)
        {
            _exitStatus = status;
        }

        /// <summary>
        /// Explicitly set the ExitStatus to report as part of the results
        /// </summary>
        public void SetExitStatus(RoboCopyExitCodes status)
        {
            _exitStatus = new RoboCopyExitStatus(status);
        }

        #region < Add Files >

        /// <inheritdoc cref="ProgressEstimator.AddFile(ProcessedFileInfo)"/>
        public virtual void AddFile(ProcessedFileInfo file)
        {
            ProgressEstimator.AddFile(file);
            LogFileInfo(file);
        }

        /// <summary>
        /// Mark an file as Copied
        /// </summary>
        /// <param name="file"></param>
        public virtual void AddFileCopied(ProcessedFileInfo file)
        {
            ProgressEstimator.AddFileCopied(file);
            LogFileInfo(file);
        }

        /// <summary>
        /// Mark an file as EXTRA
        /// </summary>
        /// <param name="file"></param>
        public virtual void AddFileExtra(ProcessedFileInfo file)
        {
            ProgressEstimator.AddFileExtra(file);
            LogFileInfo(file);
        }

        /// <summary>
        /// Mark an file as FAILED, then write the error description to the logs
        /// </summary>
        /// <param name="file">The file to mark as failed</param>
        /// <param name="ex">The exception that will be passed into <see cref="ProcessedFileInfo.ToStringFailed(IRoboCommand, Exception, DateTime?, string)"/> when generating the log line</param>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual void AddFileFailed(ProcessedFileInfo file, Exception ex = null)
        {
            if (file is null) throw new ArgumentNullException(nameof(file));
            ProgressEstimator.AddFileFailed(file);
            Print(file.ToStringFailed(Command, ex));
        }

        /// <summary>
        /// Mark a file as FAILED, and write all <paramref name="logLines"/> to the logs
        /// </summary>
        /// <param name="file">The file to mark as failed</param>
        /// <param name="logLines">A collection of log lines to be written to the logs.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public virtual void AddFileFailed(ProcessedFileInfo file, params string[] logLines)
        {
            if (file is null) throw new ArgumentNullException(nameof(file));
            if (logLines.All(c => string.IsNullOrWhiteSpace(c))) throw new ArgumentException("Must provide atleast 1 log line with error data");
            ProgressEstimator.AddFileFailed(file);
            Print(logLines);
        }

        /// <summary>
        /// Mark an file as PURGED ( EXTRA )
        /// </summary>
        /// <param name="file"></param>
        public virtual void AddFilePurged(ProcessedFileInfo file)
        {
            ProgressEstimator.AddFileExtra(file);
            LogFileInfo(file);
        }

        /// <summary>
        /// Mark an file as SKIPPED
        /// </summary>
        /// <param name="file"></param>
        public virtual void AddFileSkipped(ProcessedFileInfo file)
        {
            ProgressEstimator.AddFileSkipped(file);
            LogFileInfo(file);
        }

        /// <summary>
        /// Write the <paramref name="file"/> to the logs
        /// </summary>
        /// <param name="file"></param>
        protected virtual void LogFileInfo(ProcessedFileInfo file)
        {
            //Check to log the directory listing
            if (Command.LoggingOptions.NoFileList) return;
            WriteToLogs(file.ToString(Command.LoggingOptions));
        }

        /// <summary>
        /// Adds the file to the ProgressEstimator, then sets that the copy operation is started
        /// </summary>
        /// <param name="file"></param>
        public virtual void SetCopyOpStarted(ProcessedFileInfo file)
        {
            ProgressEstimator.AddFile(file);
            ProgressEstimator.SetCopyOpStarted();
        }

        #endregion

        #region < Add Dirs >

        private void LogDir(ProcessedFileInfo dir)
        {
            //Check to log the directory listing
            if (!Command.LoggingOptions.NoDirectoryList)
                WriteToLogs(dir.ToString(Command.LoggingOptions));
        }

        /// <summary>
        /// Process the first directory
        /// </summary>
        public void AddFirstDir(IProcessedDirectoryPair topLevelDirectory)
        {
            var info = topLevelDirectory.ProcessedFileInfo;
            if (topLevelDirectory.Destination.Exists)
                ProgressEstimator.AddDirSkipped(info);
            else
            {
                info.SetDirectoryClass(ProcessedDirectoryFlag.NewDir, Command.Configuration);
                ProgressEstimator.AddDirCopied(info);
            }
            LogDir(info);
        }

        /// <summary>
        /// Add a directory to the log and progressEstimator
        /// </summary>
        public virtual void AddDir(ProcessedFileInfo dir)
        {
            ProgressEstimator.AddDir(dir);
            LogDir(dir);
        }

        /// <summary>
        /// Add a directory to the log and progressEstimator
        /// </summary>
        public virtual void AddDirSkipped(ProcessedFileInfo dir)
        {
            ProgressEstimator.AddDirSkipped(dir);
            LogDir(dir);
        }

        #endregion

        #region < Add System Message >

        /// <summary>
        /// Adds a System Message to the logs
        /// </summary>
        /// <param name="info"></param>
        public virtual void AddSystemMessage(ProcessedFileInfo info) => AddSystemMessage(info?.FileClass);

        /// <summary>
        /// Adds a System Message to the logs
        /// </summary>
        /// <param name="info"></param>
        public virtual void AddSystemMessage(string info)
        {
            if (string.IsNullOrWhiteSpace(info)) return;
            lock (LogLines)
            {
                LogLines.Add(info);
                Command.LoggingOptions.AppendToLogs(info);
            }
        }

        #endregion

        #region < Create Header / Summary >

        /// <summary>
        /// Divider string that can be used
        /// </summary>
        public const string Divider = "------------------------------------------------------------------------------";

        /// <summary>
        /// RoboCopy uses padding of 9 on the header to align column details, such as the 'Started' time and 'Source' string
        /// </summary>
        /// <param name="RowName"></param>
        /// <returns>A padded string</returns>
        protected static string PadHeader(string RowName) => RowName.PadLeft(9);

        /// <summary>
        /// Write the header to the log - this is performed at time on construction of the object
        /// </summary>
        protected virtual void CreateHeader()
        {
            Command.LoggingOptions.DeleteLogFiles();
            if (!Command.LoggingOptions.NoJobHeader)
            {
                List<string> header = new List<string>(24)
                {
                    Divider,
                    $"\t      IRoboCommand : '{Command.GetType()}'",
                    $"\t   Results Builder : '{GetType()}'",
                    Divider,
                    "",
                    $"{PadHeader("Started")} : {StartTime.ToLongDateString()} {StartTime.ToLongTimeString()}",
                    $"{PadHeader("Source")} : {Command.CopyOptions.Source}",
                    $"{PadHeader("Dest")} : {Command.CopyOptions.Destination}",
                    ""
                };

                if (Command.CopyOptions.FileFilter.Any())
                    header.Add($"{PadHeader("Files")} : {string.Concat(Command.CopyOptions.FileFilter.Select(filter => filter + " "))}");
                else
                    header.Add($"{PadHeader("Files")} : *.*");
                header.Add("");

                if (Command.SelectionOptions.ExcludedFiles.Any())
                {
                    header.Add($"{PadHeader("Exc Files")} : {string.Concat(Command.SelectionOptions.ExcludedFiles.Select(filter => filter + " "))}");
                    header.Add("");
                }

                if (Command.SelectionOptions.ExcludedDirectories.Any())
                {
                    header.Add($"{PadHeader("Exc Dirs")} : {string.Concat(Command.SelectionOptions.ExcludedDirectories.Select(filter => filter + " "))}");
                    header.Add("");
                }

                string parsedCopyOptions = Command.CopyOptions.Parse(true);
                string parsedSelectionOptions = Command.SelectionOptions.Parse(true);
                string parsedRetryOptions = Command.RetryOptions.ToString();
                string parsedLoggingOptions = Command.LoggingOptions.ToString();
                string cmdOptions = string.Format("{0}{1}{2}{3}", parsedCopyOptions, parsedSelectionOptions, parsedRetryOptions, parsedLoggingOptions);

                header.Add($"{PadHeader("Options")} : {cmdOptions}");
                header.Add("");
                header.Add(Divider);
                header.Add("");

                Print(header.ToArray());
            }
            _isLoggingHeaderOrSummary = false;
        }

        /// <summary>
        /// Write the summary to the log
        /// </summary>
        protected virtual void CreateSummary()
        {
            if (Command.LoggingOptions.NoJobSummary) return;

            int[] GetColumnSizes()
            {
                var sizes = new List<int>();
                int GetColumnSize(string name, long bytes, long files, long dirs)
                {
                    int GetLargerValue(int length1, int length2) => length1 > length2 ? length1 : length2;
                    int length = GetLargerValue(name.Length, bytes.ToString().Length);
                    length = GetLargerValue(length, files.ToString().Length);
                    return GetLargerValue(length, dirs.ToString().Length);
                }
                sizes.Add(GetColumnSize("Total", ProgressEstimator.BytesStatistic.Total, ProgressEstimator.FilesStatistic.Total, ProgressEstimator.DirectoriesStatistic.Total));
                sizes.Add(GetColumnSize("Copied", ProgressEstimator.BytesStatistic.Copied, ProgressEstimator.FilesStatistic.Copied, ProgressEstimator.DirectoriesStatistic.Copied));
                sizes.Add(GetColumnSize("Skipped", ProgressEstimator.BytesStatistic.Skipped, ProgressEstimator.FilesStatistic.Skipped, ProgressEstimator.DirectoriesStatistic.Skipped));
                sizes.Add(GetColumnSize("Mismatch", ProgressEstimator.BytesStatistic.Mismatch, ProgressEstimator.FilesStatistic.Mismatch, ProgressEstimator.DirectoriesStatistic.Mismatch));
                sizes.Add(GetColumnSize("Failed", ProgressEstimator.BytesStatistic.Failed, ProgressEstimator.FilesStatistic.Failed, ProgressEstimator.DirectoriesStatistic.Failed));
                sizes.Add(GetColumnSize("Extras", ProgressEstimator.BytesStatistic.Extras, ProgressEstimator.FilesStatistic.Extras, ProgressEstimator.DirectoriesStatistic.Extras));
                return sizes.ToArray();
            }
            string RightAlign(int columnSize, string value)
            {
                return value.PadLeft(columnSize);
            }
            string Align(int columnSize, long value) => RightAlign(columnSize, value.ToString());

            int[] ColSizes = GetColumnSizes();
            string SummaryLine() => string.Format("    {0}{1}\t{2}\t{3}\t{4}\t{5}\t{6}", PadHeader(""), RightAlign(ColSizes[0], "Total"), RightAlign(ColSizes[1], "Copied"), RightAlign(ColSizes[2], "Skipped"), RightAlign(ColSizes[3], "Mismatch"), RightAlign(ColSizes[4], "FAILED"), RightAlign(ColSizes[5], "Extras"));
            string Tabulator(string name, IStatistic stat) => string.Format("{0} : {1}\t{2}\t{3}\t{4}\t{5}\t{6}", PadHeader(name), Align(ColSizes[0], stat.Total), Align(ColSizes[1], stat.Copied), Align(ColSizes[2], stat.Skipped), Align(ColSizes[3], stat.Mismatch), Align(ColSizes[4], stat.Failed), Align(ColSizes[5], stat.Extras));

            if (IsSummaryWritten) return;
            EndTime = DateTime.Now;

            if (!Command.LoggingOptions.NoJobSummary)
            {
                ProgressEstimator.FinalizeResults();
                TimeSpan totalTime = EndTime - StartTime;

                List<string> summary = new List<string>(20)
                {
                    "",
                    Divider,
                    "",
                    SummaryLine(),
                    Tabulator(" Dirs", ProgressEstimator.DirectoriesStatistic),
                    Tabulator("Files", ProgressEstimator.FilesStatistic),
                    Tabulator("Bytes", ProgressEstimator.BytesStatistic),
                    "",
                    $"\tEnded : {EndTime.ToLongDateString()} {EndTime.ToLongTimeString()}",
                    $"\tTotal Time: {totalTime.Hours} hours, {totalTime.Minutes} minutes, {totalTime.Seconds}.{totalTime.Milliseconds} seconds"
                };
                if (!Command.LoggingOptions.ListOnly)
                {
                    summary.Add("");
                    summary.Add($"\tSpeed: {AverageSpeed.GetBytesPerSecond()}");
                    summary.Add($"\tSpeed: {AverageSpeed.GetMegaBytesPerMin()}");
                }
                summary.Add("");
                summary.Add(Divider);
                summary.Add("");
                
                _isLoggingHeaderOrSummary = true;
                WriteToLogs(summary.ToArray());
            }
            _isLoggingHeaderOrSummary = false;
            IsSummaryWritten = true;
        }

        #endregion

        #region < Get Results / Write to Logs >

        /// <summary>
        /// Add the lines to the log lines, and also write it to the output logs
        /// </summary>
        /// <param name="lines"></param>
        protected virtual void WriteToLogs(params string[] lines)
        {
            if (!lines.Any()) return;
            lock (LogLines)
            {
                if (_isLoggingHeaderOrSummary || Command.Configuration.EnableFileLogging) LogLines.AddRange(lines);
                Command.LoggingOptions.AppendToLogs(lines);
            }
        }

        /// <summary>
        /// Write the <paramref name="logLines"/> to the logs
        /// </summary>
        public void Print(params string[] logLines)
        {
            bool oldVal = _isLoggingHeaderOrSummary;
            _isLoggingHeaderOrSummary = true;
            WriteToLogs(logLines);
            _isLoggingHeaderOrSummary=oldVal;
        }

        /// <summary>
        /// Get the results
        /// </summary>
        public virtual RoboCopyResults GetResults()
        {
            ProgressEstimator.FinalizeResults();
            CreateSummary();
            Unsubscribe();
            return RoboCopyResults.FromResultsBuilder(this);
        }

        /// <summary>
        /// Unsubscribe from the associated IRoboCommand
        /// </summary>
        public void Dispose()
        {
            Unsubscribe();
        }

        #endregion

    }
}
