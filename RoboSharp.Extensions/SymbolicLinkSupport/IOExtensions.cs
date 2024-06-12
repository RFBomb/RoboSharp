using System;
using System.IO;

namespace RoboSharp.Extensions.SymbolicLinkSupport
{
    /// <summary>
    /// Provides extensions for <see cref="DirectoryInfo"/>, <see cref="FileInfo"/> to handle symbolic directory links.
    /// <br/> Also provides methods to move files using P/Invoke-MoveWithProgressA
    /// </summary>
    public static class IOExtensions
    {
#if !NET6_0_OR_GREATER  // Extension methods are not needed in .net6 as they are now native to FileSystemInfo
        /// <summary>
        /// Creates a symbolic link to this file at the specified path.
        /// </summary>
        /// <param name="info">the source file for the symbolic link.</param>
        /// <param name="path">the path of the symbolic link.</param>
        public static void CreateAsSymbolicLink(this FileSystemInfo info, string path)
        {
            SymbolicLink.CreateSymbolicLink(path, info.FullName, info is DirectoryInfo, false);
        }
#endif

        /// <summary>
        /// Determines whether this <see cref="FileSystemInfo"/> represents a symbolic link.
        /// </summary>
        /// <param name="info">the system object in question.</param>
        /// <returns><see langword="true"/> if the <paramref name="info"/> is a symbolic link, otherwise <see langword="false"/>.</returns>
        public static bool IsSymbolicLink(this FileSystemInfo info)
        {
            if (info is null) throw new ArgumentNullException(nameof(info));
#if NET6_0_OR_GREATER
            return info.LinkTarget != null;
#else
            return info.Attributes.HasFlag(FileAttributes.ReparsePoint);
#endif
        }

        /// <summary>
        /// Checks if the object is a symbolic link, then returns if the final target exists
        /// </summary>
        /// <param name="info">The symbolic link in question.</param>
        /// <returns><see langword="true"/> if the <paramref name="info"/> target exists, otherwise <see langword="false"/>.</returns>
        public static bool TargetExists(this FileSystemInfo info)
        {
            if (info is null) throw new ArgumentNullException(nameof(info));
            if (info is FileInfo file)
                return File.Exists(GetSymbolicLinkTarget(file));
            else
                return Directory.Exists(GetSymbolicLinkTarget(info as DirectoryInfo));
        }

        /// <summary>
        /// Returns the full path to the target of this symbolic link.
        /// </summary>
        /// <param name="info">The symbolic link in question.</param>
        /// <returns>The path to the target of the symbolic link.</returns>
        /// <exception cref="System.ArgumentException">If the file in question is not a symbolic link.</exception>
        public static string GetSymbolicLinkTarget(this FileSystemInfo info)
        {
#if NET6_0_OR_GREATER
            return info.LinkTarget ?? info.FullName;
#else
            if (!info.IsSymbolicLink())
                return info.FullName;
            return SymbolicLink.GetLinkTarget(info.FullName, info is DirectoryInfo);
#endif
        }
    }
}