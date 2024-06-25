using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace fiskaltrust.Interface.Tagging.Interfaces
{
    public interface ICaseConverterFactory
    {
        ICaseConverter CreateInstance(long ftReceiptCase);
    }
}
