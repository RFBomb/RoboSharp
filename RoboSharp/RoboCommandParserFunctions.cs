using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("RoboSharp.UnitTests")]
namespace RoboSharp
{
    /// <summary>
    /// This class houses the various helper functions used to parse and apply the parameters of an input robocopy command to an IRoboCommand
    /// </summary>
    /// <remarks>Exposed for unit testing</remarks>
    
    internal static class RoboCommandParserFunctions
    {
        /// <summary>
        /// Helper object that reports the result from <see cref="Parse(string)"/>
        /// </summary>
        public readonly struct ParsedSourceDest
        {
            public readonly string Source;
            public readonly string Destination;
            public readonly string InputString;
            public readonly StringBuilder SanitizedString;

            private ParsedSourceDest(string source, string dest, string input, StringBuilder sanitized)
            {
                Source = source;
                Destination = dest;
                InputString = input;
                SanitizedString = sanitized;
            }

            internal const string SourceDestinationUnableToParseMessage = "Source and Destination were unable to be parsed.";

            //SomeServer/HiddenDrive$/RootFolder   //SomeServer\RootFolder
            //lang=regex                        no quotes                                           quotes
            internal const string uncRegex = @"([\\\/]{2}[^*:?""<>|$\s]+?[$]?[\\\/][^*:?""<>|\s]+?) | (""[\\\/]{2}[^*:?""<>|$]+?[$]?[\\\/][^*:?""<>|]+?"")";

            // c:\  D:/SomeFolder  "X:\Spaced Folder Name"
            //lang=regex                          no quotes                           quotes
            internal const string localRegex = @"([a-zA-Z][:][\\\/][^:*?""<>|\s]*) | (""([a-zA-Z][:][\\\/][^:*?""<>|]*)"")";
            private const string allowedPathsRegex = @"(""\s*"") | " + localRegex + "|" + uncRegex;

            internal const string fullRegex = @"^\s*(?<source>" + allowedPathsRegex + @") \s+ (?<dest>" + allowedPathsRegex + @") .*$";
            internal const string destinationUndefinedRegex = @"^\s*(?<source>" + allowedPathsRegex + @") .*$";
            internal const RegexOptions regexOptions = RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace;
            
            /// <returns>Strips out any Source/Dest information, then returns a new object</returns>
            public static ParsedSourceDest ParseOptionsOnly(string inputText)
            {
                string trimmedText = TrimRobocopy(inputText);
                StringBuilder builder = new StringBuilder(trimmedText).TrimStart();
                Regex regex = new Regex(allowedPathsRegex, regexOptions);
                
                // source
                var match =  regex.Match(trimmedText);
                if (match.Success) 
                    builder.TrimStart(match.Groups[0].Value).TrimStart();

                // Dest
                match = regex.Match(builder.ToString());
                if (match.Success)
                    builder.TrimStart(match.Groups[0].Value).TrimStart();

                return new ParsedSourceDest(string.Empty, string.Empty, inputText, builder);
            }

            /// <summary>
            /// Parse the input text, extracting the Source and Destination info.
            /// </summary>
            /// <param name="inputText">The input text to parse. 
            /// <br/>The expected pattern is :  robocopy "SOURCE" "DESTINATION" 
            /// <br/> - 'robocopy' is optional, but the source/destination must appear in the specified order at the beginning of the text. 
            /// <br/> - Quotes are only required if the path has whitespace.
            /// </param>
            /// <returns>A new <see cref="ParsedSourceDest"/> struct with the results</returns>
            /// <exception cref="RoboCommandParserException"/>
            public static ParsedSourceDest Parse(string inputText)
            {
                var match = Regex.Match(inputText, fullRegex, regexOptions);
                RoboCommandParserException ex;
                if (!match.Success)
                {
                    if (Regex.IsMatch(inputText, destinationUndefinedRegex, regexOptions))
                    {
                        ex = new RoboCommandParserException("Invalid command - Source was provided but destination was not.");
                        ex.AddData("input", inputText);
                        throw ex;
                    }
                    else
                    {
                        return new ParsedSourceDest(string.Empty, string.Empty, inputText, new StringBuilder(inputText));
                    }
                }

                string rawSource = match.Groups["source"].Value;
                string rawDest = match.Groups["dest"].Value;
                string source = rawSource.Trim('\"');
                string dest = rawDest.Trim('\"');

                // Validate source and destination - both must be empty, or both must be fully qualified
                bool sourceEmpty = string.IsNullOrWhiteSpace(source);
                bool destEmpty = string.IsNullOrWhiteSpace(dest);
                bool sourceQualified = source.IsPathFullyQualified();
                bool destQualified = dest.IsPathFullyQualified();

                StringBuilder commandBuilder = new StringBuilder(inputText);

                if (sourceQualified && destQualified)
                {
                    Debugger.Instance.DebugMessage($"--> Source and Destination Pattern Match Success:");
                    Debugger.Instance.DebugMessage($"----> Source : {source}");
                    Debugger.Instance.DebugMessage($"----> Destination : {dest}");
                    commandBuilder.RemoveString(rawSource);
                    commandBuilder.RemoveString(rawDest);
                    return new ParsedSourceDest(source, dest, inputText, commandBuilder);
                }
                else if (sourceEmpty && destEmpty)
                {
                    Debugger.Instance.DebugMessage($"--> Source and Destination Pattern Match Success: Neither specified");
                    if (match.Success)
                    {
                        commandBuilder.RemoveString(rawSource);
                        commandBuilder.RemoveString(rawDest);
                    }
                    return new ParsedSourceDest(string.Empty, string.Empty, inputText, commandBuilder);
                }
                else
                {
                    Debugger.Instance.DebugMessage($"--> Unable to detect a valid Source/Destination pattern match -- Input text : {inputText}");
                    Debugger.Instance.DebugMessage($"----> Source : {source}");
                    Debugger.Instance.DebugMessage($"----> Destination : {dest}");
                    ex = new RoboCommandParserException(message: true switch
                    {
                        true when sourceEmpty && destQualified => "Destination is fully qualified, but Source is empty. See exception data.",
                        true when destEmpty && sourceQualified => "Source is fully qualified, but Destination is empty. See exception data.",
                        true when !sourceQualified && !destQualified => "Source and Destination are not fully qualified. See exception data.",
                        true when !sourceQualified => "Source is not fully qualified. See exception data. ",
                        true when !destQualified => "Destination is not fully qualified. See exception data.",
                        _ => "Source / Destination Parsing Error",
                    });
                    ex.AddData("Input Text", inputText);
                    ex.AddData("Parsed Source", rawSource);
                    ex.AddData("Parsed Destination", rawDest);
                    throw ex;
                }
            }
        }

