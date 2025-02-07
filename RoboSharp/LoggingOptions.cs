﻿using System;
using System.Text;

namespace RoboSharp
{
    /// <summary>
    /// Options related to the output logs generated by RoboCopy
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/tjscience/RoboSharp/wiki/LoggingOptions"/>
    /// </remarks>
    public class LoggingOptions : ICloneable
    {
        #region Constructors 

        static LoggingOptions() { _ = ApplicationConstants.Initializer; }

        /// <summary>
        /// Create new LoggingOptions with Default Settings
        /// </summary>
        public LoggingOptions(LoggingFlags flags = LoggingFlags.RoboSharpDefault) 
        {
            ApplyLoggingFlags(flags |= LoggingFlags.RoboSharpDefault);
        }

        /// <summary>
        /// Clone a LoggingOptions Object
        /// </summary>
        /// <param name="options">LoggingOptions object to clone</param>
        public LoggingOptions(LoggingOptions options)
        {
            ListOnly = options.ListOnly;
            ReportExtraFiles = options.ReportExtraFiles;
            VerboseOutput = options.VerboseOutput;
            IncludeSourceTimeStamps = options.IncludeSourceTimeStamps;
            IncludeFullPathNames = options.IncludeFullPathNames;
            PrintSizesAsBytes = options.PrintSizesAsBytes;
            NoFileSizes = options.NoFileSizes;
            NoFileClasses = options.NoFileClasses;
            NoFileList = options.NoFileList;
            NoDirectoryList = options.NoDirectoryList;
            NoProgress = options.NoProgress;
            ShowEstimatedTimeOfArrival = options.ShowEstimatedTimeOfArrival;
            LogPath = options.LogPath;
            AppendLogPath = options.AppendLogPath;
            UnicodeLogPath = options.UnicodeLogPath;
            AppendUnicodeLogPath = options.AppendUnicodeLogPath;
            OutputToRoboSharpAndLog = options.OutputToRoboSharpAndLog;
            NoJobHeader = options.NoJobHeader;
            NoJobSummary = options.NoJobSummary;
            OutputAsUnicode = options.OutputAsUnicode;

        }

        /// <inheritdoc cref="LoggingOptions.LoggingOptions(LoggingOptions)"/>
        public LoggingOptions Clone() => new LoggingOptions(this);

        object ICloneable.Clone() => Clone();

        #endregion

        internal const string LIST_ONLY = "/L ";
        internal const string REPORT_EXTRA_FILES = "/X ";
        internal const string VERBOSE_OUTPUT = "/V ";
        internal const string INCLUDE_SOURCE_TIMESTAMPS = "/TS ";
        internal const string INCLUDE_FULL_PATH_NAMES = "/FP ";
        internal const string PRINT_SIZES_AS_BYTES = "/BYTES ";
        internal const string NO_FILE_SIZES = "/NS ";
        internal const string NO_FILE_CLASSES = "/NC ";
        internal const string NO_FILE_LIST = "/NFL ";
        internal const string NO_DIRECTORY_LIST = "/NDL ";
        internal const string NO_PROGRESS = "/NP ";
        internal const string SHOW_ESTIMATED_TIME_OF_ARRIVAL = "/ETA ";
        internal const string LOG_PATH = "/LOG:{0} ";
        internal const string APPEND_LOG_PATH = "/LOG+:{0} ";
        internal const string UNICODE_LOG_PATH = "/UNILOG:{0} ";
        internal const string APPEND_UNICODE_LOG_PATH = "/UNILOG+:{0} ";
        internal const string OUTPUT_TO_ROBOSHARP_AND_LOG = "/TEE ";
        internal const string NO_JOB_HEADER = "/NJH ";
        internal const string NO_JOB_SUMMARY = "/NJS ";
        internal const string OUTPUT_AS_UNICODE = "/UNICODE ";
        
        #region < Properties >

