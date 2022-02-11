using System;
using System.Runtime.InteropServices;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Native
{

    // copy of //using Mono.Unix.Native;
    // this is required to not get transversal dependencies to .net standard 2.0 also to builds targeting .net461
    // once, everything is pure .net standard 2.0 or .net5 this can be replaced by original Mono.Unix.Native

    // see also https://github.com/mono/mono/blob/master/mcs/class/Mono.Posix/Mono.Unix.Native/Syscall.cs

#pragma warning disable CA2101
    internal static class Syscall
    {
        private const string LIBC = "libc";

        [DllImport(LIBC, SetLastError = true)]
        // https://developer.apple.com/library/archive/documentation/System/Conceptual/ManPages_iPhoneOS/man2/fcntl.2.html
        public static extern int fcntl(int fd, int fcntlCommand);

        [DllImport(LIBC, SetLastError = true)]
        public static extern int open(string pathname, int openFlags);

        [DllImport(LIBC, SetLastError = true)]
        public static extern int close(int fd);

        [DllImport(LIBC, SetLastError = true)]
        public static extern long lseek(int fd, long offset, int whence);

        [DllImport(LIBC, SetLastError = true)]
        public static extern long read(int fd, IntPtr buf, ulong count);

        [DllImport(LIBC, SetLastError = true)]
        public static extern long write(int fd, IntPtr buf, ulong count);
    }
}