        /// <summary>
        /// Trim robocopy from that beginning of the input string
        /// </summary>
        /// <returns>A StringBuilder instance that represents the trimmed string</returns>
        public static string TrimRobocopy(string input)
        {
            //lang=regex 
            const string rc = @"^(?<rc>\s*((?<sQuote>"".+?[:$].+?robocopy(\.exe)?"")|(?<sNoQuote>([^:*?""<>|\s]+?[:$][^:*?<>|\s]+?)?robocopy(\.exe)?))\s+)";
            var match = Regex.Match(input, rc, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture| RegexOptions.CultureInvariant);
            return match.Success ? input.TrimStart(match.Groups[0].Value) : input;
        }

        /// <summary> Attempt to extract the parameter from a format pattern string </summary>
        /// <param name="inputText">The stringbuilder to evaluate and remove the substring from</param>
        /// <param name="parameterFormatString">the parameter to extract. Example :   /LEV:{0}</param>
        /// <param name="value">The extracted value. (only if returns true)</param>
        /// <returns>True if the value was extracted, otherwise false.</returns>
        public static bool TryExtractParameter(StringBuilder inputText, string parameterFormatString, out string value)
        {
            value = string.Empty;
            string prefix = parameterFormatString.Substring(0, parameterFormatString.IndexOf('{')).TrimEnd('{').Trim(); // Turn /LEV:{0} into /LEV:

            int prefixIndex = inputText.IndexOf(prefix,false);
            if (prefixIndex < 0)
            {
                Debugger.Instance.DebugMessage($"--> Switch {prefix} not detected.");
                return false;
            }

            int lastIndex = inputText.IndexOf(" /", false, prefixIndex + 1);
            int substringLength = lastIndex < 0 ? inputText.Length : lastIndex - prefixIndex +1;
            var result = inputText.SubString(prefixIndex, substringLength);
            value = result.RemoveSafe(0, prefix.Length).Trim().ToString();
            Debugger.Instance.DebugMessage($"--> Switch {prefix} found. Value : {value}");
            inputText.RemoveSafe(prefixIndex, substringLength);
            return true;
        }

        internal static StringBuilder RemoveSafe(this StringBuilder builder, int index, int length)
        {
            if (index + length > builder.Length)
                return builder.Remove(index, builder.Length - index);
            else
                return builder.Remove(index, length);
        }