        /// <summary>
        /// Do not copy, timestamp or delete any files.
        /// [/L]
        /// </summary>
        public virtual bool ListOnly { get; set; }
        /// <summary>
        /// Report all extra files, not just those selected.
        /// [X]
        /// </summary>
        public virtual bool ReportExtraFiles { get; set; }
        /// <summary>
        /// Produce verbose output, showing skipped files.
        /// [V]
        /// </summary>
        /// <remarks>
        /// If set false, RoboCommand ProgressEstimator will not be accurate due files not showing in the logs.
        /// </remarks>
        public virtual bool VerboseOutput { get; set; } = true;
        /// <summary>
        /// Include source file time stamps in the output.
        /// [/TS]
        /// </summary>
        public virtual bool IncludeSourceTimeStamps { get; set; }
        /// <summary>
        /// Include full path names of files in the output.
        /// [/FP]
        /// </summary>
        public virtual bool IncludeFullPathNames { get; set; }
        /// <summary>
        /// Print sizes as bytes in the output.
        /// [/BYTES]
        /// </summary>
        /// <remarks>
        /// Automatically appended by the base RoboCommand object to allow results to work properly
        /// </remarks>
        public virtual bool PrintSizesAsBytes { get; set; }
        /// <summary>
        /// Do not log file sizes.
        /// [/NS]
        /// </summary>
        public virtual bool NoFileSizes { get; set; }
        /// <summary>
        /// Do not log file classes.
        /// [/NC]
        /// </summary>
        public virtual bool NoFileClasses { get; set; }
        /// <summary>
        /// Do not log file names.
        /// [/NFL]
        /// WARNING: If this is set to TRUE then GUI cannot handle showing progress correctly as it can't get information it requires from the log
        /// </summary>
        public virtual bool NoFileList { get; set; }
        /// <summary>
        /// Do not log directory names.
        /// [/NDL]
        /// </summary>
        public virtual bool NoDirectoryList { get; set; }
        /// <summary>
        /// Do not log percentage copied.
        /// [/NP]
        /// </summary>
        public virtual bool NoProgress { get; set; }
        /// <summary>
        /// Show estimated time of arrival of copied files.
        /// [/ETA]
        /// </summary>
        public virtual bool ShowEstimatedTimeOfArrival { get; set; }
        /// <summary>
        /// Output status to LOG file (overwrite existing log).
        /// [/LOG:file]
        /// </summary>
        public virtual string LogPath { get; set; }
        /// <summary>
        /// Output status to LOG file (append to existing log).
        /// [/LOG+:file]
        /// </summary>
        public virtual string AppendLogPath { get; set; }
        /// <summary>
        /// Output status to LOG file as UNICODE (overwrite existing log).
        /// [/UNILOG:file]
        /// </summary>
        public virtual string UnicodeLogPath { get; set; }
        /// <summary>
        /// Output status to LOG file as UNICODE (append to existing log).
        /// [/UNILOG+:file]
        /// </summary>
        public virtual string AppendUnicodeLogPath { get; set; }
        /// <summary>
        /// Output to RoboSharp and Log.
        /// [/TEE]
        /// </summary>
        public virtual bool OutputToRoboSharpAndLog { get; set; }
        /// <summary>
        /// Do not output a Job Header.
        /// [/NJH]
        /// </summary>
        public virtual bool NoJobHeader { get; set; }
        /// <summary>
        /// Do not output a Job Summary.
        /// [/NJS]
        /// WARNING: If this is set to TRUE then statistics will not work correctly as this information is gathered from the job summary part of the log 
        /// </summary>
        public virtual bool NoJobSummary { get; set; }
        /// <summary>
        /// Output as UNICODE.
        /// [/UNICODE]
        /// </summary>
        public virtual bool OutputAsUnicode { get; set; }
        
        #endregion

        #region < Flags >

        

