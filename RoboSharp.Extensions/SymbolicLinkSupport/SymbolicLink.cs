#if !NET6_0_OR_GREATER

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using MSWindows = Windows;
using Win32 = Windows.Win32;
using REPARSE_DATA_BUFFER = Windows.Wdk.Storage.FileSystem.REPARSE_DATA_BUFFER;


namespace RoboSharp.Extensions.SymbolicLinkSupport
{
    internal static class SymbolicLink
    {
        private const uint genericReadAccess = 0x80000000;

        private const uint fileFlagsForOpenReparsePointAndBackupSemantics = 0x02200000;
        const string PlatformErrorMessage = "This function relies on Windows P/Invoke. Use .Net 6 or newer for platform comaptibility.";

        /// <summary>
        /// Flag to indicate that the reparse point is relative
        /// </summary>
        /// <remarks>
        /// This is SYMLINK_FLAG_RELATIVE from from ntifs.h
        /// See https://msdn.microsoft.com/en-us/library/cc232006.aspx
        /// </remarks>
        private const uint symlinkReparsePointFlagRelative = 0x00000001;

        private const uint pathNotAReparsePointError = 0x80071126;

        /// <summary> Reparse point tag used to identify symbolic links. </summary>
        private const uint symLinkTag = 0xA000000C; //for Files and Directories

        /// <summary> Reparse point tag used to identify mount points and junction points. </summary>
        private const uint junctionPointTag = 0xA0000003; //for Directories Only

        /// <summary>
        /// The maximum number of characters for a relative path, using Unicode 2-byte characters.
        /// </summary>
        /// <remarks>
        /// <para>This is the same as the old MAX_PATH value, because:</para>
        /// <para>
        /// "you cannot use the "\\?\" prefix with a relative path, relative paths are always limited to a total of MAX_PATH characters"
        /// </para>
        /// (https://docs.microsoft.com/en-us/windows/desktop/fileio/naming-a-file#maximum-path-length-limitation)
        /// 
        /// This value includes allowing for a terminating null character.
        /// </remarks>
        private const int maxRelativePathLengthUnicodeChars = 260;

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PathRelativePathToW(
            [Out] StringBuilder pszPath,
            [In] string pszFrom,
            [In] FileAttributes dwAttrFrom,
            [In] string pszTo,
            [In] FileAttributes dwAttrTo);

        public static void CreateSymbolicLink(string linkPath, string targetPath, bool isDirectory, bool makeTargetPathRelative = false)
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
        
        private static string GetTargetPathRelativeToLink(string linkPath, string targetPath, bool linkAndTargetAreDirectories = false)
        {
            string returnPath;

            FileAttributes relativePathAttribute = 0;
            if (linkAndTargetAreDirectories)
            {
                relativePathAttribute = FileAttributes.Directory;

                // set the link path to the parent directory, so that PathRelativePathToW returns a path that works
                // for directory symlink traversal
                linkPath = Path.GetDirectoryName(linkPath.TrimEnd(Path.DirectorySeparatorChar));
            }
            
            StringBuilder relativePath = new StringBuilder(maxRelativePathLengthUnicodeChars);
            if (!PathRelativePathToW(relativePath, linkPath, relativePathAttribute, targetPath, relativePathAttribute))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                returnPath = targetPath;
            }
            else
            {
                returnPath = relativePath.ToString();
            }

            return returnPath;

        }

        public static string GetLinkTarget(string path)
        {
            VersionManager.ThrowIfNotWindowsPlatform(PlatformErrorMessage);
            REPARSE_DATA_BUFFER? reparseData = GetReparseData(path);
            if (reparseData is null) return null;
            var reparseDataBuffer = reparseData.Value;
            if (reparseDataBuffer.ReparseTag != Win32.PInvoke.IO_REPARSE_TAG_SYMLINK)
            {
                return null;
            }
            return GetTargetFromReparseData(reparseDataBuffer, path);
        }

        /// <summary>
        /// Valid only for Directories
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetJunctionTarget(string path)
        {
            VersionManager.ThrowIfNotWindowsPlatform(PlatformErrorMessage);
            if (!Directory.Exists(path)) return null;
            REPARSE_DATA_BUFFER? reparseData = GetReparseData(path);
            if (reparseData is null) return null;
            var reparseDataBuffer = reparseData.Value;
            if (reparseDataBuffer.ReparseTag != Win32.PInvoke.IO_REPARSE_TAG_MOUNT_POINT)
            {
                return null;
            }
            return GetTargetFromReparseData(reparseDataBuffer, path);
        }

