using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Interop
{
    public interface INativeFunctionPointerFactory
    {
        public NativeFunctionPointer LoadLibrary();
    }
}
