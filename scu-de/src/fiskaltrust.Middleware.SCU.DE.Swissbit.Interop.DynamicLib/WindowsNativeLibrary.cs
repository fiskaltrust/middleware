using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.DynamicLib
{
    public class WindowsNativeLibrary : INativeLibrary
    {
        public void Free(IntPtr libraryPointer) => WindowsNativeKernel32.FreeLibrary(libraryPointer);

        public IntPtr GetSymbolAddress(IntPtr libraryPointer, string symbolName) => WindowsNativeKernel32.GetProcAddress(libraryPointer, symbolName);

        public IntPtr Load(string libraryName) => WindowsNativeKernel32.LoadLibrary(libraryName);

    }

#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
    internal class WindowsNativeKernel32
    {
        [DllImport("kernel32")]
        internal static extern IntPtr LoadLibrary(string libraryName);

        [DllImport("kernel32")]
        internal static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32")]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32")]
        internal static extern uint GetLastError();


    }
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments

}