        public static bool IsJunctionPoint(string path)
        {
            return GetJunctionTarget(path) != null;
        }

        public static bool IsJunctionOrSymbolic(string path)
        {
            VersionManager.ThrowIfNotWindowsPlatform(PlatformErrorMessage);
            REPARSE_DATA_BUFFER? reparseData = GetReparseData(path);
            if (reparseData is null) return false;
            REPARSE_DATA_BUFFER data = reparseData.Value;
            if (data.ReparseTag == symLinkTag | data.ReparseTag == junctionPointTag)
            {
                return GetTargetFromReparseData(data, path) != null;
            }
            else
            {
                return false;
            }
        }

        private static string GetTargetFromReparseData(REPARSE_DATA_BUFFER reparseDataBuffer, string inputPath)
        {
            // ToDo : Convert to AsSpan
            string target;
            if (reparseDataBuffer.ReparseTag.UintHasFlag(Win32.PInvoke.IO_REPARSE_TAG_SYMLINK))
            {
                target = reparseDataBuffer.Anonymous.SymbolicLinkReparseBuffer.PathBuffer.ToString();
            }
            else if (reparseDataBuffer.ReparseTag.UintHasFlag(Win32.PInvoke.IO_REPARSE_TAG_MOUNT_POINT))
            {
                target = reparseDataBuffer.Anonymous.MountPointReparseBuffer.PathBuffer.ToString();
            }
            else
            {
                return null;
            }

            // Handle Relative paths
            if ((reparseDataBuffer.ReparseTag & Win32.PInvoke.IO_REPARSE_TAG_SYMLINK) == symlinkReparsePointFlagRelative)
            {
                string basePath = Path.GetDirectoryName(inputPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                target = Path.Combine(basePath, target);
            }
            return target;
        }

        private static bool UintHasFlag(this uint value, uint expectedValue)
        {
            return value == expectedValue || expectedValue == (value & expectedValue);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Throws if not windows platform")]
        private unsafe static REPARSE_DATA_BUFFER? GetReparseData(string path)
        {
            VersionManager.ThrowIfNotWindowsPlatform(PlatformErrorMessage);
            using SafeFileHandle fileHandle = Win32.PInvoke.CreateFile(
                    lpFileName: path,
                    dwDesiredAccess: (uint)Win32.Foundation.GENERIC_ACCESS_RIGHTS.GENERIC_READ,
                    dwShareMode: Win32.Storage.FileSystem.FILE_SHARE_MODE.FILE_SHARE_READ | Win32.Storage.FileSystem.FILE_SHARE_MODE.FILE_SHARE_WRITE,
                    lpSecurityAttributes: default,
                    dwCreationDisposition: Win32.Storage.FileSystem.FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                    dwFlagsAndAttributes: Win32.Storage.FileSystem.FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OPEN_REPARSE_POINT,
                    hTemplateFile: default);

            if (fileHandle.IsInvalid)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            int outBufferSize = Marshal.SizeOf<REPARSE_DATA_BUFFER>();
            IntPtr outputBuffer = Marshal.AllocHGlobal(outBufferSize);
            uint* bytesReturned = default;
            try
            {

                // https://learn.microsoft.com/en-us/windows/win32/api/ioapiset/nf-ioapiset-deviceiocontrol
                // https://learn.microsoft.com/en-us/windows/win32/api/winioctl/ni-winioctl-fsctl_get_reparse_point
                bool success = Win32.PInvoke.DeviceIoControl(fileHandle, Win32.PInvoke.FSCTL_GET_REPARSE_POINT, null, 0U,
                    lpOutBuffer: outputBuffer.ToPointer(),
                    nOutBufferSize: (uint)outBufferSize,
                    lpBytesReturned: bytesReturned,
                    lpOverlapped: null
                    );

                if (!success)
                {
                    if (((uint)Marshal.GetHRForLastWin32Error()) == pathNotAReparsePointError)
                    {
                        return null;
                    }
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }

#if NETFRAMEWORK
                return (REPARSE_DATA_BUFFER)Marshal.PtrToStructure(outputBuffer, typeof(REPARSE_DATA_BUFFER));
#else
                return Marshal.PtrToStructure<REPARSE_DATA_BUFFER>(outputBuffer);
#endif
            }
            finally
            {
                Marshal.FreeHGlobal(outputBuffer);
            }
        }
    }
}

#endif