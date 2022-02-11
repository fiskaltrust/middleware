using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.DynamicLib
{
    public interface INativeLibrary
    {
        public IntPtr Load(string libraryName);

        public void Free(IntPtr libraryPointer);

        public IntPtr GetSymbolAddress(IntPtr libraryPointer, string symbolName);
    }
}
