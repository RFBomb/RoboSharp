#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using MSWin = Windows;
using Win32 = Windows.Win32;
using Windows.Win32.Storage.FileSystem;
using REPARSE_DATA_BUFFER = Windows.Wdk.Storage.FileSystem.REPARSE_DATA_BUFFER;
using System.Runtime.CompilerServices;
using Windows.Win32.Foundation;

namespace RoboSharp.Extensions.SymbolicLinkSupport
{
    /// <summary>
    /// Provides extension methods for <see cref="FileSystemInfo"/> to creating and reading symbolic links.
    /// </summary>
    public static class SymbolicLink
    {
        const string PlatformErrorMessage = "This function relies on Windows P/Invoke. Use .Net 6 or newer for platform comaptibility.";

#if NET6_0_OR_GREATER
        /// <remarks>Included to avoid breaking target compatibility. Since this is now native to ,Net6 and newer, this simply calls the native method.</remarks>
        /// <inheritdoc cref="FileSystemInfo.CreateAsSymbolicLink(string)"/>
        public static void CreateAsSymbolicLink(FileSystemInfo info, string pathToTarget) => info.CreateAsSymbolicLink(pathToTarget);
#else
        /// <summary>
        /// Creates a symbolic link at the <see cref="FileSystemInfo.FullName"/> that points to the <paramref name="pathToTarget"/>
        /// </summary>
        /// <param name="info">the file or directory to create as a link</param>
        /// <param name="pathToTarget">The path of the symbolic link target.</param>
        /// <remarks>Requires administractive privileges. Windows only.</remarks>
        public static void CreateAsSymbolicLink(this FileSystemInfo info, string pathToTarget)
        {
            VersionManager.ThrowIfNotWindowsPlatform(PlatformErrorMessage);
            if (info is null) throw new ArgumentNullException(nameof(info));
            if (string.IsNullOrWhiteSpace(pathToTarget)) throw new ArgumentException("target path can not be empty", nameof(pathToTarget));
            SymbolicLink.CreateAsSymbolicLink(pathToTarget, info.FullName, info is DirectoryInfo, false);
        }
#endif

        /// <summary>
        /// Create a new Symbolic Link at the specified location. Windows Only.
        /// </summary>
        /// <param name="linkPath">The path the link shall reside at</param>
        /// <param name="targetPath">The path of the target</param>
        /// <param name="isDirectory">set TRUE if creating a link to a directory, otherwise this will be a link to a file.</param>
        /// <param name="makeTargetPathRelative">
        /// Make the link relative to the target. Example:
        /// <br/>Target Path = "D:\Root\SomeDir\SomeFile.txt"
        /// <br/>Link Path = "D:\"
        /// <br/> Result Target = "Root\SomeDir\SomeFile.txt"
        /// </param>
        /// <exception cref="IOException"/>
        /// <exception cref="PlatformNotSupportedException"/>
        public static void CreateAsSymbolicLink(string linkPath, string targetPath, bool isDirectory, bool makeTargetPathRelative = false)
        {
            VersionManager.ThrowIfNotWindowsPlatform(PlatformErrorMessage);
            if (makeTargetPathRelative)
            {
                targetPath = GetTargetPathRelativeToLink(linkPath, targetPath, isDirectory);
            }

            var success = Win32.PInvoke.CreateSymbolicLink(linkPath, targetPath, isDirectory ? Win32.Storage.FileSystem.SYMBOLIC_LINK_FLAGS.SYMBOLIC_LINK_FLAG_DIRECTORY : 0);
            if (!success)
            {
                try
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
                catch (COMException exception)
                {
                    throw new IOException(exception.Message, exception);
                }
            }
        }

