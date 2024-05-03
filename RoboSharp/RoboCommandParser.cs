using RoboSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static RoboSharp.RoboCommandParserFunctions;

namespace RoboSharp
{
    /// <summary>
    /// Factory class used to parse a string that represents a Command Line call to robocommand, and return a command with those parameters.
    /// </summary>
    public static class RoboCommandParser
    {
        /// <summary>Attempt the parse the <paramref name="command"/> into a new IRoboCommand object</summary>
        /// <returns>True if successful, otherwise false</returns>
        /// <param name="result">If successful, a new IRobocommand, otherwise null</param>
        /// <param name="factory">The factory used to generate the robocommand. <br/>If not specified, uses <see cref="RoboCommandFactory.Default"/></param>
        /// <inheritdoc cref="Parse(string, IRoboCommandFactory)"/>
        /// <param name="command"/>
        public static bool TryParse(string command, out IRoboCommand result, IRoboCommandFactory factory = default)
        {
            try
            {
                result = Parse(command, factory ?? RoboCommandFactory.Default);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        /// <summary>Attempt the parse the <paramref name="commandOptions"/> into a new IRoboCommand object</summary>
        /// <returns>True if successful, otherwise false</returns>
        /// <param name="result">If successful, a new IRobocommand, otherwise null</param>
        /// <inheritdoc cref="ParseOptions(string, IRoboCommandFactory)"/>
        /// <param name="commandOptions"/><param name="factory"/>
        public static bool TryParseOptions(string commandOptions, out IRoboCommand result, IRoboCommandFactory factory = default)
        {
            try
            {
                result = ParseOptions(commandOptions, factory ?? RoboCommandFactory.Default);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        /// <returns>A new <see cref="RoboCommand"/></returns>
        /// <inheritdoc cref="Parse(string, Interfaces.IRoboCommandFactory)"/>
        public static IRoboCommand Parse(string command) => Parse(command, RoboCommandFactory.Default);

        /// <summary>
        /// Parse the <paramref name="command"/> text into a new IRoboCommand.
        /// </summary>
        /// <param name="command">The Command-Line string of options to parse. <br/>Example:  robocopy "C:\source" "D:\destination" *.pdf /xc /copyall </param>
        /// <param name="factory">The factory used to generate the robocommand</param>
        /// <returns>A new IRoboCommand object generated from the <paramref name="factory"/></returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="RoboCommandParserException"/>
        public static IRoboCommand Parse(string command, IRoboCommandFactory factory)
        {
            if (string.IsNullOrWhiteSpace(command)) throw new ArgumentException("Input string is null or empty!", nameof(command));
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            Debugger.Instance.DebugMessage($"RoboCommandParser.Parse - Begin parsing input string : {command}");
                        
            // Trim robocopy.exe from the beginning of the string, then extract the source/destination.
            string commandText = TrimRobocopy(command);
            ParsedSourceDest paths = ParsedSourceDest.Parse(commandText);

            // Filters SHOULD be immediately following the source/destination string at the very beginning of the text
            // Also Ensure white space at end of string because all constants have it
            paths.SanitizedString.TrimStart().TrimStart("\"*.*\"").Trim().TrimStart("*.*").TrimStart(); // Remove the DEFAULT FILTER wildcard from the text
            var roboCommand = ParseOptionsInternal(paths, factory);
            Debugger.Instance.DebugMessage("RoboCommandParser.Parse completed successfully.\n");
            return roboCommand;
        }

        /// <summary>
        /// Parse a string of text that represents a set of robocopy options into a new IRoboCommand. Source and Destination values are ignored.
        /// </summary>
        /// <param name="commandOptions">The robocopy options to parse. Must not contain the phrase 'robocopy'. Must also not contain source/destination info.</param>
        /// <param name="factory">The factory used to generate the robocommand. <br/>If not specified, uses <see cref="RoboCommandFactory.Default"/></param>
        /// <returns>An IRoboCommand that represents the specified options</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="RoboCommandParserException"/>
        public static IRoboCommand ParseOptions(string commandOptions, IRoboCommandFactory factory = default)
        {
            // Sanity Checks to ensure correct method is being utilized:
            if (string.IsNullOrWhiteSpace(commandOptions)) throw new RoboCommandParserException("Input string is null or empty!", nameof(commandOptions));
            Debugger.Instance.DebugMessage($"RoboCommandParser.ParseOptions - Begin parsing input string : {commandOptions}");
            var roboCommand = ParseOptionsInternal(ParsedSourceDest.ParseOptionsOnly(commandOptions), factory ?? RoboCommandFactory.Default);
            Debugger.Instance.DebugMessage("RoboCommandParser.ParseOptions completed successfully.\n");
            return roboCommand;
        }

        /// <summary> Parse the options text into a new IRoboCommand object. </summary>
        /// <param name="factory"/><param name="sourceDest">struct containing the source/destination data to pass into the factory</param>
        private static IRoboCommand ParseOptionsInternal(ParsedSourceDest sourceDest, IRoboCommandFactory factory)
        {
            StringBuilder sanitizedCmd = sourceDest.SanitizedString.Trim().Append(' ');
            var filters = RoboCommandParserFunctions.ExtractFileFilters(sanitizedCmd);

            // Get the command
            var roboCommand = factory.GetRoboCommand(sourceDest.Source, sourceDest.Destination, ParseCopyFlags(sanitizedCmd), ParseSelectionFlags(sanitizedCmd));

            // apply the file filters, if any were discovered
            if (filters.Any()) roboCommand.CopyOptions.AddFileFilter(filters);

            // apply the remaining options
            return roboCommand
                .ParseCopyOptions(sanitizedCmd)
                .ParseLoggingOptions(sanitizedCmd)
                .ParseSelectionOptions(sanitizedCmd)
                .ParseRetryOptions(sanitizedCmd);
        }

        #region < Copy Options Parsing >

        private static CopyActionFlags ParseCopyFlags(StringBuilder cmd)
        {
            CopyActionFlags flags = CopyActionFlags.Default;
            cmd.RemoveString(CopyOptions.NETWORK_COMPRESSION, () => flags |= CopyActionFlags.Compress);
            cmd.RemoveString(CopyOptions.COPY_SUBDIRECTORIES, () => flags |= CopyActionFlags.CopySubdirectories);
            cmd.RemoveString(CopyOptions.COPY_SUBDIRECTORIES_INCLUDING_EMPTY, () => flags |= CopyActionFlags.CopySubdirectoriesIncludingEmpty);
            cmd.RemoveString(CopyOptions.CREATE_DIRECTORY_AND_FILE_TREE, () => flags |= CopyActionFlags.CreateDirectoryAndFileTree);
            cmd.RemoveString(CopyOptions.MIRROR, () => flags |= CopyActionFlags.Mirror);
            cmd.RemoveString(CopyOptions.MOVE_FILES, () => flags |= CopyActionFlags.MoveFiles);
            cmd.RemoveString(CopyOptions.MOVE_FILES_AND_DIRECTORIES, () => flags |= CopyActionFlags.MoveFilesAndDirectories);
            cmd.RemoveString(CopyOptions.PURGE, () => flags |= CopyActionFlags.Purge);
            return flags;
        }

        /// <summary>
        /// Parse the Copy Options not discovered by ParseCopyFlags
        /// </summary>
        private static IRoboCommand ParseCopyOptions(this IRoboCommand roboCommand, StringBuilder command)
        {
            Debugger.Instance.DebugMessage($"Parsing Copy Options");
            var options = roboCommand.CopyOptions;

            options.CheckPerFile |= command.RemoveString(CopyOptions.CHECK_PER_FILE);
            options.CopyAll |= command.RemoveString(CopyOptions.COPY_ALL);
            options.CopyFilesWithSecurity |= command.RemoveString(CopyOptions.COPY_FILES_WITH_SECURITY);
            options.CopySymbolicLink |= command.RemoveString(CopyOptions.COPY_SYMBOLIC_LINK);
            options.DoNotCopyDirectoryInfo |= command.RemoveString(CopyOptions.DO_NOT_COPY_DIRECTORY_INFO);
            options.DoNotUseWindowsCopyOffload |= command.RemoveString(CopyOptions.DO_NOT_USE_WINDOWS_COPY_OFFLOAD);
            options.EnableBackupMode |= command.RemoveString(CopyOptions.ENABLE_BACKUP_MODE);
            options.EnableEfsRawMode |= command.RemoveString(CopyOptions.ENABLE_EFSRAW_MODE);
            options.EnableRestartMode |= command.RemoveString(CopyOptions.ENABLE_RESTART_MODE);
            options.EnableRestartModeWithBackupFallback |= command.RemoveString(CopyOptions.ENABLE_RESTART_MODE_WITH_BACKUP_FALLBACK);
            options.FatFiles |= command.RemoveString(CopyOptions.FAT_FILES);
            options.FixFileSecurityOnAllFiles |= command.RemoveString(CopyOptions.FIX_FILE_SECURITY_ON_ALL_FILES);
            options.FixFileTimesOnAllFiles |= command.RemoveString(CopyOptions.FIX_FILE_TIMES_ON_ALL_FILES);
            options.RemoveFileInformation |= command.RemoveString(CopyOptions.REMOVE_FILE_INFORMATION);
            options.TurnLongPathSupportOff |= command.RemoveString(CopyOptions.TURN_LONG_PATH_SUPPORT_OFF);
            options.UseUnbufferedIo |= command.RemoveString(CopyOptions.USE_UNBUFFERED_IO);

            // Non-Boolean Options

            if (TryExtractParameter(command, CopyOptions.ADD_ATTRIBUTES, out string param))
            {
                options.AddAttributes = param;
            }

            _ = TryExtractParameter(command, CopyOptions.COPY_FLAGS, out param); // Always set this value
            options.CopyFlags = param;
            
            if (TryExtractParameter(command, CopyOptions.DEPTH, out param) && int.TryParse(param, out int value))
            {
                options.Depth = value;
            }
            
            _ = TryExtractParameter(command, CopyOptions.DIRECTORY_COPY_FLAGS, out param); // Always set this value
            options.DirectoryCopyFlags = param;

            if (TryExtractParameter(command, CopyOptions.INTER_PACKET_GAP, out param) && int.TryParse(param, out value))
            {
                options.InterPacketGap = value;
            }
            if (TryExtractParameter(command, CopyOptions.MONITOR_SOURCE_CHANGES_LIMIT, out param) && int.TryParse(param, out value))
            {
                options.MonitorSourceChangesLimit = value;
            }
            if (TryExtractParameter(command, CopyOptions.MONITOR_SOURCE_TIME_LIMIT, out param) && int.TryParse(param, out value))
            {
                options.MonitorSourceTimeLimit = value;
            }
            if (TryExtractParameter(command, CopyOptions.MULTITHREADED_COPIES_COUNT, out param) && int.TryParse(param, out value))
            {
                options.MultiThreadedCopiesCount = value;
            }
            if (TryExtractParameter(command, CopyOptions.REMOVE_ATTRIBUTES, out param))
            {
                options.RemoveAttributes = param;
            }
            if (TryExtractParameter(command, CopyOptions.RUN_HOURS, out param) && CopyOptions.IsRunHoursStringValid(param))
            {
                options.RunHours = param;
            }
            return roboCommand;
        }

        #endregion

        #region < Selection Options Parsing  >
        private static SelectionFlags ParseSelectionFlags(StringBuilder cmd)
        {
            SelectionFlags flags = SelectionFlags.Default;
            cmd.RemoveString(SelectionOptions.EXCLUDE_CHANGED, () => flags |= SelectionFlags.ExcludeChanged);
            cmd.RemoveString(SelectionOptions.EXCLUDE_EXTRA, () => flags |= SelectionFlags.ExcludeExtra);
            cmd.RemoveString(SelectionOptions.EXCLUDE_JUNCTION_POINTS, () => flags |= SelectionFlags.ExcludeJunctionPoints);
            cmd.RemoveString(SelectionOptions.EXCLUDE_JUNCTION_POINTS_FOR_DIRECTORIES, () => flags |= SelectionFlags.ExcludeJunctionPointsForDirectories);
            cmd.RemoveString(SelectionOptions.EXCLUDE_JUNCTION_POINTS_FOR_FILES, () => flags |= SelectionFlags.ExcludeJunctionPointsForFiles);
            cmd.RemoveString(SelectionOptions.EXCLUDE_LONELY, () => flags |= SelectionFlags.ExcludeLonely);
            cmd.RemoveString(SelectionOptions.EXCLUDE_NEWER, () => flags |= SelectionFlags.ExcludeNewer);
            cmd.RemoveString(SelectionOptions.EXCLUDE_OLDER, () => flags |= SelectionFlags.ExcludeOlder);
            cmd.RemoveString(SelectionOptions.INCLUDE_SAME, () => flags |= SelectionFlags.IncludeSame);
            cmd.RemoveString(SelectionOptions.INCLUDE_TWEAKED, () => flags |= SelectionFlags.IncludeTweaked);
            cmd.RemoveString(SelectionOptions.INCLUDE_MODIFIED, () => flags |= SelectionFlags.IncludeModified);
            cmd.RemoveString(SelectionOptions.ONLY_COPY_ARCHIVE_FILES, () => flags |= SelectionFlags.OnlyCopyArchiveFiles);
            cmd.RemoveString(SelectionOptions.ONLY_COPY_ARCHIVE_FILES_AND_RESET_ARCHIVE_FLAG, () => flags |= SelectionFlags.OnlyCopyArchiveFilesAndResetArchiveFlag);
            return flags;
        }

        /// <summary>
        /// Parse the Selection Options not discovered by ParseSelectionFlags
        /// </summary>
        private static IRoboCommand ParseSelectionOptions(this IRoboCommand roboCommand, StringBuilder command)
        {
            Debugger.Instance.DebugMessage($"Parsing Selection Options");
            var options = roboCommand.SelectionOptions;
            options.CompensateForDstDifference |= command.RemoveString(SelectionOptions.COMPENSATE_FOR_DST_DIFFERENCE);
            options.UseFatFileTimes |= command.RemoveString(SelectionOptions.USE_FAT_FILE_TIMES);

            if (TryExtractParameter(command, SelectionOptions.INCLUDE_ATTRIBUTES, out string param))
            {
                options.IncludeAttributes = param;
            }
            if (TryExtractParameter(command, SelectionOptions.EXCLUDE_ATTRIBUTES, out param))
            {
                options.ExcludeAttributes = param;
            }
            if (TryExtractParameter(command, SelectionOptions.MAX_FILE_AGE, out param))
            {
                options.MaxFileAge = param;
            }
            if (TryExtractParameter(command, SelectionOptions.MAX_FILE_SIZE, out param) && long.TryParse(param, out var value))
            {
                options.MaxFileSize = value;
            }
            if (TryExtractParameter(command, SelectionOptions.MIN_FILE_AGE, out param))
            {
                options.MinFileAge = param;
            }
            if (TryExtractParameter(command, SelectionOptions.MIN_FILE_SIZE, out param) && long.TryParse(param, out value))
            {
                options.MinFileSize = value;
            }
            if (TryExtractParameter(command, SelectionOptions.MAX_LAST_ACCESS_DATE, out param))
            {
                options.MaxLastAccessDate = param;
            }
            if (TryExtractParameter(command, SelectionOptions.MIN_LAST_ACCESS_DATE, out param))
            {
                options.MinLastAccessDate = param;
            }

            options.ExcludedDirectories.AddRange(RoboCommandParserFunctions.ExtractExclusionDirectories(command));
            options.ExcludedFiles.AddRange(RoboCommandParserFunctions.ExtractExclusionFiles(command));

            return roboCommand;
        }

        #endregion

        private static IRoboCommand ParseLoggingOptions(this IRoboCommand roboCommand, StringBuilder command)
        {
            Debugger.Instance.DebugMessage($"Parsing Logging Options");
            var options = roboCommand.LoggingOptions;

            options.IncludeFullPathNames |= command.RemoveString(LoggingOptions.INCLUDE_FULL_PATH_NAMES);
            options.IncludeSourceTimeStamps |= command.RemoveString(LoggingOptions.INCLUDE_SOURCE_TIMESTAMPS);
            options.ListOnly |= command.RemoveString(LoggingOptions.LIST_ONLY);
            options.NoDirectoryList |= command.RemoveString(LoggingOptions.NO_DIRECTORY_LIST);
            options.NoFileClasses |= command.RemoveString(LoggingOptions.NO_FILE_CLASSES);
            options.NoFileList |= command.RemoveString(LoggingOptions.NO_FILE_LIST);
            options.NoFileSizes |= command.RemoveString(LoggingOptions.NO_FILE_SIZES);
            options.NoJobHeader |= command.RemoveString(LoggingOptions.NO_JOB_HEADER);
            options.NoJobSummary |= command.RemoveString(LoggingOptions.NO_JOB_SUMMARY);
            options.NoProgress |= command.RemoveString(LoggingOptions.NO_PROGRESS);
            options.OutputAsUnicode |= command.RemoveString(LoggingOptions.OUTPUT_AS_UNICODE);
            options.OutputToRoboSharpAndLog |= command.RemoveString(LoggingOptions.OUTPUT_TO_ROBOSHARP_AND_LOG);
            options.PrintSizesAsBytes |= command.RemoveString(LoggingOptions.PRINT_SIZES_AS_BYTES);
            options.ReportExtraFiles |= command.RemoveString(LoggingOptions.REPORT_EXTRA_FILES);
            options.ShowEstimatedTimeOfArrival |= command.RemoveString(LoggingOptions.SHOW_ESTIMATED_TIME_OF_ARRIVAL   );
            options.VerboseOutput |= command.RemoveString(LoggingOptions.VERBOSE_OUTPUT);

            options.LogPath = ExtractLogPath(LoggingOptions.LOG_PATH, command);
            options.AppendLogPath = ExtractLogPath(LoggingOptions.APPEND_LOG_PATH, command);
            options.UnicodeLogPath = ExtractLogPath(LoggingOptions.UNICODE_LOG_PATH, command);
            options.AppendUnicodeLogPath = ExtractLogPath(LoggingOptions.APPEND_UNICODE_LOG_PATH, command);
            
            return roboCommand;

            static string ExtractLogPath(string filter, StringBuilder input)
            {
                if (TryExtractParameter(input, filter, out string path))
                {
                    return path.Trim('\"');
                }
                return string.Empty;
            }
        }

        private static IRoboCommand ParseRetryOptions(this IRoboCommand roboCommand, StringBuilder command)
        {
            Debugger.Instance.DebugMessage($"Parsing Retry Options");
            var options = roboCommand.RetryOptions;
            
            options.SaveToRegistry |= command.RemoveString(RetryOptions.SAVE_TO_REGISTRY);
            options.WaitForSharenames |= command.RemoveString(RetryOptions.WAIT_FOR_SHARENAMES);

            if (TryExtractParameter(command, RetryOptions.RETRY_COUNT, out string param) && int.TryParse(param, out int value))
            {
                options.RetryCount = value;
            }
            if (TryExtractParameter(command, RetryOptions.RETRY_WAIT_TIME, out param) && int.TryParse(param, out value))
            {
                options.RetryWaitTime = value;
            }
            return roboCommand;
        }
    }
}