        /// <summary>
        /// Set the Logging Options using the <paramref name="flags"/> <br/>
        /// </summary>
        /// <remarks>
        /// The <see cref="LoggingFlags.RoboSharpDefault"/> is able to be applied, but will not be removed here to retain functionality with the library. <br/>
        /// Removal of those options must be done explicitly vai the property
        /// </remarks>
        /// <param name="flags"></param>
        public void ApplyLoggingFlags(LoggingFlags flags)
        {
            this.IncludeFullPathNames = flags.HasFlag(LoggingFlags.IncludeFullPathNames);
            this.IncludeSourceTimeStamps = flags.HasFlag(LoggingFlags.IncludeSourceTimeStamps);
            this.ListOnly = flags.HasFlag(LoggingFlags.ListOnly);
            this.NoDirectoryList = flags.HasFlag(LoggingFlags.NoDirectoryList);
            this.NoFileClasses = flags.HasFlag(LoggingFlags.NoFileClasses);
            this.NoFileList = flags.HasFlag(LoggingFlags.NoFileList);
            this.NoFileSizes = flags.HasFlag(LoggingFlags.NoFileSizes);
            this.NoJobHeader = flags.HasFlag(LoggingFlags.NoJobHeader);
            this.NoJobSummary = flags.HasFlag(LoggingFlags.NoJobSummary);
            this.NoProgress = flags.HasFlag(LoggingFlags.NoProgress);
            this.OutputAsUnicode = flags.HasFlag(LoggingFlags.OutputAsUnicode);
            this.ReportExtraFiles = flags.HasFlag(LoggingFlags.ReportExtraFiles);
            this.ShowEstimatedTimeOfArrival = flags.HasFlag(LoggingFlags.ShowEstimatedTimeOfArrival);

            //RoboSharp Defaults
            if (flags.HasFlag(LoggingFlags.VerboseOutput)) this.VerboseOutput = true;
            if (flags.HasFlag(LoggingFlags.OutputToRoboSharpAndLog)) this.OutputToRoboSharpAndLog = true;
            if (flags.HasFlag(LoggingFlags.PrintSizesAsBytes)) this.PrintSizesAsBytes = true;
        }

        /// <summary>
        /// Converts the boolean values back into the <see cref="LoggingFlags"/> enum that represents the currently selected options
        /// </summary>
        /// <returns></returns>
        public LoggingFlags GetLoggingActionFlags()
        {
            LoggingFlags flags = LoggingFlags.None;
            if(IncludeFullPathNames) flags |= LoggingFlags.IncludeFullPathNames;
            if(IncludeSourceTimeStamps) flags |= LoggingFlags.IncludeSourceTimeStamps;
            if(ListOnly) flags |= LoggingFlags.ListOnly;
            if(NoDirectoryList) flags |= LoggingFlags.NoDirectoryList;
            if(NoFileClasses) flags |= LoggingFlags.NoFileClasses;
            if(NoFileList) flags |= LoggingFlags.NoFileList;
            if(NoFileSizes) flags |= LoggingFlags.NoFileSizes;
            if(NoJobHeader) flags |= LoggingFlags.NoJobHeader;
            if(NoJobSummary) flags |= LoggingFlags.NoJobSummary;
            if(NoProgress) flags |= LoggingFlags.NoProgress;
            if(OutputAsUnicode) flags |= LoggingFlags.OutputAsUnicode;
            if(ReportExtraFiles) flags |= LoggingFlags.ReportExtraFiles;
            if(ShowEstimatedTimeOfArrival) flags |= LoggingFlags.ShowEstimatedTimeOfArrival;
            if (PrintSizesAsBytes) flags |= LoggingFlags.PrintSizesAsBytes;
            if (OutputToRoboSharpAndLog) flags |= LoggingFlags.OutputToRoboSharpAndLog;
            if (VerboseOutput) flags |= LoggingFlags.VerboseOutput;
            return flags;
        }

        #endregion

        /// <summary> Encase the LogPath in quotes if needed </summary>
        internal static string WrapPath(string logPath) => (!logPath.StartsWith("\"") && logPath.Contains(" ")) ? $"\"{logPath}\"" : logPath;

