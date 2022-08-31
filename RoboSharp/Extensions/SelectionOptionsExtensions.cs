﻿using RoboSharp.Interfaces;
using RoboSharp.Extensions.SymbolicLinkSupport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RoboSharp.Extensions
{
    /// <summary>
    /// Extension Methods for selections options to assist with custom implementations
    /// </summary>
    public static partial class SelectionOptionsExtensions
    {

        /// <summary>
        /// Translate the wildcard pattern to a regex pattern for a file name
        /// </summary>
        /// <param name="pattern">
        /// <inheritdoc cref="SelectionOptions.ExcludedFiles" path="/summary" />
        /// </param>
        /// <returns>Translated the wildcard pattern to a regex pattern</returns>
        public static Regex SanitizeFileNameRegex(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern)) throw new ArgumentException("pattern is null or empty!");
            string sanitized = pattern.Replace(@"\", @"\\").Replace("/", @"\\");
            sanitized = sanitized.Replace(".", @"\.");
            sanitized = sanitized.Replace("*", ".*");
            sanitized = sanitized.Replace("?", ".");
            return new Regex($"^{sanitized}$");
        }

        /// <inheritdoc cref="ShouldCopyFile(IRoboCommand, FileInfo, FileInfo, ref Regex[], ref Regex[], out ProcessedFileInfo)"/>
        public static bool ShouldCopyFile(this IFileSourceDestinationPair pair, IRoboCommand command, ref Regex[] includeFileNameRegexCollection, ref Regex[] excludeFileNameRegexCollection, out ProcessedFileInfo info) 
            => ShouldCopyFile(command, pair, ref includeFileNameRegexCollection, ref excludeFileNameRegexCollection, out info);

        /// <inheritdoc cref="ShouldCopyFile(IRoboCommand, FileInfo, FileInfo, ref Regex[], ref Regex[], out ProcessedFileInfo)"/>
        public static bool ShouldCopyFile(this IRoboCommand command, FileInfo source, FileInfo destination, ref Regex[] includeFileNameRegexCollection, ref Regex[] excludeFileNameRegexCollection, out ProcessedFileInfo info) 
            => ShouldCopyFile(command, new FileSourceDestinationPair(source, destination), ref includeFileNameRegexCollection, ref excludeFileNameRegexCollection, out info);

        /// <summary>
        /// Evaluate RoboCopy Options of the command, the source, and destination and compute a ProcessedFileInfo object <br/>
        /// Ignores <see cref="LoggingOptions.ListOnly"/>
        /// </summary>
        /// <param name="command">the command to evaluate the options of</param>
        /// <param name="info">a ProcessedFileInfo object generated that reflects the output of this method</param>
        /// <param name="pair">the pair of Source/Destination to compare</param>
        /// <param name="excludeFileNameRegexCollection">cache the regex generated by <see cref="ShouldExcludeFileName(SelectionOptions, IFileSourceDestinationPair, ref Regex[])"/></param>
        /// <param name="includeFileNameRegexCollection">cache the regex generated by <see cref="CopyOptionsExtensions.ShouldIncludeFileName(CopyOptions, IFileSourceDestinationPair, ref Regex[])"/></param>
        /// <returns>TRUE if the file should be copied, FALSE if the file should be skiped</returns>
        public static bool ShouldCopyFile(this IRoboCommand command, 
            IFileSourceDestinationPair pair, 
            ref Regex[] includeFileNameRegexCollection, ref Regex[] excludeFileNameRegexCollection, 
            out ProcessedFileInfo info)
        {
            info = new ProcessedFileInfo()
            {
                FileClassType = FileClassType.File,
                Name = pair.Source.Name,
                Size = pair.Source.Length,
            };
            var SO = command.SelectionOptions;

            // Order of the following checks was done to allow what are likely the fastest checks to go first. More complex checks (such as DateTime parsing) are towards the bottom.

            //EXTRA
            if (IsExtra(pair))// SO.ShouldExcludeExtra(pair))
            {
                info.SetFileClass(FileClasses.ExtraFile, command.Configuration);
                info.Name = pair.Destination.Name;
                info.Size = pair.Destination.Length;
                return false;
            }
            //Lonely
            else if (SO.ShouldExcludeLonely(pair))
            {
                info.SetFileClass(FileClasses.ExtraFile, command.Configuration); // TO-DO: Does RoboCopy identify Lonely seperately? If so, we need a token for it!
            }
            //Exclude Newer
            else if (SO.ShouldExcludeNewer(pair))
            {
                info.SetFileClass(FileClasses.NewerFile, command.Configuration);
            }
            //Exclude Older
            else if (SO.ShouldExcludeOlder(pair))
            {
                info.SetFileClass(FileClasses.OlderFile, command.Configuration);
            }
            //MaxFileSize
            else if (SO.ShouldExcludeMaxFileSize(pair.Source.Length))
            {
                info.SetFileClass(FileClasses.MaxFileSizeExclusion, command.Configuration);
            }
            //MinFileSize
            else if (SO.ShouldExcludeMinFileSize(pair.Source.Length))
            {
                info.SetFileClass(FileClasses.MinFileSizeExclusion, command.Configuration);
            }
            //FileAttributes
            else if (!SO.ShouldIncludeAttributes(pair) ||  SO.ShouldExcludeFileAttributes(pair))
            {
                info.SetFileClass(FileClasses.AttribExclusion, command.Configuration);
            }
            //Max File Age
            else if (SO.ShouldExcludeMaxFileAge(pair))
            {
                info.SetFileClass(FileClasses.MaxAgeSizeExclusion, command.Configuration);
            }
            //Min File Age
            else if (SO.ShouldExcludeMinFileAge(pair))
            {
                info.SetFileClass(FileClasses.MinAgeSizeExclusion, command.Configuration);
            }
            //Max Last Access Date
            else if (SO.ShouldExcludeMaxLastAccessDate(pair))
            {
                info.SetFileClass(FileClasses.MaxAgeSizeExclusion, command.Configuration);
            }
            //Min Last Access Date
            else if (SO.ShouldExcludeMinLastAccessDate(pair))
            {
                info.SetFileClass(FileClasses.MinAgeSizeExclusion, command.Configuration); // TO-DO: Does RoboCopy iddentify Last Access Date exclusions seperately? If so, we need a token for it!
            }
            // Name Filters - These are last check since Regex will likely take the longest to evaluate
            if (!command.CopyOptions.ShouldIncludeFileName(pair.Source.Name, ref includeFileNameRegexCollection) || SO.ShouldExcludeFileName(pair.Source.Name, ref excludeFileNameRegexCollection))
            {
                info.SetFileClass(FileClasses.FileExclusion, command.Configuration);
            }
            else if (IsExtra(pair))
            {
                info.SetFileClass(FileClasses.ExtraFile, command.Configuration);
                return false; // Source doesn't exist
            }
            else
            {
                // Check for symbolic links
                bool xjf = ExcludeSymbolicFile(command.SelectionOptions, pair.Source); // TO-DO: Likely needs its own 'FileClass' set up for proper evaluation by ProgressEstimator

                // File passed all checks - It should be copied!
                if (IsLonely(pair))
                {
                    info.SetFileClass(FileClasses.NewFile, command.Configuration);
                    return !xjf && !command.SelectionOptions.ExcludeLonely;
                }
                else if (pair.IsSourceNewer())
                {
                    info.SetFileClass(FileClasses.NewerFile, command.Configuration);
                    return !xjf && !command.SelectionOptions.ExcludeNewer;
                }
                else if (pair.IsDestinationNewer())
                {
                    info.SetFileClass(FileClasses.OlderFile, command.Configuration);
                    return !xjf && !command.SelectionOptions.ExcludeOlder;
                }
                else
                {
                    info.SetFileClass(FileClasses.SameFile, command.Configuration);
                    return !xjf && command.SelectionOptions.IncludeSame;
                }
            }

            return false; // File failed one of the checks, do not copy.

        }

        #region < Should Exclude Newer >

        /// <summary> </summary>
        /// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        public static bool ShouldExcludeOlder(this SelectionOptions options, string source, string destination) => options.ExcludeOlder && ISourceDestinationPairExtensions.IsDestinationNewer(source, destination);
        
        /// <inheritdoc cref="ShouldExcludeOlder(SelectionOptions, string, string)"/>
        public static bool ShouldExcludeOlder(this SelectionOptions options, FileInfo source, FileInfo destination) => options.ExcludeOlder && ISourceDestinationPairExtensions.IsDestinationNewer(source, destination);

        /// <inheritdoc cref="ShouldExcludeOlder(SelectionOptions, FileInfo, FileInfo)"/>
        public static bool ShouldExcludeOlder(this SelectionOptions options, IFileSourceDestinationPair pair) => options.ExcludeOlder && pair.IsDestinationNewer();

        #endregion

        #region < Should Exclude Newer >

        /// <summary> </summary>
        /// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        public static bool ShouldExcludeNewer(this SelectionOptions options, string source, string destination) => options.ExcludeNewer && ISourceDestinationPairExtensions.IsSourceNewer(source, destination);
        
        /// <inheritdoc cref="ShouldExcludeNewer(SelectionOptions, string, string)"/>
        public static bool ShouldExcludeNewer(this SelectionOptions options, FileInfo source, FileInfo destination) => options.ExcludeNewer && ISourceDestinationPairExtensions.IsSourceNewer(source, destination);
        
        /// <inheritdoc cref="ShouldExcludeNewer(SelectionOptions, FileInfo, FileInfo)"/>
        public static bool ShouldExcludeNewer(this SelectionOptions options, IFileSourceDestinationPair pair) => options.ExcludeNewer && pair.IsSourceNewer();

        #endregion

        #region < Extra >

        /// <summary>
        /// EXTRA Files are files that exist in the destination but not in the source
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="Destination"></param>
        /// <returns>TRUE if exists in the destination but not in the source, otherwise false</returns>
        public static bool IsExtra(string Source, string Destination) => File.Exists(Destination) && !File.Exists(Source);
        /// <inheritdoc cref="IsExtra(string, string)"/>
        public static bool IsExtra(FileInfo Source, FileInfo Destination) => Destination.Exists && !Source.Exists;
        /// <inheritdoc cref="IsExtra(string, string)"/>
        public static bool IsExtra(this IFileSourceDestinationPair pair) => IsExtra(pair.Source, pair.Destination);


        /// <summary>
        /// EXTRA directories are folders that exist in the destination but not in the source
        /// </summary>
        /// <returns>TRUE if exists in the destination but not in the source, otherwise false</returns>
        public static bool IsExtra(this IDirectorySourceDestinationPair pair) => !pair.Source.Exists && pair.Destination.Exists;

        ///// <summary> </summary>
        ///// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        //public static bool ShouldExcludeExtra(this SelectionOptions options, string source, string destination) => options.ExcludeExtra&& IsExtra(source, destination);

        ///// <inheritdoc cref="ShouldExcludeExtra(SelectionOptions, string, string)"/>
        //public static bool ShouldExcludeExtra(this SelectionOptions options, FileInfo source, FileInfo destination) => options.ExcludeExtra && IsExtra(source, destination);

        ///// <inheritdoc cref="ShouldExcludeExtra(SelectionOptions, FileInfo, FileInfo)"/>
        //public static bool ShouldExcludeExtra(this SelectionOptions options, IFileSourceDestinationPair copier) => options.ExcludeExtra && IsExtra(copier.Source, copier.Destination);

        #endregion

        #region < Lonely >

        /// <summary>
        /// Lonely Files are files that exist in source but not in destination
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns>TRUE if exists in both source and destination, otherwise false</returns>
        public static bool IsLonely(string source, string destination) => !(File.Exists(source) && File.Exists(destination));
        /// <inheritdoc cref="IsLonely(string, string)"/>
        public static bool IsLonely(FileInfo Source, FileInfo Destination) => Destination.Exists && !Source.Exists;
        /// <inheritdoc cref="IsLonely(string, string)"/>
        public static bool IsLonely(this IFileSourceDestinationPair pair) => IsLonely(pair.Source, pair.Destination);

        /// <summary>
        /// LONELY directories are folders that exist in the source but not in the destination 
        /// </summary>
        /// <returns>TRUE if exists in the source but not in the destination, otherwise false</returns>
        public static bool IsLonely(this IDirectorySourceDestinationPair pair) => pair.Source.Exists && !pair.Destination.Exists;

        /// <summary> </summary>
        /// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        public static bool ShouldExcludeLonely(this SelectionOptions options, string source, string destination) => options.ExcludeLonely && IsLonely(source, destination);

        /// <inheritdoc cref="ShouldExcludeNewer(SelectionOptions, string, string)"/>
        public static bool ShouldExcludeLonely(this SelectionOptions options, FileInfo source, FileInfo destination) => options.ExcludeLonely && IsLonely(source, destination);

        /// <inheritdoc cref="ShouldExcludeNewer(SelectionOptions, FileInfo, FileInfo)"/>
        public static bool ShouldExcludeLonely(this SelectionOptions options, IFileSourceDestinationPair copier) => options.ExcludeLonely && IsLonely(copier.Source, copier.Destination);

        #endregion

        #region < MaxLastAccessDate >

        /// <summary> </summary>
        /// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        public static bool ShouldExcludeMaxLastAccessDate(this SelectionOptions options, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(options.MaxLastAccessDate)) return false;
            if (DateTime.TryParseExact(options.MaxLastAccessDate, "yyyyyMMdd", default, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out var result))
            {
                return date > result;
            }
            else if (long.TryParse(options.MaxFileAge, out long days))
            {
                return (DateTime.Now - date).TotalDays > days;
            }
            return false;
        }

        /// <inheritdoc cref="ShouldExcludeMaxLastAccessDate(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMaxLastAccessDate(this SelectionOptions options, string source) 
            => ShouldExcludeMaxLastAccessDate(options, File.GetLastAccessTime(source).Date);

        /// <inheritdoc cref="ShouldExcludeMaxLastAccessDate(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMaxLastAccessDate(this SelectionOptions options, FileInfo Source) 
            => ShouldExcludeMaxLastAccessDate(options, Source.LastAccessTime.Date);

        /// <inheritdoc cref="ShouldExcludeMaxLastAccessDate(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMaxLastAccessDate(this SelectionOptions options, IFileSourceDestinationPair pair) 
            => ShouldExcludeMaxLastAccessDate(options, pair.Source.LastAccessTime.Date);

        #endregion

        #region < MinLastAccessDate >

        /// <summary> Compare the file date against the <see cref="SelectionOptions.MinLastAccessDate"/> </summary>
        /// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        public static bool ShouldExcludeMinLastAccessDate(this SelectionOptions options, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(options.MinLastAccessDate)) return false;
            if (DateTime.TryParseExact(options.MinLastAccessDate, "yyyyyMMdd", default, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out var result))
            {
                return date < result;
            }
            else if (long.TryParse(options.MaxFileAge, out long days))
            {
                return (DateTime.Now - date).TotalDays < days;
            }
            return false;
        }

        /// <inheritdoc cref="ShouldExcludeMinLastAccessDate(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMinLastAccessDate(this SelectionOptions options, string source) => ShouldExcludeMinLastAccessDate(options, File.GetLastAccessTime(source).Date);

        /// <inheritdoc cref="ShouldExcludeMinLastAccessDate(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMinLastAccessDate(this SelectionOptions options, FileInfo Source) => ShouldExcludeMinLastAccessDate(options, Source.LastAccessTime.Date);

        /// <inheritdoc cref="ShouldExcludeMinLastAccessDate(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMinLastAccessDate(this SelectionOptions options, IFileSourceDestinationPair pair) => ShouldExcludeMinLastAccessDate(options, pair.Source.LastAccessTime.Date);

        #endregion

        #region < MaxFileAge >

        /// <summary>
        /// Compare the <see cref="FileSystemInfo.CreationTime"/> to determine the file's age against the <see cref="SelectionOptions.MaxFileAge"/>
        /// </summary>
        /// <param name="options"></param>
        /// <param name="date"></param>
        /// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        public static bool ShouldExcludeMaxFileAge(this SelectionOptions options, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(options.MaxFileAge)) return false;
            if (DateTime.TryParseExact(options.MaxFileAge, "yyyyyMMdd", default, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out var result))
            {
                return date > result;
            }
            else if (long.TryParse(options.MaxFileAge, out long days))
            {
                return (DateTime.Now - date).TotalDays > days;
            }
            return false;
        }

        /// <inheritdoc cref="ShouldExcludeMaxFileAge(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMaxFileAge(this SelectionOptions options, string source)
        {
            if (string.IsNullOrWhiteSpace(options.MaxFileAge)) return false;
            return ShouldExcludeMaxFileAge(options, File.GetCreationTime(source).Date);
        }

        /// <inheritdoc cref="ShouldExcludeMaxFileAge(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMaxFileAge(this SelectionOptions options, FileInfo Source) => ShouldExcludeMaxFileAge(options, Source.CreationTime.Date);

        /// <inheritdoc cref="ShouldExcludeMaxFileAge(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMaxFileAge(this SelectionOptions options, IFileSourceDestinationPair pair) => ShouldExcludeMaxFileAge(options, pair.Source.CreationTime.Date);

        #endregion

        #region < MinFileAge >

        /// <summary>
        /// Compare the <see cref="FileSystemInfo.CreationTime"/> to determine the file's age against the <see cref="SelectionOptions.MinFileAge"/>
        /// </summary>
        /// <param name="options"></param>
        /// <param name="date"></param>
        /// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        public static bool ShouldExcludeMinFileAge(this SelectionOptions options, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(options.MinFileAge)) return false;
            if (DateTime.TryParseExact(options.MinFileAge, "yyyyyMMdd", default, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out var result))
            {
                return date < result;
            }
            else if (long.TryParse(options.MinFileAge, out long days))
            {
                return (DateTime.Now - date).TotalDays < days;
            }
            return false;
        }

        /// <inheritdoc cref="ShouldExcludeMinFileAge(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMinFileAge(this SelectionOptions options, string source)
        {
            if (string.IsNullOrWhiteSpace(options.MinFileAge)) return false;
            return ShouldExcludeMaxFileAge(options, File.GetCreationTime(source).Date);
        }

        /// <inheritdoc cref="ShouldExcludeMinFileAge(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMinFileAge(this SelectionOptions options, FileInfo Source) 
            => ShouldExcludeMinFileAge(options, Source.CreationTime.Date);

        /// <inheritdoc cref="ShouldExcludeMinFileAge(SelectionOptions, DateTime)"/>
        public static bool ShouldExcludeMinFileAge(this SelectionOptions options, IFileSourceDestinationPair pair) 
            => ShouldExcludeMinFileAge(options, pair.Source.CreationTime.Date);

        #endregion

        #region < MaxFileSize >

        /// <summary>
        /// Compare the File Size against the <see cref="SelectionOptions.MaxFileSize"/>
        /// </summary>
        /// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        public static bool ShouldExcludeMaxFileSize(this SelectionOptions options, long size)
        {
            if (options.MaxFileSize <= 0) return false;
            return size > options.MaxFileSize;
        }

        /// <inheritdoc cref="ShouldExcludeMaxFileSize(SelectionOptions, long)"/>
        public static bool ShouldExcludeMaxFileSize(this SelectionOptions options, string source)
        {
            if (options.MaxFileSize <= 0) return false;
            return ShouldExcludeMaxFileSize(options, new FileInfo(source));
        }

        /// <inheritdoc cref="ShouldExcludeMaxFileSize(SelectionOptions, long)"/>
        public static bool ShouldExcludeMaxFileSize(this SelectionOptions options, FileInfo Source) => ShouldExcludeMaxFileSize(options, Source.Length);

        /// <inheritdoc cref="ShouldExcludeMaxFileSize(SelectionOptions, long)"/>
        public static bool ShouldExcludeMaxFileSize(this SelectionOptions options, IFileSourceDestinationPair pair) => ShouldExcludeMaxFileSize(options, pair.Source.Length);

        #endregion

        #region < MinFileSize >

        /// <summary>
        /// Compare the File Size against the <see cref="SelectionOptions.MinFileSize"/>
        /// </summary>
        /// <returns> TRUE if the file should be excluded, FALSE if it should be included </returns>
        public static bool ShouldExcludeMinFileSize(this SelectionOptions options, long size)
        {
            if (options.MaxFileSize <= 0) return false;
            return size < options.MaxFileSize;
        }

        /// <inheritdoc cref="ShouldExcludeMinFileSize(SelectionOptions, long)"/>
        public static bool ShouldExcludeMinFileSize(this SelectionOptions options, string source)
        {
            if (options.MaxFileSize <= 0) return false;
            return ShouldExcludeMinFileSize(options, new FileInfo(source));
        }

        /// <inheritdoc cref="ShouldExcludeMaxFileSize(SelectionOptions, long)"/>
        public static bool ShouldExcludeMinFileSize(this SelectionOptions options, FileInfo Source) => ShouldExcludeMinFileSize(options, Source.Length);

        /// <inheritdoc cref="ShouldExcludeMinFileSize(SelectionOptions, long)"/>
        public static bool ShouldExcludeMinFileSize(this SelectionOptions options, IFileSourceDestinationPair pair) => ShouldExcludeMinFileSize(options, pair.Source.Length);

        #endregion

        #region < Included Attributes >

        /// <summary>
        /// Compare the File Attributes the <see cref="SelectionOptions.IncludeAttributes"/>
        /// </summary>
        /// <returns> TRUE if the file should be INCLUDED, FALSE if it should be EXCLUDED </returns>
        public static bool ShouldIncludeAttributes(this SelectionOptions options, FileAttributes fileAttributes)
        {
            FileAttributes? attr = options.IncludedAttributesValue;
            if (attr is null) return true; // nothing specified - include all files
            return fileAttributes.HasFlag(attr.Value);
        }

        /// <inheritdoc cref="ShouldIncludeAttributes(SelectionOptions, FileAttributes)"/>
        public static bool ShouldIncludeAttributes(this SelectionOptions options, string source) => ShouldIncludeAttributes(options, File.GetAttributes(source));

        /// <inheritdoc cref="ShouldIncludeAttributes(SelectionOptions, FileAttributes)"/>
        public static bool ShouldIncludeAttributes(this SelectionOptions options, FileInfo Source) => ShouldIncludeAttributes(options, Source.Attributes);

        /// <inheritdoc cref="ShouldIncludeAttributes(SelectionOptions, FileAttributes)"/>
        public static bool ShouldIncludeAttributes(this SelectionOptions options, IFileSourceDestinationPair pair) => ShouldIncludeAttributes(options, pair.Source.Attributes);

        #endregion

        #region < Excluded Attributes >

        /// <summary>
        /// Compare the File Attributes the <see cref="SelectionOptions.ExcludeAttributes"/>
        /// </summary>
        /// <returns> TRUE if the file should be EXCLUDED, false if the file should be INCLUDED </returns>
        public static bool ShouldExcludeFileAttributes(this SelectionOptions options, FileAttributes attributes)
        {
            FileAttributes? attr = options.ExcludedAttributesValue;
            if (attr is null) return false; // nothing specified - include all files
            return attributes.HasFlag(attr.Value);
        }

        /// <inheritdoc cref="ShouldExcludeFileAttributes(SelectionOptions, FileAttributes)"/>
        public static bool ShouldExcludeFileAttributes(this SelectionOptions options, string source) => ShouldExcludeFileAttributes(options, File.GetAttributes(source));

        /// <inheritdoc cref="ShouldExcludeFileAttributes(SelectionOptions, FileAttributes)"/>
        public static bool ShouldExcludeFileAttributes(this SelectionOptions options, FileInfo Source) => ShouldExcludeFileAttributes(options, Source.Attributes);

        /// <inheritdoc cref="ShouldExcludeFileAttributes(SelectionOptions, FileAttributes)"/>
        public static bool ShouldExcludeFileAttributes(this SelectionOptions options, IFileSourceDestinationPair pair) => ShouldExcludeFileAttributes(options, pair.Source.Attributes);

        #endregion

        #region < ExcludedFiles Names >

        /// <summary>
        /// Determine if the file should be rejected based on its filename
        /// </summary>
        /// <param name="options"></param>
        /// <param name="fileName">filename to compare</param>
        /// <param name="exclusionCollection">
        /// The collection of regex objects to compare against - If this is null, a new array will be generated from <see cref="SelectionOptions.ExcludedFiles"/>. <br/>
        /// ref is used for optimization during the course of the run, to avoid creating regex for every file check.
        /// </param>
        /// <returns></returns>
        public static bool ShouldExcludeFileName(this SelectionOptions options, string fileName, ref Regex[] exclusionCollection)
        {
            if (exclusionCollection is null)
            {
                List<Regex> reg = new List<Regex>();
                foreach(string s in options.ExcludedFiles)
                {
                    reg.Add(SanitizeFileNameRegex(s));
                }
                exclusionCollection = reg.ToArray();
            }
            if (exclusionCollection.Length == 0) return false;
            return exclusionCollection.Any(r => r.IsMatch(fileName));
        }

        /// <inheritdoc cref="ShouldExcludeFileName(SelectionOptions, string, ref Regex[])"/>
        public static bool ShouldExcludeFileName(this SelectionOptions options, FileInfo Source, ref Regex[] exclusionCollection) => ShouldExcludeFileName(options, Source.Name, ref exclusionCollection);

        /// <inheritdoc cref="ShouldExcludeFileName(SelectionOptions, string, ref Regex[])"/>
        public static bool ShouldExcludeFileName(this SelectionOptions options, IFileSourceDestinationPair pair, ref Regex[] exclusionCollection) => ShouldExcludeFileName(options, pair.Source.Name, ref exclusionCollection);

        #endregion

        #region < Excluded Dir Names >

        /// <summary>
        /// Determine if the file should be rejected based on its filename
        /// </summary>
        /// <param name="options"></param>
        /// <param name="directoryPath">directory Name to compare</param>
        /// <param name="exclusionCollection">
        /// The collection of regex objects to compare against - If this is null, a new array will be generated from <see cref="SelectionOptions.ExcludedFiles"/>. <br/>
        /// ref is used for optimization during the course of the run, to avoid creating regex for every file check.
        /// </param>
        /// <returns></returns>
        public static bool ShouldExcludeDirectoryName(this SelectionOptions options, string directoryPath, ref Tuple<bool, Regex>[] exclusionCollection)
        {
            if (exclusionCollection is null)
            {
                List<Tuple<bool, Regex>> reg = new List<Tuple<bool, Regex>>();
                foreach (string s in options.ExcludedDirectories)
                {
                    bool isPathRegex = s.Contains(Path.DirectorySeparatorChar) || s.Contains(Path.AltDirectorySeparatorChar);
                    reg.Add(new Tuple<bool, Regex>(isPathRegex, SanitizeFileNameRegex(s)));
                }
                exclusionCollection = reg.ToArray();
            }
            if (exclusionCollection.Length == 0) return false;
            return exclusionCollection.Any(
                pair =>
                {
                    if (pair.Item1)
                        return pair.Item2.IsMatch(directoryPath); // evaluate against the entire path
                    else
                        return pair.Item2.IsMatch(Path.GetFileName(directoryPath)); // evaluate against the folder name
                });
        }

        /// <inheritdoc cref="ShouldExcludeDirectoryName(SelectionOptions, string, ref Tuple{bool, Regex}[])"/>
        public static bool ShouldExcludeDirectoryName(this SelectionOptions options, DirectoryInfo Source, ref Tuple<bool, Regex>[] exclusionCollection) => ShouldExcludeDirectoryName(options, Source.FullName, ref exclusionCollection);

        /// <inheritdoc cref="ShouldExcludeDirectoryName(SelectionOptions, string, ref Tuple{bool, Regex}[])"/>
        public static bool ShouldExcludeDirectoryName(this SelectionOptions options, IDirectorySourceDestinationPair pair, ref Tuple<bool, Regex>[] exclusionCollection) => ShouldExcludeDirectoryName(options, pair.Source.FullName, ref exclusionCollection);

        #endregion

        #region < Symbolic Links (Files) >

        /// <summary>
        /// Evaluate if the file should be excluded under the JunctionPoint exclusion settings.
        /// </summary>
        /// <param name="options">Evaluates <see cref="SelectionOptions.ExcludeJunctionPoints"/> and <see cref="SelectionOptions.ExcludeJunctionPointsForFiles"/></param>
        /// <param name="file"></param>
        /// <returns>TRUE if the file should be excluded or doesn't exist, FALSE if the file should be copied.</returns>
        public static bool ExcludeSymbolicFile(this SelectionOptions options, FileInfo file)
        {
            if (!file.Exists) return true;
            if (options.ExcludeJunctionPoints | options.ExcludeJunctionPointsForFiles)
                return file.IsSymbolicLink();
            else
                return false;
        }

        /// <inheritdoc cref="ExcludeSymbolicFile(SelectionOptions, FileInfo)"/>
        public static bool ExcludeSymbolicFile(this SelectionOptions options, IFileSourceDestinationPair pair) => ExcludeSymbolicFile(options, pair.Source);

        /// <inheritdoc cref="ExcludeSymbolicFile(SelectionOptions, FileInfo)"/>
        public static bool ExcludeSymbolicFile(this SelectionOptions options, string file) => ExcludeSymbolicFile(options, new FileInfo(file));

        #endregion

        #region < Symbolic Links (Directories) >

        /// <summary>
        /// Evaluate if the Directory should be excluded under the JunctionPoint exclusion settings.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="directory"></param>
        /// <returns>TRUE if the directory is a valid symbolic and the <see cref="SelectionOptions.ExcludeJunctionPointsForDirectories"/> | <see cref="SelectionOptions.ExcludeJunctionPoints"/> options are set. </returns>
        public static bool ShouldExcludeJunctionDirectory(this SelectionOptions options, string directory)
        {
            if (!Directory.Exists(directory)) return true;
            if (options.ExcludeJunctionPoints | options.ExcludeJunctionPointsForDirectories)
                return SymbolicLink.IsJunctionOrSymbolic(directory);
            else
                return false;
        }

        /// <inheritdoc cref="ShouldExcludeJunctionDirectory(SelectionOptions, string)"/>
        public static bool ShouldExcludeJunctionDirectory(this SelectionOptions options, DirectoryInfo directory)
        {
            if (!directory.Exists) return true;
            if (options.ExcludeJunctionPoints | options.ExcludeJunctionPointsForDirectories)
                return SymbolicLink.IsJunctionOrSymbolic(directory.FullName);
            else
                return false;
        }

        /// <inheritdoc cref="ShouldExcludeJunctionDirectory(SelectionOptions, string)"/>
        public static bool ShouldExcludeJunctionDirectory(this SelectionOptions options, IDirectorySourceDestinationPair pair) => ShouldExcludeJunctionDirectory(options, pair.Source);

        #endregion

    }
}