        /// <summary>
        /// Determines whether this <see cref="FileSystemInfo"/> represents a symbolic link or junction.
        /// </summary>
        /// <param name="info">the system object in question.</param>
        /// <returns><see langword="true"/> if the <paramref name="info"/> is a symbolic link, otherwise <see langword="false"/>.</returns>
        public static bool IsSymbolicLink(this FileSystemInfo info)
        {
            if (info is null) throw new ArgumentNullException(nameof(info));
#if NET6_0_OR_GREATER
            return info.LinkTarget != null;
#else
            if (VersionManager.IsPlatformWindows)
                return info.Attributes.HasFlag(FileAttributes.ReparsePoint) && SymbolicLink.GetLinkTarget(info) != null;
            else
                return info.Attributes.HasFlag(FileAttributes.ReparsePoint);
#endif
        }

#if NET6_0_OR_GREATER
        /// <returns> The <see cref="FileSystemInfo"/> object that represents that target </returns>
        /// <inheritdoc cref="FileSystemInfo.ResolveLinkTarget(bool)"/>
        public static FileSystemInfo ResolveFinalTarget(this FileSystemInfo info)
        {
            if (info is null) throw new ArgumentNullException(nameof(info));
            if (info.LinkTarget is null) return info;
            //try
            //{
                return info.ResolveLinkTarget(true) ?? info;
            //}
            //catch (IOException)
            //{
            //    // catch too-many-links (ResolveLinkTarget can only handle up to 63 in one go, so fallback to iterating recursively. If a different fault occurs, it will throw again.
            //    FileSystemInfo prevT = info;
            //    while (prevT.ResolveLinkTarget(false) is FileInfo T)
            //        prevT = T;
            //    return prevT;
            //}
        }

        /// <inheritdoc cref="FileSystemInfo.ResolveLinkTarget(bool)"/>
        public static FileSystemInfo? ResolveLinkTarget(FileSystemInfo link, bool returnFinalTarget)
        {
            return link?.ResolveLinkTarget(returnFinalTarget);
        } 
#else
        /// <summary>
        /// Gets the target of the specified link.
        /// </summary>
        /// <param name="link"></param>
        /// <returns>A FileSystemInfo object</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <remarks><see href="https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfinalpathnamebyhandlea"/></remarks>
        public static FileSystemInfo ResolveFinalTarget(this FileSystemInfo link)
        {
            if (link is null) throw new ArgumentNullException(nameof(link));
            if (VersionManager.IsPlatformWindows && link.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                string path = GetFinalPathNameByHandle(link);
                if (link.FullName.Equals(path)) return link;
                return link is DirectoryInfo ? new DirectoryInfo(path) : new FileInfo(path);
            }
            return link;
        }

        /// <summary>
        /// Gets the target of the specified link.
        /// </summary>
        /// <remarks>This is an extension method to emulate the .Net6 native method of the same name.</remarks>
        /// <param name="link"></param>
        /// <param name="returnFinalTarget"></param>
        /// <returns>A <see cref="FileSystemInfo"/> that represents the target. If the <paramref name="link"/> is not a target, return <see langword="null"/> </returns>
        /// <exception cref="PlatformNotSupportedException"/>
        public static FileSystemInfo? ResolveLinkTarget(this FileSystemInfo link, bool returnFinalTarget)
        {
            if (link is null) return null;
            if (returnFinalTarget) return ResolveFinalTarget(link);
            if (GetLinkTarget(link) is string target) 
                return link is DirectoryInfo ? new DirectoryInfo(target) : new FileInfo(target);
            return null;
        }
#endif

        /// <summary>
        /// Returns the target of a symlink or juction.
        /// </summary>
        /// <returns>The target of the link. If the <paramref name="info"/> is not a link, returns <see langword="null"/></returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="PlatformNotSupportedException"/>
        internal static string? GetLinkTarget(FileSystemInfo info)
        {
            if (info is null) throw new ArgumentNullException(nameof(info));
#if NET6_0_OR_GREATER
            return info.LinkTarget;
#else
            info.Refresh();
            if (info.Exists && info.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                VersionManager.ThrowIfNotWindowsPlatform(PlatformErrorMessage);
                return GetReparseDataTarget(info);
            }
            return null;
#endif
        }