        internal string Parse()
        {
            var options = new StringBuilder();

            if (ListOnly)
                options.Append(LIST_ONLY);
            if (ReportExtraFiles)
                options.Append(REPORT_EXTRA_FILES);
            if (VerboseOutput)
                options.Append(VERBOSE_OUTPUT);
            if (IncludeSourceTimeStamps)
                options.Append(INCLUDE_SOURCE_TIMESTAMPS);
            if (IncludeFullPathNames)
                options.Append(INCLUDE_FULL_PATH_NAMES);
            if (PrintSizesAsBytes)
                options.Append(PRINT_SIZES_AS_BYTES);
            if (NoFileSizes)
                options.Append(NO_FILE_SIZES);
            if (NoFileClasses)
                options.Append(NO_FILE_CLASSES);
            if (NoFileList)
                options.Append(NO_FILE_LIST);
            if (NoDirectoryList)
                options.Append(NO_DIRECTORY_LIST);
            if (NoProgress)
                options.Append(NO_PROGRESS);
            if (ShowEstimatedTimeOfArrival)
                options.Append(SHOW_ESTIMATED_TIME_OF_ARRIVAL);
            if (!LogPath.IsNullOrWhiteSpace())
                options.Append(string.Format(LOG_PATH, WrapPath(LogPath)));
            if (!AppendLogPath.IsNullOrWhiteSpace())
                options.Append(string.Format(APPEND_LOG_PATH, WrapPath(AppendLogPath)));
            if (!UnicodeLogPath.IsNullOrWhiteSpace())
                options.Append(string.Format(UNICODE_LOG_PATH, WrapPath(UnicodeLogPath)));
            if (!AppendUnicodeLogPath.IsNullOrWhiteSpace())
                options.Append(string.Format(APPEND_UNICODE_LOG_PATH, WrapPath(AppendUnicodeLogPath)));
            if (OutputToRoboSharpAndLog)
                options.Append(OUTPUT_TO_ROBOSHARP_AND_LOG);
            if (NoJobHeader)
                options.Append(NO_JOB_HEADER);
            if (NoJobSummary)
                options.Append(NO_JOB_SUMMARY);
            if (OutputAsUnicode)
                options.Append(OUTPUT_AS_UNICODE);

            return options.ToString();
        }

        /// <summary>
        /// Returns the Parsed Options as it would be applied to RoboCopy
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Parse();
        }

        /// <summary>
        /// Combine this object with another LoggingOptions object. <br/>
        /// Any properties marked as true take priority. IEnumerable items are combined. <br/>
        /// String Values will only be replaced if the primary object has a null/empty value for that property.
        /// </summary>
        /// <param name="options"></param>
        public void Merge(LoggingOptions options)
        {
            ListOnly |= options.ListOnly;
            ReportExtraFiles |= options.ReportExtraFiles;
            VerboseOutput |= options.VerboseOutput;
            IncludeSourceTimeStamps |= options.IncludeSourceTimeStamps;
            IncludeFullPathNames |= options.IncludeFullPathNames;
            PrintSizesAsBytes |= options.PrintSizesAsBytes;
            NoFileSizes |= options.NoFileSizes;
            NoFileClasses |= options.NoFileClasses;
            NoFileList |= options.NoFileList;
            NoDirectoryList |= options.NoDirectoryList;
            NoProgress |= options.NoProgress;
            ShowEstimatedTimeOfArrival |= options.ShowEstimatedTimeOfArrival;
            OutputToRoboSharpAndLog |= options.OutputToRoboSharpAndLog;
            NoJobHeader |= options.NoJobHeader;
            NoJobSummary |= options.NoJobSummary;
            OutputAsUnicode |= options.OutputAsUnicode;

            LogPath = LogPath.ReplaceIfEmpty(options.LogPath);
            AppendLogPath = AppendLogPath.ReplaceIfEmpty(options.AppendLogPath);
            UnicodeLogPath = UnicodeLogPath.ReplaceIfEmpty(options.UnicodeLogPath);
            AppendUnicodeLogPath = AppendUnicodeLogPath.ReplaceIfEmpty(options.AppendUnicodeLogPath);

        }
    }
}