        /// <inheritdoc cref="RemoveString(StringBuilder, string, Action)"/>
        internal static bool RemoveString(this StringBuilder builder, string searchText)
        {
            return RemoveString(builder, searchText, null);
        }

        /// <returns>True if the first occurence was removed, otherwise false</returns>
        internal static bool RemoveString(this StringBuilder builder, string searchText, Action actionIfTrue)
        {
            int index = builder.IndexOf(searchText, false);
            Debugger.Instance.DebugMessage($"--> Switch {searchText}{(index >= 0 ? "" : " not")} detected.");
            if (index >= 0)
            {
                builder.Remove(index, searchText.Length);
                actionIfTrue?.Invoke();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Parse the string, extracting individual filters out to an IEnumerable string
        /// </summary>
        public static IEnumerable<string> ParseFilters(string stringToParse, string debugFormat)
        {
            List<string> filters = new List<string>();
            StringBuilder filterBuilder = new StringBuilder();
            bool isQuoted = false;
            bool isBuilding = false;
            foreach (char c in stringToParse)
            {
                if (isQuoted && c == '"')
                    NextFilter();
                else if (isQuoted)
                    filterBuilder.Append(c);
                else if (c == '"')
                {
                    isQuoted = true;
                    isBuilding = true;
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (isBuilding) NextFilter(); // unquoted white space indicates end of one filter and start of next. Otherwise ignore whitepsace.
                }
                else
                {
                    isBuilding = true;
                    filterBuilder.Append(c);
                }
            }
            NextFilter();
            return filters;
            void NextFilter()
            {
                isQuoted = false;
                isBuilding = false;
                string value = filterBuilder.ToString();
                if (string.IsNullOrWhiteSpace(value)) return;
                Debugger.Instance.DebugMessage(string.Format(debugFormat, value));
                filters.Add(value.Trim());
                filterBuilder.Clear();
            }
        }

        //lang=regex
        const string XF_Pattern = @"(?<filter>\/XF\s*( ((?<Quotes>""(\/\/[a-zA-Z]|[A-Z]:|[^/:\s])?[\w\*$\-\/\\.\s]+"") | (?<NoQuotes>(\/\/[a-zA-Z]|[A-Z]:|[^\/\:\s])?[\w*$\-\/\\.]+)) (\s*(?!\/[a-zA-Z])) )+)";
        //lang=regex
        const string XD_Pattern = @"(?<filter>\/XD\s*(( (?<Quotes>""(\/\/[a-zA-Z]|[A-Z]:|[^/:\s])?[\w\*$\-\/\\.\s]+"") | (?<NoQuotes>(\/\/[a-zA-Z]|[A-Z]:|[^\/\:\s])?[\w*$\-\/\\.]+)) (\s*(?!\/[a-zA-Z])) )+)";
        //lang=regex
        const string FileFilter = @"^\s*(?<filter>((?<Quotes>""[^""]+"") | (?<NoQuotes>((?<!\/)[^\/""])+) )+)"; // anything up until the first standalone option 

        /// <summary>
        /// File Filters to INCLUDE - These are always be at the beginning of the input string
        /// </summary>
        /// <param name="command">An input string with the source and destination removed. 
        /// <br/>Valid : *.*  ""text"" /XF  -- Reads up until the first OPTION switch
        /// <br/>Not Valid : robocopy Source destination -- these will be consdidered 3 seperate filters.
        /// <br/>Not Valid : Source/destination -- these will be considered as file filters.
        /// </param>
        public static IEnumerable<string> ExtractFileFilters(StringBuilder command)
        {
            const string debugFormat = "--> Found File Filter : {0}";
            Debugger.Instance.DebugMessage($"Parsing Copy Options - Extracting File Filters");

            var input = command.ToString();
            var match = Regex.Match(input, FileFilter, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);
            string foundFilters = match.Groups["filter"].Value;
            command.RemoveString(foundFilters);

            if (match.Success && !string.IsNullOrWhiteSpace(foundFilters))
            {
                return ParseFilters(foundFilters, debugFormat);
            }
            else
            {
                Debugger.Instance.DebugMessage($"--> No file filters found.");
#if NET452
                return new string[] { };
#else
                return Array.Empty<string>();
#endif
            }
        }

        public static IEnumerable<string> ExtractExclusionFiles(StringBuilder command)
        {
            // Get Excluded Files
            Debugger.Instance.DebugMessage($"Parsing Selection Options - Extracting Excluded Files");
            string input = command.ToString();
            var matchCollection = Regex.Matches(input, XF_Pattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
            if (matchCollection.Count == 0) Debugger.Instance.DebugMessage($"--> No File Exclusions found.");
            List<string> result = new List<string>();
            foreach (Match c in matchCollection)
            {
                string s = c.Groups["filter"].Value;
                command.RemoveString(s);
                s = s.TrimStart("/XF").Trim();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    result.AddRange(ParseFilters(s, "---> Excluded File : {0}"));
                }
            }
            return result;
        }

        public static IEnumerable<string> ExtractExclusionDirectories(StringBuilder command)
        {
            // Get Excluded Dirs
            Debugger.Instance.DebugMessage($"Parsing Selection Options - Extracting Excluded Directories");
            string input = command.ToString();
            var matchCollection = Regex.Matches(input, XD_Pattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
            if (matchCollection.Count == 0) Debugger.Instance.DebugMessage($"--> No Directory Exclusions found.");
            List<string> result = new List<string>();
            foreach (Match c in matchCollection)
            {
                string s = c.Groups["filter"].Value;
                command.RemoveString(s);
                s = s.TrimStart("/XD").Trim();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    result.AddRange(ParseFilters(s, "---> Excluded Directory : {0}")); ;
                }
            }
            return result;
        }

        /// <inheritdoc cref="IndexOf(StringBuilder, string, bool, int)"/>
        private static int IndexOf(this StringBuilder builder, string text, bool isCaseSensitive = false)
            => IndexOf(builder, text, isCaseSensitive, 0);

        /// <returns>-1 if the text does not exist within the builder, otherwise the starting index</returns>
        private static int IndexOf(this StringBuilder builder, string text, bool isCaseSensitive, int startIndex)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (startIndex > builder.Length) throw new ArgumentException("startIndex value greater than input string length");
            if (string.IsNullOrEmpty(text)) return -1;
            int builderIndex = startIndex;
            int searchIndex= 0;
            int? foundIndex = null;
            char builderChar;
            char searchChar;

            while (builderIndex < builder.Length)
            {
                // Get Char
                if (isCaseSensitive)
                {
                    builderChar = builder[builderIndex];
                    searchChar = text[searchIndex];
                }
                else
                {
                    builderChar = char.ToLowerInvariant(builder[builderIndex]);
                    searchChar = char.ToLowerInvariant(text[searchIndex]);
                }
                // Compare
                if (builderChar.Equals(searchChar))
                {
                    searchIndex++;
                    if (foundIndex is null) foundIndex = builderIndex;
                    if (searchIndex >= text.Length)
                        return foundIndex.Value;
                }
                else
                {
                    searchIndex = 0;
                    foundIndex = null;
                }
                builderIndex++;
            }
            return -1;
        }

        public static StringBuilder SubString(this StringBuilder builder, int startIndex) => SubString(builder, startIndex, -1);
        public static StringBuilder SubString(this StringBuilder builder, int startIndex, int length)
        {
            StringBuilder result = new StringBuilder();
            int i = startIndex;
            while (i < builder.Length && result.Length < length)
            {
                result.Append(builder[i]);
                i++;
            }
            return result;
        }

        public static bool StartsWith(this StringBuilder builder, string value, bool caseSensitive = false)
        {
            int index = 0;
            foreach(char c in value)
            {
                if (caseSensitive)
                {
                    if (!c.Equals(builder[index]))
                        return false;
                }
                else if (!char.ToLowerInvariant(c).Equals(char.ToLowerInvariant(builder[index])))
                {
                    return false;
                }
                index++;
            }
            return true;
        }

        public static StringBuilder Trim(this StringBuilder builder) => builder.TrimStart().TrimEnd();
        public static StringBuilder TrimStart(this StringBuilder builder)
        {
            if (builder.Length < 1) return builder;
            while (builder.Length > 0 && char.IsWhiteSpace(builder[0]))
                builder.Remove(0, 1);
            return builder;
        }
        public static StringBuilder TrimEnd(this StringBuilder builder)
        {
            if (builder.Length < 1) return builder;
            int lastIndex;
            while (char.IsWhiteSpace(builder[lastIndex = builder.Length - 1]))
                builder.Remove(lastIndex, 1);
            return builder;
        }
        public static StringBuilder TrimStart(this StringBuilder builder, string textToRemove)
        {
            if (builder.StartsWith(textToRemove))
                builder.Remove(0, textToRemove.Length);
            return builder;
        }
    }
}