        private static string GetTargetPathRelativeToLink(string linkPath, string targetPath, bool linkAndTargetAreDirectories = false)
        {
            FileAttributes relativePathAttribute = 0;
            if (linkAndTargetAreDirectories)
            {
                relativePathAttribute = FileAttributes.Directory;

                // set the link path to the parent directory, so that PathRelativePathToW returns a path that works
                // for directory symlink traversal
                linkPath = Path.GetDirectoryName(linkPath.TrimEnd(Path.DirectorySeparatorChar));
            }

            Span<char> relativePath = new Span<char>(new char[Win32.PInvoke.MAX_PATH]);
            bool result;
            unsafe
            {
                fixed (char* builder = relativePath)
                {
                    result = Win32.PInvoke.PathRelativePathTo(builder, linkPath, (uint)relativePathAttribute, targetPath, (uint)relativePathAttribute);
                }
            }
            if (result is false)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                return targetPath;
            }
            else
            {
                return relativePath.ToString();
            }
        }

        private static SafeFileHandle? GetSafeFileHandle(FileSystemInfo info)
        {
            const FILE_FLAGS_AND_ATTRIBUTES dirAttr = FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OPEN_REPARSE_POINT | FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_DIRECTORY;
            const FILE_FLAGS_AND_ATTRIBUTES fileAttr = FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OPEN_REPARSE_POINT | FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL;
            
            return Win32.PInvoke.CreateFile(
                    lpFileName: info.FullName,
                    dwDesiredAccess: (uint)GENERIC_ACCESS_RIGHTS.GENERIC_READ,
                    dwShareMode: FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE | FILE_SHARE_MODE.FILE_SHARE_DELETE,
                    lpSecurityAttributes: default,
                    dwCreationDisposition: FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                    dwFlagsAndAttributes: info is DirectoryInfo ? dirAttr : fileAttr,
                    hTemplateFile: default);
        }

