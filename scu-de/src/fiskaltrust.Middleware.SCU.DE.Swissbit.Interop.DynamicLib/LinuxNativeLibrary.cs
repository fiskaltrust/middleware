using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.DynamicLib
{
    public class LinuxNativeLibrary : INativeLibrary
    {

        private delegate IntPtr dlopenDelegate(string filename, int flags);
        private delegate IntPtr dlsymDelegate(IntPtr handle, string symbol);
        private delegate int dlcloseDelegate(IntPtr hModule);


        private readonly dlopenDelegate dlopen;
        private readonly dlsymDelegate dlsym;
        private readonly dlcloseDelegate dlclose;

        // Flags for dlopen 
        private const int RTLD_NOW = 2;
        private const int RTLD_GLOBAL = 8;

        public LinuxNativeLibrary()
        {

            //libdl.so, libdl.so.2, libdl.so.6, libdl.dylib
            try
            {
                dlopen = LinuxNativeLibdl.dlopen;
                dlclose = LinuxNativeLibdl.dlclose;
                dlsym = LinuxNativeLibdl.dlsym;
                return;
            }
            catch (Exception)
            {

            }

            //libc.so, libc.so.6
            try
            {
                dlopen = LinuxNativeLibc.dlopen;
                dlclose = LinuxNativeLibc.dlclose;
                dlsym = LinuxNativeLibc.dlsym;
                return;
            }
            catch (Exception)
            {

            }

            throw new NativeLibraryException("interface to the dynamic linking loader not found");
        }

        public void Free(IntPtr libraryPointer)
        {
            _ = dlclose(libraryPointer);
        }

        public IntPtr GetSymbolAddress(IntPtr libraryPointer, string symbolName)
        {
            return dlsym(libraryPointer, symbolName);
        }

        public IntPtr Load(string libraryName)
        {
            return dlopen(libraryName, RTLD_GLOBAL | RTLD_NOW);
        }
    }

    internal class LinuxNativeLibc
    {
#pragma warning disable

        [DllImport("c")]
        public static extern IntPtr dlopen(string path, int mode);

        [DllImport("c")]
        public static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("c")]
        public static extern int dlclose(IntPtr handle);

#pragma warning enable
    }

    internal class LinuxNativeLibdl
    {
#pragma warning disable

        [DllImport("dl")]
        public static extern IntPtr dlopen(string filename, int flags);

        [DllImport("dl")]
        public static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("dl")]
        public static extern int dlclose(IntPtr hModule);

#pragma warning enable
    }
}