        /// <remarks>
        /// If the file resides on a mapped network drive, this will return the UNC path. Ex: L:\SomeFile.txt >> \\Server\Files$\SomeFile.txt
        /// </remarks>
        /// <returns>The target path. It may match the supplied <paramref name="info"/> object.</returns>
        private unsafe static string GetFinalPathNameByHandle(FileSystemInfo info)
        {
            VersionManager.ThrowIfNotWindowsPlatform(PlatformErrorMessage);

            using SafeFileHandle fileHandle = GetSafeFileHandle(info);
            if (fileHandle.IsInvalid)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetLastWin32Error());
                throw new IOException("Invalid File Handle");
            }

            uint result;
            Span<char> text = new char[Win32.PInvoke.MAX_PATH];
            text.Clear();
            fixed (char* builder = text)
            {
                result = Win32.PInvoke.GetFinalPathNameByHandle(fileHandle, builder, Win32.PInvoke.MAX_PATH, GETFINALPATHNAMEBYHANDLE_FLAGS.FILE_NAME_NORMALIZED);
            }
            if (result > Win32.PInvoke.MAX_PATH) // if not enough characters alloted, retry with required character count (supplied by the previous call's result)
            {   
                text = new char[result];
                text.Clear();
                fixed (char* builder = text)
                {
                    result = Win32.PInvoke.GetFinalPathNameByHandle(fileHandle, builder, Win32.PInvoke.MAX_PATH, GETFINALPATHNAMEBYHANDLE_FLAGS.FILE_NAME_NORMALIZED);
                }
            }

            if (result == 0)
            {
                int errCode = Marshal.GetHRForLastWin32Error();
                switch ((WIN32_ERROR)errCode)
                {
                    case WIN32_ERROR.ERROR_SUCCESS:
                    case WIN32_ERROR.ERROR_FSFILTER_OP_COMPLETED_SUCCESSFULLY:
                        break;
                    default:
                        Marshal.ThrowExceptionForHR(errCode);
                        return null;
                }
            }

            // remove the appended prefix from the result, and ensure UNC paths get their slashes
            const string prefix = @"\\?\";
            const string uncPrefix = @"\\?\UNC\";

            return true switch
            {
#if NETCOREAPP3_0_OR_GREATER
                true when text.StartsWith(uncPrefix.AsSpan()) => string.Concat("\\", text.Slice(7).TrimSymLink()),
#else
                true when text.StartsWith(uncPrefix.AsSpan()) => $"\\{text.Slice(7).TrimSymLink().ToString()}",
#endif
                true when text.StartsWith(prefix.AsSpan()) => text.Slice(4).TrimSymLink().ToString(),
                _ => text.TrimSymLink().ToString()
            };
        }

        private unsafe static string? GetReparseDataTarget(FileSystemInfo info)
        {
            VersionManager.ThrowIfNotWindowsPlatform(PlatformErrorMessage);

            using SafeFileHandle fileHandle = GetSafeFileHandle(info);
            if (fileHandle.IsInvalid)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetLastWin32Error());
                return null;
            }

            int bufferSize;
            bufferSize = (int)Win32.PInvoke.MAXIMUM_REPARSE_DATA_BUFFER_SIZE;
            //bufferSize = REPARSE_DATA_BUFFER._Anonymous_e__Union._SymbolicLinkReparseBuffer_e__Struct.SizeOf(260); // WIN32.PInvoke.MAX_PATH
            //bufferSize = (int)Win32.PInvoke.MAXIMUM_REPARSE_DATA_BUFFER_SIZE + REPARSE_DATA_BUFFER._Anonymous_e__Union._SymbolicLinkReparseBuffer_e__Struct.SizeOf(260); // WIN32.PInvoke.MAX_PATH

            Span<sbyte> outBuffer = new sbyte[bufferSize];
            outBuffer.Clear();

            fixed (sbyte* outBufferPtr = outBuffer)
            {
                uint bytes;
                bool success = Win32.PInvoke.DeviceIoControl(fileHandle, Win32.PInvoke.FSCTL_GET_REPARSE_POINT, null, 0U, outBufferPtr, (uint)outBuffer.Length, &bytes, null);
                if (!success)
                {
                    int errCode = Marshal.GetHRForLastWin32Error();

                    switch ((WIN32_ERROR)errCode)
                    {
                        case WIN32_ERROR.ERROR_SUCCESS:
                        case WIN32_ERROR.ERROR_FSFILTER_OP_COMPLETED_SUCCESSFULLY:
                            break;
                        case WIN32_ERROR.ERROR_NOT_A_REPARSE_POINT:
                        case (WIN32_ERROR)0x80071126: //alternate not reparse point
                            return null;

                        default:
                            Marshal.ThrowExceptionForHR(errCode);
                            return null;
                    }
                }

                ref var data = ref *(REPARSE_DATA_BUFFER*)outBufferPtr;

                Span<char> targetSpan;
                switch (data.ReparseTag)
                {
                    case Win32.PInvoke.IO_REPARSE_TAG_SYMLINK:
                        ref var symReparse = ref data.Anonymous.SymbolicLinkReparseBuffer;

                        targetSpan = symReparse.PathBuffer.AsSpan((int)bytes / sizeof(char)).Slice(symReparse.SubstituteNameOffset / sizeof(char)).TrimSymLink();
                        
                        if (targetSpan.Length < 4 || !targetSpan.StartsWith(@"\??\".AsSpan()))
                            throw new Exception("Invalid SymLink data was detected");
                        
                        targetSpan = targetSpan.Slice(4); 

                        if (symReparse.Flags == MSWin.Wdk.PInvoke.SYMLINK_FLAG_RELATIVE) // Handle Relative paths
                        {
                            throw new NotImplementedException("Symbolic Link Relative Paths are not implemented yet!");
                        }
                        return targetSpan.ToString();

                    case Win32.PInvoke.IO_REPARSE_TAG_MOUNT_POINT:
                        ref var mountReparse = ref data.Anonymous.MountPointReparseBuffer;
                        return mountReparse.PathBuffer.AsSpan((int)bytes / sizeof(char)).Slice(mountReparse.PrintNameOffset / sizeof(char)).TrimSymLink().ToString();
                }
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool UintHasFlag(this uint value, uint flag) => value == flag || flag == (value & flag);

        private static Span<char> TrimSymLink(this Span<char> span)
        {
#if NET6_0_OR_GREATER
            return span.TrimEnd('\0').TrimEnd();
#else
            int lastIndex = span.Length - 1;
            while (lastIndex >= 0)
            {
                char c = span[lastIndex];
                if (c == '\0' || char.IsWhiteSpace(c))
                    lastIndex--;
                else
                    break;
            }
            if (lastIndex == 0 || lastIndex == span.Length - 1) return span;
            return span.Slice(0, lastIndex + 1);
#endif
        }

    }
}

#pragma warning restore CA1416 // Validate patform compatibility
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.